using Homi.Data;
using Homi.Helpers;
using Homi.Models;
using Homi.Services;
using Homi.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Homi.Controllers;

[Authorize]
public class BookingsController : Controller
{
    private readonly BookingService _reservas;

    public BookingsController(BookingService reservas) => _reservas = reservas;

    /// Crea la reserva pendiente (30 minutos para pagar) y lleva a la pantalla de pago.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(int alojamientoId, DateOnly entrada, DateOnly salida, int huespedes)
    {
        try
        {
            var id = await _reservas.CrearReservaAsync(alojamientoId, User.ObtenerUsuarioId(), entrada, salida, huespedes);
            return RedirectToAction(nameof(Pagar), new { id });
        }
        catch (ReglaNegocioException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction("Details", "Properties", new
            {
                id = alojamientoId,
                entrada = entrada.ToString("yyyy-MM-dd"),
                salida = salida.ToString("yyyy-MM-dd"),
                huespedes
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Pagar(int id)
    {
        var uid = User.ObtenerUsuarioId();
        try
        {
            // Primero se expiran las reservas vencidas; así nunca se muestra una reserva vencida como pendiente.
            await _reservas.ExpirarPendientesAsync();

            var reserva = await _reservas.DetalleAsync(id, uid);
            if (reserva is null || reserva.HuespedId != uid) return NotFound();

            if (reserva.EstadoReservaId == CatalogoIds.ReservaConfirmada)
                return RedirectToAction(nameof(Comprobante), new { id });

            if (reserva.EstadoReservaId == CatalogoIds.ReservaExpirada)
            {
                TempData["Error"] = "Tu reserva expiró porque no se pagó a tiempo. Vuelve a buscar el alojamiento y reserva de nuevo.";
                return RedirectToAction(nameof(Mis));
            }

            if (reserva.EstadoReservaId != CatalogoIds.ReservaPendientePago)
            {
                TempData["Error"] = "Esta reserva ya no está pendiente de pago.";
                return RedirectToAction(nameof(Mis));
            }

            var segundos = reserva.FechaLimitePago.HasValue
                ? Math.Max(0, (int)(reserva.FechaLimitePago.Value - Homi.Helpers.Reloj.Ahora).TotalSeconds)
                : 0;

            var vm = new ReservaPagoViewModel
            {
                Reserva = reserva,
                Cargos = await _reservas.CargosAsync(id, uid),
                SegundosRestantes = segundos
            };
            return View(vm);
        }
        catch (ReglaNegocioException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Mis));
        }
    }

    /// Pago: retiene el monto, confirma la reserva y crea el comprobante.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarPago(int id)
    {
        try
        {
            await _reservas.PagarAsync(id, User.ObtenerUsuarioId());
            TempData["Exito"] = "Pago confirmado. Tu reserva está confirmada.";
            return RedirectToAction(nameof(Comprobante), new { id });
        }
        catch (ReglaNegocioException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Pagar), new { id });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Comprobante(int id)
    {
        try
        {
            var comprobante = await _reservas.ComprobanteAsync(id, User.ObtenerUsuarioId());
            if (comprobante is null) return NotFound();
            var uid = User.ObtenerUsuarioId();
            ViewBag.Pago = await _reservas.PagoDeReservaAsync(id, uid);
            ViewBag.Resena = await _reservas.ResenaDeReservaAsync(id);
            return View(comprobante);
        }
        catch (ReglaNegocioException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Mis));
        }
    }

    /// Lista de comprobantes del huésped.
    [HttpGet]
    public async Task<IActionResult> Comprobantes() =>
        View(await _reservas.ComprobantesDeHuespedAsync(User.ObtenerUsuarioId()));

    [HttpGet]
    public async Task<IActionResult> Mis(string filtro = "Todas")
    {
        var lista = await _reservas.ListarAsync(User.ObtenerUsuarioId(), filtro);
        return View(new MisReservasViewModel { Reservas = lista, Filtro = filtro });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(int id)
    {
        try
        {
            await _reservas.CancelarAsync(id, User.ObtenerUsuarioId());
            TempData["Exito"] = "La reserva fue cancelada.";
        }
        catch (ReglaNegocioException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Mis));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Resenar(int reservaId, int calificacion, string? comentario)
    {
        try
        {
            await _reservas.ResenarAsync(reservaId, User.ObtenerUsuarioId(), calificacion, comentario);
            TempData["Exito"] = "Gracias por tu reseña.";
        }
        catch (ReglaNegocioException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Mis));
    }
}
