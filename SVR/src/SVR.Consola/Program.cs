using System.Globalization;
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
Pendiente("HU-02 Procesar ventas_0457.csv y escribir rechazos.log (línea y motivo)");

// Avance 2 (10:15–11:15)
Pendiente("HU-03 Aplicar la promoción vigente de cada producto");
Pendiente("HU-04 Validar el pago según su medio: efectivo, tarjeta o vales");
Pendiente("HU-05 No vender alcohol fuera de horario ni productos bloqueados");
Pendiente("HU-06 Descontar inventario y avisar si no hay stock suficiente");

// Avance 3 (11:15–12:15)
Pendiente("HU-07 Consultar existencias del CEDIS en paralelo con timeout de 3 s");
Pendiente("HU-08 Pedido sugerido, top de ventas y corte de caja en CSV");

static void Pendiente(string historia) => Console.WriteLine($"[pendiente] {historia}");
