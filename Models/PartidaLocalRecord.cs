using SQLite;

namespace LaVaquita.Models;

[Table("partida_local")]
public class PartidaLocalRecord
{
    [PrimaryKey]
    public int Id { get; set; } = 1;
    public decimal Total { get; set; }
    public string Moneda { get; set; } = "USD";
    public string ParticipantesJson { get; set; } = "[]";
    public string? ResultadoJson { get; set; }
    public DateTime ActualizadaUtc { get; set; }
}
