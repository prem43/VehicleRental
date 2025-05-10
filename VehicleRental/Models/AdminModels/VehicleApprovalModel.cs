namespace VehicleRental.Models.AdminModels
{
    public class VehicleApprovalModel
    {
        public int VehicleId { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public int Year { get; set; }
        public string SellerName { get; set; }
        public string SellerEmail { get; set; }
        public string Status { get; set; }
        public DateTime SubmissionDate { get; set; }
        public decimal PricePerDay { get; set; }
        public bool IsAvailable { get; set; }
        public string Description { get; set; }
        public string InsuranceDetails { get; set; }
        public List<string> DocumentUrls { get; set; } = new List<string>();
        public List<string> ImageUrls { get; set; } = new List<string>();
    }
}