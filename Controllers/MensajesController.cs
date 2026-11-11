using Homi.Data;
using Homi.Helpers;
using Homi.Services;
using Homi.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Homi.Controllers;

[Authorize]
public class MensajesController : Controller
{
    private readonly MensajeriaService _mensajes;

    public MensajesController(MensajeriaService mensajes) => _mensajes = mensajes;

    /// Buzón del usuario (como huésped y como anfitrión).
    [HttpGet]
    public async Task<IActionResult> Index() =>
        View(new BuzonViewModel { Conversaciones = await _mensajes.BuzonAsync(User.ObtenerUsuarioId()) });

    /// Abre o recupera la conversación con el anfitrión de un alojamiento.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Abrir(int alojamientoId)
    {
        try
        {
            var id = await _mensajes.AbrirAsync(alojamientoId, User.ObtenerUsuarioId());
            return RedirectToAction(nameof(Conversacion), new { id });
        }
        catch (ReglaNegocioException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction("Details", "Properties", new { id = alojamientoId });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Conversacion(int id)
    {
        try
        {
            var (header, mensajes) = await _mensajes.VerAsync(id, User.ObtenerUsuarioId());
            if (header is null) return NotFound();

            return View(new ConversacionViewModel
            {
                ConversacionId = id,
                Header = header,
                Mensajes = mensajes
            });
        }
        catch (ReglaNegocioException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Enviar(int conversacionId, string texto)
    {
        try
        {
            await _mensajes.EnviarAsync(conversacionId, User.ObtenerUsuarioId(), texto);
        }
        catch (ReglaNegocioException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Conversacion), new { id = conversacionId });
    }
}
