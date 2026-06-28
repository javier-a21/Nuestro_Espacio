using System.Security.Claims;

namespace Cooperativa.Api.Common;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? throw new InvalidOperationException("El token no contiene el identificador de usuario.");
        return Guid.Parse(id);
    }
}
