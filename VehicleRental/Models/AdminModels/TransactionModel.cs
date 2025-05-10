namespace VehicleRental.Models.AdminModels
{
    public class TransactionModel
    {
        public int TransactionId { get; set; }
        public int RequestId { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public int Year { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Status { get; set; }
    }
}