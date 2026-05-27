using VehicleRental.Models.AccountModels;
using VehicleRental.Repositories.IRepositories;
using VehicleRental.Services.IServices;

namespace VehicleRental.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountRepository;

        public AccountService(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        public async Task<string> GetUserRoleAsync(string email)
        {
            var role = await _accountRepository.GetUserRoleAsync(email);
            return role?.RoleName;
        }

        public async Task<UserMaster> LoginUserAsync(LoginModel model)
        {
            return await _accountRepository.AuthenticateUserAsync(model.Email, model.Password);
        }

        public async Task<bool> RegisterUserAsync(RegisterModel model)
        {
            try
            {
                var user = new UserMaster
                {
                    Full_name = $"{model.FirstName} {model.LastName}",
                    Email = model.Email,
                    Address = model.Role == "Seller" ? model.CompanyName ?? "Not specified" : "Not specified",
                    Birthdate = DateTime.Now.AddYears(-18), // Default 18 years ago
                    Contact_No = model.Role == "Seller" ? model.TaxId ?? "0000000000" : "0000000000",
                    Status = model.Role == "Seller" ? "Pending" : "Approved"
                };

                var result = await _accountRepository.RegisterUserAsync(user, model.Password);
                return result != null;
            }
            catch (Exception ex)
            {
                // Log error
                return false;
            }
        }
    }
}
