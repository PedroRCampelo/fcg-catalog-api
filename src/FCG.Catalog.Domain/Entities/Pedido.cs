using FCG.Catalog.Domain.Enums;
using FCG.Catalog.Domain.Exceptions;

namespace FCG.Catalog.Domain.Entities;

// O Id do pedido é o OrderId dos eventos: amarra OrderPlacedEvent e PaymentProcessedEvent.
public sealed class Pedido
{
    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public Guid JogoId { get; private set; }

    // Título e preço são uma fotografia do momento da compra: mudar o jogo depois não reescreve o pedido.
    public string TituloJogo { get; private set; } = null!;
    public decimal Preco { get; private set; }

    public StatusPedido Status { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public DateTime? FinalizadoEm { get; private set; }

    private Pedido()
    {
    }

    public static Pedido Criar(Guid usuarioId, Jogo jogo)
    {
        if (!jogo.Ativo)
            throw new JogoIndisponivelException("O jogo não está disponível para compra.");

        return new Pedido
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            JogoId = jogo.Id,
            TituloJogo = jogo.Titulo,
            Preco = jogo.Preco,
            Status = StatusPedido.Pendente,
            CriadoEm = DateTime.UtcNow
        };
    }

    public void Concluir() => Finalizar(StatusPedido.Concluido);

    public void Recusar() => Finalizar(StatusPedido.Recusado);

    private void Finalizar(StatusPedido novoStatus)
    {
        if (Status != StatusPedido.Pendente)
            throw new TransicaoDePedidoInvalidaException(
                $"O pedido {Id} está {Status} e não pode passar para {novoStatus}. Só pedidos pendentes podem ser finalizados.");

        Status = novoStatus;
        FinalizadoEm = DateTime.UtcNow;
    }
}
