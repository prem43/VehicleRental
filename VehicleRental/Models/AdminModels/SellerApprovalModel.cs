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
        public string ProfileImage { get; set; }
        public string DrivingLicense { get; set; }
        public string AdditionalInfo { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public string Country { get; set; }

        // Add these missing properties
        public string TaxDocumentPath { get; set; }
        public string CompanyRegistrationPath { get; set; }
    }
}