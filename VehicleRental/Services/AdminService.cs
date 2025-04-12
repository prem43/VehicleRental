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

                // Get counts
                dashboardData.PendingSellerCount = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM UserMaster WHERE RoleId = (SELECT RoleId FROM UserRole WHERE RoleName = 'Seller') AND Status = 'Pending'");

                dashboardData.PendingVehicleCount = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Vehicles WHERE Status = 'Pending'");

                dashboardData.ActiveUsersCount = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM UserMaster WHERE IsActive = 1");

                dashboardData.TotalVehiclesCount = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Vehicles");

                // Get recent activities
                dashboardData.RecentActivities = (await connection.QueryAsync<RecentActivity>(
                    @"SELECT TOP 5 a.ActionType AS Action, a.ActionDate AS Date, u.Email AS UserEmail
                      FROM AdminActions a
                      JOIN UserMaster u ON a.TargetId = u.U_Id
                      ORDER BY a.ActionDate DESC")).ToList();

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

                // Get documents and images for each vehicle
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

                        // Log the action
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
    }
}