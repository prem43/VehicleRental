using VehicleRental.Models.AccountModels;

namespace VehicleRental.Services.IServices
{
    public interface IAccountService
    {
        Task<bool> RegisterUserAsync(RegisterModel model);
        Task<UserMaster> LoginUserAsync(LoginModel model);
        Task<string> GetUserRoleAsync(string email);
    }
}