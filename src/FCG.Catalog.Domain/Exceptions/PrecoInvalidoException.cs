namespace FCG.Catalog.Domain.Exceptions;

public sealed class PrecoInvalidoException : DomainException
{
    public PrecoInvalidoException(string mensagem) : base(mensagem)
    {
    }
}
