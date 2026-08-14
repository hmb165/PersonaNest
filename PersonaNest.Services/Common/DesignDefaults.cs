namespace PersonaNest.Services.Common;

/// <summary>
/// Fallback accent pair, used when a user has neither a custom <c>AccentColor</c> nor a selected
/// <see cref="PersonaNest.Domain.Entities.Theme"/>. Matches the "Electric Violet" theme seeded as
/// <c>IsDefault</c>, and the design system's <c>--primary</c> / <c>--primary-dim</c> tokens.
/// <para>
/// Held as constants rather than looked up per request: a projection cannot run a second query,
/// and this pair is fixed by the design system.
/// </para>
/// </summary>
public static class DesignDefaults
{
    public const string AccentColor = "#7c5cfc";
    public const string AccentColorDark = "#5b3fe8";
}
