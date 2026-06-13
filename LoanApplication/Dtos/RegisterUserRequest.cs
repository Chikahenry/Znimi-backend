using LoanApplication.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace LoanApplication.Dtos
{
    public class RegisterUserRequest
    {
        [Required]
        public string FullName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, MinLength(8)]
        public string Password { get; set; }

        [Required]
        public UserRole Role { get; set; }

        public string PhoneNumber { get; set; }
    }
}
