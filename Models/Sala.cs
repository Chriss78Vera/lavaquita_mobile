using System.Text.Json;

namespace LaVaquita.Models;

public class Sala
{
    public string Codigo { get; set; } = string.Empty;
    public string Estado { get; set; } = "waiting";
    public DateTime FechaExpiracion { get; set; }
    public Cuenta Cuenta { get; set; } = new();
    public List<Participante> Participantes { get; set; } = [];
    public JsonElement? UltimoEvento { get; set; }
}
