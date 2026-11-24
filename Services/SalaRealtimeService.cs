using System.Text.Json;
using SocketIOClient;

namespace LaVaquita.Services;

public sealed class SalaRealtimeService(Uri serverUri)
{
    private readonly Uri _serverUri = serverUri;
    private SocketIO? _socket;
    private string _codigoSala = string.Empty;

    public event Action? SalaActualizada;
    public event Action<JsonElement>? JuegoRecibido;
    public event Action<string>? SalaFinalizada;
    public event Action<bool>? ConexionCambiada;

    public bool EstaConectado => _socket?.Connected == true;

    public async Task ConectarAsync(string codigoSala)
    {
        codigoSala = codigoSala.Trim().ToUpperInvariant();
        if (EstaConectado && _codigoSala == codigoSala) return;

        await DesconectarAsync();
        _codigoSala = codigoSala;
        _socket = new SocketIO(_serverUri, new SocketIOOptions
        {
            Reconnection = true,
            ReconnectionAttempts = int.MaxValue,
            ReconnectionDelayMax = 5000,
            ConnectionTimeout = TimeSpan.FromSeconds(15)
        });

        _socket.OnConnected += async (_, _) =>
        {
            ConexionCambiada?.Invoke(true);
            await SuscribirseAsync();
            SalaActualizada?.Invoke();
        };
        _socket.OnDisconnected += (_, _) => ConexionCambiada?.Invoke(false);
        _socket.On("participant_joined", _ =>
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
            var game = response.GetValue<JsonElement>(0).Clone();
            JuegoRecibido?.Invoke(game);
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

    private Task SuscribirseAsync() =>
        _socket is null || string.IsNullOrWhiteSpace(_codigoSala)
            ? Task.CompletedTask
            : _socket.EmitAsync("join_room", [_codigoSala]);

    public async Task DesconectarAsync()
    {
        if (_socket is null) return;

        try
        {
            if (_socket.Connected) await _socket.DisconnectAsync();
        }
        finally
        {
            _socket.Dispose();
            _socket = null;
            _codigoSala = string.Empty;
            ConexionCambiada?.Invoke(false);
        }
    }
}
