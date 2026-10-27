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

    public void ValidarDisponibilidad(int piezas)
{
    if (piezas <= 0)
    {
        throw new ArgumentOutOfRangeException(
            nameof(piezas),
            "La cantidad debe ser mayor que cero.");
    }

    if (Stock < piezas)
    {
        throw new StockInsuficienteException(Ean, Stock, piezas);
    }
}

/// <summary>
/// Descuenta existencias después de verificar que sean suficientes.
/// </summary>
/// <param name="piezas">Unidades que se venden.</param>
public void DescontarStock(int piezas)
{
    // Se comprueba antes de modificar para no dejar el inventario negativo.
    ValidarDisponibilidad(piezas);
    Stock -= piezas;
}




    public bool EsAlcoholico =>
        string.Equals(Categoria, "Alcohol", StringComparison.OrdinalIgnoreCase);

    public string Ean { get; }
    public string Descripcion { get; }
    public string Categoria { get; }
    public decimal Precio { get; }
    public int PuntoReorden { get; }
    public int Stock { get; private set;}
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

/// <summary>
/// Excepción base para errores de negocio de la tienda.
/// </summary>
public abstract class TiendaException : Exception
{
    /// <summary>
    /// Crea una excepción de negocio con un mensaje descriptivo.
    /// </summary>
    /// <param name="mensaje">Descripción del error.</param>
    protected TiendaException(string mensaje) : base(mensaje)
    {
    }
}

/// <summary>
/// Indica que el EAN solicitado no existe en el catálogo.
/// </summary>
public sealed class ProductoNoEncontradoException : TiendaException
{
    /// <summary>
    /// Crea la excepción para el EAN no encontrado.
    /// </summary>
    /// <param name="ean">EAN buscado.</param>
    public ProductoNoEncontradoException(string ean)
        : base($"El producto '{ean}' no existe en el catálogo.")
    {
        Ean = ean;
    }

    /// <summary>
    /// Obtiene el EAN que no se encontró.
    /// </summary>
    public string Ean { get; }
}

/// <summary>
/// Indica que la cantidad solicitada supera el stock disponible.
/// </summary>
public sealed class StockInsuficienteException : TiendaException
{
    /// <summary>
    /// Crea la excepción con el detalle de existencias y cantidad solicitada.
    /// </summary>
    public StockInsuficienteException(string ean, int disponible, int solicitado)
        : base($"Stock insuficiente de '{ean}': hay {disponible}, se solicitaron {solicitado}.")
    {
        Ean = ean;
        Disponible = disponible;
        Solicitado = solicitado;
    }

    /// <summary>
    /// Obtiene el EAN del producto afectado.
    /// </summary>
    public string Ean { get; }

    /// <summary>
    /// Obtiene las piezas disponibles.
    /// </summary>
    public int Disponible { get; }

    /// <summary>
    /// Obtiene las piezas solicitadas.
    /// </summary>
    public int Solicitado { get; }
}

/// <summary>
/// Indica que una venta se bloqueó por una restricción comercial.
/// </summary>
public sealed class VentaRestringidaException : TiendaException
{
    /// <summary>
    /// Crea la excepción para la venta restringida del producto y la hora indicados.
    /// </summary>
    public VentaRestringidaException(string ean, TimeOnly hora)
        : base($"No se permite vender el producto '{ean}' a las {hora:HH:mm}.")
    {
        Ean = ean;
        Hora = hora;
    }

    /// <summary>
    /// Obtiene el EAN del producto restringido.
    /// </summary>
    public string Ean { get; }

    /// <summary>
    /// Obtiene la hora de la venta restringida.
    /// </summary>
    public TimeOnly Hora { get; }
}




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
/// <summary>
/// Representa el importe que se intenta cubrir con un medio de pago.
/// </summary>
public abstract record Pago
{
    /// <summary>
    /// Crea un pago para un importe positivo.
    /// </summary>
    /// <param name="monto">Importe del pago.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// El importe no es mayor que cero.
    /// </exception>
    protected Pago(decimal monto)
    {
        if (monto <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(monto),
                "El monto del pago debe ser mayor que cero.");
        }

        Monto = monto;
    }

    /// <summary>
    /// Obtiene el importe asociado al pago.
    /// </summary>
    public decimal Monto { get; }

    /// <summary>
    /// Valida las reglas propias del tipo de pago.
    /// </summary>
    /// <returns>Resultado de la validación y su motivo.</returns>
    public abstract ResultadoValidacionPago Validar();
}

/// <summary>
/// Representa un pago en efectivo.
/// </summary>
public sealed record Efectivo : Pago
{
    /// <summary>
    /// Crea un pago en efectivo por el importe indicado.
    /// </summary>
    public Efectivo(decimal monto) : base(monto)
    {
    }

    /// <inheritdoc />
    public override ResultadoValidacionPago Validar() =>
        new(true, "Medio efectivo reconocido; importe registrado.");
}

/// <summary>
/// Representa un pago con tarjeta.
/// </summary>
public sealed record Tarjeta : Pago
{
    /// <summary>
    /// Crea un pago con tarjeta por el importe indicado.
    /// </summary>
    public Tarjeta(decimal monto) : base(monto)
    {
    }

    /// <inheritdoc />
    public override ResultadoValidacionPago Validar() =>
        new(true, "Medio tarjeta reconocido; autorización bancaria no disponible en el CSV.");
}

/// <summary>
/// Representa un pago con vales.
/// </summary>
public sealed record Vales : Pago
{
    /// <summary>
    /// Importe máximo permitido para pagar con vales.
    /// </summary>
    public const decimal LimiteMonto = 1000m;

    /// <summary>
    /// Crea un pago con vales por el importe indicado.
    /// </summary>
    public Vales(decimal monto) : base(monto)
    {
    }

    /// <inheritdoc />
    public override ResultadoValidacionPago Validar() =>
        Monto <= LimiteMonto
            ? new(true, "Pago con vales dentro del límite permitido.")
            : new(false, $"El monto excede el límite de vales de {LimiteMonto:C2}.");
}

/// <summary>
/// Resultado de validar un pago.
/// </summary>
/// <param name="Aceptado">Indica si el pago cumple las reglas disponibles.</param>
/// <param name="Mensaje">Explicación del resultado.</param>
public sealed record ResultadoValidacionPago(bool Aceptado, string Mensaje);
