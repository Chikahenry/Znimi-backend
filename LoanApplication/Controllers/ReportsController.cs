using LoanApplication.Dtos;
using LoanApplication.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanApplication.Controllers
{
    /// <summary>
    /// Admin reporting endpoints.
    /// All routes require authentication. Restrict to Owner/Admin roles as needed.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        /// <summary>
        /// Loans disbursed in a date range, filterable by officer, amount range.
        /// Includes per-officer breakdown summary.
        /// GET /api/reports/disbursements?fromDate=2026-01-01&toDate=2026-06-30&officerId=2&pageNumber=1&pageSize=50
        /// </summary>
        [HttpGet("disbursements")]
        public async Task<IActionResult> GetDisbursementReport(
            [FromQuery] DateTime? fromDate    = null,
            [FromQuery] DateTime? toDate      = null,
            [FromQuery] int?      officerId   = null,
            [FromQuery] decimal?  minAmount   = null,
            [FromQuery] decimal?  maxAmount   = null,
            [FromQuery] int       pageNumber  = 1,
            [FromQuery] int       pageSize    = 50)
        {
            try
            {
                var request = new DisbursementReportRequest
                {
                    FromDate   = fromDate,
                    ToDate     = toDate,
                    OfficerId  = officerId,
                    MinAmount  = minAmount,
                    MaxAmount  = maxAmount,
                    PageNumber = pageNumber,
                    PageSize   = pageSize
                };

                var result = await _reportService.GetDisbursementReport(request);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Overdue aging report — groups all overdue loans into buckets:
        /// 1–30 days, 31–60 days, 61–90 days, 90+ days.
        /// Includes PAR % per bucket and full loan + borrower + guarantor details.
        /// GET /api/reports/overdue-aging
        /// </summary>
        [HttpGet("overdue-aging")]
        public async Task<IActionResult> GetOverdueAgingReport()
        {
            try
            {
                var result = await _reportService.GetOverdueAgingReport();
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Collector/officer performance — payments recorded per staff member in a date range.
        /// Shows total collected, breakdown by principal/interest/penalties, unique borrowers served.
        /// GET /api/reports/collector-performance?fromDate=2026-01-01&toDate=2026-06-30&officerId=2
        /// </summary>
        [HttpGet("collector-performance")]
        public async Task<IActionResult> GetCollectorPerformance(
            [FromQuery] DateTime? fromDate   = null,
            [FromQuery] DateTime? toDate     = null,
            [FromQuery] int?      officerId  = null,
            [FromQuery] int       pageNumber = 1,
            [FromQuery] int       pageSize   = 50)
        {
            try
            {
                var request = new CollectorPerformanceRequest
                {
                    FromDate   = fromDate,
                    ToDate     = toDate,
                    OfficerId  = officerId,
                    PageNumber = pageNumber,
                    PageSize   = pageSize
                };

                var result = await _reportService.GetCollectorPerformance(request);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Full borrower activity — profile, loan history, all payments, audit trail.
        /// Optionally filter by date range to scope the activity window.
        /// GET /api/reports/borrower-activity/5?fromDate=2026-01-01&toDate=2026-06-30
        /// </summary>
        [HttpGet("borrower-activity/{borrowerId:int}")]
        public async Task<IActionResult> GetBorrowerActivity(
            int borrowerId,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate   = null)
        {
            try
            {
                var request = new BorrowerActivityRequest
                {
                    FromDate = fromDate,
                    ToDate   = toDate
                };

                var result = await _reportService.GetBorrowerActivity(borrowerId, request);
                return Ok(new { success = true, data = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}
