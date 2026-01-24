namespace Nexus_Service_Marketing.Models
{

        public class FeasibilityCheck
        {
            public int FeasibilityCheckId { get; set; }

            public int OrderId { get; set; }
            public Order Order { get; set; }

            public bool IsFeasible { get; set; }   // true = Yes, false = No
            public string Remarks { get; set; }

            public DateTime CheckedDate { get; set; }
            public string CheckedBy { get; set; }  // Technician username
        }
    

}
