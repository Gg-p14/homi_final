using Homi.Data;
using Homi.Helpers;
using Homi.Models;
using Homi.Services;
using Homi.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Homi.Controllers;

[Authorize]
public class HostController : Controller
{
    private readonly HostService _host;
    private readonly PropertyService _propiedades;
    private readonly ImageStorageService _imagenes;
    private readonly MensajeriaService _mensajes;

    public HostController(HostService host, PropertyService propiedades, ImageStorageService imagenes,
        MensajeriaService mensajes)
    {
        _host = host;
        _propiedades = propiedades;
        _imagenes = imagenes;
        _mensajes = mensajes;
    }

    public async Task<IActionResult> Index()
    {
        var uid = User.ObtenerUsuarioId();
        var vm = new HostPanelViewModel
        {
            Resumen = await _host.ResumenAsync(uid),
            Alojamientos = await _host.AlojamientosAsync(uid),
            Reservas = await _host.ReservasAsync(uid)
        };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await CargarUbicacionesAsync();
        return View("Form", new PropertyFormViewModel());
    }

    /// Publica como borrador. Después se suben las fotos y se activa.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PropertyFormViewModel vm)
    {
        if (!ModelState.IsValid) return View("Form", vm);

        try
        {
            var id = await _host.RegistrarAsync(User.ObtenerUsuarioId(), vm);
            TempData["Exito"] = "Borrador creado. Sube al menos una foto para poder activarlo.";
            return RedirectToAction(nameof(Fotos), new { id });
        }
        catch (ReglaNegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Form", vm);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        await CargarUbicacionesAsync();
        var uid = User.ObtenerUsuarioId();
        var (d, _) = await _propiedades.ObtenerDetalleAsync(id, uid);
        if (d is null || d.AnfitrionId != uid) return NotFound();

        var vm = new PropertyFormViewModel
        {
            Id = d.Id,
            Titulo = d.Titulo,
            Descripcion = d.Descripcion,
            Calle = d.Calle ?? string.Empty,
            NumeroExterior = d.NumeroExterior ?? string.Empty,
            Colonia = d.Colonia ?? string.Empty,
            CodigoPostal = d.CodigoPostal ?? string.Empty,
            Referencias = d.Referencias,
            Pais = d.Pais,
            Estado = d.Estado,
            Ciudad = d.Ciudad,
            PrecioPorNoche = d.PrecioPorNoche,
            CobraLimpieza = d.CobraLimpieza,
            CapacidadMaxima = d.CapacidadMaxima
        };
        return View("Form", vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PropertyFormViewModel vm)
    {
        vm.Id = id;
        // La ubicación no se edita aquí; el procedimiento solo cambia título, descripción, tarifas y dirección.
        ModelState.Remove(nameof(vm.Pais));
        ModelState.Remove(nameof(vm.Estado));
        ModelState.Remove(nameof(vm.Ciudad));
        if (!ModelState.IsValid) return View("Form", vm);

        try
        {
            await _host.ActualizarAsync(id, User.ObtenerUsuarioId(), vm);
            TempData["Exito"] = "Cambios guardados.";
            return RedirectToAction(nameof(Index));
        }
        catch (ReglaNegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Form", vm);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Fotos(int id)
    {
        var uid = User.ObtenerUsuarioId();
        var (d, _) = await _propiedades.ObtenerDetalleAsync(id, uid);
        if (d is null || d.AnfitrionId != uid) return NotFound();

        var vm = new FotosViewModel
        {
            Alojamiento = d,
            Fotos = await _host.FotosAsync(id)
        };
        return View(vm);
    }

    /// Sube varias fotos. Revisa el total con sp_ContarFotos antes de guardar; si una falla, borra su archivo.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SubirFotos(int id, List<IFormFile> fotos)
    {
        var uid = User.ObtenerUsuarioId();
        if (!await EsDuenoAsync(id, uid)) return NotFound();

        if (fotos.Count == 0)
        {
            TempData["Error"] = "Selecciona al menos una imagen.";
            return RedirectToAction(nameof(Fotos), new { id });
        }

        var total = await _host.ContarFotosAsync(id);
        if (total + fotos.Count > HostService.MaximoFotos)
        {
            TempData["Error"] = $"El alojamiento admite máximo {HostService.MaximoFotos} fotos. Ya tiene {total}.";
            return RedirectToAction(nameof(Fotos), new { id });
        }

        var subidas = 0;
        var errores = new List<string>();

        foreach (var archivo in fotos)
        {
            string? ruta = null;
            try
            {
                ruta = await _imagenes.GuardarAsync(archivo);
                await _host.AgregarFotoAsync(id, uid, ruta);
                subidas++;
            }
            catch (InvalidOperationException ex)
            {
                if (ruta is not null) _imagenes.Borrar(ruta);
                errores.Add(ex.Message);
            }
            catch (ReglaNegocioException ex)
            {
                if (ruta is not null) _imagenes.Borrar(ruta);
                errores.Add(ex.Message);
                break;
            }
            catch
            {
                if (ruta is not null) _imagenes.Borrar(ruta);
                throw;
            }
        }

        if (subidas > 0) TempData["Exito"] = $"Se subieron {subidas} foto(s).";
        if (errores.Count > 0) TempData["Error"] = string.Join(" ", errores);
        return RedirectToAction(nameof(Fotos), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarFoto(int id, int fotoId)
    {
        var uid = User.ObtenerUsuarioId();
        if (!await EsDuenoAsync(id, uid)) return NotFound();

        try
        {
            var ruta = await _host.EliminarFotoAsync(fotoId, uid);
            if (ruta is not null) _imagenes.Borrar(ruta);
            TempData["Exito"] = "Foto eliminada.";
        }
        catch (ReglaNegocioException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Fotos), new { id });
    }

    /// Mueve una foto una posición arriba (-1) o abajo (1).
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MoverFoto(int id, int fotoId, int direccion)
    {
        var uid = User.ObtenerUsuarioId();
        if (!await EsDuenoAsync(id, uid)) return NotFound();

        var fotos = await _host.FotosAsync(id);
        var indice = fotos.FindIndex(f => f.Id == fotoId);
        var nuevo = indice + direccion;

        if (indice >= 0 && nuevo >= 0 && nuevo < fotos.Count)
        {
            (fotos[indice], fotos[nuevo]) = (fotos[nuevo], fotos[indice]);
            try
            {
                await _host.ReordenarAsync(id, uid, fotos.Select(f => f.Id));
            }
            catch (ReglaNegocioException ex)
            {
                TempData["Error"] = ex.Message;
            }
        }
        return RedirectToAction(nameof(Fotos), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id, bool activo)
    {
        try
        {
            await _host.CambiarEstadoAsync(id, User.ObtenerUsuarioId(), activo);
            TempData["Exito"] = activo ? "Alojamiento activado." : "Alojamiento pausado.";
        }
        catch (ReglaNegocioException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    /// Elimina el alojamiento. Si tiene reservas activas, se cancelan con reembolso total.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        try
        {
            await _host.EliminarAsync(id, User.ObtenerUsuarioId());
            TempData["Exito"] = "Alojamiento eliminado. Las reservas activas se cancelaron con reembolso total.";
        }
        catch (ReglaNegocioException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    /// Pagos del anfitrión: cobrado, comisión de Homi y lo que le corresponde.
    [HttpGet]
    public async Task<IActionResult> Pagos() =>
        View(new PagosViewModel { Pagos = await _host.PagosAsync(User.ObtenerUsuarioId()) });

    /// Desglose de una reserva: solo lo ve el anfitrión del alojamiento.
    [HttpGet]
    public async Task<IActionResult> Desglose(int id)
    {
        try
        {
            var cargos = await _host.DesgloseAsync(id, User.ObtenerUsuarioId());
            return View(new DesgloseViewModel { ReservaId = id, Cargos = cargos });
        }
        catch (ReglaNegocioException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Pagos));
        }
    }

    /// Estadísticas de ingresos por mes del año elegido.
    [HttpGet]
    public async Task<IActionResult> Estadisticas(int? anio)
    {
        var uid = User.ObtenerUsuarioId();
        var year = anio ?? Homi.Helpers.Reloj.Ahora.Year;
        return View(new EstadisticasViewModel
        {
            Anio = year,
            Meses = await _host.IngresosPorMesAsync(uid, year),
            Resumen = await _host.ResumenAsync(uid)
        });
    }

    /// Reputación: promedio general, promedio por alojamiento y reseñas recientes.
    [HttpGet]
    public async Task<IActionResult> Reputacion()
    {
        var resenas = await _host.ResenasDeAnfitrionAsync(User.ObtenerUsuarioId());
        var porPropiedad = resenas
            .GroupBy(r => r.Titulo)
            .Select(g => new PromedioPropiedad
            {
                Titulo = g.Key,
                Total = g.Count(),
                Promedio = Math.Round((decimal)g.Average(x => x.Calificacion), 1)
            })
            .OrderBy(p => p.Titulo)
            .ToList();

        return View(new ReputacionViewModel
        {
            Total = resenas.Count,
            Promedio = resenas.Count > 0 ? Math.Round((decimal)resenas.Average(x => x.Calificacion), 1) : null,
            PorPropiedad = porPropiedad,
            Resenas = resenas.Take(30).ToList()
        });
    }

    /// Historial de huéspedes del anfitrión.
    [HttpGet]
    public async Task<IActionResult> Huespedes() =>
        View(new HuespedesViewModel { Huespedes = await _host.HuespedesAsync(User.ObtenerUsuarioId()) });

    /// Tarifas de cada alojamiento, con acceso directo a editarlas.
    [HttpGet]
    public async Task<IActionResult> Tarifas() =>
        View(new TarifasViewModel { Alojamientos = await _host.AlojamientosAsync(User.ObtenerUsuarioId()) });

    /// El anfitrión cancela una reserva de sus alojamientos: reembolso total y aviso al huésped.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelarReserva(int id)
    {
        try
        {
            await _host.CancelarReservaAsync(id, User.ObtenerUsuarioId(), null);
            TempData["Exito"] = "Reserva cancelada. Se devolvió el pago completo al huésped.";
        }
        catch (ReglaNegocioException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    /// Libera el pago de una reserva después del check-in.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> LiberarPago(int id)
    {
        try
        {
            await _host.LiberarPagoAsync(id, User.ObtenerUsuarioId());
            TempData["Exito"] = "Pago liberado. El monto ya está disponible para ti.";
        }
        catch (ReglaNegocioException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Pagos));
    }

    /// Listas para escribir país, estado y ciudad sin errores de ortografía (se puede escribir uno nuevo).
    private async Task CargarUbicacionesAsync()
    {
        ViewBag.Paises = await _propiedades.PaisesAsync();
        ViewBag.Estados = await _propiedades.EstadosAsync();
        ViewBag.Ciudades = await _propiedades.ListarCiudadesAsync();
    }

    private async Task<bool> EsDuenoAsync(int alojamientoId, int usuarioId)
    {
        var (d, _) = await _propiedades.ObtenerDetalleAsync(alojamientoId, usuarioId);
        return d is not null && d.AnfitrionId == usuarioId;
    }
}
