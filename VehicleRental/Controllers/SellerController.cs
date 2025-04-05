using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleRental.Models;

namespace VehicleRental.Controllers
{
    [Authorize(Roles = "Seller")]
    public class SellerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        // Add other seller-specific actions here
    }
}