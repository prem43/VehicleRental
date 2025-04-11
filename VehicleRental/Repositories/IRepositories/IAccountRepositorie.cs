using VehicleRental.Models.AccountModels;

namespace VehicleRental.Repositories.IRepositories
{
    public interface IAccountRepository
    {
        Task<UserMaster> RegisterUserAsync(UserMaster user, string password);
        Task<UserMaster> AuthenticateUserAsync(string email, string password);
        Task<UserRole> GetUserRoleAsync(string email);
        Task<UserMaster> GetUserByEmailAsync(string email);
    }
}