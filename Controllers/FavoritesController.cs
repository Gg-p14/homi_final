using Homi.Data;
using Homi.Helpers;
using Homi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Homi.Controllers;

[Authorize]
public class FavoritesController : Controller
{
    private readonly FavoritesService _favoritos;

    public FavoritesController(FavoritesService favoritos) => _favoritos = favoritos;

    public async Task<IActionResult> Index() =>
        View(await _favoritos.ListarAsync(User.ObtenerUsuarioId()));

    /// Agrega o quita según el estado actual. Sirve al botón de la ficha del alojamiento.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Alternar(int alojamientoId, string? returnUrl = null)
    {
        var uid = User.ObtenerUsuarioId();
        try
        {
            if (await _favoritos.EsFavoritoAsync(uid, alojamientoId))
                await _favoritos.QuitarAsync(uid, alojamientoId);
            else
                await _favoritos.AgregarAsync(uid, alojamientoId);
        }
        catch (ReglaNegocioException ex)
        {
            TempData["Error"] = ex.Message;
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction(nameof(Index));
    }

    /// Quita un favorito desde la misma lista.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Quitar(int alojamientoId)
    {
        await _favoritos.QuitarAsync(User.ObtenerUsuarioId(), alojamientoId);
        TempData["Exito"] = "Quitado de favoritos.";
        return RedirectToAction(nameof(Index));
    }
}
