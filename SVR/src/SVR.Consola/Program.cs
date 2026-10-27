using System.Globalization;
using SVR.Dominio;
using SVR.Infraestructura;

// Sistema de Venta y Reabastecimiento (SVR) · cierre del día de la tienda 0457.
// Proyecto base: la lectura de los CSV y el CEDIS simulado ya funcionan.
// Tu trabajo: completar HU-01 a HU-08 (ver README.md) y reemplazar cada "Pendiente(...)".

CultureInfo.CurrentCulture = CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("es-MX");
Console.OutputEncoding = System.Text.Encoding.UTF8;

string datos = Path.Combine(AppContext.BaseDirectory, "datos");
var filasCatalogo = LectorCsv.Leer(Path.Combine(datos, "catalogo.csv")).ToList();
var filasPromociones = LectorCsv.Leer(Path.Combine(datos, "promociones.csv")).ToList();
var filasVentas = LectorCsv.Leer(Path.Combine(datos, "ventas_0457.csv")).ToList();
var cedis = new CedisSimulado();   // lo usarás en HU-07

Console.WriteLine("=== SVR · TIENDA 0457 ===");
Console.WriteLine($"Filas leídas: catálogo {filasCatalogo.Count} · promociones {filasPromociones.Count} · ventas {filasVentas.Count}");
Console.WriteLine($"Ejemplo: línea {filasVentas[0].Linea} -> {string.Join(" | ", filasVentas[0].Campos)}");
Console.WriteLine();

// Avance 1 (9:35–10:15)
Pendiente("HU-01 Cargar el catálogo y rechazar EAN vacío o precio inválido");

// HU-01: convierte las filas leídas del CSV en productos y registra las inválidas.
var productos = new List<Producto>();
var rechazosCatalogo = new List<(int Linea, string Motivo)>();

foreach (FilaCsv fila in filasCatalogo)
{
    string[] campos = fila.Campos;

    // El catálogo debe tener las seis columnas definidas en el encabezado.
    if (campos.Length != 6)
    {
        rechazosCatalogo.Add((
            fila.Linea,
            $"Se esperaban 6 columnas y se encontraron {campos.Length}."));
        continue;
    }

    // El precio del archivo usa punto decimal, por ejemplo 18.50.
    if (!decimal.TryParse(
            campos[3],
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out decimal precio))
    {
        rechazosCatalogo.Add((
            fila.Linea,
            $"El precio '{campos[3]}' no es válido."));
        continue;
    }

    if (!int.TryParse(
            campos[4],
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out int puntoReorden))
    {
        rechazosCatalogo.Add((
            fila.Linea,
            $"El punto_reorden '{campos[4]}' no es un entero válido."));
        continue;
    }

    if (!int.TryParse(
            campos[5],
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out int stock))
    {
        rechazosCatalogo.Add((
            fila.Linea,
            $"El stock '{campos[5]}' no es un entero válido."));
        continue;
    }

    try
    {
        // Producto valida, entre otras reglas, que el EAN no esté vacío
        // y que el precio sea mayor que cero.
        productos.Add(new Producto(
            campos[0],
            campos[1],
            campos[2],
            precio,
            puntoReorden,
            stock));
    }
    catch (ArgumentException excepcion)
    {
        // El rechazo conserva el número de línea original del archivo.
        rechazosCatalogo.Add((fila.Linea, excepcion.Message));
    }
}

Console.WriteLine($"Productos válidos: {productos.Count}");
Console.WriteLine($"Filas de catálogo rechazadas: {rechazosCatalogo.Count}");

foreach (var rechazo in rechazosCatalogo)
{
    Console.WriteLine(
        $"Catálogo, línea {rechazo.Linea}: {rechazo.Motivo}");
}

Console.WriteLine();

Pendiente("HU-02 Procesar ventas_0457.csv y escribir rechazos.log (línea y motivo)");

// HU-02: valida las filas de ventas y conserva las válidas para las siguientes historias.
var ventasValidas = new List<LineaVenta>();

var rechazosVentas = new List<string>();

// Se usa el catálogo validado en HU-01 para detectar EAN que no existen.
var eansCatalogo = productos
    .Select(producto => producto.Ean)
    .ToHashSet(StringComparer.Ordinal);

foreach (FilaCsv fila in filasVentas)
{
    string[] campos = fila.Campos;
    var errores = new List<string>();

    // El formato de ventas requiere folio, caja, hora, EAN, piezas y medio de pago.
    if (campos.Length != 6)
    {
        rechazosVentas.Add(
            $"Línea {fila.Linea}: se esperaban 6 columnas y se encontraron {campos.Length}.");
        continue;
    }

    // Se valida que los campos de identificación y pago tengan contenido.
    if (string.IsNullOrWhiteSpace(campos[0]))
        errores.Add("el folio está vacío");

    if (string.IsNullOrWhiteSpace(campos[1]))
        errores.Add("la caja está vacía");

    if (string.IsNullOrWhiteSpace(campos[3]))
        errores.Add("el EAN está vacío");
    else if (!eansCatalogo.Contains(campos[3]))
        errores.Add($"el EAN '{campos[3]}' no existe en el catálogo");

    if (string.IsNullOrWhiteSpace(campos[5]))
        errores.Add("el medio de pago está vacío");

    // Se convierte la hora para que pueda reutilizarse en HU-05.
    if (!TimeOnly.TryParse(
            campos[2],
            CultureInfo.InvariantCulture,
            out TimeOnly hora))
    {
        errores.Add($"la hora '{campos[2]}' no es válida");
    }

    // La cantidad debe ser un entero positivo.
    if (!int.TryParse(
            campos[4],
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out int piezas))
    {
        errores.Add($"la cantidad '{campos[4]}' no es un entero válido");
    }
    else if (piezas <= 0)
    {
        errores.Add("la cantidad debe ser mayor que cero");
    }

    // Si algún dato falla, se registra el renglón y se continúa con el siguiente.
    if (errores.Count > 0)
    {
        rechazosVentas.Add(
            $"Línea {fila.Linea}: {string.Join("; ", errores)}.");
        continue;
    }

    // La fila queda disponible para aplicar promociones, pagos, restricciones e inventario.
    ventasValidas.Add(new LineaVenta(
        campos[0],
        campos[1],
        hora,
        campos[3],
        piezas,
        campos[5]));
}

// Se genera el log junto a los CSV de entrada.
string rutaRechazos = Path.Combine(datos, "rechazos.log");
File.WriteAllLines(rutaRechazos, rechazosVentas);

Console.WriteLine($"Ventas válidas: {ventasValidas.Count}");
Console.WriteLine($"Ventas rechazadas: {rechazosVentas.Count}");
Console.WriteLine($"Archivo de rechazos: {rutaRechazos}");
Console.WriteLine();




// Avance 2 (10:15–11:15)
Pendiente("HU-03 Aplicar la promoción vigente de cada producto");
Pendiente("HU-04 Validar el pago según su medio: efectivo, tarjeta o vales");
Pendiente("HU-05 No vender alcohol fuera de horario ni productos bloqueados");
Pendiente("HU-06 Descontar inventario y avisar si no hay stock suficiente");

// Avance 3 (11:15–12:15)
Pendiente("HU-07 Consultar existencias del CEDIS en paralelo con timeout de 3 s");
Pendiente("HU-08 Pedido sugerido, top de ventas y corte de caja en CSV");

static void Pendiente(string historia) => Console.WriteLine($"[pendiente] {historia}");
