using System.Globalization;
using Homi.Data;
using Homi.Helpers;
using Homi.Services;
using Homi.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Homi.Controllers;

public class PropertiesController : Controller
{
    private readonly PropertyService _propiedades;
    private readonly FavoritesService _favoritos;

    public PropertiesController(PropertyService propiedades, FavoritesService favoritos)
    {
        _propiedades = propiedades;
        _favoritos = favoritos;
    }

    public async Task<IActionResult> Index(int? ciudadId, DateOnly? entrada, DateOnly? salida, int huespedes = 1)
    {
        var vm = new BuscarViewModel
        {
            CiudadId = ciudadId,
            Entrada = entrada,
            Salida = salida,
            Huespedes = huespedes,
            Ciudades = await _propiedades.ListarCiudadesAsync()
        };

        try
        {
            vm.Resultados = await _propiedades.BuscarAsync(ciudadId, entrada, salida, huespedes);
        }
        catch (ReglaNegocioException ex)
        {
            vm.Error = ex.Message;
        }

        return View(vm);
    }

    /// Ficha del alojamiento. "mes" (yyyy-MM) indica el mes que muestra el calendario.
    public async Task<IActionResult> Details(int id, DateOnly? entrada, DateOnly? salida, int huespedes = 1, string? mes = null)
    {
        int? usuarioId = User.Identity?.IsAuthenticated == true ? User.ObtenerUsuarioId() : null;

        var (detalle, fotos) = await _propiedades.ObtenerDetalleAsync(id, usuarioId);
        if (detalle is null) return NotFound();

        var hoy = Homi.Helpers.Reloj.Hoy;
        var mesBase = ParseMes(mes) ?? new DateOnly(hoy.Year, hoy.Month, 1);
        var ventanaFin = mesBase.AddMonths(2).AddDays(-1);
        var ocupadas = await _propiedades.OcupadasAsync(id, mesBase, ventanaFin);

        var vm = new DetalleViewModel
        {
            Alojamiento = detalle,
            Fotos = fotos,
            Promedio = await _propiedades.PromedioAsync(id),
            Resenas = await _propiedades.ResenasAsync(id),
            Entrada = entrada,
            Salida = salida,
            Huespedes = huespedes,
            EsFavorito = usuarioId is not null && await _favoritos.EsFavoritoAsync(usuarioId.Value, id),
            Mes = mesBase,
            Ocupadas = ocupadas.Select(o => o.Fecha).ToHashSet()
        };

        if (entrada.HasValue && salida.HasValue && salida > entrada)
            vm.Desglose = await _propiedades.CalcularDesgloseAsync(id, entrada.Value, salida.Value);

        return View(vm);
    }

    private static DateOnly? ParseMes(string? mes) =>
        !string.IsNullOrEmpty(mes) && DateOnly.TryParseExact(mes + "-01", "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
}
