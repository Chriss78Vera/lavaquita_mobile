namespace LaVaquita.Models;

public class AvatarOption
{
    public string Id { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string IconFile { get; set; } = string.Empty;

    public static List<AvatarOption> Todos =>
    [
        new() { Id = "diablo_huma", Nombre = "Diablo Huma", IconFile = "diablo_huma.png" },
        new() { Id = "condor", Nombre = "Cóndor", IconFile = "condor.png" },
        new() { Id = "cacao", Nombre = "Cacao", IconFile = "cacao.png" },
        new() { Id = "mitad_mundo", Nombre = "Mitad", IconFile = "mitad_mundo.png" },
        new() { Id = "volcan", Nombre = "Volcán", IconFile = "volcan.png" },
        new() { Id = "sombrero", Nombre = "Sombrero", IconFile = "sombrero.png" }
    ];
}
