using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Nexus_Service_Marketing.Models
{

    public class Equipment
    {
        [Key]
        public int EquipmentId { get; set; }

        public string EquipmentName { get; set; }   // Router, Modem etc
        public string EquipmentType { get; set; }   // Broadband / Dialup / Telephone

        public int VendorId { get; set; }
        [ForeignKey("VendorId")]
        public Vendor Vendor { get; set; }

        public decimal Price { get; set; }
        public int WarrantyMonths { get; set; }

        // 🔹 INVENTORY COLUMNS (NEW)
        public int TotalQuantity { get; set; }      // Admin add karega
        public int AvailableQuantity { get; set; }  // System auto manage karega
    }


}
