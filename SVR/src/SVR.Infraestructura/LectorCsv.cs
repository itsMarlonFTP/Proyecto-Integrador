namespace SVR.Infraestructura;

/// <summary>
/// Una fila de un archivo CSV: su número de línea en el archivo (el encabezado es la línea 1)
/// y sus campos como texto, sin espacios al inicio ni al final.
/// </summary>
public sealed record FilaCsv(int Linea, string[] Campos);

/// <summary>
/// YA RESUELTO: lee un CSV en streaming (línea por línea), salta el encabezado y las líneas vacías,
/// y regresa cada fila con su número de línea.
/// No convierte tipos ni valida: eso es parte de HU-01 y HU-02.
/// </summary>
public static class LectorCsv
{
    public static IEnumerable<FilaCsv> Leer(string ruta, char separador = ',')
    {
        int linea = 0;
        foreach (string texto in File.ReadLines(ruta))
        {
            linea++;
            if (linea == 1 || string.IsNullOrWhiteSpace(texto))
                continue;
            yield return new FilaCsv(linea, texto.Split(separador).Select(c => c.Trim()).ToArray());
        }
    }
}
