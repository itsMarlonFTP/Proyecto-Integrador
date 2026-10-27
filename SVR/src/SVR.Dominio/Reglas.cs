namespace SVR.Dominio;

/// <summary>
/// YA RESUELTO: reglas del negocio en un solo lugar, para no usar números mágicos.
/// </summary>
public static class Reglas
{
    /// <summary>El precio de anaquel ya incluye IVA: el ticket lo desglosa (subtotal = total / 1.16).</summary>
    public const decimal TasaIva = 0.16m;

    /// <summary>Horario permitido para vender alcohol (HU-05).</summary>
    public static readonly TimeOnly InicioVentaAlcohol = new(7, 0);
    public static readonly TimeOnly FinVentaAlcohol = new(23, 0);

    /// <summary>Máximo que se acepta en vales por ticket (HU-04).</summary>
    public const decimal MaximoEnVales = 1_000m;
}
