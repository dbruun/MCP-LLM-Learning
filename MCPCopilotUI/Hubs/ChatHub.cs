using Microsoft.AspNetCore.SignalR;
using MCPCopilotUI.Services;

namespace MCPCopilotUI.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(IChatService chatService, ILogger<ChatHub> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        public async Task SendMessage(string user, string message)
        {
            _logger.LogInformation($"Received message from {user}: {message}");
            
            try
            {
                // Echo the user message first
                await Clients.All.SendAsync("ReceiveMessage", user, message, "user");
                
                // Send typing indicator
                await Clients.All.SendAsync("TypingIndicator", true);
                
                // Get bot response
                var botResponse = await _chatService.GetResponseAsync(message);
                
                // Send bot response
                await Clients.All.SendAsync("TypingIndicator", false);
                await Clients.All.SendAsync("ReceiveMessage", "Copilot", botResponse, "bot");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
                await Clients.All.SendAsync("TypingIndicator", false);
                await Clients.All.SendAsync("ReceiveMessage", "Copilot", "I'm sorry, I encountered an error processing your request.", "error");
            }
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation($"Client connected: {Context.ConnectionId}");
            await Clients.Caller.SendAsync("ReceiveMessage", "Copilot", "Hello! I'm your AI assistant. How can I help you today?", "bot");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
            await base.OnDisconnectedAsync(exception);
        }
    }
}
