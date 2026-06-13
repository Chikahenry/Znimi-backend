using LoanApplication.Dtos;
using LoanApplication.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanApplication.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoansController : ControllerBase
    {
        private readonly ILoanService _loanService;
        private readonly ILoanCalculatorService _calculatorService;

        public LoansController(ILoanService loanService, ILoanCalculatorService calculatorService)
        {
            _loanService = loanService;
            _calculatorService = calculatorService;
        }

        

        [HttpPost("calculate")]
        [AllowAnonymous] // Public endpoint
        public IActionResult CalculateLoan([FromBody] LoanCalculatorRequest request)
        {
            try
            {
                var result = _calculatorService.CalculateLoan(request);
                return Ok(new
                {
                    success = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("quick-quote")]
        [AllowAnonymous]
        public IActionResult QuickQuote(
            [FromQuery] decimal amount,
            [FromQuery] int months,
            [FromQuery] decimal? phase1Rate = 5.0m,
            [FromQuery] decimal? phase2Rate = 3.0m)
        {
            try
            {
                var request = new LoanCalculatorRequest
                {
                    LoanAmount = amount,
                    PeriodInMonths = months,
                    Phase1InterestRate = phase1Rate,
                    Phase2InterestRate = phase2Rate
                };

                var result = _calculatorService.CalculateLoan(request);

                return Ok(new
                {
                    success = true,
                    loanAmount = result.LoanAmount,
                    totalInterest = result.TotalInterest,
                    totalRepayment = result.TotalRepayment,
                    monthlyAverage = result.MonthlyAveragePayment,
                    effectiveRate = result.Summary.EffectiveInterestRate
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    
        [HttpPost("application")]
        public async Task<IActionResult> CreateApplication([FromBody] CreateLoanApplicationRequest request)
        {
            var userId = 1; // Get from authenticated user
            var loan = await _loanService.CreateLoanApplication(request, userId);
            return Ok(loan);
        }

        [HttpPost("approve")]
        public async Task<IActionResult> ApproveLoan([FromBody] ApproveLoanRequest request)
        {
            var userId = 1; // Get from authenticated user
            var loan = await _loanService.ApproveLoan(request, userId);
            return Ok(loan);
        }

        [HttpPost("disburse")]
        public async Task<IActionResult> DisburseLoan([FromBody] DisburseLoanRequest request)
        {
            var loan = await _loanService.DisburseLoan(request);
            return Ok(loan);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetLoanDetails(int id)
        {
            var loan = await _loanService.GetLoanDetails(id);
            return Ok(loan);
        }

        [HttpPost("filter")]
        public async Task<IActionResult> GetLoansWithFilters([FromBody] LoanFilterRequest filter)
        {
            var loans = await _loanService.GetLoansWithFilters(filter);
            return Ok(loans);
        }

        [HttpPost("update-statuses")]
        public async Task<IActionResult> UpdateLoanStatuses()
        {
            await _loanService.UpdateLoanStatuses();
            return Ok();
        }
    }
}
