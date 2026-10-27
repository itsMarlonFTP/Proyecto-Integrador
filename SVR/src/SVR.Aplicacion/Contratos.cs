namespace SVR.Aplicacion;

/// <summary>
/// YA RESUELTO (contrato): servicio externo del CEDIS.
/// La implementación simulada está en SVR.Infraestructura.CedisSimulado.
/// </summary>
public interface ICedisServicio
{
    Task<int> ExistenciaAsync(string ean, CancellationToken ct);
}

/// <summary>
/// YA RESUELTO (contrato): inventario de la tienda.
/// Tu implementación va en SVR.Infraestructura (HU-06).
/// </summary>
public interface IInventarioRepo
{
    int Stock(string ean);

    /// <summary>Descuenta piezas; si no alcanzan, lanza tu StockInsuficienteException.</summary>
    void Descontar(string ean, int piezas);
}
