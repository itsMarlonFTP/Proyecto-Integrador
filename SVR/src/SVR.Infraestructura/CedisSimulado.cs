using SVR.Aplicacion;

namespace SVR.Infraestructura;

/// <summary>
/// YA RESUELTO: simula el servicio del CEDIS (centro de distribución).
/// Cada consulta tarda como una llamada real por red y respeta el CancellationToken.
/// Ojo: el yogurt tarda 4 segundos; con un timeout de 3 s, esa consulta debe cancelarse (HU-07).
/// </summary>
public sealed class CedisSimulado : ICedisServicio
{
    private static readonly Dictionary<string, (int Existencia, int DemoraMs)> Datos = new()
    {
        ["7501055300075"] = (1240, 400),   // Refresco Cola 600ml
        ["7501000111206"] = (380, 700),    // Galletas Marias 170g
        ["7501234500011"] = (960, 900),    // Cerveza Clara 355ml
        ["7501030411111"] = (55, 4000),    // Yogurt Fresa 1L: responde lento
    };

    public async Task<int> ExistenciaAsync(string ean, CancellationToken ct)
    {
        var (existencia, demoraMs) = Datos.TryGetValue(ean, out var dato) ? dato : (0, 300);
        await Task.Delay(demoraMs, ct);
        return existencia;
    }
}
