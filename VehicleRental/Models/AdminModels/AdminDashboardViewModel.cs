namespace VehicleRental.Models.AdminModels
{
    public class AdminDashboardViewModel
    {
    
        public int PendingSellerCount { get; set; }
        public int PendingVehicleCount { get; set; }
        public int ActiveUsersCount { get; set; }
        public int TotalVehiclesCount { get; set; }
        public List<RecentActivity> RecentActivities { get; set; }
       
    }
}
