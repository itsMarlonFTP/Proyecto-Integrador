namespace SVR.Aplicacion;

// HU-02 a HU-06 · TODO: ServicioVenta
//   - Recibe IInventarioRepo (y lo que necesites) por constructor: DIP, Módulo 8.
//   - Por cada línea de venta: busca el producto, valida la regla de venta (alcohol y horario),
//     aplica la promoción vigente, valida el medio de pago y descuenta inventario.
//   - Una línea inválida va a rechazos.log con número de línea y motivo; el proceso no se detiene.

// HU-07 y HU-08 · TODO: ServicioReabasto
//   - Recibe ICedisServicio por constructor.
//   - Consulta la existencia de todos los productos en paralelo (Task.WhenAll) con un timeout
//     de 3 segundos (CancellationTokenSource). Si el CEDIS no responde, sigue con el stock local
//     y lo informa. Módulo 6.
//   - Calcula el pedido sugerido con LINQ: productos en o bajo su punto de reorden. Módulo 4.
