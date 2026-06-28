namespace Cooperativa.Api.Auth;

public class JwtOptions
{
    public string Key { get; set; } = default!;
    public string Issuer { get; set; } = "cooperativa";
    public string Audience { get; set; } = "cooperativa-client";
    public int ExpiryDays { get; set; } = 30;
}
