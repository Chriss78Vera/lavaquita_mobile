namespace LaVaquita.Models;

public class Cuenta
{
    public decimal Total { get; set; }
    public string Moneda { get; set; } = "COP";
    public bool FueDetectadaPorOcr { get; set; }
    public double ConfianzaOcr { get; set; }
}
