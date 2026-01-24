using System.ComponentModel.DataAnnotations;

namespace Nexus_Service_Marketing.Models
{
    public class User
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
        
        // NEW FIELDS
        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public string Gender { get; set; }   // Male / Female / Other

        public string Role { get; set; }// Admin / User / Account / Employee / Teachnical 

        public bool IsActive { get; set; } = true;
    }
}
