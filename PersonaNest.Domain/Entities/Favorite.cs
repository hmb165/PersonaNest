namespace PersonaNest.Domain.Entities;

/// <summary>
/// A user marking a Media item as a favourite (§19). Note the asymmetry with likes: users
/// favourite <em>media</em> but like other people's <em>entries</em>.
/// </summary>
public class Favorite
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int MediaId { get; set; }
    public Media Media { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
