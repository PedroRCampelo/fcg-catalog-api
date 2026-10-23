namespace FCG.Catalog.Application.Exceptions;

public sealed class NaoEncontradoException : Exception
{
    public NaoEncontradoException(string mensagem) : base(mensagem)
    {
    }
}
