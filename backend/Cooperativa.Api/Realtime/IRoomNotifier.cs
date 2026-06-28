using Cooperativa.Api.Contracts;

namespace Cooperativa.Api.Realtime;

/// <summary>Reemite cambios a los clientes conectados de una cooperativa (grupo SignalR).</summary>
public interface IRoomNotifier
{
    Task RoomStateAsync(Guid coopId, RoomStateDto state);
    Task NoteUpdatedAsync(Guid coopId, NoteDto note);
    Task ActionPerformedAsync(Guid coopId, ActionPerformedDto action);
    Task PresenceUpdatedAsync(Guid coopId, bool bothOnline);
    Task PendingTimeZoneAsync(Guid coopId, string? timeZoneId);
    Task BloomCreatedAsync(Guid coopId, BloomDto bloom);
}
