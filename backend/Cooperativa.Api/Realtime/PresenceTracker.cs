using System.Collections.Concurrent;

namespace Cooperativa.Api.Realtime;

/// <summary>
/// Rastrea qué usuarios están conectados por cooperativa (puede haber varias pestañas/dispositivos).
/// Singleton: el estado de presencia vive en memoria del proceso.
/// </summary>
public class PresenceTracker
{
    // coopId -> (userId -> nº de conexiones activas)
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, int>> _online = new();

    public void Connect(Guid coopId, Guid userId)
    {
        var users = _online.GetOrAdd(coopId, _ => new ConcurrentDictionary<Guid, int>());
        users.AddOrUpdate(userId, 1, (_, count) => count + 1);
    }

    public void Disconnect(Guid coopId, Guid userId)
    {
        if (!_online.TryGetValue(coopId, out var users)) return;
        var remaining = users.AddOrUpdate(userId, 0, (_, count) => count - 1);
        if (remaining <= 0) users.TryRemove(userId, out _);
        if (users.IsEmpty) _online.TryRemove(coopId, out _);
    }

    public bool IsOnline(Guid coopId, Guid userId) =>
        _online.TryGetValue(coopId, out var users) && users.ContainsKey(userId);

    public int OnlineUsers(Guid coopId) =>
        _online.TryGetValue(coopId, out var users) ? users.Count : 0;

    /// <summary>Ambos miembros presentes → se enciende la luciérnaga.</summary>
    public bool BothOnline(Guid coopId) => OnlineUsers(coopId) >= 2;
}
