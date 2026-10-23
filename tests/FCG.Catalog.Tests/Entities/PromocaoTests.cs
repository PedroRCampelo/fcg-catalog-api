using FCG.Catalog.Domain.Entities;
using FCG.Catalog.Domain.Exceptions;
using Xunit;

namespace FCG.Catalog.Tests.Entities;

public class PromocaoTests
{
    private static readonly Guid JogoId = Guid.NewGuid();
    private static readonly DateTime Inicio = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Fim = new(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc);

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
    public void Data_Sem_Fuso_Deve_Ser_Tratada_Como_Utc_Independente_Da_Maquina()
    {
        var inicioSemFuso = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var fimSemFuso = new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Unspecified);

        var promocao = new Promocao(JogoId, 10m, inicioSemFuso, fimSemFuso);

        Assert.Equal(DateTimeKind.Utc, promocao.DataInicio.Kind);
        Assert.Equal(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc), promocao.DataInicio);
        Assert.Equal(new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc), promocao.DataFim);
    }

    [Fact]
    public void Data_Em_Horario_Local_Deve_Ser_Convertida_Para_Utc()
    {
        var inicioLocal = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Local);
        var fimLocal = new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Local);

        var promocao = new Promocao(JogoId, 10m, inicioLocal, fimLocal);

        Assert.Equal(DateTimeKind.Utc, promocao.DataInicio.Kind);
        Assert.Equal(inicioLocal.ToUniversalTime(), promocao.DataInicio);
    }

    [Fact]
    public void Data_Utc_Deve_Ser_Mantida_Sem_Alteracao()
    {
        var promocao = new Promocao(JogoId, 10m, Inicio, Fim);

        Assert.Equal(Inicio, promocao.DataInicio);
        Assert.Equal(Fim, promocao.DataFim);
    }

    [Fact]
    public void Comparacao_Do_Periodo_Deve_Ser_Feita_Em_Utc_E_Nao_Pelo_Relogio_Local()
    {
        // 2026-09-01 02:00 UTC vem ANTES de 2026-09-01 01:00 em UTC-3 (= 04:00 UTC), mesmo com hora local maior.
        var inicio = new DateTime(2026, 9, 1, 4, 0, 0, DateTimeKind.Utc);
        var fim = new DateTime(2026, 9, 1, 2, 0, 0, DateTimeKind.Utc);

        Assert.Throws<PromocaoInvalidaException>(() => new Promocao(JogoId, 10m, inicio, fim));
    }
}
