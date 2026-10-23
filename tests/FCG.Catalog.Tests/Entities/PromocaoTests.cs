using FCG.Catalog.Domain.Entities;
using FCG.Catalog.Domain.Exceptions;
using Xunit;

namespace FCG.Catalog.Tests.Entities;

public class PromocaoTests
{
    private static readonly Guid JogoId = Guid.NewGuid();
    private static readonly DateTimeOffset Inicio = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Fim = new(2026, 9, 10, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Deve_Criar_Promocao_Quando_Os_Dados_Forem_Validos()
    {
        var promocao = new Promocao(JogoId, 25m, Inicio, Fim);

        Assert.NotEqual(Guid.Empty, promocao.Id);
        Assert.Equal(JogoId, promocao.JogoId);
        Assert.Equal(25m, promocao.PercentualDesconto);
    }

    [Fact]
    public void Deve_Aceitar_Desconto_De_Cem_Por_Cento()
    {
        Assert.Equal(100m, new Promocao(JogoId, 100m, Inicio, Fim).PercentualDesconto);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(100.01)]
    public void Deve_Lancar_Excecao_Quando_Percentual_For_Invalido(double percentual)
    {
        Assert.Throws<PromocaoInvalidaException>(() => new Promocao(JogoId, (decimal)percentual, Inicio, Fim));
    }

    [Fact]
    public void Deve_Lancar_Excecao_Quando_Fim_For_Igual_Ou_Anterior_Ao_Inicio()
    {
        Assert.Throws<PromocaoInvalidaException>(() => new Promocao(JogoId, 10m, Inicio, Inicio));
        Assert.Throws<PromocaoInvalidaException>(() => new Promocao(JogoId, 10m, Fim, Inicio));
    }

    [Fact]
    public void Deve_Guardar_As_Datas_Em_Utc_Mesmo_Quando_Recebidas_Com_Fuso()
    {
        var inicioComFuso = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.FromHours(-3));
        var fimComFuso = new DateTimeOffset(2026, 9, 10, 0, 0, 0, TimeSpan.FromHours(-3));

        var promocao = new Promocao(JogoId, 10m, inicioComFuso, fimComFuso);

        Assert.Equal(DateTimeKind.Utc, promocao.DataInicio.Kind);
        Assert.Equal(DateTimeKind.Utc, promocao.DataFim.Kind);
        Assert.Equal(new DateTime(2026, 9, 1, 3, 0, 0, DateTimeKind.Utc), promocao.DataInicio);
    }
}
