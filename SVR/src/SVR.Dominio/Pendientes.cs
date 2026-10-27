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

/// <summary>
/// Regla de descuento asociada a un producto del catálogo.
/// </summary>
public abstract class Promocion
{
    /// <summary>
    /// Crea una promoción asociada a un producto.
    /// </summary>
    /// <param name="ean">EAN del producto promocionado.</param>
    /// <param name="nombre">Nombre descriptivo de la promoción.</param>
    protected Promocion(string ean, string nombre)
    {
        if (string.IsNullOrWhiteSpace(ean))
        {
            throw new ArgumentException(
                "El EAN de la promoción no puede estar vacío.",
                nameof(ean));
        }

        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException(
                "El nombre de la promoción no puede estar vacío.",
                nameof(nombre));
        }

        Ean = ean.Trim();
        Nombre = nombre.Trim();
    }

    /// <summary>
    /// Obtiene el EAN del producto al que aplica la promoción.
    /// </summary>
    public string Ean { get; }

    /// <summary>
    /// Obtiene el nombre descriptivo de la promoción.
    /// </summary>
    public string Nombre { get; }

    /// <summary>
    /// Indica si la promoción aplica a la cantidad indicada.
    /// </summary>
    /// <param name="piezas">Cantidad de piezas compradas.</param>
    public abstract bool Aplica(int piezas);

    /// <summary>
    /// Calcula el descuento total para el precio y cantidad indicados.
    /// </summary>
    /// <param name="precioUnitario">Precio normal por pieza.</param>
    /// <param name="piezas">Cantidad comprada.</param>
    public abstract decimal Descuento(decimal precioUnitario, int piezas);
}

/// <summary>
/// Promoción en la que, por cada grupo de N piezas, se cobran M.
/// </summary>
public sealed class LlevaNPagaM : Promocion
{
    /// <summary>
    /// Crea una promoción de tipo lleva N y paga M.
    /// </summary>
    public LlevaNPagaM(string ean, int lleva, int paga)
        : base(ean, $"Lleva {lleva}, paga {paga}")
    {
        if (lleva <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lleva),
                "La cantidad que se lleva debe ser mayor que cero.");
        }

        if (paga <= 0 || paga > lleva)
        {
            throw new ArgumentOutOfRangeException(
                nameof(paga),
                "La cantidad que se paga debe ser positiva y no superar la cantidad que se lleva.");
        }

        Lleva = lleva;
        Paga = paga;
    }

    /// <summary>
    /// Obtiene las piezas requeridas para formar un grupo promocional.
    /// </summary>
    public int Lleva { get; }

    /// <summary>
    /// Obtiene las piezas que se cobran por grupo.
    /// </summary>
    public int Paga { get; }

    /// <inheritdoc />
    public override bool Aplica(int piezas) => piezas >= Lleva;

    /// <inheritdoc />
    public override decimal Descuento(decimal precioUnitario, int piezas)
    {
        ValidarCompra(precioUnitario, piezas);

        // Solo los grupos completos reciben piezas gratis.
        int gruposCompletos = piezas / Lleva;
        int piezasGratis = gruposCompletos * (Lleva - Paga);

        return Math.Round(
            precioUnitario * piezasGratis,
            2,
            MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Valida los valores usados en el cálculo del descuento.
    /// </summary>
    private static void ValidarCompra(decimal precioUnitario, int piezas)
    {
        if (precioUnitario < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(precioUnitario),
                "El precio no puede ser negativo.");
        }

        if (piezas < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(piezas),
                "La cantidad no puede ser negativa.");
        }
    }
}

/// <summary>
/// Promoción que aplica un porcentaje de descuento sobre la compra.
/// </summary>
public sealed class PorcentajeDesc : Promocion
{
    /// <summary>
    /// Crea una promoción porcentual.
    /// </summary>
    public PorcentajeDesc(string ean, decimal porcentaje)
        : base(ean, $"{porcentaje}% de descuento")
    {
        if (porcentaje <= 0 || porcentaje > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(porcentaje),
                "El porcentaje debe ser mayor que cero y no superar 100.");
        }

        Porcentaje = porcentaje;
    }

    /// <summary>
    /// Obtiene el porcentaje de descuento.
    /// </summary>
    public decimal Porcentaje { get; }

    /// <inheritdoc />
    public override bool Aplica(int piezas) => piezas > 0;

    /// <inheritdoc />
    public override decimal Descuento(decimal precioUnitario, int piezas)
    {
        if (precioUnitario < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(precioUnitario),
                "El precio no puede ser negativo.");
        }

        if (piezas < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(piezas),
                "La cantidad no puede ser negativa.");
        }

        return Math.Round(
            precioUnitario * piezas * Porcentaje / 100m,
            2,
            MidpointRounding.AwayFromZero);
    }
}

/// <summary>
/// Promoción que aplica un precio unitario especial desde cierta cantidad.
/// </summary>
public sealed class PrecioPorVolumen : Promocion
{
    /// <summary>
    /// Crea una promoción de precio por volumen.
    /// </summary>
    public PrecioPorVolumen(string ean, int desdePiezas, decimal precioEspecial)
        : base(ean, $"Desde {desdePiezas} piezas a {precioEspecial:F2}")
    {
        if (desdePiezas <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(desdePiezas),
                "La cantidad mínima debe ser mayor que cero.");
        }

        if (precioEspecial <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(precioEspecial),
                "El precio especial debe ser mayor que cero.");
        }

        DesdePiezas = desdePiezas;
        PrecioEspecial = precioEspecial;
    }

    /// <summary>
    /// Obtiene la cantidad mínima para activar la promoción.
    /// </summary>
    public int DesdePiezas { get; }

    /// <summary>
    /// Obtiene el precio especial por pieza.
    /// </summary>
    public decimal PrecioEspecial { get; }

    /// <inheritdoc />
    public override bool Aplica(int piezas) => piezas >= DesdePiezas;

    /// <inheritdoc />
    public override decimal Descuento(decimal precioUnitario, int piezas)
    {
        if (precioUnitario < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(precioUnitario),
                "El precio no puede ser negativo.");
        }

        if (piezas < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(piezas),
                "La cantidad no puede ser negativa.");
        }

        if (!Aplica(piezas))
        {
            return 0m;
        }

        // El precio promocional no debe incrementar el precio normal.
        decimal ahorroPorPieza = Math.Max(0m, precioUnitario - PrecioEspecial);

        return Math.Round(
            ahorroPorPieza * piezas,
            2,
            MidpointRounding.AwayFromZero);
    }
}

// HU-04 · TODO: Pago (abstract record) -> Efectivo, Tarjeta, Vales
//   Valida cada pago con switch expression y property patterns. Módulo 7.
