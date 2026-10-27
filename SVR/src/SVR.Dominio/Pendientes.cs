namespace SVR.Dominio;

// HU-01 · Producto
//   Ean, Descripcion, Categoria, Precio (decimal), PuntoReorden, Stock. Módulo 2.
public sealed class Producto
{
    public Producto(
        string ean,
        string descripcion,
        string categoria,
        decimal precio,
        int puntoReorden,
        int stock)
    {
        // El dominio no acepta productos inválidos.
        if (string.IsNullOrWhiteSpace(ean))
        {
            throw new ArgumentException("El EAN no puede estar vacío.", nameof(ean));
        }

        if (string.IsNullOrWhiteSpace(descripcion))
        {
            throw new ArgumentException(
                "La descripción no puede estar vacía.",
                nameof(descripcion));
        }

        if (string.IsNullOrWhiteSpace(categoria))
        {
            throw new ArgumentException(
                "La categoría no puede estar vacía.",
                nameof(categoria));
        }
        if (precio <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(precio),
                "El precio debe ser mayor que cero.");
        }

        if (puntoReorden < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(puntoReorden),
                "El punto de reorden no puede ser negativo.");
        }

        if (stock < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(stock),
                "El stock no puede ser negativo.");
        }


        // Se eliminan espacios exteriores, pero el EAN se conserva como texto.
        Ean = ean.Trim();
        Descripcion = descripcion.Trim();
        Categoria = categoria.Trim();
        Precio = precio;
        PuntoReorden = puntoReorden;
        Stock = stock;
    }

    public string Ean { get; }
    public string Descripcion { get; }
    public string Categoria { get; }
    public decimal Precio { get; }
    public int PuntoReorden { get; }
    public int Stock { get; }
}


// HU-02 · TODO: LineaVenta (record)
//   Folio, Caja, Hora (TimeOnly), Ean, Piezas, MedioPago. Módulo 7.
public sealed record LineaVenta(
     string Folio,
    string Caja,
    TimeOnly Hora,
    string Ean,
    int Piezas,
    string MedioPago
);





// HU-01 a HU-06 · TODO: excepciones de dominio
//   TiendaException (abstract) -> ProductoNoEncontradoException, StockInsuficienteException,
//   VentaRestringidaException. Con propiedades de contexto (Ean, Disponible...). Módulo 5.

// HU-03 · TODO: Promocion (abstract) con Descuento(precio, piezas)
//   -> LlevaNPagaM (NXM 2;1), PorcentajeDesc (PORCENTAJE 15), PrecioPorVolumen (VOLUMEN 6;18.00).
//   Una promoción nueva = una clase nueva; la caja no cambia. Módulo 8.

// HU-04 · TODO: Pago (abstract record) -> Efectivo, Tarjeta, Vales
//   Valida cada pago con switch expression y property patterns. Módulo 7.
