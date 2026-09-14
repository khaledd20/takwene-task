using Takwene.Domain.Enums;

namespace Takwene.Domain.Entities;

public class TrackDistribution
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TrackId { get; set; }
    public Guid DspId { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DistributionStatus Status { get; set; } = DistributionStatus.Pending;

    // Navigation properties
    public Track? Track { get; set; }
    public Dsp? Dsp { get; set; }
}
