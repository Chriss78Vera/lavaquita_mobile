namespace LaVaquita.Models;

// Estado temporal compartido entre las vistas. No contiene datos permanentes ni login.
public class AppSession
{
    public bool EsCompartida { get; set; }
    public bool EsAnfitrion { get; set; }
    public string CodigoSala { get; set; } = string.Empty;
    public string HostToken { get; set; } = string.Empty;
    public string NombreActual { get; set; } = string.Empty;
    public Cuenta Cuenta { get; set; } = new();
    public List<Participante> Participantes { get; set; } = [];
    public ResultadoJuego? UltimoResultado { get; set; }

    public void Reiniciar()
    {
        EsCompartida = false;
        EsAnfitrion = false;
        CodigoSala = HostToken = NombreActual = string.Empty;
        Cuenta = new();
        Participantes = [];
        UltimoResultado = null;
    }
}
