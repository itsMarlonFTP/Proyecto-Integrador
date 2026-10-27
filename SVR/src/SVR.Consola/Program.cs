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

// HU-03: convierte promociones.csv en reglas del dominio asociadas por EAN.
var promocionesPorEan = new Dictionary<string, List<Promocion>>(
    StringComparer.Ordinal);

var rechazosPromociones = new List<string>();

foreach (FilaCsv fila in filasPromociones)
{
    string[] campos = fila.Campos;

    if (campos.Length != 3)
    {
        rechazosPromociones.Add(
            $"Línea {fila.Linea}: se esperaban 3 columnas y se encontraron {campos.Length}.");
        continue;
    }

    string tipo = campos[0].ToUpperInvariant();
    string ean = campos[1];
    string[] parametros = campos[2].Split(';', StringSplitOptions.TrimEntries);

    if (string.IsNullOrWhiteSpace(ean))
    {
        rechazosPromociones.Add($"Línea {fila.Linea}: el EAN está vacío.");
        continue;
    }

    // Una promoción solo se puede asociar a un producto ya cargado.
    if (!eansCatalogo.Contains(ean))
    {
        rechazosPromociones.Add(
            $"Línea {fila.Linea}: el EAN '{ean}' no existe en el catálogo.");
        continue;
    }

    try
    {
        Promocion promocion;

        // Se convierten los parámetros según el tipo especificado en el CSV.
        switch (tipo)
        {
            case "NXM":
                if (parametros.Length != 2 ||
                    !int.TryParse(
                        parametros[0],
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out int lleva) ||
                    !int.TryParse(
                        parametros[1],
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out int paga))
                {
                    throw new FormatException(
                        "NXM requiere dos enteros separados por punto y coma.");
                }

                promocion = new LlevaNPagaM(ean, lleva, paga);
                break;

            case "PORCENTAJE":
                if (parametros.Length != 1 ||
                    !decimal.TryParse(
                        parametros[0],
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        out decimal porcentaje))
                {
                    throw new FormatException(
                        "PORCENTAJE requiere un valor numérico.");
                }

                promocion = new PorcentajeDesc(ean, porcentaje);
                break;

            case "VOLUMEN":
                if (parametros.Length != 2 ||
                    !int.TryParse(
                        parametros[0],
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out int desdePiezas) ||
                    !decimal.TryParse(
                        parametros[1],
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        out decimal precioEspecial))
                {
                    throw new FormatException(
                        "VOLUMEN requiere una cantidad entera y un precio, separados por punto y coma.");
                }

                promocion = new PrecioPorVolumen(
                    ean,
                    desdePiezas,
                    precioEspecial);
                break;

            default:
                throw new FormatException(
                    $"El tipo de promoción '{campos[0]}' no es reconocido.");
        }

        // Se permiten varias reglas por EAN; luego se elegirá la de mayor descuento.
        if (!promocionesPorEan.TryGetValue(ean, out List<Promocion>? lista))
        {
            lista = [];
            promocionesPorEan.Add(ean, lista);
        }

        lista.Add(promocion);
    }
    catch (ArgumentException excepcion)
    {
        rechazosPromociones.Add(
            $"Línea {fila.Linea}: {excepcion.Message}");
    }
    catch (FormatException excepcion)
    {
        rechazosPromociones.Add(
            $"Línea {fila.Linea}: {excepcion.Message}");
    }
}

// Calcula el precio bruto, el descuento y el total por cada línea de venta válida.
var ventasConPromocion =
    new List<(LineaVenta Venta, decimal Bruto, decimal Descuento,
              decimal Total, string? PromocionAplicada)>();

var productosPorEan = productos.ToDictionary(
    producto => producto.Ean,
    StringComparer.Ordinal);

foreach (LineaVenta venta in ventasValidas)
{
    Producto producto = productosPorEan[venta.Ean];
    decimal bruto = producto.Precio * venta.Piezas;

    Promocion? mejorPromocion = null;
    decimal descuento = 0m;

    if (promocionesPorEan.TryGetValue(
            venta.Ean,
            out List<Promocion>? promocionesProducto))
    {
        // No se acumulan promociones: se aplica la que ofrece el mayor ahorro.
        mejorPromocion = promocionesProducto
            .Where(promocion => promocion.Aplica(venta.Piezas))
            .MaxBy(promocion =>
                promocion.Descuento(producto.Precio, venta.Piezas));

        if (mejorPromocion is not null)
        {
            descuento = mejorPromocion.Descuento(
                producto.Precio,
                venta.Piezas);
        }
    }

    decimal total = bruto - descuento;

    ventasConPromocion.Add((
        venta,
        bruto,
        descuento,
        total,
        mejorPromocion?.Nombre));

    Console.WriteLine(
        $"{venta.Folio} | {producto.Descripcion} | " +
        $"{venta.Piezas} x {producto.Precio:C2} | " +
        $"Descuento: {descuento:C2} | Total: {total:C2}" +
        (mejorPromocion is null
            ? " | Sin promoción"
            : $" | {mejorPromocion.Nombre}"));
}

Console.WriteLine($"Promociones cargadas: {promocionesPorEan.Values.Sum(lista => lista.Count)}");
Console.WriteLine($"Promociones rechazadas: {rechazosPromociones.Count}");

foreach (string rechazo in rechazosPromociones)
{
    Console.WriteLine(rechazo);
}

Console.WriteLine();


Pendiente("HU-04 Validar el pago según su medio: efectivo, tarjeta o vales");

// HU-04: valida el medio de pago contra el importe total de cada ticket.
var pagosValidos = new List<(string Folio, Pago Pago)>();
var foliosConPagoValido = new HashSet<string>(StringComparer.Ordinal);
var rechazosPagos = new List<string>();

// El CSV puede tener varias líneas por ticket, así que el pago se valida
// sobre el total promocional del folio, no individualmente por cada producto.
foreach (var grupo in ventasConPromocion.GroupBy(
             registro => registro.Venta.Folio,
             StringComparer.Ordinal))
{
    string folio = grupo.Key;
    string[] medios = grupo
        .Select(registro => registro.Venta.MedioPago.Trim().ToUpperInvariant())
        .Distinct(StringComparer.Ordinal)
        .ToArray();

    // Todas las líneas de un mismo ticket deben indicar el mismo medio de pago.
    if (medios.Length != 1)
    {
        rechazosPagos.Add(
            $"Ticket {folio}: las líneas tienen medios de pago diferentes.");
        continue;
    }

    decimal subtotalTicket = grupo.Sum(registro => registro.Total);
    decimal ivaTicket = Math.Round(
    subtotalTicket * 0.16m,
    2,
    MidpointRounding.AwayFromZero);
    decimal montoTotal = subtotalTicket + ivaTicket;

    // Se crea el tipo de pago del dominio a partir del medio declarado en el CSV.
    Pago? pago = medios[0] switch
    {
        "EFECTIVO" => new Efectivo(montoTotal),
        "TARJETA" => new Tarjeta(montoTotal),
        "VALES" => new Vales(montoTotal),
        _ => null
    };

    if (pago is null)
    {
        rechazosPagos.Add(
            $"Ticket {folio}: medio de pago '{medios[0]}' no soportado.");
        continue;
    }

    ResultadoValidacionPago resultado = pago.Validar();

    if (!resultado.Aceptado)
    {
        rechazosPagos.Add($"Ticket {folio}: {resultado.Mensaje}");
        continue;
    }

    pagosValidos.Add((folio, pago));
    foliosConPagoValido.Add(folio);

    Console.WriteLine(
        $"Pago {folio}: {medios[0]} por {pago.Monto:C2} — {resultado.Mensaje}");
}

// Se agregan los rechazos de pago al log creado por HU-02, sin borrar sus registros.
if (rechazosPagos.Count > 0)
{
    File.AppendAllLines(rutaRechazos, rechazosPagos);
}

Console.WriteLine($"Tickets con pago válido: {pagosValidos.Count}");
Console.WriteLine($"Tickets con pago rechazado: {rechazosPagos.Count}");

foreach (string rechazo in rechazosPagos)
{
    Console.WriteLine(rechazo);
}

Console.WriteLine();

Pendiente("HU-05 No vender alcohol fuera de horario ni productos bloqueados");

// HU-05: rechaza el ticket completo si contiene alcohol vendido después de las 23:00.
// HU-05: bloquea tickets que incluyan alcohol vendido después de las 23:00.
TimeOnly horaLimiteAlcohol = new(23, 0);
var foliosRestringidos = new HashSet<string>(StringComparer.Ordinal);
var rechazosRestricciones = new List<string>();

foreach (var registro in ventasConPromocion)
{
    LineaVenta venta = registro.Venta;
    Producto producto = productosPorEan[venta.Ean];

    // La categoría proviene del catálogo; se compara ignorando mayúsculas y espacios.
    bool esAlcohol = string.Equals(
        producto.Categoria.Trim(),
        "Alcohol",
        StringComparison.OrdinalIgnoreCase);

    if (esAlcohol && venta.Hora > horaLimiteAlcohol)
    {
        // Se excluye el ticket completo, aunque tenga otras líneas no alcohólicas.
        foliosRestringidos.Add(venta.Folio);

        string motivo =
            $"Ticket {venta.Folio}, EAN {venta.Ean}: " +
            $"venta de alcohol restringida a las {venta.Hora:HH:mm}.";

        rechazosRestricciones.Add(motivo);
    }
}

// Solo pasan tickets con pago válido y sin restricción comercial.
var ventasPermitidas = ventasConPromocion
    .Where(registro =>
        foliosConPagoValido.Contains(registro.Venta.Folio) &&
        !foliosRestringidos.Contains(registro.Venta.Folio))
    .ToList();

// Se agregan los rechazos al log existente de HU-02.
if (rechazosRestricciones.Count > 0)
{
    File.AppendAllLines(rutaRechazos, rechazosRestricciones);
}

Console.WriteLine($"Tickets restringidos: {foliosRestringidos.Count}");
Console.WriteLine($"Líneas permitidas para HU-06: {ventasPermitidas.Count}");

foreach (string rechazo in rechazosRestricciones)
{
    Console.WriteLine(rechazo);
}

Console.WriteLine();


Pendiente("HU-06 Descontar inventario y avisar si no hay stock suficiente");


// HU-06: valida existencias por ticket y descuenta solo si todas sus líneas caben en stock.
var ventasConStockDescontado =
    new List<(LineaVenta Venta, decimal Bruto, decimal Descuento,
              decimal Total, string? PromocionAplicada)>();

var rechazosStock = new List<string>();

foreach (var grupoTicket in ventasPermitidas.GroupBy(
             registro => registro.Venta.Folio,
             StringComparer.Ordinal))
{
    // Se suman líneas del mismo producto dentro del ticket para validar el total requerido.
    var piezasPorEan = grupoTicket
        .GroupBy(
            registro => registro.Venta.Ean,
            StringComparer.Ordinal)
        .Select(grupoEan => new
        {
            Ean = grupoEan.Key,
            Piezas = grupoEan.Sum(registro => registro.Venta.Piezas)
        })
        .ToList();

    var erroresStock = new List<string>();

    // Primero se valida todo el ticket; así no se descuenta parcialmente si una línea falla.
    foreach (var solicitud in piezasPorEan)
    {
        Producto producto = productosPorEan[solicitud.Ean];

        try
        {
            producto.ValidarDisponibilidad(solicitud.Piezas);
        }
        catch (StockInsuficienteException excepcion)
        {
            erroresStock.Add(excepcion.Message);
        }
    }

    if (erroresStock.Count > 0)
    {
        string motivo =
            $"Ticket {grupoTicket.Key}: {string.Join("; ", erroresStock)}";

        rechazosStock.Add(motivo);
        Console.WriteLine($"RECHAZADO — {motivo}");
        continue;
    }

    // Solo se actualiza el inventario cuando todas las líneas del ticket son viables.
    foreach (var solicitud in piezasPorEan)
    {
        productosPorEan[solicitud.Ean].DescontarStock(solicitud.Piezas);
    }

    ventasConStockDescontado.AddRange(grupoTicket);

    Console.WriteLine(
        $"Ticket {grupoTicket.Key}: inventario descontado correctamente.");
}

// Se agregan los rechazos de stock al mismo log de las historias anteriores.
if (rechazosStock.Count > 0)
{
    File.AppendAllLines(rutaRechazos, rechazosStock);
}

Console.WriteLine($"Tickets con inventario descontado: {ventasConStockDescontado.Select(x => x.Venta.Folio).Distinct(StringComparer.Ordinal).Count()}");
Console.WriteLine($"Tickets rechazados por stock: {rechazosStock.Count}");

Console.WriteLine("Existencias finales:");
foreach (Producto producto in productos)
{
    Console.WriteLine($"{producto.Ean} | {producto.Descripcion} | Stock: {producto.Stock}");
}

Console.WriteLine();


// Avance 3 (11:15–12:15)
Pendiente("HU-07 Consultar existencias del CEDIS en paralelo con timeout de 3 s");

// HU-07: consulta las existencias del CEDIS en paralelo con un límite de 3 segundos.
using var ctsCedis = new CancellationTokenSource(TimeSpan.FromSeconds(3));
var cronometroCedis = System.Diagnostics.Stopwatch.StartNew();

// Se consulta una vez por cada EAN distinto; las tareas comienzan antes de esperar.
string[] eansCedis = productos
    .Select(producto => producto.Ean)
    .Distinct(StringComparer.Ordinal)
    .ToArray();

Task<(string Ean, int? Existencia, string? Error)>[] tareasCedis = eansCedis
    .Select(ean => ConsultarCedisSeguroAsync(ean, cedis, ctsCedis.Token))
    .ToArray();

// Todas las consultas se esperan juntas; el token cancela las que excedan 3 segundos.
var resultadosCedis = await Task.WhenAll(tareasCedis);

cronometroCedis.Stop();

// Este diccionario queda disponible para HU-08.
var existenciasCedis = resultadosCedis
    .Where(resultado => resultado.Existencia.HasValue)
    .ToDictionary(
        resultado => resultado.Ean,
        resultado => resultado.Existencia!.Value,
        StringComparer.Ordinal);

foreach (var resultado in resultadosCedis)
{
    if (resultado.Existencia.HasValue)
    {
        Console.WriteLine(
            $"CEDIS {resultado.Ean}: {resultado.Existencia.Value} piezas.");
    }
    else
    {
        Console.WriteLine(
            $"CEDIS {resultado.Ean}: consulta cancelada o fallida — {resultado.Error}");
    }
}

Console.WriteLine(
    $"Consultas CEDIS: {existenciasCedis.Count} exitosas de {eansCedis.Length} " +
    $"en {cronometroCedis.ElapsedMilliseconds} ms (timeout: 3000 ms).");
Console.WriteLine();

/// <summary>
/// Consulta un EAN al CEDIS y devuelve el error como dato para no interrumpir el cierre.
/// </summary>
/// <param name="ean">EAN que se consulta.</param>
/// <param name="servicio">Servicio simulado del CEDIS.</param>
/// <param name="ct">Token compartido para aplicar el timeout de HU-07.</param>
/// <returns>EAN, existencias o el motivo por el que no se obtuvieron.</returns>
static async Task<(string Ean, int? Existencia, string? Error)>
    ConsultarCedisSeguroAsync(
        string ean,
        CedisSimulado servicio,
        CancellationToken ct)
{
    try
    {
        int existencia = await servicio.ExistenciaAsync(ean, ct);
        return (ean, existencia, null);
    }
    catch (OperationCanceledException) when (ct.IsCancellationRequested)
    {
        // El servicio recibió el token; esta respuesta indica que se alcanzó el límite.
        return (ean, null, "timeout de 3 segundos");
    }
    catch (Exception excepcion)
    {
        // Un fallo individual se informa sin impedir que el cierre continúe.
        return (ean, null, excepcion.Message);
    }
}


Pendiente("HU-08 Pedido sugerido, top de ventas y corte de caja en CSV");

// HU-08: pedido sugerido, top de ventas y corte de caja.
const decimal tasaIva = 0.16m;

// Agrupa las líneas por folio para calcular cada ticket una sola vez.
var corteTickets = ventasConStockDescontado
    .GroupBy(registro => registro.Venta.Folio, StringComparer.Ordinal)
    .Select(grupo =>
    {
        decimal bruto = grupo.Sum(registro => registro.Bruto);
        decimal descuento = grupo.Sum(registro => registro.Descuento);
        decimal subtotal = bruto - descuento;

        // El IVA se redondea por ticket para mantener importes monetarios a centavos.
        decimal iva = Math.Round(
            subtotal * tasaIva,
            2,
            MidpointRounding.AwayFromZero);

        return new
        {
            Folio = grupo.Key,
            MedioPago = grupo.First().Venta.MedioPago.Trim().ToUpperInvariant(),
            Bruto = bruto,
            Descuento = descuento,
            Subtotal = subtotal,
            Iva = iva,
            Total = subtotal + iva
        };
    })
    .OrderBy(ticket => ticket.Folio, StringComparer.Ordinal)
    .ToList();

// Calcula el top 3 por piezas vendidas, usando solo ventas que llegaron a descontar stock.
var topVentas = ventasConStockDescontado
    .GroupBy(registro => registro.Venta.Ean, StringComparer.Ordinal)
    .Select(grupo =>
    {
        Producto producto = productosPorEan[grupo.Key];

        return new
        {
            Ean = grupo.Key,
            Descripcion = producto.Descripcion,
            Piezas = grupo.Sum(registro => registro.Venta.Piezas),
            ImporteNeto = grupo.Sum(registro => registro.Total)
        };
    })
    .OrderByDescending(producto => producto.Piezas)
    .ThenBy(producto => producto.Descripcion, StringComparer.Ordinal)
    .Take(3)
    .ToList();

// Sugiere reabastecimiento hasta el punto de reorden, limitado por la existencia del CEDIS.
var pedidoSugerido = productos
    .Select(producto =>
    {
        int faltante = Math.Max(0, producto.PuntoReorden - producto.Stock);

        bool cedisConsultado = existenciasCedis.TryGetValue(
            producto.Ean,
            out int existenciaCedis);

        int piezasSugeridas = cedisConsultado
            ? Math.Min(faltante, existenciaCedis)
            : 0;

        string estado = faltante == 0
            ? "SIN_REORDEN"
            : !cedisConsultado
                ? "CEDIS_NO_DISPONIBLE"
                : existenciaCedis == 0
                    ? "CEDIS_SIN_EXISTENCIA"
                    : "SUGERIDO";

        return new
        {
            Producto = producto,
            Faltante = faltante,
            ExistenciaCedis = cedisConsultado
                ? existenciaCedis.ToString(CultureInfo.InvariantCulture)
                : "NO_DISPONIBLE",
            PiezasSugeridas = piezasSugeridas,
            Estado = estado
        };
    })
    .Where(item => item.Faltante > 0)
    .OrderBy(item => item.Producto.Ean, StringComparer.Ordinal)
    .ToList();

// Define los archivos de salida junto a los datos usados por la aplicación.
string rutaPedido = Path.Combine(datos, "pedido_0457.csv");
string rutaTopVentas = Path.Combine(datos, "top_ventas_0457.csv");
string rutaCorteCaja = Path.Combine(datos, "corte_caja_0457.csv");

// Genera el pedido sugerido, incluso si solo hay encabezado cuando no hay productos bajo reorden.
var filasPedido = new List<string[]>
{
    new[]
    {
        "ean",
        "descripcion",
        "stock_tienda",
        "punto_reorden",
        "existencia_cedis",
        "piezas_sugeridas",
        "estado"
    }
};

filasPedido.AddRange(pedidoSugerido.Select(item => new[]
{
    item.Producto.Ean,
    item.Producto.Descripcion,
    item.Producto.Stock.ToString(CultureInfo.InvariantCulture),
    item.Producto.PuntoReorden.ToString(CultureInfo.InvariantCulture),
    item.ExistenciaCedis,
    item.PiezasSugeridas.ToString(CultureInfo.InvariantCulture),
    item.Estado
}));

CorteCajaExportador.EscribirCsv(rutaPedido, filasPedido);

// Genera el top 3; el archivo tendrá al menos su encabezado.
var filasTopVentas = new List<string[]>
{
    new[] { "ean", "descripcion", "piezas_vendidas", "importe_neto" }
};

filasTopVentas.AddRange(topVentas.Select(item => new[]
{
    item.Ean,
    item.Descripcion,
    item.Piezas.ToString(CultureInfo.InvariantCulture),
    item.ImporteNeto.ToString("F2", CultureInfo.InvariantCulture)
}));

CorteCajaExportador.EscribirCsv(rutaTopVentas, filasTopVentas);

// Construye el corte por ticket.
var filasCorte = new List<string[]>
{
    new[]
    {
        "tipo",
        "folio",
        "medio_pago",
        "bruto",
        "descuento",
        "subtotal",
        "iva",
        "total"
    }
};

foreach (var ticket in corteTickets)
{
    filasCorte.Add(new[]
    {
        "TICKET",
        ticket.Folio,
        ticket.MedioPago,
        ticket.Bruto.ToString("F2", CultureInfo.InvariantCulture),
        ticket.Descuento.ToString("F2", CultureInfo.InvariantCulture),
        ticket.Subtotal.ToString("F2", CultureInfo.InvariantCulture),
        ticket.Iva.ToString("F2", CultureInfo.InvariantCulture),
        ticket.Total.ToString("F2", CultureInfo.InvariantCulture)
    });
}

// Resume los importes por medio de pago, una vez por ticket.
foreach (var grupoPago in corteTickets.GroupBy(
             ticket => ticket.MedioPago,
             StringComparer.Ordinal))
{
    filasCorte.Add(new[]
    {
        "PAGO",
        "",
        grupoPago.Key,
        "",
        "",
        "",
        "",
        grupoPago.Sum(ticket => ticket.Total)
            .ToString("F2", CultureInfo.InvariantCulture)
    });
}

// El cierre general permite comprobar bruto, descuentos, subtotal, IVA y total.
decimal brutoGeneral = corteTickets.Sum(ticket => ticket.Bruto);
decimal descuentoGeneral = corteTickets.Sum(ticket => ticket.Descuento);
decimal subtotalGeneral = corteTickets.Sum(ticket => ticket.Subtotal);
decimal ivaGeneral = corteTickets.Sum(ticket => ticket.Iva);
decimal totalGeneral = corteTickets.Sum(ticket => ticket.Total);

filasCorte.Add(new[]
{
    "CIERRE",
    "",
    "",
    brutoGeneral.ToString("F2", CultureInfo.InvariantCulture),
    descuentoGeneral.ToString("F2", CultureInfo.InvariantCulture),
    subtotalGeneral.ToString("F2", CultureInfo.InvariantCulture),
    ivaGeneral.ToString("F2", CultureInfo.InvariantCulture),
    totalGeneral.ToString("F2", CultureInfo.InvariantCulture)
});

CorteCajaExportador.EscribirCsv(rutaCorteCaja, filasCorte);

// Muestra los detalles en consola además de generar los tres CSV.
Console.WriteLine("=== HU-08 · PEDIDO SUGERIDO ===");

if (pedidoSugerido.Count == 0)
{
    Console.WriteLine("No hay productos por debajo de su punto de reorden.");
}
else
{
    foreach (var item in pedidoSugerido)
    {
        Console.WriteLine(
            $"{item.Producto.Ean} | {item.Producto.Descripcion} | " +
            $"Stock tienda: {item.Producto.Stock} | " +
            $"Punto de reorden: {item.Producto.PuntoReorden} | " +
            $"CEDIS: {item.ExistenciaCedis} | " +
            $"Piezas sugeridas: {item.PiezasSugeridas} | {item.Estado}");
    }
}

Console.WriteLine();
Console.WriteLine("=== HU-08 · TOP 3 DE VENTAS POR PIEZAS ===");

if (topVentas.Count == 0)
{
    Console.WriteLine("No hay ventas aceptadas para calcular el top.");
}
else
{
    for (int indice = 0; indice < topVentas.Count; indice++)
    {
        var item = topVentas[indice];

        Console.WriteLine(
            $"{indice + 1}. {item.Descripcion} | " +
            $"{item.Piezas} piezas | Importe neto: {item.ImporteNeto:C2}");
    }
}

Console.WriteLine();
Console.WriteLine("=== HU-08 · CORTE DE CAJA POR TICKET ===");

if (corteTickets.Count == 0)
{
    Console.WriteLine("No hay tickets aceptados para el corte.");
}
else
{
    foreach (var ticket in corteTickets)
    {
        Console.WriteLine(
            $"{ticket.Folio} | {ticket.MedioPago} | " +
            $"Subtotal: {ticket.Subtotal:C2} | " +
            $"IVA: {ticket.Iva:C2} | Total: {ticket.Total:C2}");
    }
}

Console.WriteLine();
Console.WriteLine("=== HU-08 · TOTALES DEL CIERRE ===");
Console.WriteLine($"Bruto: {brutoGeneral:C2}");
Console.WriteLine($"Descuentos: {descuentoGeneral:C2}");
Console.WriteLine($"Subtotal: {subtotalGeneral:C2}");
Console.WriteLine($"IVA: {ivaGeneral:C2}");
Console.WriteLine($"Total: {totalGeneral:C2}");

Console.WriteLine();
Console.WriteLine("CSV generados:");
Console.WriteLine($"- Pedido sugerido: {rutaPedido}");
Console.WriteLine($"- Top de ventas: {rutaTopVentas}");
Console.WriteLine($"- Corte de caja: {rutaCorteCaja}");
Console.WriteLine();
static void Pendiente(string historia) => Console.WriteLine($"[pendiente] {historia}");
