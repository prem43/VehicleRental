namespace VehicleRental.Models.AdminModels
{
    public class SellerApprovalModel
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public DateTime RegistrationDate { get; set; }
        public string Status { get; set; }
        public string CompanyName { get; set; }
        public string TaxId { get; set; }
    }
}
