
using System.ComponentModel.DataAnnotations;
namespace Nexus_Service_Marketing.Models
{


    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        [Required]
        public int CustomerId { get; set; } // FK to Customer
        public Customer Customer { get; set; }

        [Required]
        public string ConnectionType { get; set; } // Dialup / Broadband / Telephone

        [Required]
        public int PlanId { get; set; }
        public Plan Plan { get; set; }

        public string OrderCode { get; set; } // D0000000001

        public string Status { get; set; } // Pending, Approved, Completed

        public DateTime OrderDate { get; set; }

        public Connection? Connection { get; set; }
    }


}
