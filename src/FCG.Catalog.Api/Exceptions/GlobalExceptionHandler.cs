using System.Diagnostics;
using FCG.Catalog.Application.Exceptions;
using FCG.Catalog.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace FCG.Catalog.Api.Exceptions;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, titulo) = exception switch
        {
            DomainException => (StatusCodes.Status400BadRequest, "Requisição inválida"),
            BadHttpRequestException => (StatusCodes.Status400BadRequest, "Requisição inválida"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Não autorizado"),
            NaoEncontradoException => (StatusCodes.Status404NotFound, "Recurso não encontrado"),
            ConflitoException => (StatusCodes.Status409Conflict, "Conflito"),
            _ => (StatusCodes.Status500InternalServerError, "Erro interno do servidor")
        };

        var erroInesperado = statusCode == StatusCodes.Status500InternalServerError;

        if (erroInesperado)
            _logger.LogError(exception, "Erro não tratado ao processar {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        else
            _logger.LogWarning("Requisição recusada em {Method} {Path}: {Mensagem}", httpContext.Request.Method, httpContext.Request.Path, exception.Message);

        var problema = new ProblemDetails
        {
            Status = statusCode,
            Title = titulo,
            // A mensagem de uma exceção inesperada pode expor detalhes internos, então não sai da API.
            Detail = erroInesperado ? "Ocorreu um erro inesperado. Informe o traceId ao suporte." : exception.Message,
            Instance = httpContext.Request.Path
        };
        problema.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problema, cancellationToken);

        return true;
    }
}
