using System.ComponentModel.DataAnnotations;

namespace Nexus_Service_Marketing.Models
{
    public class Vendor
    {
        [Key]
        public int VendorId { get; set; }

        public string VendorName { get; set; }
        public string ContactNo { get; set; }
        public string Address { get; set; }
    }
}
