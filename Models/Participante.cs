namespace LaVaquita.Models;

public class Participante
{
    public string Id { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Avatar { get; set; } = "sparkles";
    public bool EsAnfitrion { get; set; }
    public string Rol => EsAnfitrion ? "Anfitrión" : "Participante";
}
