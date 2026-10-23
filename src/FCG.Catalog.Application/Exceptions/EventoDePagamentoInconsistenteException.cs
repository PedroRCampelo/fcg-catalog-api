namespace FCG.Catalog.Application.Exceptions;

// O evento cita um pedido existente, mas com outro usuário, outro jogo ou um status desconhecido.
// Também não adianta retentar: vai para a fila de erro.
public sealed class EventoDePagamentoInconsistenteException : Exception
{
    public EventoDePagamentoInconsistenteException(string mensagem) : base(mensagem)
    {
    }
}
