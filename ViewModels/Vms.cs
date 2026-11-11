using System.ComponentModel.DataAnnotations;
using Homi.Models;

namespace Homi.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Escribe tu nombre."), StringLength(60)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe tu apellido."), StringLength(60)]
    public string Apellido { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe tu correo."), StringLength(120)]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [RegularExpression(@"^[A-Za-z0-9._%+-]+@[A-Za-z0-9-]+(\.[A-Za-z0-9-]+)*\.[A-Za-z]{2,}$", ErrorMessage = "Escribe un correo válido, por ejemplo nombre@dominio.com.")]
    public string Correo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe tu teléfono."), StringLength(10, MinimumLength = 10), RegularExpression(@"^\d{10}$", ErrorMessage = "El teléfono celular debe tener 10 dígitos, sin espacios.")]
    public string Telefono { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).+$", ErrorMessage = "La contraseña debe incluir al menos una mayúscula, un número y un carácter especial.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    [DataType(DataType.Password)]
    public string ConfirmarPassword { get; set; } = string.Empty;
}

public class LoginViewModel
{
    [Required(ErrorMessage = "Escribe tu correo."), EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [RegularExpression(@"^[A-Za-z0-9._%+-]+@[A-Za-z0-9-]+(\.[A-Za-z0-9-]+)*\.[A-Za-z]{2,}$", ErrorMessage = "Escribe un correo válido, por ejemplo nombre@dominio.com.")]
    public string Correo { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}

public class PropertyFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(120)] public string Titulo { get; set; } = string.Empty;
    [Required, StringLength(2000)] public string Descripcion { get; set; } = string.Empty;
    [Required, StringLength(120)] public string Calle { get; set; } = string.Empty;
    [Required, StringLength(10)] public string NumeroExterior { get; set; } = string.Empty;
    [Required, StringLength(80)] public string Colonia { get; set; } = string.Empty;
    [Required, StringLength(10)] public string CodigoPostal { get; set; } = string.Empty;
    [StringLength(250)] public string? Referencias { get; set; }

    [Required, StringLength(80)] public string Pais { get; set; } = "México";
    [Required, StringLength(80)] public string Estado { get; set; } = string.Empty;
    [Required, StringLength(80)] public string Ciudad { get; set; } = string.Empty;

    [Range(0.01, 1000000, ErrorMessage = "El precio debe ser mayor a cero.")] public decimal PrecioPorNoche { get; set; }
    public bool CobraLimpieza { get; set; }
    [Range(1, 50, ErrorMessage = "La capacidad debe estar entre 1 y 50.")] public int CapacidadMaxima { get; set; } = 1;
}

public class HomeViewModel
{
    public List<CiudadItem> Ciudades { get; set; } = new();
    public List<AlojamientoResumen> Destacados { get; set; } = new();
}

public class BuscarViewModel
{
    public int? CiudadId { get; set; }
    public DateOnly? Entrada { get; set; }
    public DateOnly? Salida { get; set; }
    public int Huespedes { get; set; } = 1;
    public List<CiudadItem> Ciudades { get; set; } = new();
    public List<AlojamientoResumen> Resultados { get; set; } = new();
    public string? Error { get; set; }
}

public class DetalleViewModel
{
    public AlojamientoDetalle Alojamiento { get; set; } = null!;
    public List<FotoItem> Fotos { get; set; } = new();
    public decimal? Promedio { get; set; }
    public List<ResenaItem> Resenas { get; set; } = new();
    public DateOnly? Entrada { get; set; }
    public DateOnly? Salida { get; set; }
    public int Huespedes { get; set; } = 1;
    public DesgloseTarifa? Desglose { get; set; }
    public bool EsFavorito { get; set; }
    public DateOnly Mes { get; set; }
    public HashSet<DateOnly> Ocupadas { get; set; } = new();
}

public class HostPanelViewModel
{
    public ResumenAnfitrion Resumen { get; set; } = new();
    public List<HostAlojamiento> Alojamientos { get; set; } = new();
    public List<HostReserva> Reservas { get; set; } = new();
}

public class FotosViewModel
{
    public AlojamientoDetalle Alojamiento { get; set; } = null!;
    public List<FotoItem> Fotos { get; set; } = new();
}

public class ReservaPagoViewModel
{
    public ReservaDetalle Reserva { get; set; } = null!;
    public List<CargoItem> Cargos { get; set; } = new();
    public int SegundosRestantes { get; set; }
}

public class MisReservasViewModel
{
    public List<ReservaLista> Reservas { get; set; } = new();
    public string Filtro { get; set; } = "Todas";
}

public class ComentariosViewModel
{
    [Required(ErrorMessage = "Elige un tipo.")]
    public string Tipo { get; set; } = "Comentario";

    [Required(ErrorMessage = "El asunto es obligatorio."), StringLength(120)]
    public string Asunto { get; set; } = string.Empty;

    [Required(ErrorMessage = "El mensaje es obligatorio."), StringLength(2000)]
    public string Mensaje { get; set; } = string.Empty;

    public List<ComentarioItem> Historial { get; set; } = new();
}

public class UsuariosAdminViewModel
{
    public string? Buscar { get; set; }
    public bool? Activo { get; set; }
    public int ActualId { get; set; }
    public List<UsuarioAdmin> Usuarios { get; set; } = new();
}

public class ComentariosAdminViewModel
{
    public string? Estado { get; set; }
    public string? Tipo { get; set; }
    public List<ComentarioAdmin> Comentarios { get; set; } = new();
}

public class BitacoraViewModel
{
    public DateOnly? Desde { get; set; }
    public DateOnly? Hasta { get; set; }
    public int? UsuarioId { get; set; }
    public string? Accion { get; set; }
    public List<BitacoraItem> Registros { get; set; } = new();
}

public class ReportesViewModel
{
    public DateOnly Desde { get; set; }
    public DateOnly Hasta { get; set; }
    public string? Error { get; set; }
    public List<ReporteDia> ReservasPorDia { get; set; } = new();
    public List<ReporteCiudad> IngresosPorCiudad { get; set; } = new();
    public decimal TotalIngresos => IngresosPorCiudad.Sum(i => i.Ingresos);
    public int TotalReservas => ReservasPorDia.Sum(r => r.Reservas);
}
