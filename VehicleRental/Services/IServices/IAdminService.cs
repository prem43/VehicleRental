using VehicleRental.Models.AdminModels;

namespace VehicleRental.Services.IServices
{
    public interface IAdminService
    {
        Task<AdminDashboardViewModel> GetDashboardData();
        Task<List<SellerApprovalModel>> GetPendingSellers();
        Task<List<VehicleApprovalModel>> GetPendingVehicles();
        Task<List<UserManagementModel>> GetAllUsers();
        Task<bool> ProcessApproval(ApprovalActionModel model, int adminId);
    }
}
