namespace VehicleRental.Models.AdminModels
{
    public class RentalRequestModel
    {
        public int RequestId { get; set; }
        public int VehicleId { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public int Year { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string SellerName { get; set; }
        public string SellerEmail { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; }
        public DateTime RequestDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentStatus { get; set; }
        public string PaymentMethod { get; set; }
        public string Notes { get; set; }
        public string AdminNotes { get; set; }
    }
}