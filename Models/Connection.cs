using System.ComponentModel.DataAnnotations;

namespace Nexus_Service_Marketing.Models
{
        public class Connection
        {
            public int ConnectionId { get; set; }

            [Required]
            [MaxLength(30)]
            public string AccountId { get; set; }
            public int OrderId { get; set; }
            public Order Order { get; set; }

            public string Status { get; set; }
            // Pending / Active / TempInactive / PermanentInactive

            public DateTime ActivatedDate { get; set; }
            public DateTime? DeactivatedDate { get; set; }

            public string Remarks { get; set; }
        }
    

}
