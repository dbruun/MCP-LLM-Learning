using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Client;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;




IConfigurationRoot config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory()) // Or AppContext.BaseDirectory for console apps
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables() // Optional: for environment variables
    .Build();

// Prepare and build kernel
var builder = Kernel.CreateBuilder();
builder.Services.AddLogging(c => c.AddDebug().SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace));
builder.Services.AddAzureOpenAIChatCompletion(
    deploymentName: config["OpenAI:DeploymentName"]!,
    endpoint: config["OpenAI:Endpoint"]!,
    apiKey: config["OpenAI:ApiKey"]!);
    
// #pragma warning disable SKEXP0010
// builder.Services.AddAzureOpenAITextToImage(
//     deploymentName: config["OpenAI:DeploymentNameImage"]!,
//     endpoint: config["OpenAI:Endpoint"]!,
//     apiKey: config["OpenAI:ApiKey"]!);
Kernel kernel = builder.Build();




IMcpClient mcpClient = await McpClientFactory.CreateAsync(
    new StdioClientTransport(new()
    {
        Command = "dotnet run",
        Arguments = ["--project", "../MCP/MCP.csproj"],
        Name = "Minimal MCP Server",
    }));


IList<McpClientTool> tools = await mcpClient.ListToolsAsync();
foreach (McpClientTool tool in tools)
{
    Console.WriteLine($"{tool}");
}
#pragma warning disable SKEXP0001
kernel.Plugins.AddFromFunctions("MCPSimple", tools.Select(aiFunction => aiFunction.AsKernelFunction()));

AzureOpenAIPromptExecutionSettings executionSettings = new()
{
     MaxTokens = 2000,
    Temperature = 0.7,
    TopP = 0.5,
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new() { RetainArgumentTypes = true })
};

const string skPrompt = @"
Bot can have a conversation with you about any topic, only use tools gathered from the MCP server.
It can give explicit instructions or say 'I don't know' if it does not have an answer.

{{$history}}
User: {{$userInput}}
Bot:";

var chatFunction = kernel.CreateFunctionFromPrompt(skPrompt, executionSettings);


var history = "";
var arguments = new KernelArguments()
{
    ["history"] = history
};


List<ChatMessage> messages = [];
while (true)
{
    Console.WriteLine("Enter a message to the bot (or 'exit' to quit):");
    var userInput = Console.ReadLine();
    arguments["userInput"] = userInput;
    var bot_answer = await chatFunction.InvokeAsync(kernel, arguments);
    Console.WriteLine("bot: " + bot_answer);
    history += $"\nUser: {userInput}\nAI: {bot_answer}\n";
    arguments["history"] = history;

}


