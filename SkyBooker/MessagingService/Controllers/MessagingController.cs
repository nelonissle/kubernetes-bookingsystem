using MessagingService.Messaging;
using Microsoft.AspNetCore.Mvc;

namespace MessagingService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessagingController : ControllerBase
    {

        public MessagingController()
        {
        }

        [HttpPost("send")]
        public IActionResult SendMessage([FromBody] string message)
        {
            Console.WriteLine("Nothing");
            return Ok("Message sent to RabbitMQ");
        }
    }
}