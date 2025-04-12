namespace VehicleRental.Models.AdminModels
{
    public class ApprovalActionModel
    {
        public int TargetId { get; set; }
        public string TargetType { get; set; } // "Seller" or "Vehicle"
        public string Action { get; set; } // "Approve", "Reject", "Suspend", "UnderObservation"
        public string Notes { get; set; }
    }
}
