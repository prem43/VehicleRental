using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VehicleRental.Models.SellerModels;
using VehicleRental.Services.IServices;

namespace VehicleRental.Controllers
{
    [Authorize(Roles = "Seller")]
    public class SellerController : Controller
    {
        private readonly ISellerService _sellerService;
        private readonly int _sellerId;

        public SellerController(ISellerService sellerService, IHttpContextAccessor httpContextAccessor)
        {
            _sellerService = sellerService;
            _sellerId = int.Parse(httpContextAccessor.HttpContext.User.FindFirstValue("UserId"));
        }

        public async Task<IActionResult> Index()
        {
            var dashboardData = await _sellerService.GetDashboardData(_sellerId);
            return View(dashboardData);
        }

        public async Task<IActionResult> Vehicles()
        {
            var vehicles = await _sellerService.GetSellerVehicles(_sellerId);
            return View(vehicles);
        }

        [HttpGet]
        public IActionResult AddVehicle()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVehicle([FromForm] AddVehicleModel vehicleModel)
        {
            if (vehicleModel.Images == null || vehicleModel.Images.Count < 3)
            {
                ModelState.AddModelError(nameof(vehicleModel.Images), "Upload at least 3 vehicle images.");
            }

            if (vehicleModel.Documents == null || vehicleModel.Documents.Count == 0)
            {
                ModelState.AddModelError(nameof(vehicleModel.Documents), "Upload at least one vehicle document.");
            }

            if (!ModelState.IsValid)
            {
                return View(vehicleModel);
            }

            try
            {
                var result = await _sellerService.AddVehicle(vehicleModel, _sellerId);
                if (result)
                {
                    TempData["SuccessMessage"] = "Vehicle added successfully and submitted for admin approval.";
                    return RedirectToAction("Vehicles");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error: " + ex.Message);
            }

            return View(vehicleModel);
        }
        //public async Task<IActionResult> AddVehicle(AddVehicleModel model)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return View(model);
        //    }

        //    try
        //    {
        //        var result = await _sellerService.AddVehicle(model, _sellerId);
        //        if (result)
        //        {
        //            TempData["SuccessMessage"] = "Vehicle added successfully and submitted for approval!";
        //            return RedirectToAction("Vehicles");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        ModelState.AddModelError("", "An error occurred while adding the vehicle: " + ex.Message);
        //    }

        //    return View(model);
        //}

        [HttpPost]
        public async Task<IActionResult> ToggleAvailability(int vehicleId, bool isAvailable)
        {
            var result = await _sellerService.UpdateVehicleAvailability(vehicleId, isAvailable);
            return Json(new { success = result });
        }

        public async Task<IActionResult> RentalRequests()
        {
            var requests = await _sellerService.GetRentalRequests(_sellerId);
            return View(requests);
        }

        [HttpGet]
        public async Task<IActionResult> GetPendingRequestsCount()
        {
            var requests = await _sellerService.GetRentalRequests(_sellerId);
            return Json(new { count = requests.Count(x => x.Status == "Pending") });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessRentalRequest(ProcessRentalModel model)
        {
            if (model.Action == "Approve")
            {
                ModelState.Remove(nameof(model.Reason));
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Unable to process rental request. Please try again.";
                return RedirectToAction("RentalRequests");
            }

            var result = await _sellerService.ProcessRentalRequest(model, _sellerId);
            if (result)
            {
                TempData["SuccessMessage"] = $"Request {model.Action.ToLower()}ed successfully!";
                return RedirectToAction("RentalRequests");
            }

            return BadRequest("Failed to process the request");
        }
    }
}
