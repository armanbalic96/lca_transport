namespace TransportGwp.Domain.Entities
{
    /// <summary>A saved transport calculation in the register.</summary>
    public class Transport
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        /// <summary>Serialized <see cref="Models.TransportState"/>; null until the wizard has been opened.</summary>
        public string? StateJson { get; set; }

        // Denormalised summary for the register list
        public string? FromLabel { get; set; }
        public string? ToLabel { get; set; }
        public string? Mode { get; set; }
        public string? ModeLabel { get; set; }
        public double? MassTonnes { get; set; }
        public double? Gwp { get; set; }
        public double? DistanceKm { get; set; }
        public int MaxReached { get; set; }
    }
}
