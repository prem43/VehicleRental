using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VehicleRental.Models.AdminModels;
using VehicleRental.Services.IServices;

namespace VehicleRental.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        public async Task<IActionResult> Index()
        {
            var dashboardData = await _adminService.GetDashboardData();
            return View(dashboardData);
        }

        public async Task<IActionResult> PendingSellers()
        {
            var sellers = await _adminService.GetPendingSellers();
            return View(sellers);
        }

        public async Task<IActionResult> PendingVehicles()
        {
            var vehicles = await _adminService.GetPendingVehicles();
            return View(vehicles);
        }

        public async Task<IActionResult> UserManagement()
        {
            var users = await _adminService.GetAllUsers();
            return View(users);
        }
        [HttpGet]
        public async Task<IActionResult> GetPendingCounts()
        {
            var dashboardData = await _adminService.GetDashboardData();
            return Json(new
            {
                pendingSellers = dashboardData.PendingSellerCount,
                pendingVehicles = dashboardData.PendingVehicleCount
            });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessApproval(ApprovalActionModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var adminId = int.Parse(User.FindFirstValue("UserId"));
            var result = await _adminService.ProcessApproval(model, adminId);

            if (result)
            {
                return RedirectToAction(model.TargetType == "Seller" ? "PendingSellers" : "PendingVehicles");
            }

            return BadRequest("Approval processing failed");
        }
    }
}