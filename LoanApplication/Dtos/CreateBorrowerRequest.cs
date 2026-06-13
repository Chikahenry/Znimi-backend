namespace LoanApplication.Dtos
{
    public class CreateBorrowerRequest
    {
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string AlternatePhoneNumber { get; set; }
        public string Email { get; set; }
        public string NationalIdNumber { get; set; }
        public string HomeAddress { get; set; }
        public string EmployerOrBusiness { get; set; }
        public string GuarantorName { get; set; }
        public string GuarantorPhone { get; set; }
    }
}
