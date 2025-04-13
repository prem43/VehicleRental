namespace VehicleRental.Models.AdminModels
{
    public class AdminDashboardViewModel
    {
        public int PendingSellerCount { get; set; }
        public int PendingVehicleCount { get; set; }
        public int ActiveUsersCount { get; set; }
        public int TotalVehiclesCount { get; set; }
        public List<RecentActivity> RecentActivities { get; set; }
        public List<RecentlyApprovedVehicle> RecentlyApprovedVehicles { get; set; }
    }

    public class RecentlyApprovedVehicle
    {
        public int VehicleId { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public int Year { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public List<string> ImageUrls { get; set; }
    }
}
