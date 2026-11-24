using System.Text.Json;
using LaVaquita.Models;
using SQLite;

namespace LaVaquita.Services;

public sealed class PartidaLocalStore
{
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private SQLiteAsyncConnection? _database;

    private async Task<SQLiteAsyncConnection> ObtenerBaseAsync()
    {
        if (_database is not null) return _database;
        await _initLock.WaitAsync();
        try
        {
            if (_database is not null) return _database;
            var path = Path.Combine(FileSystem.AppDataDirectory, "lavaquita_local.db3");
            _database = new SQLiteAsyncConnection(path,
                SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
            await _database.CreateTableAsync<PartidaLocalRecord>();
            await _database.EnableWriteAheadLoggingAsync();
            return _database;
        }
        finally { _initLock.Release(); }
    }

    public async Task GuardarAsync(AppSession session)
    {
        if (session.EsCompartida || session.Participantes.Count < 2 || session.Cuenta.Total <= 0) return;
        var database = await ObtenerBaseAsync();
        await database.InsertOrReplaceAsync(new PartidaLocalRecord
        {
            Id = 1,
            Total = session.Cuenta.Total,
            Moneda = session.Cuenta.Moneda,
            ParticipantesJson = JsonSerializer.Serialize(session.Participantes),
            ResultadoJson = session.UltimoResultado is null ? null : JsonSerializer.Serialize(session.UltimoResultado),
            ActualizadaUtc = DateTime.UtcNow
        });
    }

    public async Task<bool> RestaurarAsync(AppSession session)
    {
        var database = await ObtenerBaseAsync();
        var record = await database.Table<PartidaLocalRecord>().Where(item => item.Id == 1).FirstOrDefaultAsync();
        if (record is null) return false;

        var participantes = JsonSerializer.Deserialize<List<Participante>>(record.ParticipantesJson) ?? [];
        if (participantes.Count < 2 || record.Total <= 0) return false;
        session.Reiniciar();
        session.Cuenta = new Cuenta { Total = record.Total, Moneda = record.Moneda };
        session.Participantes = participantes;
        session.UltimoResultado = string.IsNullOrWhiteSpace(record.ResultadoJson)
            ? null
            : JsonSerializer.Deserialize<ResultadoJuego>(record.ResultadoJson);
        return true;
    }

    public async Task<bool> ExisteAsync()
    {
        var database = await ObtenerBaseAsync();
        return await database.Table<PartidaLocalRecord>().CountAsync() > 0;
    }

    public async Task LimpiarAsync()
    {
        var database = await ObtenerBaseAsync();
        await database.DeleteAllAsync<PartidaLocalRecord>();
    }
}
