using System.Data;

namespace VehicleRental.Infrastructure
{
    /// <summary>
    /// Implimenting interface for connection factory
    /// </summary>
    public interface IDatabaseHelper
    {
        /// <summary>
        /// Implimenting IDb connection with GetConnection method
        /// </summary>
        IDbConnection GetConnection { get; }
    }
}