using System.ComponentModel.DataAnnotations;

namespace ApiFinalProject.DTO.Account
{
    public class RegisterModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
        [Compare("Password")]
        public string PasswordConfirmed { get; set; }
        public string address { get; set; }
       public string Role { get; set; } // "Student" or "Teacher"


        // Common fields
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public byte? experienceAge { get; set; }

        // Teacher-specific field
        public int? SpecializationId { get; set; } // Reference to the Specialization table

        // Student-specific fields
        public string? Grade { get; set; }
        [Required]
        // Image file
       // public IFormFile Image { get; set; } = default!; // Image upload
        public IFormFile image { get; set; } = default!;

    }

}
