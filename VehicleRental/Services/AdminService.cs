using Dapper;
using System.Data;
using VehicleRental.Infrastructure;
using VehicleRental.Models.AdminModels;
using VehicleRental.Repositories.IRepositories;
using VehicleRental.Services.IServices;

namespace VehicleRental.Services
{
    public class AdminService : IAdminService
    {
        private readonly IDatabaseHelper _databaseHelper;

        public AdminService(IDatabaseHelper databaseHelper)
        {
            _databaseHelper = databaseHelper;
        }

        public async Task<AdminDashboardViewModel> GetDashboardData()
        {
            using (var connection = _databaseHelper.GetConnection)
            {
                var dashboardData = new AdminDashboardViewModel();

                dashboardData.PendingSellerCount = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM UserMaster WHERE RoleId = (SELECT RoleId FROM UserRole WHERE RoleName = 'Seller') AND Status = 'Pending'");

                dashboardData.PendingVehicleCount = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Vehicles WHERE Status = 'Pending'");

                dashboardData.PendingRentalRequestCount = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM RentalRequests WHERE Status = 'Pending'");

                dashboardData.ActiveUsersCount = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM UserMaster WHERE IsActive = 1");

                dashboardData.TotalVehiclesCount = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Vehicles");

                dashboardData.RecentActivities = (await connection.QueryAsync<RecentActivity>(
                    @"SELECT TOP 5 a.ActionType AS Action, a.ActionDate AS Date, u.Email AS UserEmail
                      FROM AdminActions a
                      JOIN UserMaster u ON a.TargetId = u.U_Id
                      ORDER BY a.ActionDate DESC")).ToList();

                dashboardData.RecentlyApprovedVehicles = (await connection.QueryAsync<RecentlyApprovedVehicle>(
                    @"SELECT TOP 2 v.VehicleId, v.Make, v.Model, v.Year, v.ApprovedDate AS ApprovalDate
                      FROM Vehicles v
                      WHERE v.Status = 'Approved'
                      ORDER BY v.ApprovedDate DESC")).ToList();

                foreach (var vehicle in dashboardData.RecentlyApprovedVehicles)
                {
                    vehicle.ImageUrls = (await connection.QueryAsync<string>(
                        "SELECT TOP 3 ImageUrl FROM VehicleImages WHERE VehicleId = @VehicleId",
                        new { VehicleId = vehicle.VehicleId })).ToList();
                }

                return dashboardData;
            }
        }

        public async Task<List<SellerApprovalModel>> GetPendingSellers()
        {
            using (var connection = _databaseHelper.GetConnection)
            {
                var sellers = await connection.QueryAsync<SellerApprovalModel>(
                    @"SELECT u.U_Id AS UserId, u.Full_name AS FullName, u.Email, u.CreatedDate AS RegistrationDate, 
                             u.Status, u.Address AS CompanyName, u.Contact_No AS TaxId
                      FROM UserMaster u
                      JOIN UserRole r ON u.RoleId = r.RoleId
                      WHERE r.RoleName = 'Seller' AND u.Status = 'Pending'");

                return sellers.ToList();
            }
        }

        public async Task<List<VehicleApprovalModel>> GetPendingVehicles()
        {
            using (var connection = _databaseHelper.GetConnection)
            {
                var vehicles = await connection.QueryAsync<VehicleApprovalModel>(
                    @"SELECT v.VehicleId, v.Make, v.Model, v.Year, v.Status, v.CreatedDate AS SubmissionDate,
                             u.Full_name AS SellerName, u.Email AS SellerEmail
                      FROM Vehicles v
                      JOIN UserMaster u ON v.SellerId = u.U_Id
                      WHERE v.Status = 'Pending'");

                var vehicleList = vehicles.ToList();
                foreach (var vehicle in vehicleList)
                {
                    vehicle.DocumentUrls = (await connection.QueryAsync<string>(
                        "SELECT DocumentUrl FROM VehicleDocuments WHERE VehicleId = @VehicleId",
                        new { vehicle.VehicleId })).ToList();

                    vehicle.ImageUrls = (await connection.QueryAsync<string>(
                        "SELECT ImageUrl FROM VehicleImages WHERE VehicleId = @VehicleId",
                        new { vehicle.VehicleId })).ToList();
                }

                return vehicleList;
            }
        }

        public async Task<List<UserManagementModel>> GetAllUsers()
        {
            using (var connection = _databaseHelper.GetConnection)
            {
                var users = await connection.QueryAsync<UserManagementModel>(
                    @"SELECT u.U_Id AS UserId, u.Full_name AS FullName, u.Email, r.RoleName AS Role, 
                             u.CreatedDate AS RegistrationDate, u.LastLoginDate, u.IsActive
                      FROM UserMaster u
                      JOIN UserRole r ON u.RoleId = r.RoleId");

                return users.ToList();
            }
        }

        public async Task<List<RentalRequestModel>> GetRentalRequests()
        {
            using (var connection = _databaseHelper.GetConnection)
            {
                var requests = await connection.QueryAsync<RentalRequestModel>(
                    @"SELECT r.RequestId, r.VehicleId, v.Make, v.Model, v.Year, 
                             u1.Full_name AS CustomerName, u1.Email AS CustomerEmail,
                             u2.Full_name AS SellerName, u2.Email AS SellerEmail,
                             r.StartDate, r.EndDate, r.Status, r.RequestDate, r.TotalAmount
                      FROM RentalRequests r
                      JOIN Vehicles v ON r.VehicleId = v.VehicleId
                      JOIN UserMaster u1 ON r.CustomerId = u1.U_Id
                      JOIN UserMaster u2 ON v.SellerId = u2.U_Id
                      ORDER BY r.RequestDate DESC");

                return requests.ToList();
            }
        }

        public async Task<List<TransactionModel>> GetTransactions()
        {
            using (var connection = _databaseHelper.GetConnection)
            {
                var transactions = await connection.QueryAsync<TransactionModel>(
                    @"SELECT t.TransactionId, t.RequestId, t.Amount, t.TransactionDate, t.Status,
                             v.Make, v.Model, v.Year,
                             u.Full_name AS CustomerName, u.Email AS CustomerEmail
                      FROM RentalTransactions t
                      JOIN Vehicles v ON t.VehicleId = v.VehicleId
                      JOIN UserMaster u ON t.CustomerId = u.U_Id
                      ORDER BY t.TransactionDate DESC");

                return transactions.ToList();
            }
        }

        public async Task<bool> ProcessApproval(ApprovalActionModel model, int adminId)
        {
            using (var connection = _databaseHelper.GetConnection)
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string updateQuery = "";
                        string notes = model.Notes ?? "";

                        if (model.TargetType == "Seller")
                        {
                            updateQuery = "UPDATE UserMaster SET Status = @Status WHERE U_Id = @TargetId";

                            if (model.Action == "Approve")
                            {
                                await connection.ExecuteAsync(updateQuery,
                                    new { Status = "Approved", model.TargetId }, transaction);
                            }
                            else if (model.Action == "Reject")
                            {
                                await connection.ExecuteAsync(updateQuery,
                                    new { Status = "Rejected", model.TargetId }, transaction);
                            }
                            else if (model.Action == "Suspend")
                            {
                                await connection.ExecuteAsync(updateQuery,
                                    new { Status = "Suspended", model.TargetId }, transaction);
                            }
                        }
                        else if (model.TargetType == "Vehicle")
                        {
                            updateQuery = "UPDATE Vehicles SET Status = @Status";

                            if (model.Action == "Approve")
                            {
                                updateQuery += ", ApprovedDate = GETDATE(), ApprovedBy = @AdminId";
                            }
                            else if (model.Action == "Reject")
                            {
                                updateQuery += ", RejectionReason = @Notes";
                            }

                            updateQuery += " WHERE VehicleId = @TargetId";

                            await connection.ExecuteAsync(updateQuery,
                                new
                                {
                                    Status = model.Action == "UnderObservation" ? "UnderObservation" :
                                            model.Action == "Approve" ? "Approved" : "Rejected",
                                    model.TargetId,
                                    AdminId = adminId,
                                    model.Notes
                                }, transaction);
                        }

                        await connection.ExecuteAsync(
                            @"INSERT INTO AdminActions (AdminId, ActionType, TargetId, TargetType, ActionDate, Notes)
                              VALUES (@AdminId, @ActionType, @TargetId, @TargetType, GETDATE(), @Notes)",
                            new
                            {
                                AdminId = adminId,
                                ActionType = model.TargetType + model.Action,
                                model.TargetId,
                                model.TargetType,
                                model.Notes
                            }, transaction);

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

        public async Task<SellerApprovalModel> GetSellerDetails(int userId)
        {
            using (var connection = _databaseHelper.GetConnection)
            {
                var seller = await connection.QueryFirstOrDefaultAsync<SellerApprovalModel>(
                    @"SELECT u.U_Id AS UserId, u.Full_name AS FullName, u.Email, u.CreatedDate AS RegistrationDate, 
                             u.Status, u.Address AS CompanyName, u.Contact_No AS TaxId
                      FROM UserMaster u
                      WHERE u.U_Id = @UserId", new { UserId = userId });

                return seller;
            }
        }

        public async Task<VehicleApprovalModel> GetVehicleDetails(int vehicleId)
        {
            using (var connection = _databaseHelper.GetConnection)
            {
                var vehicle = await connection.QueryFirstOrDefaultAsync<VehicleApprovalModel>(
                    @"SELECT v.VehicleId, v.Make, v.Model, v.Year, v.Status, v.CreatedDate AS SubmissionDate,
                             v.Description, v.PricePerDay, v.IsAvailable, v.InsuranceDetails,
                             u.Full_name AS SellerName, u.Email AS SellerEmail
                      FROM Vehicles v
                      JOIN UserMaster u ON v.SellerId = u.U_Id
                      WHERE v.VehicleId = @VehicleId", new { VehicleId = vehicleId });

                if (vehicle != null)
                {
                    vehicle.DocumentUrls = (await connection.QueryAsync<string>(
                        "SELECT DocumentUrl FROM VehicleDocuments WHERE VehicleId = @VehicleId",
                        new { vehicle.VehicleId })).ToList();

                    vehicle.ImageUrls = (await connection.QueryAsync<string>(
                        "SELECT ImageUrl FROM VehicleImages WHERE VehicleId = @VehicleId",
                        new { vehicle.VehicleId })).ToList();
                }

                return vehicle;
            }
        }

        public async Task<RentalRequestModel> GetRentalRequestDetails(int requestId)
        {
            using (var connection = _databaseHelper.GetConnection)
            {
                var request = await connection.QueryFirstOrDefaultAsync<RentalRequestModel>(
                    @"SELECT r.RequestId, r.VehicleId, v.Make, v.Model, v.Year, 
                             u1.Full_name AS CustomerName, u1.Email AS CustomerEmail,
                             u2.Full_name AS SellerName, u2.Email AS SellerEmail,
                             r.StartDate, r.EndDate, r.Status, r.RequestDate, r.TotalAmount,
                             r.Notes, r.AdminNotes, r.PaymentStatus, r.PaymentMethod
                      FROM RentalRequests r
                      JOIN Vehicles v ON r.VehicleId = v.VehicleId
                      JOIN UserMaster u1 ON r.CustomerId = u1.U_Id
                      JOIN UserMaster u2 ON v.SellerId = u2.U_Id
                      WHERE r.RequestId = @RequestId", new { RequestId = requestId });

                return request;
            }
        }

        public async Task<TransactionDetailModel> GetTransactionDetails(int transactionId)
        {
            using (var connection = _databaseHelper.GetConnection)
            {
                var transaction = await connection.QueryFirstOrDefaultAsync<TransactionDetailModel>(
                    @"SELECT t.TransactionId, t.RequestId, t.Amount, t.TransactionDate, t.Status,
                             v.Make, v.Model, v.Year, v.VehicleId,
                             u.Full_name AS CustomerName, u.Email AS CustomerEmail, u.U_Id AS CustomerId,
                             r.StartDate, r.EndDate, r.PaymentMethod, r.PaymentStatus
                      FROM RentalTransactions t
                      JOIN RentalRequests r ON t.RequestId = r.RequestId
                      JOIN Vehicles v ON t.VehicleId = v.VehicleId
                      JOIN UserMaster u ON t.CustomerId = u.U_Id
                      WHERE t.TransactionId = @TransactionId", new { TransactionId = transactionId });

                return transaction;
            }
        }

        public async Task<bool> UpdateUser(int userId, string email, string role, bool isActive, int adminId)
        {
            using (var connection = _databaseHelper.GetConnection)
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var roleId = await connection.ExecuteScalarAsync<int>(
                            "SELECT RoleId FROM UserRole WHERE RoleName = @Role", new { Role = role }, transaction);

                        await connection.ExecuteAsync(
                            "UPDATE UserMaster SET RoleId = @RoleId, IsActive = @IsActive WHERE U_Id = @UserId",
                            new { RoleId = roleId, IsActive = isActive, UserId = userId }, transaction);

                        await connection.ExecuteAsync(
                            @"INSERT INTO AdminActions (AdminId, ActionType, TargetId, TargetType, ActionDate, Notes)
                              VALUES (@AdminId, @ActionType, @TargetId, @TargetType, GETDATE(), @Notes)",
                            new
                            {
                                AdminId = adminId,
                                ActionType = "UserUpdate",
                                TargetId = userId,
                                TargetType = "User",
                                Notes = $"Updated user {email} to role {role} and status {(isActive ? "Active" : "Inactive")}"
                            }, transaction);

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

        public async Task<bool> SuspendUser(int userId, int adminId)
        {
            using (var connection = _databaseHelper.GetConnection)
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        await connection.ExecuteAsync(
                            "UPDATE UserMaster SET IsActive = 0 WHERE U_Id = @UserId",
                            new { UserId = userId }, transaction);

                        await connection.ExecuteAsync(
                            @"INSERT INTO AdminActions (AdminId, ActionType, TargetId, TargetType, ActionDate, Notes)
                              VALUES (@AdminId, @ActionType, @TargetId, @TargetType, GETDATE(), @Notes)",
                            new
                            {
                                AdminId = adminId,
                                ActionType = "UserSuspend",
                                TargetId = userId,
                                TargetType = "User",
                                Notes = "User suspended"
                            }, transaction);

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

        public async Task<bool> ActivateUser(int userId, int adminId)
        {
            using (var connection = _databaseHelper.GetConnection)
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        await connection.ExecuteAsync(
                            "UPDATE UserMaster SET IsActive = 1 WHERE U_Id = @UserId",
                            new { UserId = userId }, transaction);

                        await connection.ExecuteAsync(
                            @"INSERT INTO AdminActions (AdminId, ActionType, TargetId, TargetType, ActionDate, Notes)
                              VALUES (@AdminId, @ActionType, @TargetId, @TargetType, GETDATE(), @Notes)",
                            new
                            {
                                AdminId = adminId,
                                ActionType = "UserActivate",
                                TargetId = userId,
                                TargetType = "User",
                                Notes = "User activated"
                            }, transaction);

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

        public async Task<bool> DeleteUser(int userId, int adminId)
        {
            using (var connection = _databaseHelper.GetConnection)
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var email = await connection.ExecuteScalarAsync<string>(
                            "SELECT Email FROM UserMaster WHERE U_Id = @UserId", new { UserId = userId }, transaction);

                        await connection.ExecuteAsync(
                            "DELETE FROM UserMaster WHERE U_Id = @UserId",
                            new { UserId = userId }, transaction);

                        await connection.ExecuteAsync(
                            @"INSERT INTO AdminActions (AdminId, ActionType, TargetId, TargetType, ActionDate, Notes)
                              VALUES (@AdminId, @ActionType, @TargetId, @TargetType, GETDATE(), @Notes)",
                            new
                            {
                                AdminId = adminId,
                                ActionType = "UserDelete",
                                TargetId = userId,
                                TargetType = "User",
                                Notes = $"Deleted user {email}"
                            }, transaction);

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

        public async Task<bool> ProcessRentalRequest(int requestId, string action, int adminId)
        {
            using (var connection = _databaseHelper.GetConnection)
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string status = action;
                        string updateQuery = "UPDATE RentalRequests SET Status = @Status WHERE RequestId = @RequestId";

                        if (action == "Complete")
                        {
                            await connection.ExecuteAsync(
                                "UPDATE Vehicles SET IsAvailable = 1 WHERE VehicleId = " +
                                "(SELECT VehicleId FROM RentalRequests WHERE RequestId = @RequestId)",
                                new { RequestId = requestId }, transaction);
                        }
                        else if (action == "Approve")
                        {
                            await connection.ExecuteAsync(
                                "UPDATE Vehicles SET IsAvailable = 0 WHERE VehicleId = " +
                                "(SELECT VehicleId FROM RentalRequests WHERE RequestId = @RequestId)",
                                new { RequestId = requestId }, transaction);
                        }

                        await connection.ExecuteAsync(updateQuery, new { Status = status, RequestId = requestId }, transaction);

                        if (action == "Approve")
                        {
                            var rentalRequest = await connection.QueryFirstOrDefaultAsync(
                                @"SELECT CustomerId, VehicleId, StartDate, EndDate, TotalAmount 
                                  FROM RentalRequests WHERE RequestId = @RequestId",
                                new { RequestId = requestId }, transaction);

                            await connection.ExecuteAsync(
                                @"INSERT INTO RentalTransactions 
                                  (RequestId, CustomerId, VehicleId, StartDate, EndDate, Amount, TransactionDate, Status)
                                  VALUES (@RequestId, @CustomerId, @VehicleId, @StartDate, @EndDate, @Amount, GETDATE(), 'Completed')",
                                new
                                {
                                    RequestId = requestId,
                                    rentalRequest.CustomerId,
                                    rentalRequest.VehicleId,
                                    rentalRequest.StartDate,
                                    rentalRequest.EndDate,
                                    Amount = rentalRequest.TotalAmount
                                }, transaction);
                        }

                        await connection.ExecuteAsync(
                            @"INSERT INTO AdminActions (AdminId, ActionType, TargetId, TargetType, ActionDate, Notes)
                              VALUES (@AdminId, @ActionType, @TargetId, @TargetType, GETDATE(), @Notes)",
                            new
                            {
                                AdminId = adminId,
                                ActionType = "Rental" + action,
                                TargetId = requestId,
                                TargetType = "RentalRequest",
                                Notes = $"Rental request {requestId} {action}d"
                            }, transaction);

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