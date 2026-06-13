using LoanApplication.Domain.Enums;

namespace LoanApplication.Dtos
{
    public class UpdateUserRequest
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public UserRole? Role { get; set; }
    }
}
