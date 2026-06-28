using Cooperativa.Api.Common;
using Cooperativa.Api.Services;
using Cooperativa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Cooperativa.Api.Realtime;

[Authorize]
public class CooperativeHub : Hub
{
    private const string CoopItemKey = "coopId";

    private readonly AppDbContext _db;
    private readonly PresenceTracker _presence;
    private readonly RoomService _room;

    public CooperativeHub(AppDbContext db, PresenceTracker presence, RoomService room)
    {
        _db = db;
        _presence = presence;
        _room = room;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User!.GetUserId();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user?.CooperativeId is Guid coopId)
        {
            Context.Items[CoopItemKey] = coopId;
            await Groups.AddToGroupAsync(Context.ConnectionId, coopId.ToString());
            _presence.Connect(coopId, userId);

            user.LastSeen = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();

            await Clients.Group(coopId.ToString()).SendAsync("PresenceUpdated", _presence.BothOnline(coopId));
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User!.GetUserId();
        if (Context.Items.TryGetValue(CoopItemKey, out var raw) && raw is Guid coopId)
        {
            _presence.Disconnect(coopId, userId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, coopId.ToString());
            await Clients.Group(coopId.ToString()).SendAsync("PresenceUpdated", _presence.BothOnline(coopId));
        }

        await base.OnDisconnectedAsync(exception);
    }

    // Acciones en tiempo real: el servicio persiste y reemite al grupo (<1s).
    public Task PerformAction() => _room.PerformActionAsync(Context.User!.GetUserId());
    public Task Abonar() => _room.AbonarAsync(Context.User!.GetUserId());
    public Task ProtectPlant() => _room.ProtectPlantAsync(Context.User!.GetUserId());
    public Task SendNote(string text) => _room.SendNoteAsync(Context.User!.GetUserId(), text);
    public Task ProposeTimeZone(string timeZoneId) => _room.ProposeTimeZoneAsync(Context.User!.GetUserId(), timeZoneId);
}
