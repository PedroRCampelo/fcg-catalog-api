namespace FCG.Catalog.Domain.Exceptions;

public sealed class TransicaoDePedidoInvalidaException : DomainException
{
    public TransicaoDePedidoInvalidaException(string mensagem) : base(mensagem)
    {
    }
}
