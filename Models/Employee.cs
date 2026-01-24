namespace Nexus_Service_Marketing.Models
{
    using System.ComponentModel.DataAnnotations;

    public class Employee
    {
        [Key]
        public int EmployeeId { get; set; }

        public int Id { get; set; }   // Login User

        public string Designation { get; set; }
        public string ContactNo { get; set; }
    }

}
