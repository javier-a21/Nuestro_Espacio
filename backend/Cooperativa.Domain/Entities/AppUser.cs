using Microsoft.AspNetCore.Identity;

namespace Cooperativa.Domain.Entities;

/// <summary>Usuario (identidad gestionada por ASP.NET Core Identity).</summary>
public class AppUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = default!;

    public Guid? CooperativeId { get; set; }
    public Cooperative? Cooperative { get; set; }

    /// <summary>A = Riego, B = Poda. Se asigna al unirse a la cooperativa.</summary>
    public Role? Role { get; set; }

    /// <summary>Último acceso, usado para el indicador de presencia (luciérnaga).</summary>
    public DateTimeOffset? LastSeen { get; set; }

    /// <summary>Post-it personal del usuario (su nota actual en el corcho).</summary>
    public string? NoteText { get; set; }
    public DateTimeOffset? NoteAt { get; set; }
}
