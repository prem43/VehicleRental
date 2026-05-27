using System.ComponentModel.DataAnnotations;

namespace VehicleRental.Models.SellerModels
{
    public class AddVehicleModel
    {
        
        [Required]
        public string Make { get; set; }
        [Required]
        public string Model { get; set; }
        [Range(1900, 2035)]
        public int Year { get; set; }
        [Required]
        public string Description { get; set; }
        [Range(1, 1000000)]
        public decimal DailyRate { get; set; }
        [Range(1, 365)]
        public int MinimumRentalDays { get; set; }
        public List<IFormFile> Images { get; set; } = new List<IFormFile>();
        public List<IFormFile> Documents { get; set; } = new List<IFormFile>();
    }

}
