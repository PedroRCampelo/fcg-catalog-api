using System.Security.Claims;

namespace FCG.Catalog.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    // O token do UsersAPI leva o id do usuário na claim padrão "sub" (sem remapeamento de claims).
    public static Guid ObterUsuarioId(this ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue("sub");

        return Guid.TryParse(sub, out var id)
            ? id
            : throw new UnauthorizedAccessException("O token não identifica o usuário (claim 'sub' ausente ou inválida).");
    }
}
