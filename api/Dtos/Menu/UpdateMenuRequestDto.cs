
namespace api.Dtos.Menu
{
    public class UpdateMenuRequestDto
    {
        public string ItemName { get; set; } = string.Empty;
        public double Price { get; set; }
        public string ImageURL { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Stock { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}