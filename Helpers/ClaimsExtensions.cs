using System.Security.Claims;

namespace Homi.Helpers;

public static class ClaimsExtensions
{
    public static int ObtenerUsuarioId(this ClaimsPrincipal usuario) =>
        int.Parse(usuario.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
}
