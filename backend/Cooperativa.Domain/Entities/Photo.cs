namespace Cooperativa.Domain.Entities;

/// <summary>
/// Una foto del corcho compartido. Hay como mucho <see cref="MaxSlots"/> (3) por cooperativa,
/// una por ranura. Al subir una foto a una ranura ocupada, la anterior se BORRA (no se conserva),
/// así el almacenamiento queda acotado a lo que se está mostrando.
/// </summary>
public class Photo
{
    public const int MaxSlots = 3;

    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CooperativeId { get; set; }
    public Cooperative Cooperative { get; set; } = default!;

    /// <summary>Ranura del corcho (0..<see cref="MaxSlots"/>-1).</summary>
    public int Slot { get; set; }

    /// <summary>Bytes de la imagen (ya reducida en el cliente antes de subir).</summary>
    public byte[] Data { get; set; } = default!;

    public string ContentType { get; set; } = "image/jpeg";

    public Guid UploadedById { get; set; }
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
}
