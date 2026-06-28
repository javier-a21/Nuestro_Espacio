using Cooperativa.Api.Realtime;
using Cooperativa.Api.Services;
using Cooperativa.Domain;
using Cooperativa.Domain.Entities;
using Cooperativa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cooperativa.Api.Jobs;

/// <summary>
/// Tick horario orquestado por Hangfire. Para cada cooperativa, en SU huso compartido:
///  1) Aplica un cambio de huso pendiente de madrugada (reinicia planta/racha).
///  2) Cierra rachas de los días completos no evaluados.
///  3) Recalcula la decadencia / nivel de la planta.
///  4) Avanza el clima escriptado.
/// Notifica a los clientes conectados los cambios.
/// </summary>
public class RoomTickJob
{
    private readonly AppDbContext _db;
    private readonly IRoomNotifier _notifier;
    private readonly RoomService _room;

    public RoomTickJob(AppDbContext db, IRoomNotifier notifier, RoomService room)
    {
        _db = db;
        _notifier = notifier;
        _room = room;
    }

    public async Task RunAsync()
    {
        var coops = await _db.Cooperatives
            .Include(c => c.Members)
            .Include(c => c.Room)
            .Where(c => c.Room != null)
            .ToListAsync();

        foreach (var coop in coops)
            await ProcessAsync(coop);
    }

    private async Task ProcessAsync(Cooperative coop)
    {
        var room = coop.Room!;
        var tz = RoomService.SafeZone(room.TimeZoneId);
        var localNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);
        var localToday = DateOnly.FromDateTime(localNow.DateTime);
        var changed = false;

        // 1) Cambio de huso pendiente: se aplica de madrugada (00:00–05:59) y reinicia.
        if (room.PendingTimeZoneId is not null && localNow.Hour < 6)
        {
            room.TimeZoneId = room.PendingTimeZoneId;
            room.PendingTimeZoneId = null;
            room.CurrentStreak = 0;
            room.PlantLevel = PlantLevel.Stable;

            tz = RoomService.SafeZone(room.TimeZoneId);
            localNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);
            localToday = DateOnly.FromDateTime(localNow.DateTime);
            room.LastStreakEvaluation = localToday;

            changed = true;
            await _notifier.PendingTimeZoneAsync(coop.Id, null);
        }

        // 2) Cierre de rachas para días completos aún no evaluados.
        //    Un día sin el ritual de AMBOS marchita un paso la planta en cultivo (se cura abonando).
        var activePlant = await _db.Plants
            .FirstOrDefaultAsync(p => p.CooperativeId == coop.Id && p.MaturedAt == null);
        var lastEval = room.LastStreakEvaluation ?? localToday.AddDays(-1);
        for (var day = lastEval.AddDays(1); day < localToday; day = day.AddDays(1))
        {
            var acted = await _db.DailyActions
                .Where(a => a.CooperativeId == coop.Id && a.Date == day)
                .Select(a => a.UserId).Distinct().CountAsync();

            var both = coop.Members.Count >= 2 && acted >= 2;
            room.CurrentStreak = both ? room.CurrentStreak + 1 : 0;
            if (!both && activePlant is not null && activePlant.Health != PlantHealth.Wilted)
                activePlant.Health = (PlantHealth)Math.Min((int)PlantHealth.Wilted, (int)activePlant.Health + 1);

            room.LastStreakEvaluation = day;
            changed = true;
        }

        // 3) Decadencia + nivel actual de la planta.
        var todayActed = await _db.DailyActions
            .Where(a => a.CooperativeId == coop.Id && a.Date == localToday)
            .Select(a => a.UserId).Distinct().CountAsync();

        bool bothToday = coop.Members.Count >= 2 && todayActed >= 2;
        double hours = room.LastInteractionAt is null
            ? double.MaxValue
            : (DateTimeOffset.UtcNow - room.LastInteractionAt.Value).TotalHours;

        var level = PlantEvaluator.Evaluate(room.CurrentStreak, bothToday, hours);
        if (level != room.PlantLevel) { room.PlantLevel = level; changed = true; }

        // 4) Clima escriptado (opción B): cambio ocasional por tick.
        if (Random.Shared.NextDouble() < 0.34)
        {
            var all = Enum.GetValues<WeatherType>();
            var next = all[Random.Shared.Next(all.Length)];
            if (next != room.Weather) { room.Weather = next; changed = true; }
        }

        // 5) Eventos climáticos (frío/calor): se AVISAN antes y luego golpean.
        //    Si al activarse no se protegió la planta, sufre un paso de marchitez.
        var now = DateTimeOffset.UtcNow;
        const double ForecastHours = 8;  // margen de aviso
        const double ActiveHours = 12;   // duración del golpe
        if (room.EventStage == WeatherEventStage.None)
        {
            if (Random.Shared.NextDouble() < 0.04) // poco frecuente
            {
                room.WeatherEvent = Random.Shared.Next(2) == 0 ? WeatherEventType.Heatwave : WeatherEventType.ColdSnap;
                room.EventStage = WeatherEventStage.Forecast;
                room.EventHandled = false;
                room.EventSince = now;
                changed = true;
            }
        }
        else if (room.EventStage == WeatherEventStage.Forecast)
        {
            if (room.EventSince is { } since && (now - since).TotalHours >= ForecastHours)
            {
                room.EventStage = WeatherEventStage.Active;
                room.EventSince = now;
                changed = true;
            }
        }
        else // Active
        {
            if (room.EventSince is { } since && (now - since).TotalHours >= ActiveHours)
            {
                if (!room.EventHandled && activePlant is not null && activePlant.Health != PlantHealth.Wilted)
                    activePlant.Health = (PlantHealth)Math.Min((int)PlantHealth.Wilted, (int)activePlant.Health + 1);

                room.WeatherEvent = WeatherEventType.None;
                room.EventStage = WeatherEventStage.None;
                room.EventHandled = false;
                room.EventSince = null;
                changed = true;
            }
        }

        if (changed)
        {
            await _db.SaveChangesAsync();
            var dto = await _room.GetStateAsync(coop.Id);
            if (dto is not null) await _notifier.RoomStateAsync(coop.Id, dto);
        }
    }
}
