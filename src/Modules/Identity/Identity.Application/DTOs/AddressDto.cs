
namespace Identity.Application.DTOs
{
    public class AddressDto
    {
        public int Id { get; set; }
        public string RecipientName { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string FullAddress { get; set; } = null!;
        public string? AddressDetail { get; set; }
        public string? Province { get; set; }
        public string? District { get; set; }
        public string? Commune { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool IsDefault { get; set; }
    }
}
