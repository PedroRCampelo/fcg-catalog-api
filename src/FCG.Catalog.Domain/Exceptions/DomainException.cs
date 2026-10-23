namespace FCG.Catalog.Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string mensagem) : base(mensagem)
    {
    }
}
