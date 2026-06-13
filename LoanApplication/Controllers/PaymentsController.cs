using LoanApplication.Dtos;
using LoanApplication.Services;
using Microsoft.AspNetCore.Mvc;

namespace LoanApplication.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost]
        public async Task<IActionResult> RecordPayment([FromBody] RecordPaymentRequest request)
        {
            var userId = 1; // Get from authenticated user
            var payment = await _paymentService.RecordPayment(request, userId);
            return Ok(payment);
        }

        [HttpPost("filter")]
        public async Task<IActionResult> GetPaymentsWithFilters([FromBody] PaymentFilterRequest filter)
        {
            var payments = await _paymentService.GetPaymentsWithFilters(filter);
            return Ok(payments);
        }

        [HttpGet("{id}/receipt")]
        public async Task<IActionResult> GenerateReceipt(int id)
        {
            var receipt = await _paymentService.GenerateReceipt(id);
            return Ok(new { receipt });
        }
    }
}
