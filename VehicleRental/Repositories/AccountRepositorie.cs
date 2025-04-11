using Dapper;
using System.Data;
using VehicleRental.Infrastructure;
using VehicleRental.Models.AccountModels;
using VehicleRental.Repositories.IRepositories;

namespace VehicleRental.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly IDatabaseHelper _databaseHelper;

        public AccountRepository(IDatabaseHelper databaseHelper)
        {
            _databaseHelper = databaseHelper;
        }

        public async Task<UserMaster> RegisterUserAsync(UserMaster user, string password)
        {
            using (var connection = _databaseHelper.GetConnection)
            {
                

                
                try
                {
                    connection.Open(); // ✅ Now this is valid since it's not opened earlier

                    // Hash the password
                    user.Password = PasswordHelper.HashPassword(password);

                    // Set defaults
                    user.Address ??= "Not specified";
                    user.Birthdate = user.Birthdate == default ? DateTime.Now.AddYears(-18) : user.Birthdate;
                    user.Contact_No ??= "0000000000";
                    user.CreatedDate = DateTime.Now;
                    user.IsActive = true;

                    // Check if email exists first
                    var emailExists = await connection.ExecuteScalarAsync<bool>(
                        "SELECT CAST(CASE WHEN EXISTS (SELECT 1 FROM UserMaster WHERE Email = @Email) THEN 1 ELSE 0 END AS BIT)",
                        new { user.Email });

                    if (emailExists)
                    {
                        throw new Exception("Email address is already registered");
                    }

                    var parameters = new DynamicParameters();
                    parameters.Add("@Full_name", user.Full_name);
                    parameters.Add("@Password", user.Password);
                    parameters.Add("@Address", user.Address);
                    parameters.Add("@Birthdate", user.Birthdate);
                    parameters.Add("@Contact_No", user.Contact_No);
                    parameters.Add("@Email", user.Email);
                    parameters.Add("@RoleName", "User");
                    parameters.Add("@ReturnValue", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

                    // Execute procedure
                    await connection.ExecuteAsync(
                        "usp_RegisterUser",
                        parameters,
                        commandType: CommandType.StoredProcedure);

                    var newUserId = parameters.Get<int>("@ReturnValue");

                    if (newUserId > 0)
                    {
                        user.U_Id = newUserId;
                        return user;
                    }

                    // Handle specific error codes
                    switch (newUserId)
                    {
                        case -1:
                            throw new Exception("Email already exists");
                        case -2:
                            throw new Exception("Invalid role specified");
                        case -3:
                            throw new Exception("Database error during registration");
                        default:
                            throw new Exception("Registration failed");
                    }
                }
                catch (Exception ex)
                {
                    // Log error (implement proper logging)
                    Console.WriteLine($"Registration error: {ex.Message}");
                    throw;
                }
            }
        }
        public async Task<UserMaster> AuthenticateUserAsync(string email, string password)
        {
            using (var connection = _databaseHelper.GetConnection)
            {
                var user = await GetUserByEmailAsync(email);
                if (user == null) return null;

                // Verify the hashed password
                if (PasswordHelper.VerifyPassword(password, user.Password))
                {
                    // Update last login date
                    await connection.ExecuteAsync(
                        "UPDATE UserMaster SET LastLoginDate = GETDATE() WHERE U_Id = @U_Id",
                        new { user.U_Id });

                    return user;
                }

                return null;
            }
        }

        public async Task<UserRole> GetUserRoleAsync(string email)
        {
            using (var connection = _databaseHelper.GetConnection)
            {
                var user = await GetUserByEmailAsync(email);
                if (user == null) return null;

                return await connection.QueryFirstOrDefaultAsync<UserRole>(
                    "SELECT * FROM UserRole WHERE RoleId = @RoleId",
                    new { RoleId = user.RoleId });
            }
        }

        public async Task<UserMaster> GetUserByEmailAsync(string email)
        {
            using (var connection = _databaseHelper.GetConnection)
            {
                return await connection.QueryFirstOrDefaultAsync<UserMaster>(
                    "SELECT * FROM UserMaster WHERE Email = @Email",
                    new { Email = email });
            }
        }

    }
}