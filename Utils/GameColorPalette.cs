namespace LaVaquita.Utils;

public static class GameColorPalette
{
    private static readonly string[] HexColors = ["#B8F340", "#24D6C8", "#FFD84D", "#6D8DFF", "#FF8A5B"];

    public static Color ForIndex(int index) => Color.FromArgb(HexColors[Math.Abs(index) % HexColors.Length]);

    public static Color ForParticipant(string participantId, IReadOnlyList<Models.Participante> participants)
    {
        var index = participants.ToList().FindIndex(item => item.Id == participantId);
        return ForIndex(index < 0 ? 0 : index);
    }
}
