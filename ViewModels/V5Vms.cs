using System.ComponentModel.DataAnnotations;
using Homi.Models;

namespace Homi.ViewModels;

public class EstadisticasViewModel
{
    public int Anio { get; set; }
    public List<IngresoMes> Meses { get; set; } = new();
    public ResumenAnfitrion Resumen { get; set; } = new();
    public decimal MaximoMes => Meses.Count == 0 ? 0 : Meses.Max(m => m.Ingresos);
    public decimal TotalAnio => Meses.Sum(m => m.Ingresos);
}

public class PromedioPropiedad
{
    public string Titulo { get; set; } = string.Empty;
    public decimal Promedio { get; set; }
    public int Total { get; set; }
}

public class ReputacionViewModel
{
    public decimal? Promedio { get; set; }
    public int Total { get; set; }
    public List<PromedioPropiedad> PorPropiedad { get; set; } = new();
    public List<ResenaAnfitrion> Resenas { get; set; } = new();
}

public class HuespedesViewModel
{
    public List<HuespedHistorial> Huespedes { get; set; } = new();
}

public class TarifasViewModel
{
    public List<HostAlojamiento> Alojamientos { get; set; } = new();
}


public class VerificarViewModel
{
    [Required(ErrorMessage = "Escribe tu correo."), EmailAddress(ErrorMessage = "Escribe un correo válido.")]
    public string Correo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe el código."), RegularExpression(@"^\d{6}$", ErrorMessage = "El código tiene 6 dígitos.")]
    public string Codigo { get; set; } = string.Empty;
}

public class AlojamientosAdminViewModel
{
    public string? Buscar { get; set; }
    public bool? Activo { get; set; }
    public List<Homi.Models.AdminAlojamientoItem> Alojamientos { get; set; } = new();
}

public class ReservasAdminViewModel
{
    public DateOnly? Desde { get; set; }
    public DateOnly? Hasta { get; set; }
    public int? Estado { get; set; }
    public List<Homi.Models.AdminReservaItem> Reservas { get; set; } = new();
}

public class CatalogosAdminViewModel
{
    public string Catalogo { get; set; } = "EstadosReserva";
    public List<Homi.Models.CatalogoItem> Items { get; set; } = new();
}

public class EditarUsuarioViewModel
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe el nombre."), StringLength(60)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe el apellido."), StringLength(60)]
    public string Apellido { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe el teléfono."), RegularExpression(@"^\d{10}$", ErrorMessage = "El teléfono debe tener 10 dígitos.")]
    public string Telefono { get; set; } = string.Empty;
}
