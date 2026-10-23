namespace FCG.Catalog.Domain.Exceptions;

public sealed class PromocaoInvalidaException : DomainException
{
    public PromocaoInvalidaException(string mensagem) : base(mensagem)
    {
    }
}
