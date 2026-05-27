using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VehicleRental.Infrastructure;
using VehicleRental.Models.UserModels;
using VehicleRental.Services.IServices;

namespace VehicleRental.Controllers
{
    public class VehiclesController : Controller
    {
        private readonly IDatabaseHelper _databaseHelper;
        private readonly IFileStorageService _fileStorageService;

        public VehiclesController(IDatabaseHelper databaseHelper, IFileStorageService fileStorageService)
        {
            _databaseHelper = databaseHelper;
            _fileStorageService = fileStorageService;
        }

        public async Task<IActionResult> Index(string? search)
        {
            using var connection = _databaseHelper.GetConnection;
            var vehicles = (await connection.QueryAsync<VehicleCardModel>(
                @"SELECT v.VehicleId, v.Make, v.Model, v.Year, v.Description, v.DailyRate,
                         v.MinimumRentalDays, v.IsAvailable, u.Full_name AS SellerName, u.Email AS SellerEmail
                  FROM Vehicles v
                  JOIN UserMaster u ON v.SellerId = u.U_Id
                  WHERE v.Status = 'Approved'
                    AND (@Search IS NULL OR @Search = ''
                         OR v.Make LIKE '%' + @Search + '%'
                         OR v.Model LIKE '%' + @Search + '%')
                  ORDER BY v.IsAvailable DESC, v.CreatedDate DESC",
                new { Search = search })).ToList();

            foreach (var vehicle in vehicles)
            {
                vehicle.ImageUrls = (await connection.QueryAsync<string>(
                    "SELECT ImageUrl FROM VehicleImages WHERE VehicleId = @VehicleId",
                    new { vehicle.VehicleId })).ToList();
            }

            return View(new VehicleSearchModel { Search = search, Vehicles = vehicles });
        }

        public async Task<IActionResult> Details(int id)
        {
            var vehicle = await GetVehicle(id);
            if (vehicle == null)
            {
                return NotFound();
            }

            return View(vehicle);
        }

        [Authorize(Roles = "User")]
        [HttpGet]
        public async Task<IActionResult> RequestRental(int id)
        {
            var vehicle = await GetVehicle(id);
            if (vehicle == null || !vehicle.IsAvailable)
            {
                return NotFound();
            }

            return View(new RentalRequestCreateModel
            {
                VehicleId = id,
                Vehicle = vehicle,
                EstimatedAmount = CalculateAmount(vehicle.DailyRate, vehicle.MinimumRentalDays, DateTime.Today.AddDays(1), DateTime.Today.AddDays(2))
            });
        }

        [Authorize(Roles = "User")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestRental(RentalRequestCreateModel model)
        {
            var vehicle = await GetVehicle(model.VehicleId);
            if (vehicle == null || !vehicle.IsAvailable)
            {
                return NotFound();
            }

            if (model.EndDate <= model.StartDate)
            {
                ModelState.AddModelError(nameof(model.EndDate), "End date must be after start date.");
            }

            var days = Math.Max(1, (model.EndDate.Date - model.StartDate.Date).Days);
            if (days < vehicle.MinimumRentalDays)
            {
                ModelState.AddModelError(nameof(model.EndDate), $"Minimum rental is {vehicle.MinimumRentalDays} day(s).");
            }

            if (model.CustomerImage == null || model.CustomerImage.Length == 0)
            {
                ModelState.AddModelError(nameof(model.CustomerImage), "Customer photo is required.");
            }

            if (model.Documents == null || model.Documents.Count == 0)
            {
                ModelState.AddModelError(nameof(model.Documents), "Upload at least one document such as driving license or ID proof.");
            }

            if (!ModelState.IsValid)
            {
                model.Vehicle = vehicle;
                model.EstimatedAmount = CalculateAmount(vehicle.DailyRate, vehicle.MinimumRentalDays, model.StartDate, model.EndDate);
                return View(model);
            }

            var userId = int.Parse(User.FindFirstValue("UserId"));
            var customerImageUrl = await _fileStorageService.SaveFileAsync(model.CustomerImage!, "rental-customer-images");
            using var connection = _databaseHelper.GetConnection;
            connection.Open();
            using var transaction = connection.BeginTransaction();

            var requestId = await connection.ExecuteScalarAsync<int>(
                @"INSERT INTO RentalRequests
                  (VehicleId, SellerId, CustomerId, UserId, StartDate, EndDate, TotalAmount, Status, Notes,
                   PaymentStatus, CustomerImageUrl, IdProofNumber, RequestDate)
                  SELECT VehicleId, SellerId, @UserId, @UserId, @StartDate, @EndDate, @TotalAmount, 'Pending', @Notes,
                         'Unpaid', @CustomerImageUrl, @IdProofNumber, GETDATE()
                  FROM Vehicles
                  WHERE VehicleId = @VehicleId AND Status = 'Approved' AND IsAvailable = 1;
                  SELECT CAST(SCOPE_IDENTITY() AS INT);",
                new
                {
                    model.VehicleId,
                    UserId = userId,
                    model.StartDate,
                    model.EndDate,
                    TotalAmount = CalculateAmount(vehicle.DailyRate, vehicle.MinimumRentalDays, model.StartDate, model.EndDate),
                    model.Notes,
                    CustomerImageUrl = customerImageUrl,
                    model.IdProofNumber
                },
                transaction);

            foreach (var document in model.Documents)
            {
                if (document.Length == 0)
                {
                    continue;
                }

                var documentUrl = await _fileStorageService.SaveFileAsync(document, "rental-documents");
                await connection.ExecuteAsync(
                    @"INSERT INTO RentalRequestDocuments (RequestId, DocumentUrl, DocumentName)
                      VALUES (@RequestId, @DocumentUrl, @DocumentName)",
                    new { RequestId = requestId, DocumentUrl = documentUrl, DocumentName = document.FileName },
                    transaction);
            }

            transaction.Commit();

            TempData["SuccessMessage"] = "Rental request submitted. You can complete dummy payment after seller approval.";
            return RedirectToAction(nameof(MyRentals));
        }

        [Authorize(Roles = "User")]
        public async Task<IActionResult> MyRentals()
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            using var connection = _databaseHelper.GetConnection;
            var rentals = await connection.QueryAsync<MyRentalModel>(
                @"SELECT r.RequestId, v.Make + ' ' + v.Model AS VehicleName, s.Full_name AS SellerName,
                         r.StartDate, r.EndDate, r.TotalAmount, r.Status, r.PaymentStatus,
                         r.PaymentMethod, r.RequestDate
                  FROM RentalRequests r
                  JOIN Vehicles v ON r.VehicleId = v.VehicleId
                  JOIN UserMaster s ON r.SellerId = s.U_Id
                  WHERE r.CustomerId = @UserId
                  ORDER BY r.RequestDate DESC",
                new { UserId = userId });

            return View(rentals.ToList());
        }

        [Authorize(Roles = "User")]
        [HttpGet]
        public async Task<IActionResult> Payment(int requestId)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            using var connection = _databaseHelper.GetConnection;
            var payment = await connection.QueryFirstOrDefaultAsync<PaymentViewModel>(
                @"SELECT r.RequestId, v.Make + ' ' + v.Model AS VehicleName, s.Full_name AS SellerName,
                         r.StartDate, r.EndDate, r.TotalAmount, r.PaymentStatus
                  FROM RentalRequests r
                  JOIN Vehicles v ON r.VehicleId = v.VehicleId
                  JOIN UserMaster s ON r.SellerId = s.U_Id
                  WHERE r.RequestId = @RequestId AND r.CustomerId = @UserId AND r.Status = 'Approved'",
                new { RequestId = requestId, UserId = userId });

            if (payment == null)
            {
                TempData["ErrorMessage"] = "Payment is available only after seller approval.";
                return RedirectToAction(nameof(MyRentals));
            }

            return View(payment);
        }

        [Authorize(Roles = "User")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(int requestId, string paymentMethod = "Dummy Card")
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            using var connection = _databaseHelper.GetConnection;
            connection.Open();
            using var transaction = connection.BeginTransaction();

            var rental = await connection.QueryFirstOrDefaultAsync(
                @"SELECT RequestId, CustomerId, VehicleId, StartDate, EndDate, TotalAmount
                  FROM RentalRequests
                  WHERE RequestId = @RequestId AND CustomerId = @UserId AND Status = 'Approved' AND PaymentStatus <> 'Paid'",
                new { RequestId = requestId, UserId = userId },
                transaction);

            if (rental == null)
            {
                transaction.Rollback();
                TempData["ErrorMessage"] = "Payment is available only after seller approval.";
                return RedirectToAction(nameof(MyRentals));
            }

            await connection.ExecuteAsync(
                @"UPDATE RentalRequests
                  SET PaymentStatus = 'Paid', PaymentMethod = @PaymentMethod
                  WHERE RequestId = @RequestId",
                new { RequestId = requestId, PaymentMethod = paymentMethod },
                transaction);

            await connection.ExecuteAsync(
                @"INSERT INTO RentalTransactions
                  (RequestId, CustomerId, VehicleId, StartDate, EndDate, Amount, TransactionDate, Status, TransactionStatus, PaymentMethod, ReferenceNo)
                  VALUES (@RequestId, @CustomerId, @VehicleId, @StartDate, @EndDate, @Amount, GETDATE(), 'Completed', 'Completed', @PaymentMethod, @ReferenceNo)",
                new
                {
                    rental.RequestId,
                    rental.CustomerId,
                    rental.VehicleId,
                    rental.StartDate,
                    rental.EndDate,
                    Amount = rental.TotalAmount,
                    PaymentMethod = paymentMethod,
                    ReferenceNo = $"DUMMY-{DateTime.UtcNow:yyyyMMddHHmmss}"
                },
                transaction);

            transaction.Commit();
            TempData["SuccessMessage"] = "Dummy payment completed successfully.";
            return RedirectToAction(nameof(MyRentals));
        }

        private async Task<VehicleCardModel?> GetVehicle(int id)
        {
            using var connection = _databaseHelper.GetConnection;
            var vehicle = await connection.QueryFirstOrDefaultAsync<VehicleCardModel>(
                @"SELECT v.VehicleId, v.Make, v.Model, v.Year, v.Description, v.DailyRate,
                         v.MinimumRentalDays, v.IsAvailable, u.Full_name AS SellerName, u.Email AS SellerEmail
                  FROM Vehicles v
                  JOIN UserMaster u ON v.SellerId = u.U_Id
                  WHERE v.VehicleId = @Id AND v.Status = 'Approved'",
                new { Id = id });

            if (vehicle != null)
            {
                vehicle.ImageUrls = (await connection.QueryAsync<string>(
                    "SELECT ImageUrl FROM VehicleImages WHERE VehicleId = @VehicleId",
                    new { vehicle.VehicleId })).ToList();
            }

            return vehicle;
        }

        private static decimal CalculateAmount(decimal dailyRate, int minimumDays, DateTime startDate, DateTime endDate)
        {
            var days = Math.Max(minimumDays, Math.Max(1, (endDate.Date - startDate.Date).Days));
            return dailyRate * days;
        }
    }
}
