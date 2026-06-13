using LoanApplication.Services;
using Microsoft.AspNetCore.Mvc;

namespace LoanApplication.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class CommunicationsController : ControllerBase
    {
        private readonly ICommunicationService _communicationService;

        public CommunicationsController(ICommunicationService communicationService)
        {
            _communicationService = communicationService;
        }

        [HttpPost("schedule-reminders")]
        public async Task<IActionResult> ScheduleReminders()
        {
            await _communicationService.SchedulePaymentReminders();
            return Ok();
        }

        [HttpPost("send-sms")]
        public async Task<IActionResult> SendSMS([FromBody] dynamic request)
        {
            await _communicationService.SendSMS(request.borrowerId, request.message);
            return Ok();
        }
    }
}
