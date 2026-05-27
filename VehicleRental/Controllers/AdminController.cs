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

        public async Task<IActionResult> RentalRequests()
        {
            var requests = await _adminService.GetRentalRequests();
            return View(requests);
        }

        public async Task<IActionResult> Transactions()
        {
            var transactions = await _adminService.GetTransactions();
            return View(transactions);
        }

        [HttpGet]
        public async Task<IActionResult> GetPendingCounts()
        {
            var dashboardData = await _adminService.GetDashboardData();
            return Json(new
            {
                pendingSellers = dashboardData.PendingSellerCount,
                pendingVehicles = dashboardData.PendingVehicleCount,
                pendingRentalRequests = dashboardData.PendingRentalRequestCount
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetSellerDetails(int userId)
        {
            var seller = await _adminService.GetSellerDetails(userId);
            return PartialView("_SellerDetailsPartial", seller);
        }

        [HttpGet]
        public async Task<IActionResult> GetVehicleDetails(int vehicleId)
        {
            var vehicle = await _adminService.GetVehicleDetails(vehicleId);
            return PartialView("_VehicleDetailsPartial", vehicle);
        }

        [HttpGet]
        public async Task<IActionResult> GetRentalRequestDetails(int requestId)
        {
            var request = await _adminService.GetRentalRequestDetails(requestId);
            return PartialView("_RentalRequestDetailsPartial", request);
        }

        [HttpGet]
        public async Task<IActionResult> GetTransactionDetails(int transactionId)
        {
            var transaction = await _adminService.GetTransactionDetails(transactionId);
            return PartialView("_TransactionDetailsPartial", transaction);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUser(int userId, string email, string role, bool isActive)
        {
            var result = await _adminService.UpdateUser(userId, email, role, isActive, int.Parse(User.FindFirstValue("UserId")));
            return RedirectToAction("UserManagement");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuspendUser(int userId)
        {
            var result = await _adminService.SuspendUser(userId, int.Parse(User.FindFirstValue("UserId")));
            return RedirectToAction("UserManagement");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateUser(int userId)
        {
            var result = await _adminService.ActivateUser(userId, int.Parse(User.FindFirstValue("UserId")));
            return RedirectToAction("UserManagement");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            var result = await _adminService.DeleteUser(userId, int.Parse(User.FindFirstValue("UserId")));
            return RedirectToAction("UserManagement");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessRentalRequest(int requestId, string action)
        {
            var result = await _adminService.ProcessRentalRequest(requestId, action, int.Parse(User.FindFirstValue("UserId")));
            return Ok();
        }

    }
}
