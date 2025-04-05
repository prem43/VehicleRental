using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace VehicleRental.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; }

        [Required]
        [MaxLength(20)]
        public string Role { get; set; } // "Admin", "User", or "Seller"

        // Additional properties for Seller
        public string? CompanyName { get; set; }
        public string? TaxId { get; set; }
        public string? Address { get; set; }
    }
}