namespace LaVaquita.Models;

public class Cuenta
{
    public decimal Total { get; set; }
    public string Moneda { get; set; } = "USD";
    public bool FueDetectadaPorOcr { get; set; }
    public double ConfianzaOcr { get; set; }
}
