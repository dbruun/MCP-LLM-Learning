using Microsoft.AspNetCore.Mvc;
using MCPCopilotUI.Services;

namespace MCPCopilotUI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        public class ChatRequest { public string Input { get; set; } = string.Empty; }
        public class ChatResponse { public string Response { get; set; } = string.Empty; }

        [HttpPost]
        public async Task<ActionResult<ChatResponse>> Post([FromBody] ChatRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Input))
                return BadRequest(new { error = "Input is required" });

            var result = await _chatService.GetResponseAsync(request.Input);
            return Ok(new ChatResponse { Response = result });
        }
    }
}
