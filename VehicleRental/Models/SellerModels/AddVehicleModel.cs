using System.ComponentModel.DataAnnotations;

namespace VehicleRental.Models.SellerModels
{
    public class AddVehicleModel
    {
        
        [Required]
        public string Make { get; set; }
        [Required]
        public string Model { get; set; }
        [Range(1900, 2025)]
        public int Year { get; set; }
        public string Description { get; set; }
        public decimal DailyRate { get; set; }
        public int MinimumRentalDays { get; set; }
        public List<IFormFile> Images { get; set; } = new List<IFormFile>();
        public List<IFormFile> Documents { get; set; } = new List<IFormFile>();
    }

}
