namespace LaVaquita.Models;

public enum TipoResultado
{
    Division,
    Ruleta,
    Dados,
    Empate
}

public class ResultadoJuego
{
    public TipoResultado Tipo { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Subtitulo { get; set; } = string.Empty;
    public List<DetallePago> Detalles { get; set; } = [];
}

public class DetallePago
{
    public string ParticipanteId { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal? Monto { get; set; }
    public List<int> Dados { get; set; } = [];
    public string Descripcion { get; set; } = string.Empty;
    public string MontoTexto => Monto.HasValue ? $"{Monto.Value:N2} COP" : Descripcion;
}
