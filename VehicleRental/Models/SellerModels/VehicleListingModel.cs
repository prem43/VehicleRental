// Models/SellerModels/VehicleListingModel.cs
namespace VehicleRental.Models.SellerModels
{
    public class VehicleListingModel
    {
        public int VehicleId { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public int Year { get; set; }
        public string Description { get; set; }
        public decimal DailyRate { get; set; }
        public bool IsAvailable { get; set; }
        public int MinimumRentalDays { get; set; }
        public string Status { get; set; }
        public List<string> ImageUrls { get; set; } = new List<string>();
        public List<string> DocumentUrls { get; set; } = new List<string>();
    }


    public class RentalRequestModel
    {
        public int RequestId { get; set; }
        public int VehicleId { get; set; }
        public string VehicleName { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int RentalDays { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public string PaymentStatus { get; set; }
        public string Notes { get; set; }
        public string CustomerImageUrl { get; set; }
        public string IdProofNumber { get; set; }
        public List<string> DocumentUrls { get; set; } = new List<string>();
        public DateTime RequestDate { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public string RejectionReason { get; set; } // Only applicable if rejected

    }

    public class ProcessRentalModel
    {
        public int RequestId { get; set; }
        public string Action { get; set; } = ""; // Approve or Reject
        public string? Reason { get; set; }
    }

    public class SellerDashboardModel
    {
        public int TotalListings { get; set; }
        public int ActiveListings { get; set; }
        public int PendingApproval { get; set; }
        public int PendingRentalRequests { get; set; }
        public int ActiveRentals { get; set; }
        public decimal TotalEarnings { get; set; }
        public List<RentalRequestModel> RecentRequests { get; set; } = new List<RentalRequestModel>();
    }
}
