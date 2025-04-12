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
        public List<string> DocumentUrls { get; set; }
        public List<string> ImageUrls { get; set; }
    }
}
