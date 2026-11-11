using Homi.Data;
using Homi.Helpers;
using Homi.Services;
using Homi.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Homi.Controllers;

[Authorize]
public class ComentariosController : Controller
{
    private readonly ComentarioService _comentarios;

    public ComentariosController(ComentarioService comentarios) => _comentarios = comentarios;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var vm = new ComentariosViewModel
        {
            Historial = await _comentarios.ListarAsync(User.ObtenerUsuarioId())
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Enviar(ComentariosViewModel formulario)
    {
        if (!ModelState.IsValid)
        {
            formulario.Historial = await _comentarios.ListarAsync(User.ObtenerUsuarioId());
            return View("Index", formulario);
        }

        try
        {
            await _comentarios.CrearAsync(User.ObtenerUsuarioId(), formulario.Tipo, formulario.Asunto, formulario.Mensaje);
            TempData["Exito"] = "Gracias. Recibimos tu mensaje.";
        }
        catch (ReglaNegocioException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }
}
