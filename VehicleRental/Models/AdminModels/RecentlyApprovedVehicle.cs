namespace VehicleRental.Models.AdminModels
{
    public class RecentlyApprovedVehicle
    {
        public string Make { get; set; }
        public string Model { get; set; }
        public int Year { get; set; }
        public string PrimaryImageUrl { get; set; }
        public DateTime? ApprovedDate { get; set; }
    }
}
