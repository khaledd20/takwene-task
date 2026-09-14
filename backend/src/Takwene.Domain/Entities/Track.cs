using Takwene.Domain.Enums;

namespace Takwene.Domain.Entities;

public class Track
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public Guid ArtistId { get; set; }
    public string Isrc { get; set; } = string.Empty;
    public DateTime ReleaseDate { get; set; }
    public string Genre { get; set; } = string.Empty;
    public TrackStatus Status { get; set; } = TrackStatus.Draft;

    // Navigation properties
    public Artist? Artist { get; set; }
    public ICollection<TrackDistribution> Distributions { get; set; } = new List<TrackDistribution>();
}
