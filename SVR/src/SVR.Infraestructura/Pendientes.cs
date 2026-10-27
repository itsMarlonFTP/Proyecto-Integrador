using System.Text;
namespace SVR.Infraestructura;

// HU-06 · TODO: InventarioEnMemoria : IInventarioRepo
//   - Se llena con el stock de catalogo.csv (Dictionary<string, int> por EAN).
//   - Descontar lanza StockInsuficienteException si no alcanza; el stock nunca queda negativo.
//   - Módulos 3, 5 y 8 (es la implementación "de producción" de este proyecto).

// HU-08 · TODO: CorteCajaExportador : IDisposable
//   - Escribe el corte de caja a CSV con un StreamWriter privado.
//   - Se usa con using para que el archivo siempre se cierre.
//   - Módulos 7 y 8.

/// <summary>
/// Escribe filas CSV usando punto decimal y escapando campos con comas o comillas.
/// </summary>
/// <param name="ruta">Ruta del archivo de salida.</param>
/// <param name="filas">Filas y campos que se escribirán.</param>

/// <summary>
/// Escribe archivos CSV y garantiza que el escritor se cierre al liberar el objeto.
/// </summary>
public sealed class CorteCajaExportador : IDisposable
{
    private readonly StreamWriter _escritor;
    private bool _disposed;

    /// <summary>
    /// Abre el archivo CSV para escritura, reemplazando su contenido anterior.
    /// </summary>
    /// <param name="ruta">Ruta del archivo que se va a generar.</param>
    public CorteCajaExportador(string ruta)
    {
        if (string.IsNullOrWhiteSpace(ruta))
        {
            throw new ArgumentException("La ruta no puede estar vacía.", nameof(ruta));
        }

        _escritor = new StreamWriter(
            ruta,
            append: false,
            encoding: new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    /// <summary>
    /// Escribe una fila CSV, escapando los campos que contienen comas o comillas.
    /// </summary>
    /// <param name="campos">Valores que forman la fila.</param>
    public void EscribirFila(IEnumerable<string> campos)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(campos);

        string linea = string.Join(",", campos.Select(EscalarCampoCsv));
        _escritor.WriteLine(linea);
    }

    /// <summary>
    /// Escribe todas las filas de una colección en un archivo CSV.
    /// </summary>
    /// <param name="ruta">Ruta del archivo de salida.</param>
    /// <param name="filas">Filas que se escribirán.</param>
    public static void EscribirCsv(string ruta, IEnumerable<string[]> filas)
    {
        ArgumentNullException.ThrowIfNull(filas);

        // El using cierra el archivo incluso si falla la escritura.
        using var exportador = new CorteCajaExportador(ruta);

        foreach (string[] fila in filas)
        {
            exportador.EscribirFila(fila);
        }
    }

    /// <summary>
    /// Cierra el archivo y libera los recursos asociados.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _escritor.Dispose();
        _disposed = true;
    }

    /// <summary>
    /// Escapa comillas y encierra entre comillas los valores CSV que lo necesitan.
    /// </summary>
    private static string EscalarCampoCsv(string valor)
    {
        if (valor.Contains('"') ||
            valor.Contains(',') ||
            valor.Contains('\r') ||
            valor.Contains('\n'))
        {
            return $"\"{valor.Replace("\"", "\"\"")}\"";
        }

        return valor;
    }
}