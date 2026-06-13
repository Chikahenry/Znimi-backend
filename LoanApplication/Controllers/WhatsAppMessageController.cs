using LoanApplication.Dtos;
using LoanApplication.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanApplication.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WhatsAppController : ControllerBase
    {
        private readonly IWhatsAppService _whatsAppService;

        public WhatsAppController(IWhatsAppService whatsAppService)
        {
            _whatsAppService = whatsAppService;
        }

        [HttpPost("send")]
       // [Authorize(Roles = "Owner,LoanOfficer")]
        public async Task<IActionResult> SendMessage([FromBody] SendWhatsAppRequest request)
        {
            try
            {
                var result = await _whatsAppService.SendMessage(
                    request.PhoneNumber,
                    request.Message,
                    request.MessageType
                );

                return Ok(new { success = result.Success, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("send-payment-reminder/{loanId}")]
        [Authorize(Roles = "Owner,LoanOfficer")]
        public async Task<IActionResult> SendPaymentReminder(int loanId)
        {
            try
            {
                var result = await _whatsAppService.SendPaymentReminder(loanId);
                return Ok(new { success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("send-statement/{borrowerId}/{loanId}")]
        [Authorize(Roles = "Owner,LoanOfficer")]
        public async Task<IActionResult> SendStatement(int borrowerId, int loanId)
        {
            try
            {
                var result = await _whatsAppService.SendStatementToWhatsApp(borrowerId, loanId);
                return Ok(new { success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("bulk-send")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> SendBulkMessages([FromBody] BulkWhatsAppRequest request)
        {
            try
            {
                var successCount = await _whatsAppService.SendBulkMessages(request);
                return Ok(new
                {
                    success = true,
                    totalSent = successCount,
                    totalRequested = request.BorrowerIds.Count
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("history/{borrowerId}")]
        [Authorize]
        public async Task<IActionResult> GetMessageHistory(int borrowerId)
        {
            try
            {
                var history = await _whatsAppService.GetMessageHistory(borrowerId);
                return Ok(new { success = true, data = history });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("webhook/status")]
        [AllowAnonymous]
        public async Task<IActionResult> TwilioWebhook([FromForm] TwilioStatusUpdate update)
        {
            try
            {
                await _whatsAppService.UpdateMessageStatus(update.MessageSid, update.MessageStatus);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    public class TwilioStatusUpdate
    {
        public string MessageSid { get; set; }
        public string MessageStatus { get; set; }
    }
}
