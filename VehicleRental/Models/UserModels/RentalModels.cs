using System.ComponentModel.DataAnnotations;

namespace VehicleRental.Models.UserModels
{
    public class RentalRequestCreateModel
    {
        public int VehicleId { get; set; }
        public VehicleCardModel? Vehicle { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Today.AddDays(1);

        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; } = DateTime.Today.AddDays(2);

        [StringLength(1000)]
        public string? Notes { get; set; }

        [Required]
        [Display(Name = "ID Proof Number")]
        public string IdProofNumber { get; set; } = "";

        [Required]
        [Display(Name = "Customer Photo")]
        public IFormFile? CustomerImage { get; set; }

        [Display(Name = "Required Documents")]
        public List<IFormFile> Documents { get; set; } = new();

        public decimal EstimatedAmount { get; set; }
    }

    public class MyRentalModel
    {
        public int RequestId { get; set; }
        public string VehicleName { get; set; } = "";
        public string SellerName { get; set; } = "";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "";
        public string PaymentStatus { get; set; } = "";
        public string? PaymentMethod { get; set; }
        public DateTime RequestDate { get; set; }
    }

    public class PaymentViewModel
    {
        public int RequestId { get; set; }
        public string VehicleName { get; set; } = "";
        public string SellerName { get; set; } = "";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentStatus { get; set; } = "";
    }
}
