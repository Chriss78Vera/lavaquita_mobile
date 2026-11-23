namespace LaVaquita.Models;

public class OcrResult
{
    public decimal Total { get; set; }
    public string Currency { get; set; } = "USD";
    public double Confidence { get; set; }
}
