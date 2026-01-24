using System.ComponentModel.DataAnnotations;

namespace Nexus_Service_Marketing.Models
{
    public class EquipmentIssue
    {
        [Key]
        public int IssueId { get; set; }
        public int CustomerId { get; set; }
        public int EquipmentId { get; set; }
        public int EmployeeId { get; set; }
        public DateTime IssueDate { get; set; }
        public string Status { get; set; } // Issued / Returned

        public Customer Customer { get; set; }
        public Equipment Equipment { get; set; }
        public User Employee { get; set; }

    }

}
