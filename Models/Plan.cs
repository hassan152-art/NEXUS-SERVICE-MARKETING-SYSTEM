namespace Nexus_Service_Marketing.Models
{
    public class Plan
    {
        public int PlanId { get; set; }

        public string ConnectionType { get; set; }
        public string PlanName { get; set; }
        public string SpeedOrType { get; set; }
        public decimal Charges { get; set; }
        public int ValidityMonths { get; set; }

        // 🔗 Equipment Link
        public int EquipmentId { get; set; }
        public Equipment Equipment { get; set; }
    }

}
