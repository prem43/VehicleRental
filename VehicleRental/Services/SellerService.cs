using Dapper;
using System.Data;
using VehicleRental.Infrastructure;
using VehicleRental.Models.SellerModels;
using VehicleRental.Repositories.IRepositories;
using VehicleRental.Services.IServices;

namespace VehicleRental.Services
{
    public class SellerService : ISellerService
    {

        private readonly IDatabaseHelper _databaseHelper;
        private readonly IFileStorageService _fileStorageService;

        public SellerService(IDatabaseHelper databaseHelper, IFileStorageService fileStorageService)
        {
            _databaseHelper = databaseHelper;
            _fileStorageService = fileStorageService;
        }

        public async Task<SellerDashboardModel> GetDashboardData(int sellerId)
        {
            using (var connection = _databaseHelper.GetConnection)
            {
                var dashboard = new SellerDashboardModel();

                // Get counts
                dashboard.TotalListings = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Vehicles WHERE SellerId = @sellerId", new { sellerId });

                dashboard.ActiveListings = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Vehicles WHERE SellerId = @sellerId AND IsAvailable = 1 AND Status = 'Approved'",
                    new { sellerId });

                dashboard.PendingApproval = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Vehicles WHERE SellerId = @sellerId AND Status = 'Pending'",
                    new { sellerId });

                dashboard.PendingRentalRequests = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM RentalRequests WHERE SellerId = @sellerId AND Status = 'Pending'",
                    new { sellerId });

                dashboard.ActiveRentals = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM RentalRequests WHERE SellerId = @sellerId AND Status = 'Approved' AND EndDate >= GETDATE()",
                    new { sellerId });

                dashboard.TotalEarnings = await connection.ExecuteScalarAsync<decimal>(
                    @"SELECT ISNULL(SUM(rt.Amount), 0) 
                      FROM RentalTransactions rt
                      JOIN RentalRequests rr ON rt.RequestId = rr.RequestId
                      WHERE rr.SellerId = @sellerId AND rt.TransactionStatus = 'Completed'",
                    new { sellerId });

                // Get recent requests
                dashboard.RecentRequests = (await connection.QueryAsync<RentalRequestModel>(
                    @"SELECT TOP 5 r.RequestId, r.VehicleId, v.Make + ' ' + v.Model AS VehicleName, 
                             r.UserId, u.Full_name AS UserName, u.Email AS UserEmail,
                             r.StartDate, r.EndDate, 
                             DATEDIFF(day, r.StartDate, r.EndDate) AS RentalDays,
                             r.TotalAmount, r.Status, r.RequestDate
                      FROM RentalRequests r
                      JOIN Vehicles v ON r.VehicleId = v.VehicleId
                      JOIN UserMaster u ON r.UserId = u.U_Id
                      WHERE r.SellerId = @sellerId
                      ORDER BY r.RequestDate DESC",
                    new { sellerId })).ToList();

                return dashboard;
            }
        }

        public async Task<List<VehicleListingModel>> GetSellerVehicles(int sellerId)
        {
            using (var connection = _databaseHelper.GetConnection)
            {
                var vehicles = await connection.QueryAsync<VehicleListingModel>(
                    @"SELECT v.VehicleId, v.Make, v.Model, v.Year, v.Description, 
                             v.DailyRate, v.IsAvailable, v.MinimumRentalDays, v.Status
                      FROM Vehicles v
                      WHERE v.SellerId = @sellerId
                      ORDER BY v.CreatedDate DESC",
                    new { sellerId });

                var vehicleList = vehicles.ToList();
                foreach (var vehicle in vehicleList)
                {
                    vehicle.ImageUrls = (await connection.QueryAsync<string>(
                        "SELECT ImageUrl FROM VehicleImages WHERE VehicleId = @VehicleId",
                        new { vehicle.VehicleId })).ToList();

                    vehicle.DocumentUrls = (await connection.QueryAsync<string>(
                        "SELECT DocumentUrl FROM VehicleDocuments WHERE VehicleId = @VehicleId",
                        new { vehicle.VehicleId })).ToList();
                }

                return vehicleList;
            }
        }

        public async Task<bool> AddVehicle(AddVehicleModel model, int sellerId)
        {
            using (var connection = _databaseHelper.GetConnection)
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Insert vehicle
                        var vehicleId = await connection.ExecuteScalarAsync<int>(
                            @"INSERT INTO Vehicles (SellerId, Make, Model, Year, Description, 
                                                     DailyRate, MinimumRentalDays, Status, CreatedDate)
                              VALUES (@SellerId, @Make, @Model, @Year, @Description, 
                                      @DailyRate, @MinimumRentalDays, 'Pending', GETDATE());
                              SELECT CAST(SCOPE_IDENTITY() as int)",
                            new
                            {
                                SellerId = sellerId,
                                model.Make,
                                model.Model,
                                model.Year,
                                model.Description,
                                model.DailyRate,
                                model.MinimumRentalDays
                            }, transaction);

                        // Upload and save images
                        foreach (var image in model.Images)
                        {
                            var imageUrl = await _fileStorageService.SaveFileAsync(image, "vehicle-images");
                            await connection.ExecuteAsync(
                                "INSERT INTO VehicleImages (VehicleId, ImageUrl) VALUES (@VehicleId, @ImageUrl)",
                                new { VehicleId = vehicleId, ImageUrl = imageUrl }, transaction);
                        }

                        // Upload and save documents
                        foreach (var doc in model.Documents)
                        {
                            var docUrl = await _fileStorageService.SaveFileAsync(doc, "vehicle-documents");
                            await connection.ExecuteAsync(
                                "INSERT INTO VehicleDocuments (VehicleId, DocumentUrl) VALUES (@VehicleId, @DocumentUrl)",
                                new { VehicleId = vehicleId, DocumentUrl = docUrl }, transaction);
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public async Task<bool> UpdateVehicleAvailability(int vehicleId, bool isAvailable)
        {
            using (var connection = _databaseHelper.GetConnection)
            {
                var affected = await connection.ExecuteAsync(
                    "UPDATE Vehicles SET IsAvailable = @isAvailable WHERE VehicleId = @vehicleId",
                    new { vehicleId, isAvailable });

                return affected > 0;
            }
        }

        public async Task<List<RentalRequestModel>> GetRentalRequests(int sellerId)
        {
            using (var connection = _databaseHelper.GetConnection)
            {
                var requests = await connection.QueryAsync<RentalRequestModel>(
                    @"SELECT r.RequestId, r.VehicleId, v.Make + ' ' + v.Model AS VehicleName, 
                             r.UserId, u.Full_name AS UserName, u.Email AS UserEmail,
                             r.StartDate, r.EndDate, 
                             DATEDIFF(day, r.StartDate, r.EndDate) AS RentalDays,
                             r.TotalAmount, r.Status, r.RequestDate
                      FROM RentalRequests r
                      JOIN Vehicles v ON r.VehicleId = v.VehicleId
                      JOIN UserMaster u ON r.UserId = u.U_Id
                      WHERE r.SellerId = @sellerId
                      ORDER BY r.Status, r.RequestDate DESC",
                    new { sellerId });

                return requests.ToList();
            }
        }

        public async Task<bool> ProcessRentalRequest(ProcessRentalModel model, int sellerId)
        {
            using (var connection = _databaseHelper.GetConnection)
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Update request status
                        string status = model.Action == "Approve" ? "Approved" : "Rejected";
                        var updateQuery = @"UPDATE RentalRequests 
                                           SET Status = @status, 
                                               ApprovalDate = CASE WHEN @status = 'Approved' THEN GETDATE() ELSE NULL END,
                                               RejectionReason = CASE WHEN @status = 'Rejected' THEN @reason ELSE NULL END
                                           WHERE RequestId = @requestId AND SellerId = @sellerId";

                        await connection.ExecuteAsync(updateQuery,
                            new
                            {
                                status,
                                reason = model.Reason,
                                requestId = model.RequestId,
                                sellerId
                            }, transaction);

                        // If approved, mark vehicle as unavailable during rental period
                        if (model.Action == "Approve")
                        {
                            await connection.ExecuteAsync(
                                "UPDATE Vehicles SET IsAvailable = 0 WHERE VehicleId = " +
                                "(SELECT VehicleId FROM RentalRequests WHERE RequestId = @requestId)",
                                new { requestId = model.RequestId }, transaction);
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
    }
}