using LoanApplication.Services;
using Microsoft.AspNetCore.Mvc;

namespace LoanApplication.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService;

        public AnalyticsController(IAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardSummary()
        {
            var summary = await _analyticsService.GetDashboardSummary();
            return Ok(summary);
        }

        [HttpGet("portfolio-health")]
        public async Task<IActionResult> GetPortfolioHealth()
        {
            var health = await _analyticsService.GetPortfolioHealth();
            return Ok(health);
        }

        [HttpGet("cash-flow")]
        public async Task<IActionResult> GetCashFlowProjection([FromQuery] int days = 30)
        {
            var projection = await _analyticsService.GetCashFlowProjection(days);
            return Ok(projection);
        }

        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenueReport([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
        {
            var report = await _analyticsService.GetRevenueReport(fromDate, toDate);
            return Ok(report);
        }

        [HttpGet("top-borrowers")]
        public async Task<IActionResult> GetTopPerformingBorrowers([FromQuery] int count = 10)
        {
            var borrowers = await _analyticsService.GetTopPerformingBorrowers(count);
            return Ok(borrowers);
        }

        [HttpGet("high-risk-borrowers")]
        public async Task<IActionResult> GetHighRiskBorrowers([FromQuery] int count = 10)
        {
            var borrowers = await _analyticsService.GetHighRiskBorrowers(count);
            return Ok(borrowers);
        }
    }
}
