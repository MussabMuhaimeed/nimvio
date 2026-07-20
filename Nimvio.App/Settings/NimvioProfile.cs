namespace Nimvio;

internal sealed class NimvioProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public NimvioCharacterName Name { get; set; } = NimvioCharacterName.Nova;

    public NimvioPersonality Personality { get; set; } = NimvioPersonality.Curious;

    public int AccentArgb { get; set; } = Color.FromArgb(86, 221, 242).ToArgb();

    public float Energy { get; set; } = 72;
    public float Curiosity { get; set; } = 66;
    public float Boredom { get; set; } = 18;
    public float Happiness { get; set; } = 75;

    public List<Point> RecentPlaces { get; set; } = [];

    public string? FavoriteScreen { get; set; }

    public DateTime LastSeenUtc { get; set; } = DateTime.UtcNow;

    public DateTime LastInteractionUtc { get; set; } = DateTime.UtcNow;

    public Dictionary<string, float> Relationships { get; set; } = [];
    
    public string? FavoriteFriendId { get; set; }
}
