// Services/IServices/ISellerService.cs
using VehicleRental.Models.SellerModels;

namespace VehicleRental.Services.IServices
{
    public interface ISellerService
    {
        Task<SellerDashboardModel> GetDashboardData(int sellerId);
        Task<List<VehicleListingModel>> GetSellerVehicles(int sellerId);
        Task<bool> AddVehicle(AddVehicleModel model, int sellerId);
        Task<bool> UpdateVehicleAvailability(int vehicleId, bool isAvailable);
        Task<List<RentalRequestModel>> GetRentalRequests(int sellerId);
        Task<bool> ProcessRentalRequest(ProcessRentalModel model, int sellerId);
    }
}