
namespace Identity.Application.DTOs
{
    public class AddressDto
    {
        public int Id { get; set; }
        public string RecipientName { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string FullAddress { get; set; } = null!;
        public bool IsDefault { get; set; }
    }
}
