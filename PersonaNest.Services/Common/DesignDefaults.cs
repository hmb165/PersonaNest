namespace PersonaNest.Services.Common;

/// <summary>
/// Fallback accent, used when a user has neither a custom <c>AccentColor</c> nor a selected
/// <see cref="PersonaNest.Domain.Entities.Theme"/>. Matches the "Electric Violet" theme seeded as
/// <c>IsDefault</c>, and the design system's <c>--primary</c> token.
/// <para>
/// Held as a constant rather than looked up per request: a projection cannot run a second query,
/// and this value is fixed by the design system.
/// </para>
/// </summary>
public static class DesignDefaults
{
    public const string AccentColor = "#7c5cfc";
}
