using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

namespace MCPCopilotUI.Services
{
    public interface IChatService
    {
        Task InitializeAsync();
        Task<string> GetResponseAsync(string userInput);
    }

    public class ChatService : IChatService
    {
        private readonly ILogger<ChatService> _logger;
        private readonly IConfiguration _configuration;
        private Kernel? _kernel;
        private IMcpClient? _mcpClient;
        private KernelFunction? _chatFunction;
        private string _history = "";

        public ChatService(ILogger<ChatService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Initializing ChatService with Semantic Kernel and MCP...");

                // Initialize Semantic Kernel
                var kernelBuilder = Kernel.CreateBuilder();
                kernelBuilder.Services.AddLogging(c => c.AddDebug().SetMinimumLevel(LogLevel.Trace));
                kernelBuilder.Services.AddAzureOpenAIChatCompletion(
                    deploymentName: _configuration["OpenAI:DeploymentName"]!,
                    endpoint: _configuration["OpenAI:Endpoint"]!,
                    apiKey: _configuration["OpenAI:ApiKey"]!);

#pragma warning disable SKEXP0010
                kernelBuilder.Services.AddAzureOpenAITextToImage(
                    deploymentName: _configuration["OpenAI:DeploymentNameImage"]!,
                    endpoint: _configuration["OpenAI:Endpoint"]!,
                    apiKey: _configuration["OpenAI:ApiKey"]!);

                _kernel = kernelBuilder.Build();

                // Initialize MCP Client
                _logger.LogInformation("Connecting to MCP Server...");
                _mcpClient = await McpClientFactory.CreateAsync(
                    new StdioClientTransport(new()
                    {
                        Command = "dotnet run",
                        Arguments = ["--project", "../MCP/MCP.csproj"],
                        Name = "Minimal MCP Server",
                    }));

                // Get tools from MCP server
                var tools = await _mcpClient.ListToolsAsync();
                _logger.LogInformation($"Found {tools.Count} tools from MCP server");

                foreach (var tool in tools)
                {
                    _logger.LogInformation($"Tool: {tool}");
                }

                // Add tools to kernel
#pragma warning disable SKEXP0001
                _kernel.Plugins.AddFromFunctions("MCPSimple", tools.Select(aiFunction => aiFunction.AsKernelFunction()));

                // Create execution settings
                var executionSettings = new AzureOpenAIPromptExecutionSettings()
                {
                    MaxTokens = 2000,
                    Temperature = 0.7,
                    TopP = 0.5,
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new() { RetainArgumentTypes = true })
                };

                // Create chat function
                const string skPrompt = @"
You are a helpful AI assistant with access to specialized tools. You can have a conversation with users about any topic and use the available MCP tools when appropriate.

Always be helpful, accurate, and engaging. If you need to use tools to answer a question, do so naturally. If you don't have the information needed, be honest about your limitations.

{{$history}}
User: {{$userInput}}
Assistant:";

                _chatFunction = _kernel.CreateFunctionFromPrompt(skPrompt, executionSettings);
                
                _logger.LogInformation("ChatService initialized successfully with MCP tools");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize ChatService");
                throw;
            }
        }

        public async Task<string> GetResponseAsync(string userInput)
        {
            if (_chatFunction == null || _kernel == null)
            {
                return "I'm sorry, the chat service is not properly initialized. Please try again later.";
            }

            try
            {
                var arguments = new KernelArguments()
                {
                    ["history"] = _history,
                    ["userInput"] = userInput
                };

                var result = await _chatFunction.InvokeAsync(_kernel, arguments);
                var response = result.ToString();

                // Update history
                _history += $"\nUser: {userInput}\nAssistant: {response}\n";

                // Keep history manageable (last 10 exchanges)
                var lines = _history.Split('\n');
                if (lines.Length > 40) // 20 exchanges * 2 lines each
                {
                    _history = string.Join('\n', lines.Skip(lines.Length - 40));
                }

                return response ?? "I'm sorry, I couldn't generate a response.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating response for input: {UserInput}", userInput);
                return "I'm sorry, I encountered an error processing your request. Please try again.";
            }
        }
    }
}
