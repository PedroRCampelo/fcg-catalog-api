using FCG.Catalog.Application.Dtos;
using FCG.Catalog.Application.Exceptions;
using FCG.Catalog.Application.Interfaces;
using FCG.Catalog.Domain.Entities;
using FCG.Catalog.Domain.Enums;
using FCG.Contracts;
using Microsoft.Extensions.Logging;

namespace FCG.Catalog.Application.Services;

public class PedidoService
{
    private readonly IPedidoRepository _pedidos;
    private readonly IJogoRepository _jogos;
    private readonly IBibliotecaRepository _biblioteca;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventos;
    private readonly ILogger<PedidoService> _logger;

    public PedidoService(
        IPedidoRepository pedidos,
        IJogoRepository jogos,
        IBibliotecaRepository biblioteca,
        IUnitOfWork unitOfWork,
        IEventPublisher eventos,
        ILogger<PedidoService> logger)
    {
        _pedidos = pedidos;
        _jogos = jogos;
        _biblioteca = biblioteca;
        _unitOfWork = unitOfWork;
        _eventos = eventos;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PedidoResponse>> ListarAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        var pedidos = await _pedidos.ListarPorUsuarioAsync(usuarioId, cancellationToken);
        return pedidos.Select(PedidoMapper.ParaResponse).ToList();
    }

    // Comprar não coloca o jogo na biblioteca: abre um pedido Pendente e anuncia o OrderPlacedEvent.
    // O jogo só entra quando o pagamento for aprovado (ProcessarPagamentoAsync).
    public async Task<PedidoResponse> ComprarAsync(Guid usuarioId, Guid jogoId, CancellationToken cancellationToken = default)
    {
        var jogo = await _jogos.ObterPorIdAsync(jogoId, cancellationToken);

        if (jogo is not { Ativo: true })
            throw new NaoEncontradoException("Jogo não encontrado.");

        if (await _biblioteca.ExisteAsync(usuarioId, jogoId, cancellationToken))
            throw new ConflitoException("Você já possui este jogo.");

        if (await _pedidos.ExistePendenteAsync(usuarioId, jogoId, cancellationToken))
            throw new ConflitoException("Já existe um pedido pendente para este jogo. Aguarde o resultado do pagamento.");

        var pedido = Pedido.Criar(usuarioId, jogo);

        await _pedidos.AdicionarAsync(pedido, cancellationToken);

        // O evento é registrado antes do SalvarAsync de propósito: a infraestrutura grava pedido e
        // evento na mesma transação (outbox), então não existe pedido sem evento nem evento sem pedido.
        await _eventos.PublicarAsync(
            new OrderPlacedEvent(pedido.Id, pedido.UsuarioId, pedido.JogoId, pedido.TituloJogo, pedido.Preco),
            cancellationToken);
        await _unitOfWork.SalvarAsync(cancellationToken);

        _logger.LogInformation("Pedido {OrderId} aberto: usuário {UserId} comprando {GameId} por {Price}",
            pedido.Id, pedido.UsuarioId, pedido.JogoId, pedido.Preco);

        return PedidoMapper.ParaResponse(pedido);
    }

    // Idempotente pelo OrderId: o broker entrega "pelo menos uma vez", então o mesmo evento pode chegar
    // de novo. Só um pedido Pendente é alterado; qualquer outro estado significa que este evento já
    // foi tratado e a mensagem é descartada sem efeito.
    public async Task ProcessarPagamentoAsync(PaymentProcessedEvent evento, CancellationToken cancellationToken = default)
    {
        var pedido = await _pedidos.ObterPorIdAsync(evento.OrderId, cancellationToken)
            ?? throw new PedidoDoEventoNaoEncontradoException(evento.OrderId);

        if (pedido.UsuarioId != evento.UserId || pedido.JogoId != evento.GameId)
            throw new EventoDePagamentoInconsistenteException(
                $"O pagamento do pedido {evento.OrderId} cita o usuário {evento.UserId} e o jogo {evento.GameId}, " +
                $"mas o pedido é do usuário {pedido.UsuarioId} para o jogo {pedido.JogoId}.");

        if (pedido.Status != StatusPedido.Pendente)
        {
            _logger.LogWarning(
                "PaymentProcessedEvent descartado sem efeito: o pedido {OrderId} já está {StatusDoPedido} " +
                "(o evento informava {StatusDoPagamento}). Provável reentrega de uma mensagem já processada.",
                pedido.Id, pedido.Status, evento.Status);
            return;
        }

        switch (evento.Status)
        {
            case PaymentStatus.Approved:
                await ConcluirPedidoAsync(pedido, cancellationToken);
                break;

            case PaymentStatus.Rejected:
                pedido.Recusar();
                _logger.LogInformation("Pedido {OrderId} recusado: pagamento rejeitado, o jogo {GameId} não entra na biblioteca de {UserId}",
                    pedido.Id, pedido.JogoId, pedido.UsuarioId);
                break;

            default:
                throw new EventoDePagamentoInconsistenteException(
                    $"O pagamento do pedido {evento.OrderId} veio com um status desconhecido: {(int)evento.Status}.");
        }

        await _unitOfWork.SalvarAsync(cancellationToken);
    }

    private async Task ConcluirPedidoAsync(Pedido pedido, CancellationToken cancellationToken)
    {
        pedido.Concluir();

        // Segunda barreira, além do índice único do banco: se o jogo já estiver na biblioteca, o pedido
        // fecha sem criar um item repetido.
        if (await _biblioteca.ExisteAsync(pedido.UsuarioId, pedido.JogoId, cancellationToken))
        {
            _logger.LogWarning("O usuário {UserId} já tinha o jogo {GameId} na biblioteca: pedido {OrderId} concluído sem duplicar o item",
                pedido.UsuarioId, pedido.JogoId, pedido.Id);
            return;
        }

        await _biblioteca.AdicionarAsync(new JogoAdquirido(pedido.UsuarioId, pedido.JogoId, pedido.Id), cancellationToken);

        _logger.LogInformation("Pedido {OrderId} concluído: o jogo {GameId} entrou na biblioteca de {UserId}",
            pedido.Id, pedido.JogoId, pedido.UsuarioId);
    }
}
