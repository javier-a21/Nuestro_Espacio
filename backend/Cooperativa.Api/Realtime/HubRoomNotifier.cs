using Cooperativa.Api.Contracts;
using Microsoft.AspNetCore.SignalR;

namespace Cooperativa.Api.Realtime;

public class HubRoomNotifier : IRoomNotifier
{
    private readonly IHubContext<CooperativeHub> _hub;
    public HubRoomNotifier(IHubContext<CooperativeHub> hub) => _hub = hub;

    private IClientProxy Group(Guid coopId) => _hub.Clients.Group(coopId.ToString());

    public Task RoomStateAsync(Guid coopId, RoomStateDto state) => Group(coopId).SendAsync("RoomState", state);
    public Task NoteUpdatedAsync(Guid coopId, NoteDto note) => Group(coopId).SendAsync("NoteUpdated", note);
    public Task ActionPerformedAsync(Guid coopId, ActionPerformedDto action) => Group(coopId).SendAsync("ActionPerformed", action);
    public Task PresenceUpdatedAsync(Guid coopId, bool bothOnline) => Group(coopId).SendAsync("PresenceUpdated", bothOnline);
    public Task PendingTimeZoneAsync(Guid coopId, string? timeZoneId) => Group(coopId).SendAsync("PendingTimeZone", timeZoneId);
    public Task BloomCreatedAsync(Guid coopId, BloomDto bloom) => Group(coopId).SendAsync("BloomCreated", bloom);
}
