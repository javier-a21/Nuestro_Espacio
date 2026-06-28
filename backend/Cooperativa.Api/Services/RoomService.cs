using Cooperativa.Api.Contracts;
using Cooperativa.Api.Realtime;
using Cooperativa.Domain;
using Cooperativa.Domain.Entities;
using Cooperativa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cooperativa.Api.Services;

public class RoomService
{
    private readonly AppDbContext _db;
    private readonly IRoomNotifier _notifier;
    private readonly PresenceTracker _presence;

    public RoomService(AppDbContext db, IRoomNotifier notifier, PresenceTracker presence)
    {
        _db = db;
        _notifier = notifier;
        _presence = presence;
    }

    /// <summary>"Hoy" en el huso compartido de la cooperativa.</summary>
    public static DateOnly LocalToday(string timeZoneId)
    {
        var tz = SafeZone(timeZoneId);
        var local = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);
        return DateOnly.FromDateTime(local.DateTime);
    }

    public static TimeZoneInfo SafeZone(string timeZoneId)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId); }
        catch { return TimeZoneInfo.Utc; }
    }

    public async Task<RoomStateDto?> GetStateForUserAsync(Guid userId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        return user?.CooperativeId is Guid coopId ? await GetStateAsync(coopId) : null;
    }

    public async Task<RoomStateDto?> GetStateAsync(Guid coopId)
    {
        var coop = await _db.Cooperatives
            .Include(c => c.Members)
            .Include(c => c.Room)
            .FirstOrDefaultAsync(c => c.Id == coopId);
        return coop?.Room is null ? null : await BuildStateAsync(coop);
    }

    private async Task<RoomStateDto> BuildStateAsync(Cooperative coop)
    {
        var room = coop.Room!;
        var today = LocalToday(room.TimeZoneId);

        var actedUserIds = await _db.DailyActions
            .Where(a => a.CooperativeId == coop.Id && a.Date == today)
            .Select(a => a.UserId).Distinct().ToListAsync();

        var members = coop.Members.Select(m => new MemberStatusDto(
            m.Id,
            m.DisplayName,
            (m.Role ?? Role.A).ToString(),
            actedUserIds.Contains(m.Id),
            _presence.IsOnline(coop.Id, m.Id),
            m.NoteText)).ToList();

        var todayBloom = await _db.DailyBlooms
            .FirstOrDefaultAsync(b => b.CooperativeId == coop.Id && b.Date == today);

        var activePlant = await _db.Plants
            .FirstOrDefaultAsync(p => p.CooperativeId == coop.Id && p.MaturedAt == null);

        // Maduras ordenadas de la más reciente a la más antigua (SQLite no ordena DateTimeOffset → en cliente).
        var matured = await _db.Plants
            .Where(p => p.CooperativeId == coop.Id && p.MaturedAt != null)
            .ToListAsync();
        var ordered = matured.OrderByDescending(p => p.MaturedAt).ToList();

        // Las 4 más recientes decoran la SALA; el resto vive en el invernadero.
        var roomPlants = ordered.Take(RoomShelfSize)
            .Select(p => new ShelfPlantDto(p.Id, p.Species.ToString(), p.Seed))
            .ToList();
        var greenhouseCount = Math.Max(0, ordered.Count - RoomShelfSize);

        var eventDto = room.WeatherEvent == WeatherEventType.None
            ? null
            : new WeatherEventDto(room.WeatherEvent.ToString(), room.EventStage.ToString(), room.EventHandled);

        var photoSlots = await _db.Photos
            .Where(p => p.CooperativeId == coop.Id)
            .Select(p => p.Slot)
            .ToListAsync();

        return new RoomStateDto(
            coop.Id,
            (int)room.PlantLevel,
            room.CurrentStreak,
            room.LastNote,
            room.LastNoteAuthorId,
            room.LastNoteAt,
            room.TimeZoneId,
            room.PendingTimeZoneId,
            room.Weather.ToString(),
            _presence.BothOnline(coop.Id),
            members,
            todayBloom is null ? null : ToBloomDto(todayBloom),
            activePlant is null ? null : ToPlantDto(activePlant),
            roomPlants,
            greenhouseCount,
            eventDto,
            photoSlots);
    }

    /// <summary>Plazas de plantas maduras que se muestran en la SALA; el resto va al invernadero.</summary>
    public const int RoomShelfSize = 4;

    private static BloomDto ToBloomDto(DailyBloom b) =>
        new(b.Date.ToString("O"), b.Seed, b.Weather.ToString(), b.Streak, b.Note, b.CreatedAt);

    private static PlantDto ToPlantDto(Plant p) =>
        new(p.Id, p.Species.ToString(), p.Seed, p.GrowthStage, Plant.MaxStage, p.Health.ToString(), p.ActionsCount, p.NotesCount, p.StartedAt);

    private static Plant NewPlant(Guid coopId) => new()
    {
        CooperativeId = coopId,
        Species = (Species)Random.Shared.Next(2),
        Seed = Random.Shared.Next(),
    };

    /// <summary>Devuelve la planta ACTIVA (en crecimiento); si no existe, la crea.</summary>
    private async Task<Plant> EnsureActivePlantAsync(Guid coopId)
    {
        var active = await _db.Plants.FirstOrDefaultAsync(p => p.CooperativeId == coopId && p.MaturedAt == null);
        if (active is null)
        {
            active = NewPlant(coopId);
            _db.Plants.Add(active);
            await _db.SaveChangesAsync();
        }
        return active;
    }

    /// <summary>Estante/invernadero: plantas maduras (trofeos), de la más reciente a la más antigua.</summary>
    public async Task<IReadOnlyList<GardenItemDto>> GetGardenForUserAsync(Guid userId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.CooperativeId is not Guid coopId) return Array.Empty<GardenItemDto>();

        // Nota: SQLite no ordena por DateTimeOffset, así que ordenamos en cliente tras materializar.
        // El invernadero muestra el EXCEDENTE: las que no caben en el estante de la sala (las 4 más recientes).
        var plants = await _db.Plants
            .Where(p => p.CooperativeId == coopId && p.MaturedAt != null)
            .ToListAsync();

        return plants
            .OrderByDescending(p => p.MaturedAt)
            .Skip(RoomShelfSize)
            .Select(p => new GardenItemDto(p.Id, p.Species.ToString(), p.Seed, p.ActionsCount, p.NotesCount, p.StartedAt, p.MaturedAt!.Value))
            .ToList();
    }

    /// <summary>Ficha de una flor: stats + librito de notas. Solo si pertenece a la cooperativa del usuario.</summary>
    public async Task<PlantDetailDto?> GetPlantDetailForUserAsync(Guid userId, Guid plantId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.CooperativeId is not Guid coopId) return null;

        var plant = await _db.Plants.FirstOrDefaultAsync(p => p.Id == plantId && p.CooperativeId == coopId);
        if (plant is null) return null;

        // SQLite no ordena por DateTimeOffset → materializamos y ordenamos en cliente.
        var notes = await _db.PlantNotes.Where(n => n.PlantId == plantId).ToListAsync();
        var noteDtos = notes
            .OrderBy(n => n.CreatedAt)
            .Select(n => new PlantNoteDto(n.Text, n.AuthorName, n.CreatedAt))
            .ToList();

        return new PlantDetailDto(
            plant.Id,
            plant.Species.ToString(),
            plant.Seed,
            plant.GrowthStage,
            Plant.MaxStage,
            plant.Health.ToString(),
            plant.ActionsCount,
            plant.NotesCount,
            plant.StartedAt,
            plant.MaturedAt,
            noteDtos);
    }

    // ---------------------------------------------------------------------------
    //  Fotos del corcho (3 ranuras compartidas; reemplazar BORRA la anterior)
    // ---------------------------------------------------------------------------
    public async Task SetPhotoAsync(Guid userId, int slot, string dataUrl)
    {
        if (slot < 0 || slot >= Photo.MaxSlots) return;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.CooperativeId is not Guid coopId) return;

        var (bytes, contentType) = ParseDataUrl(dataUrl);
        if (bytes is null || bytes.Length == 0 || bytes.Length > 2_000_000) return; // inválida o demasiado grande

        // La anterior de esa ranura se borra y NO se guarda.
        var existing = await _db.Photos.FirstOrDefaultAsync(p => p.CooperativeId == coopId && p.Slot == slot);
        if (existing is not null)
        {
            _db.Photos.Remove(existing);
            await _db.SaveChangesAsync(); // borra antes de insertar (índice único Coop+Slot)
        }

        _db.Photos.Add(new Photo
        {
            CooperativeId = coopId,
            Slot = slot,
            Data = bytes,
            ContentType = contentType,
            UploadedById = userId,
        });
        await _db.SaveChangesAsync();

        await NotifyStateAsync(coopId);
    }

    public async Task DeletePhotoAsync(Guid userId, int slot)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.CooperativeId is not Guid coopId) return;

        var existing = await _db.Photos.FirstOrDefaultAsync(p => p.CooperativeId == coopId && p.Slot == slot);
        if (existing is null) return;

        _db.Photos.Remove(existing);
        await _db.SaveChangesAsync();
        await NotifyStateAsync(coopId);
    }

    public async Task<IReadOnlyList<PhotoDto>> GetPhotosForUserAsync(Guid userId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.CooperativeId is not Guid coopId) return Array.Empty<PhotoDto>();

        var photos = await _db.Photos.Where(p => p.CooperativeId == coopId).ToListAsync();
        return photos
            .OrderBy(p => p.Slot)
            .Select(p => new PhotoDto(p.Slot, $"data:{p.ContentType};base64,{Convert.ToBase64String(p.Data)}"))
            .ToList();
    }

    /// <summary>Extrae (bytes, contentType) de un data URL "data:image/...;base64,XXXX".</summary>
    private static (byte[]? Bytes, string ContentType) ParseDataUrl(string? dataUrl)
    {
        if (string.IsNullOrEmpty(dataUrl) || !dataUrl.StartsWith("data:")) return (null, "");
        var comma = dataUrl.IndexOf(',');
        if (comma < 0) return (null, "");

        var contentType = dataUrl[5..comma].Split(';')[0];
        if (contentType is not ("image/jpeg" or "image/png" or "image/webp")) return (null, "");

        try { return (Convert.FromBase64String(dataUrl[(comma + 1)..]), contentType); }
        catch { return (null, ""); }
    }

    private async Task NotifyStateAsync(Guid coopId)
    {
        var dto = await GetStateAsync(coopId);
        if (dto is not null) await _notifier.RoomStateAsync(coopId, dto);
    }

    /// <summary>Protege la planta ante el evento climático activo (dar sombra / cubrir).</summary>
    public async Task ProtectPlantAsync(Guid userId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.CooperativeId is not Guid coopId) return;

        var room = await _db.RoomStates.FirstOrDefaultAsync(r => r.CooperativeId == coopId);
        if (room is null || room.EventStage == WeatherEventStage.None || room.EventHandled) return;

        room.EventHandled = true;
        var plant = await _db.Plants.FirstOrDefaultAsync(p => p.CooperativeId == coopId && p.MaturedAt == null);
        if (plant is not null) plant.ActionsCount++;
        await _db.SaveChangesAsync();

        var coop = await _db.Cooperatives
            .Include(c => c.Members).Include(c => c.Room)
            .FirstOrDefaultAsync(c => c.Id == coopId);
        if (coop?.Room is not null)
            await _notifier.RoomStateAsync(coopId, await BuildStateAsync(coop));
    }

    /// <summary>Acción puntual "abonar": cura la marchitez de la planta activa.</summary>
    public async Task AbonarAsync(Guid userId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.CooperativeId is not Guid coopId) return;

        var plant = await _db.Plants.FirstOrDefaultAsync(p => p.CooperativeId == coopId && p.MaturedAt == null);
        if (plant is null || plant.Health == PlantHealth.Healthy) return; // nada que curar

        plant.Health = PlantHealth.Healthy;
        plant.ActionsCount++;
        await _db.SaveChangesAsync();

        var coop = await _db.Cooperatives
            .Include(c => c.Members).Include(c => c.Room)
            .FirstOrDefaultAsync(c => c.Id == coopId);
        if (coop?.Room is not null)
            await _notifier.RoomStateAsync(coopId, await BuildStateAsync(coop));
    }

    /// <summary>El ramo completo de la cooperativa (recuerdos), del más reciente al más antiguo.</summary>
    public async Task<IReadOnlyList<BloomDto>> GetBloomsForUserAsync(Guid userId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.CooperativeId is not Guid coopId) return Array.Empty<BloomDto>();

        var blooms = await _db.DailyBlooms
            .Where(b => b.CooperativeId == coopId)
            .OrderByDescending(b => b.Date)
            .ToListAsync();

        return blooms.Select(ToBloomDto).ToList();
    }

    public async Task PerformActionAsync(Guid userId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.CooperativeId is not Guid coopId || user.Role is not Role role) return;

        var coop = await _db.Cooperatives
            .Include(c => c.Members)
            .Include(c => c.Room)
            .FirstOrDefaultAsync(c => c.Id == coopId);
        if (coop?.Room is null) return;

        var actionType = await _db.ActionTypes.FirstOrDefaultAsync(t => t.RequiredRole == role && t.Active && t.Daily);
        if (actionType is null) return;

        var today = LocalToday(coop.Room.TimeZoneId);

        var already = await _db.DailyActions.AnyAsync(a =>
            a.CooperativeId == coopId && a.UserId == userId &&
            a.ActionTypeId == actionType.Id && a.Date == today);
        if (already) return; // un botón por ciclo de 24h

        _db.DailyActions.Add(new DailyAction
        {
            CooperativeId = coopId,
            UserId = userId,
            ActionTypeId = actionType.Id,
            Date = today
        });

        // La acción cuenta para la ficha de la planta en cultivo.
        var plant = await EnsureActivePlantAsync(coopId);
        plant.ActionsCount++;

        coop.Room.LastInteractionAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        await RecomputePlantAsync(coop);     // feedback inmediato de salud
        await _db.SaveChangesAsync();

        // Recompensa diaria: si esta acción completa el ritual de AMBOS, acuña el brote del día.
        await TryCreateBloomAsync(coop, today);

        await _notifier.ActionPerformedAsync(coopId, new ActionPerformedDto(userId, actionType.Code, today.ToString("O")));
        await _notifier.RoomStateAsync(coopId, await BuildStateAsync(coop));
    }

    /// <summary>
    /// Crea el "brote del día" la primera vez que, en una jornada, ambos miembros han
    /// completado su acción. Idempotente: como mucho un brote por cooperativa y día.
    /// </summary>
    private async Task TryCreateBloomAsync(Cooperative coop, DateOnly today)
    {
        if (coop.Members.Count < 2) return;

        var actedCount = await _db.DailyActions
            .Where(a => a.CooperativeId == coop.Id && a.Date == today)
            .Select(a => a.UserId).Distinct().CountAsync();
        if (actedCount < 2) return;

        var already = await _db.DailyBlooms.AnyAsync(b => b.CooperativeId == coop.Id && b.Date == today);
        if (already) return;

        var room = coop.Room!;
        var bloom = new DailyBloom
        {
            CooperativeId = coop.Id,
            Date = today,
            Seed = Random.Shared.Next(),
            Weather = room.Weather,
            Streak = room.CurrentStreak,
            Note = room.LastNote,
        };
        _db.DailyBlooms.Add(bloom);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Carrera: el otro miembro acuñó el brote a la vez. El índice único lo impide;
            // soltamos la entidad y no notificamos por duplicado.
            _db.Entry(bloom).State = EntityState.Detached;
            return;
        }

        // Crecimiento: el día exitoso hace AVANZAR una fase (solo si está sana).
        await AdvanceGrowthAsync(coop.Id);

        await _notifier.BloomCreatedAsync(coop.Id, ToBloomDto(bloom));
    }

    /// <summary>
    /// Sube una fase la planta activa (si está sana). Al llegar a la fase máxima, "madura":
    /// pasa al estante/invernadero y nace una nueva planta activa.
    /// La marchitez pausa el crecimiento: hay que abonar para volver a crecer.
    /// </summary>
    private async Task AdvanceGrowthAsync(Guid coopId)
    {
        var plant = await EnsureActivePlantAsync(coopId);
        if (plant.Health != PlantHealth.Healthy || plant.GrowthStage >= Plant.MaxStage) return;

        plant.GrowthStage++;
        if (plant.GrowthStage >= Plant.MaxStage)
        {
            plant.MaturedAt = DateTimeOffset.UtcNow;     // trofeo permanente
            _db.Plants.Add(NewPlant(coopId));            // nace la siguiente planta activa
        }
        await _db.SaveChangesAsync();
    }

    public async Task SendNoteAsync(Guid userId, string text)
    {
        text = (text ?? string.Empty).Trim();
        if (text.Length > 50) text = text[..50]; // límite estricto de la nota fugaz

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.CooperativeId is not Guid coopId) return;

        var room = await _db.RoomStates.FirstOrDefaultAsync(r => r.CooperativeId == coopId);
        if (room is null) return;

        var now = DateTimeOffset.UtcNow;

        // Post-it PERSONAL del usuario (cada miembro tiene el suyo).
        user.NoteText = text.Length == 0 ? null : text;
        user.NoteAt = now;
        room.LastInteractionAt = now;

        // Una nota (no vacía) se conserva en el "librito" de la planta en cultivo y cuenta para su ficha.
        if (text.Length > 0)
        {
            var plant = await EnsureActivePlantAsync(coopId);
            plant.NotesCount++;
            _db.PlantNotes.Add(new PlantNote
            {
                PlantId = plant.Id,
                CooperativeId = coopId,
                AuthorId = userId,
                AuthorName = user.DisplayName,
                Text = text,
            });
        }
        await _db.SaveChangesAsync();

        // Reemite el estado completo: así ambos clientes ven los dos post-its actualizados.
        var dto = await GetStateAsync(coopId);
        if (dto is not null) await _notifier.RoomStateAsync(coopId, dto);
    }

    /// <summary>Registra un cambio de huso PENDIENTE; el job lo aplica de madrugada y reinicia la planta.</summary>
    public async Task ProposeTimeZoneAsync(Guid userId, string timeZoneId)
    {
        try { _ = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId); }
        catch { return; } // zona inválida

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.CooperativeId is not Guid coopId) return;

        var room = await _db.RoomStates.FirstOrDefaultAsync(r => r.CooperativeId == coopId);
        if (room is null) return;

        room.PendingTimeZoneId = timeZoneId;
        await _db.SaveChangesAsync();

        await _notifier.PendingTimeZoneAsync(coopId, timeZoneId);
    }

    private async Task RecomputePlantAsync(Cooperative coop)
    {
        var room = coop.Room!;
        var today = LocalToday(room.TimeZoneId);

        var actedCount = await _db.DailyActions
            .Where(a => a.CooperativeId == coop.Id && a.Date == today)
            .Select(a => a.UserId).Distinct().CountAsync();

        bool bothActedToday = coop.Members.Count >= 2 && actedCount >= 2;
        double hours = room.LastInteractionAt is null
            ? double.MaxValue
            : (DateTimeOffset.UtcNow - room.LastInteractionAt.Value).TotalHours;

        room.PlantLevel = PlantEvaluator.Evaluate(room.CurrentStreak, bothActedToday, hours);
    }
}
