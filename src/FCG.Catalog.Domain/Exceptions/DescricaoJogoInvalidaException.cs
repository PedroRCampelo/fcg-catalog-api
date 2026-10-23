namespace FCG.Catalog.Domain.Exceptions;

public sealed class DescricaoJogoInvalidaException : DomainException
{
    public DescricaoJogoInvalidaException(string mensagem) : base(mensagem)
    {
    }
}
