namespace Takwene.Application.DTOs.Dsps;

public class DspDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class TrackDistributionDto
{
    public Guid Id { get; set; }
    public Guid TrackId { get; set; }
    public Guid DspId { get; set; }
    public string DspName { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public DateTime? ReviewedAt { get; set; }
}
