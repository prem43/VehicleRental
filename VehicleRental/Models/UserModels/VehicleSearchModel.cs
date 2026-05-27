namespace VehicleRental.Models.UserModels
{
    public class VehicleSearchModel
    {
        public string? Search { get; set; }
        public List<VehicleCardModel> Vehicles { get; set; } = new();
    }

    public class VehicleCardModel
    {
        public int VehicleId { get; set; }
        public string Make { get; set; } = "";
        public string Model { get; set; } = "";
        public int Year { get; set; }
        public string? Description { get; set; }
        public decimal DailyRate { get; set; }
        public int MinimumRentalDays { get; set; }
        public bool IsAvailable { get; set; }
        public string SellerName { get; set; } = "";
        public string SellerEmail { get; set; } = "";
        public List<string> ImageUrls { get; set; } = new();
    }
}
