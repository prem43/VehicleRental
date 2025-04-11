using Microsoft.Extensions.Configuration;
using System.Data;
using Microsoft.Data.SqlClient;

namespace VehicleRental.Infrastructure
{
    public class DatabaseHelper : IDatabaseHelper
    {
        private readonly IConfiguration _configuration;

        public DatabaseHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IDbConnection GetConnection
        {
            get
            {
                var connectionString = _configuration["ConnectionStrings:IdentityDBLocal"];
                return new SqlConnection(connectionString); // ✅ Safe and modern
            }
        }
    }
}
