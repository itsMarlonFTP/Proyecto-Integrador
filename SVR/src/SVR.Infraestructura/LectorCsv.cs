using System.Globalization;
using SVR.Dominio;

namespace SVR.Infraestructura;

/// <summary>
/// Una fila de un archivo CSV: su número de línea en el archivo (el encabezado es la línea 1)
/// y sus campos como texto, sin espacios al inicio ni al final.
/// </summary>
public sealed record FilaCsv(int Linea, string[] Campos);

/// <summary>
/// Describe una fila del catálogo que no se pudo cargar.
/// </summary>
/// <param name="Linea">Número de línea del archivo, contando el encabezado como línea 1.</param>
/// <param name="Motivo">Explicación del rechazo.</param>
public sealed record RechazoCatalogo(int Linea, string Motivo);

/// <summary>
/// Contiene los productos válidos y las filas rechazadas al cargar un catálogo.
/// </summary>
public sealed class ResultadoCargaCatalogo
{
    /// <summary>
    /// Crea el resultado de una carga de catálogo.
    /// </summary>
    /// <param name="productos">Productos que pasaron las validaciones.</param>
    /// <param name="rechazos">Filas rechazadas con su número y motivo.</param>
    public ResultadoCargaCatalogo(
        IEnumerable<Producto> productos,
        IEnumerable<RechazoCatalogo> rechazos)
    {
        Productos = Array.AsReadOnly(productos.ToArray());
        Rechazos = Array.AsReadOnly(rechazos.ToArray());
    }

    /// <summary>
    /// Obtiene los productos válidos del catálogo.
    /// </summary>
    public IReadOnlyList<Producto> Productos { get; }

    /// <summary>
    /// Obtiene las filas rechazadas durante la carga.
    /// </summary>
    public IReadOnlyList<RechazoCatalogo> Rechazos { get; }
}

/// <summary>
/// Lee CSV genéricos y carga el catálogo de productos.
/// </summary>
public static class LectorCsv
{
    private static readonly string[] EncabezadosCatalogo =
    [
        "ean",
        "descripcion",
        "categoria",
        "precio",
        "punto_reorden",
        "stock"
    ];

    /// <summary>
    /// Lee un CSV en streaming, omite el encabezado y las líneas vacías,
    /// y devuelve cada fila con su número de línea.
    /// </summary>
    /// <param name="ruta">Ruta del archivo CSV.</param>
    /// <param name="separador">Carácter que separa los campos.</param>
    /// <returns>Filas del archivo sin el encabezado.</returns>
    public static IEnumerable<FilaCsv> Leer(string ruta, char separador = ',')
    {
        int linea = 0;

        foreach (string texto in File.ReadLines(ruta))
        {
            linea++;

            // El encabezado y las líneas vacías no son filas de datos.
            if (linea == 1 || string.IsNullOrWhiteSpace(texto))
            {
                continue;
            }

            // Se recortan espacios exteriores; el EAN permanece como texto.
            string[] campos = texto
                .Split(separador)
                .Select(campo => campo.Trim())
                .ToArray();

            yield return new FilaCsv(linea, campos);
        }
    }

    /// <summary>
    /// Carga y valida las filas del catálogo desde un CSV.
    /// Los errores de una fila se registran como rechazos sin detener las demás.
    /// </summary>
    /// <param name="ruta">Ruta del archivo de catálogo.</param>
    /// <returns>Productos válidos y filas rechazadas.</returns>
    /// <exception cref="InvalidDataException">
    /// Se produce si el archivo está vacío o el encabezado no coincide con el formato esperado.
    /// </exception>
    public static ResultadoCargaCatalogo CargarCatalogo(string ruta)
    {
        var productos = new List<Producto>();
        var rechazos = new List<RechazoCatalogo>();

        // El enumerador mantiene la lectura en streaming, igual que el lector genérico.
        using IEnumerator<string> lineas = File.ReadLines(ruta).GetEnumerator();

        if (!lineas.MoveNext())
        {
            throw new InvalidDataException("El archivo del catálogo está vacío.");
        }

        ValidarEncabezado(lineas.Current);

        int numeroLinea = 1;

        while (lineas.MoveNext())
        {
            numeroLinea++;
            string texto = lineas.Current;

            // Las líneas vacías no representan productos.
            if (string.IsNullOrWhiteSpace(texto))
            {
                continue;
            }

            string[] campos = texto
                .Split(',')
                .Select(campo => campo.Trim())
                .ToArray();

            var fila = new FilaCsv(numeroLinea, campos);

            if (campos.Length != EncabezadosCatalogo.Length)
            {
                rechazos.Add(new RechazoCatalogo(
                    numeroLinea,
                    $"Se esperaban {EncabezadosCatalogo.Length} columnas y se encontraron {campos.Length}."));
                continue;
            }

            if (IntentarCrearProducto(fila, out Producto? producto, out string motivo))
            {
                productos.Add(producto!);
            }
            else
            {
                rechazos.Add(new RechazoCatalogo(numeroLinea, motivo));
            }
        }

        return new ResultadoCargaCatalogo(productos, rechazos);
    }

    /// <summary>
    /// Comprueba que el archivo tenga los encabezados requeridos y en el orden esperado.
    /// </summary>
    /// <param name="encabezado">Primera línea del archivo.</param>
    /// <exception cref="InvalidDataException">
    /// Se produce si las columnas no coinciden con el formato del catálogo.
    /// </exception>
    private static void ValidarEncabezado(string encabezado)
    {
        // Se elimina un posible BOM UTF-8 antes de comparar la primera columna.
        string encabezadoSinBOM = encabezado.TrimStart('\uFEFF');

        string[] columnas = encabezadoSinBOM
            .Split(',')
            .Select(columna => columna.Trim())
            .ToArray();

        if (!columnas.SequenceEqual(
                EncabezadosCatalogo,
                StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                "El encabezado debe ser: ean,descripcion,categoria,precio,punto_reorden,stock.");
        }
    }

    /// <summary>
    /// Convierte y valida una fila de catálogo creando un producto del dominio.
    /// </summary>
    /// <param name="fila">Fila CSV que contiene los datos del producto.</param>
    /// <param name="producto">Producto creado, si la fila es válida.</param>
    /// <param name="motivo">Motivo del rechazo, si la fila no es válida.</param>
    /// <returns><see langword="true"/> si se creó el producto; de lo contrario, <see langword="false"/>.</returns>
    private static bool IntentarCrearProducto(
        FilaCsv fila,
        out Producto? producto,
        out string motivo)
    {
        producto = null;
        motivo = string.Empty;

        string[] campos = fila.Campos;

        // El precio usa punto decimal independientemente de la configuración regional.
        if (!decimal.TryParse(
                campos[3],
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out decimal precio))
        {
            motivo = $"El precio '{campos[3]}' no es válido.";
            return false;
        }

        if (!int.TryParse(
                campos[4],
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int puntoReorden))
        {
            motivo = $"El punto_reorden '{campos[4]}' no es un entero válido.";
            return false;
        }

        if (!int.TryParse(
                campos[5],
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int stock))
        {
            motivo = $"El stock '{campos[5]}' no es un entero válido.";
            return false;
        }

        try
        {
            // Producto aplica las reglas del dominio, incluido EAN obligatorio y precio positivo.
            producto = new Producto(
                campos[0],
                campos[1],
                campos[2],
                precio,
                puntoReorden,
                stock);

            return true;
        }
        catch (ArgumentException excepcion)
        {
            // Se transforma el error de dominio en un rechazo asociado a esta fila.
            motivo = excepcion.Message;
            return false;
        }
    }
}