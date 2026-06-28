namespace Cooperativa.Api.Contracts;

// --- Auth ---
public record RegisterRequest(string Email, string Password, string DisplayName);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, Guid UserId, string DisplayName, Guid? CooperativeId, string? Role);

// --- Cooperativa / emparejamiento ---
public record CreateCooperativeRequest(string? Name);
public record CreateCooperativeResponse(Guid CooperativeId, string InviteCode);
public record JoinCooperativeRequest(string InviteCode);

// --- Estado de la habitación (carga inicial y push en tiempo real) ---
public record MemberStatusDto(Guid Id, string DisplayName, string Role, bool ActedToday, bool Online, string? Note);
public record RoomStateDto(
    Guid CooperativeId,
    int PlantLevel,
    int CurrentStreak,
    string? LastNote,
    Guid? LastNoteAuthorId,
    DateTimeOffset? LastNoteAt,
    string TimeZoneId,
    string? PendingTimeZoneId,
    string Weather,
    bool BothOnline,
    IReadOnlyList<MemberStatusDto> Members,
    BloomDto? TodayBloom,
    PlantDto? ActivePlant,
    IReadOnlyList<ShelfPlantDto> RoomPlants,
    int GreenhouseCount,
    WeatherEventDto? Event,
    IReadOnlyList<int> PhotoSlots);

// --- Fotos del corcho (3 ranuras compartidas) ---
public record SetPhotoRequest(int Slot, string DataUrl);
public record PhotoDto(int Slot, string DataUrl);

// Planta madura mostrada en el estante de la SALA (decoración, hasta 4).
public record ShelfPlantDto(Guid Id, string Species, int Seed);

// Evento climático (frío/calor): tipo, fase (pronóstico/activo) y si ya se protegió.
public record WeatherEventDto(string Type, string Stage, bool Handled);

// --- Brote del día / ramo (recompensa diaria) ---
public record BloomDto(string Date, int Seed, string Weather, int Streak, string? Note, DateTimeOffset CreatedAt);

// --- Planta en cultivo (eje crecimiento + salud) y jardín/invernadero ---
public record PlantDto(
    Guid Id,
    string Species,
    int Seed,
    int GrowthStage,
    int MaxStage,
    string Health,
    int ActionsCount,
    int NotesCount,
    DateTimeOffset StartedAt);

public record GardenItemDto(
    Guid Id,
    string Species,
    int Seed,
    int ActionsCount,
    int NotesCount,
    DateTimeOffset StartedAt,
    DateTimeOffset MaturedAt);

// --- Ficha de una flor: stats + librito de notas de la pareja ---
public record PlantNoteDto(string Text, string AuthorName, DateTimeOffset CreatedAt);
public record PlantDetailDto(
    Guid Id,
    string Species,
    int Seed,
    int GrowthStage,
    int MaxStage,
    string Health,
    int ActionsCount,
    int NotesCount,
    DateTimeOffset StartedAt,
    DateTimeOffset? MaturedAt,
    IReadOnlyList<PlantNoteDto> Notes);

// --- Payloads de eventos del Hub ---
public record NoteDto(string? Text, Guid? AuthorId, DateTimeOffset? At);
public record ActionPerformedDto(Guid UserId, string ActionCode, string Date);
