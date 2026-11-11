using System.Data;
using System.Reflection;
using Microsoft.Data.SqlClient;

namespace Homi.Data;

/// Error de regla de negocio: el procedimiento lanzó THROW 50xxx con un mensaje para el usuario.
public class ReglaNegocioException : Exception
{
    public ReglaNegocioException(string mensaje) : base(mensaje) { }
}

/// Acceso a la base Homi. La lógica vive en los procedimientos almacenados de la v3.
public class Db
{
    private readonly string _cadena;

    public Db(IConfiguration configuracion)
    {
        _cadena = configuracion.GetConnectionString("HomiDb")
            ?? throw new InvalidOperationException("Falta la cadena HomiDb en appsettings.json.");
    }


    /// Prueba de conexión: abre la base y devuelve el nombre de la base y del servidor.
    public async Task<string> ProbarConexionAsync()
    {
        try
        {
            await using var cn = new SqlConnection(_cadena);
            await cn.OpenAsync();
            using var cmd = new SqlCommand("SELECT DB_NAME() + N' en ' + @@SERVERNAME", cn);
            var texto = (string?)await cmd.ExecuteScalarAsync();
            return "Conexión correcta: " + texto;
        }
        catch (Exception ex)
        {
            return "Error de conexión: " + ex.Message;
        }
    }

    public static SqlParameter P(string nombre, object? valor) => new(nombre, valor ?? DBNull.Value);

    public static SqlParameter PFecha(string nombre, DateOnly? fecha) => new(nombre, SqlDbType.Date)
    {
        Value = fecha.HasValue ? fecha.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value
    };

    public static SqlParameter Salida(string nombre, SqlDbType tipo, int tamano = 0) =>
        new(nombre, tipo, tamano) { Direction = ParameterDirection.Output };

    public async Task<List<T>> ListarAsync<T>(string procedimiento, params SqlParameter[] parametros) where T : new()
    {
        await using var cn = new SqlConnection(_cadena);
        using var cmd = Crear(cn, procedimiento, parametros);
        try
        {
            await cn.OpenAsync();
            await using var rd = await cmd.ExecuteReaderAsync();
            return await LeerAsync<T>(rd);
        }
        catch (SqlException ex)
        {
            throw TraducirError(ex);
        }
    }

    public async Task<(List<A> Primera, List<B> Segunda)> ListarDosAsync<A, B>(string procedimiento, params SqlParameter[] parametros)
        where A : new()
        where B : new()
    {
        await using var cn = new SqlConnection(_cadena);
        using var cmd = Crear(cn, procedimiento, parametros);
        try
        {
            await cn.OpenAsync();
            await using var rd = await cmd.ExecuteReaderAsync();
            var primera = await LeerAsync<A>(rd);
            if (!await rd.NextResultAsync()) return (primera, new List<B>());
            var segunda = await LeerAsync<B>(rd);
            return (primera, segunda);
        }
        catch (SqlException ex)
        {
            throw TraducirError(ex);
        }
    }

    public async Task EjecutarAsync(string procedimiento, params SqlParameter[] parametros)
    {
        await using var cn = new SqlConnection(_cadena);
        using var cmd = Crear(cn, procedimiento, parametros);
        try
        {
            await cn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }
        catch (SqlException ex)
        {
            throw TraducirError(ex);
        }
    }

    private static bool EsReglaNegocio(SqlException ex) => ex.Number >= 50000 && ex.Number < 60000;

    /// Reglas de negocio se muestran tal cual. Otros errores de SQL (restricciones CHECK, UNIQUE, FK)
    /// se registran en la consola y el usuario recibe un mensaje claro en lugar de una página de error.
    private static Exception TraducirError(SqlException ex)
    {
        if (EsReglaNegocio(ex)) return new ReglaNegocioException(ex.Message);
        Homi.Helpers.Registro.Error($"SQL {ex.Number}: {ex.Message}");
        return new ReglaNegocioException("No se pudo completar la operación. Revisa los datos e intenta de nuevo.");
    }

    private static SqlCommand Crear(SqlConnection cn, string procedimiento, SqlParameter[] parametros)
    {
        var cmd = new SqlCommand(procedimiento, cn) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.AddRange(parametros);
        return cmd;
    }

    private static async Task<List<T>> LeerAsync<T>(SqlDataReader rd) where T : new()
    {
        var lista = new List<T>();
        var propiedades = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        while (await rd.ReadAsync())
        {
            var objeto = new T();
            for (var i = 0; i < rd.FieldCount; i++)
            {
                if (!propiedades.TryGetValue(rd.GetName(i), out var propiedad)) continue;
                var valor = rd.GetValue(i);
                if (valor == DBNull.Value) continue;
                propiedad.SetValue(objeto, Convertir(valor, propiedad.PropertyType));
            }
            lista.Add(objeto);
        }
        return lista;
    }

    private static object Convertir(object valor, Type tipo)
    {
        var destino = Nullable.GetUnderlyingType(tipo) ?? tipo;
        if (destino == typeof(DateOnly)) return DateOnly.FromDateTime((DateTime)valor);
        return Convert.ChangeType(valor, destino);
    }
}
