using VehicleRental.Models.AdminModels;

namespace VehicleRental.Services.IServices
{
    public interface IAdminService
    {
        Task<AdminDashboardViewModel> GetDashboardData();
        Task<List<SellerApprovalModel>> GetPendingSellers();
        Task<List<VehicleApprovalModel>> GetPendingVehicles();
        Task<List<UserManagementModel>> GetAllUsers();
        Task<List<RentalRequestModel>> GetRentalRequests();
        Task<List<TransactionModel>> GetTransactions();
        Task<bool> ProcessApproval(ApprovalActionModel model, int adminId);
        Task<SellerApprovalModel> GetSellerDetails(int userId);
        Task<VehicleApprovalModel> GetVehicleDetails(int vehicleId);
        Task<RentalRequestModel> GetRentalRequestDetails(int requestId);
        Task<TransactionDetailModel> GetTransactionDetails(int transactionId);
        Task<bool> UpdateUser(int userId, string email, string role, bool isActive, int adminId);
        Task<bool> SuspendUser(int userId, int adminId);
        Task<bool> ActivateUser(int userId, int adminId);
        Task<bool> DeleteUser(int userId, int adminId);
        Task<bool> ProcessRentalRequest(int requestId, string action, int adminId);
    }
}