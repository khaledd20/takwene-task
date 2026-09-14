namespace Takwene.Domain.Entities;

public class Artist
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;

    public ICollection<Track> Tracks { get; set; } = new List<Track>();
}
