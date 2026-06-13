using LoanApplication.Dtos;
using LoanApplication.Services;
using Microsoft.AspNetCore.Mvc;

namespace LoanApplication.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BorrowersController : ControllerBase
    {
        private readonly IBorrowerService _borrowerService;

        public BorrowersController(IBorrowerService borrowerService)
        {
            _borrowerService = borrowerService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateBorrower([FromBody] CreateBorrowerRequest request)
        {
            var borrower = await _borrowerService.CreateBorrower(request);
            return Ok(borrower);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBorrower(int id, [FromBody] CreateBorrowerRequest request)
        {
            var borrower = await _borrowerService.UpdateBorrower(id, request);
            return Ok(borrower);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBorrowerDetails(int id)
        {
            var borrower = await _borrowerService.GetBorrowerDetails(id);
            return Ok(borrower);
        }

        [HttpPost("filter")]
        public async Task<IActionResult> GetBorrowersWithFilters([FromBody] BorrowerFilterRequest filter)
        {
            var borrowers = await _borrowerService.GetBorrowersWithFilters(filter);
            return Ok(borrowers);
        }

        [HttpGet("{borrowerId}/statement/{loanId}")]
        public async Task<IActionResult> GenerateStatement(int borrowerId, int loanId)
        {
            var statement = await _borrowerService.GenerateBorrowerStatement(borrowerId, loanId);
            return Ok(statement);
        }

        [HttpGet("{id}/loans")]
        public async Task<IActionResult> GetBorrowerLoanHistory(int id)
        {
            var loans = await _borrowerService.GetBorrowerLoanHistory(id);
            return Ok(loans);
        }

        [HttpPost("{id}/update-credit-score")]
        public async Task<IActionResult> UpdateCreditScore(int id)
        {
            await _borrowerService.UpdateBorrowerCreditScore(id);
            return Ok();
        }
    }
}
