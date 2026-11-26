using System.Text.Json;
using SocketIOClient;

namespace LaVaquita.Services;

public sealed class SalaRealtimeService(Uri serverUri)
{
    private readonly Uri _serverUri = serverUri;
    private SocketIO? _socket;
    private string _codigoSala = string.Empty;
    private volatile bool _salaConfirmada;
    private ResultadoRecibido? _ultimoJuego;

    private sealed record ResultadoRecibido(JsonElement Datos);

    public event Action? SalaActualizada;
    public event Action<JsonElement>? JuegoRecibido;
    public event Action<string>? JuegoIniciado;
    public event Action<string>? SalaFinalizada;
    public event Action<bool>? ConexionCambiada;

    public bool EstaConectado => _salaConfirmada && _socket?.Connected == true;
    public JsonElement? UltimoJuego => Volatile.Read(ref _ultimoJuego)?.Datos;

    public async Task ConectarAsync(string codigoSala)
    {
        codigoSala = codigoSala.Trim().ToUpperInvariant();
        if (EstaConectado && _codigoSala == codigoSala) return;

        await DesconectarAsync();
        _codigoSala = codigoSala;
        var socket = _socket = new SocketIO(_serverUri, new SocketIOOptions
        {
            Reconnection = true,
            ReconnectionAttempts = int.MaxValue,
            ReconnectionDelayMax = 5000,
            ConnectionTimeout = TimeSpan.FromSeconds(15)
        });

        socket.OnConnected += async (_, _) =>
        {
            try
            {
                await SuscribirseAsync(socket, codigoSala);
            }
            catch (Exception error)
            {
                System.Diagnostics.Debug.WriteLine($"No se pudo confirmar la sala: {error.Message}");
                if (ReferenceEquals(_socket, socket)) CambiarEstado(false);
            }
        };
        socket.OnDisconnected += (_, _) =>
        {
            if (ReferenceEquals(_socket, socket)) CambiarEstado(false);
        };
        _socket.On("participant_joined", _ =>
        {
            SalaActualizada?.Invoke();
            return Task.CompletedTask;
        });
        _socket.On("participant_left", _ =>
        {
            SalaActualizada?.Invoke();
            return Task.CompletedTask;
        });
        _socket.On("bill_updated", _ =>
        {
            SalaActualizada?.Invoke();
            return Task.CompletedTask;
        });
        _socket.On("game_event", response =>
        {
            if (ReferenceEquals(_socket, socket))
            {
                var game = response.GetValue<JsonElement>(0).Clone();
                Volatile.Write(ref _ultimoJuego, new ResultadoRecibido(game));
                JuegoRecibido?.Invoke(game);
            }
            return Task.CompletedTask;
        });
        _socket.On("game_started", response =>
        {
            var payload = response.GetValue<JsonElement>(0);
            var type = payload.TryGetProperty("type", out var typeNode) ? typeNode.GetString() : null;
            if (!string.IsNullOrWhiteSpace(type)) JuegoIniciado?.Invoke(type);
            return Task.CompletedTask;
        });
        _socket.On("room_closed", _ =>
        {
            SalaFinalizada?.Invoke("El anfitrión cerró la sala.");
            return Task.CompletedTask;
        });
        _socket.On("room_deleted", _ =>
        {
            SalaFinalizada?.Invoke("La sala fue eliminada.");
            return Task.CompletedTask;
        });

        await _socket.ConnectAsync();
    }

    private Task SuscribirseAsync(SocketIO socket, string codigoSala)
    {
        var juegoAnterior = Volatile.Read(ref _ultimoJuego);
        return socket.EmitAsync("join_room", [codigoSala], ack =>
        {
            if (!ReferenceEquals(_socket, socket) || !socket.Connected) return Task.CompletedTask;

            var respuesta = ack.GetValue<JsonElement>(0);
            var aceptada = respuesta.TryGetProperty("ok", out var ok) && ok.ValueKind == JsonValueKind.True;
            CambiarEstado(aceptada);
            if (!aceptada) return Task.CompletedTask;

            if (respuesta.TryGetProperty("room", out var sala)
                && sala.TryGetProperty("latestEvent", out var evento)
                && evento.ValueKind == JsonValueKind.Object)
            {
                var recuperado = new ResultadoRecibido(evento.Clone());
                var anterior = Interlocked.CompareExchange(ref _ultimoJuego, recuperado, juegoAnterior);
                if (ReferenceEquals(anterior, juegoAnterior)) JuegoRecibido?.Invoke(recuperado.Datos);
            }

            SalaActualizada?.Invoke();
            return Task.CompletedTask;
        });
    }

    private void CambiarEstado(bool confirmada)
    {
        _salaConfirmada = confirmada;
        ConexionCambiada?.Invoke(confirmada);
    }

    public async Task DesconectarAsync()
    {
        var socket = _socket;
        if (socket is null) return;

        _socket = null;
        _codigoSala = string.Empty;
        Volatile.Write(ref _ultimoJuego, null);
        CambiarEstado(false);

        try
        {
            if (socket.Connected) await socket.DisconnectAsync();
        }
        finally
        {
            socket.Dispose();
        }
    }
}
