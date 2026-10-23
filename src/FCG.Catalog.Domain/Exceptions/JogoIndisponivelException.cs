namespace FCG.Catalog.Domain.Exceptions;

public sealed class JogoIndisponivelException : DomainException
{
    public JogoIndisponivelException(string mensagem) : base(mensagem)
    {
    }
}
