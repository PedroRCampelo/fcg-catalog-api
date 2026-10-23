namespace FCG.Catalog.Application.Exceptions;

// Evento de pagamento para um pedido que este serviço não conhece. Repetir não resolve,
// então o consumidor não retenta: a mensagem vai para a fila de erro, onde pode ser investigada.
public sealed class PedidoDoEventoNaoEncontradoException : Exception
{
    public PedidoDoEventoNaoEncontradoException(Guid orderId)
        : base($"Recebido um pagamento para o pedido {orderId}, que não existe no catálogo.")
    {
    }
}
