namespace FCG.Catalog.Domain.Exceptions;

public sealed class TituloJogoInvalidoException : DomainException
{
    public TituloJogoInvalidoException(string mensagem) : base(mensagem)
    {
    }
}
