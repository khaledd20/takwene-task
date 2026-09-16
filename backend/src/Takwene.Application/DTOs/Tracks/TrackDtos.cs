using Takwene.Application.DTOs.Dsps;

namespace Takwene.Application.DTOs.Tracks;

public class TrackDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid ArtistId { get; set; }
    public string ArtistName { get; set; } = string.Empty;
    public string Isrc { get; set; } = string.Empty;
    public DateTime ReleaseDate { get; set; }
    public string Genre { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class TrackDetailDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid ArtistId { get; set; }
    public string ArtistName { get; set; } = string.Empty;
    public string ArtistEmail { get; set; } = string.Empty;
    public string ArtistCountry { get; set; } = string.Empty;
    public string Isrc { get; set; } = string.Empty;
    public DateTime ReleaseDate { get; set; }
    public string Genre { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<TrackDistributionDto> Distributions { get; set; } = new();
}

public class CreateTrackRequest
{
    public string Title { get; set; } = string.Empty;
    public Guid ArtistId { get; set; }
    public string Isrc { get; set; } = string.Empty;
    public DateTime ReleaseDate { get; set; }
    public string Genre { get; set; } = string.Empty;
    public string? Status { get; set; } = "draft";
}

public class UpdateTrackStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class DistributeTrackRequest
{
    public List<Guid> DspIds { get; set; } = new();
}

public class UpdateDistributionStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
}
