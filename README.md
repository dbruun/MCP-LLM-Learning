# MCP-LLM-Learning

A demonstration project showcasing the integration of **Model Context Protocol (MCP)** with **Azure OpenAI** and **.NET**, featuring a modern web-based chat interface powered by **Semantic Kernel**, **SignalR**, and **Azure Blob Storage** for knowledge retrieval.

## 🚀 Overview

This repository demonstrates various features and learnings around building intelligent chatbots with:
- **Model Context Protocol (MCP)** for extensible tool integration
- **Azure OpenAI** for natural language understanding and generation
- **Semantic Kernel** for orchestrating AI workflows
- **Azure Blob Storage** for knowledge base and RAG (Retrieval-Augmented Generation)
- **SignalR** for real-time chat communication
- **ASP.NET Core** with Razor Pages and REST APIs

## 📋 Table of Contents

- [Features](#-features)
- [Architecture](#-architecture)
- [Projects](#-projects)
- [Prerequisites](#-prerequisites)
- [Getting Started](#-getting-started)
- [Configuration](#-configuration)
- [Usage](#-usage)
- [Deployment](#-deployment)
- [GitHub Workflows](#-github-workflows)
- [Contributing](#-contributing)
- [License](#-license)

## ✨ Features

### Chat Interface
- **Real-time chat** via SignalR for instant messaging
- **REST API endpoint** (`/api/chat`) for programmatic access
- **Swagger UI** for API exploration and testing
- **Predefined responses** for specific queries (e.g., speeding ticket enforcement)
- **Conversation history** tracking (last 10 exchanges)

### AI & ML Capabilities
- **Azure OpenAI integration** using Semantic Kernel
- **RAG (Retrieval-Augmented Generation)** with Azure Blob Storage
- **Embedding-based knowledge retrieval** for context-aware responses
- **Prompt engineering** with customizable system prompts

### MCP Tools
- **Echo Tool**: Simple message echoing for testing
- **Reverse Echo**: Returns reversed messages
- **Blob CSV Plotter**: Downloads CSV from Azure Blob, sorts data, and generates visualizations

### Developer Experience
- **Hot reload** support for rapid development
- **Comprehensive logging** with configurable levels
- **Error handling** with graceful fallbacks
- **Swagger documentation** for easy API testing

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                       User Interface                        │
│  (Razor Pages + SignalR Hub + REST API + Swagger)          │
└─────────────────┬───────────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────────────────┐
│                     MCPCopilotUI                            │
│  ┌────────────────┐  ┌─────────────────────────────────┐   │
│  │  ChatService   │  │  BlobKnowledgeService           │   │
│  │  - Semantic    │  │  - Azure Blob Storage           │   │
│  │    Kernel      │  │  - Embedding Generation         │   │
│  │  - Azure       │  │  - Vector Similarity Search     │   │
│  │    OpenAI      │  │  - Context Retrieval            │   │
│  └────────┬───────┘  └──────────┬──────────────────────┘   │
│           │                     │                           │
│           └──────────┬──────────┘                           │
└──────────────────────┼──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│                    MCP Client                               │
│  (Model Context Protocol Client for Tool Integration)      │
└─────────────────┬───────────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────────────────┐
│                    MCP Server                               │
│  (Stdio Transport + Custom Tools)                           │
│  - Echo Tool                                                │
│  - Reverse Echo Tool                                        │
│  - Blob CSV Plotter Tool                                    │
└─────────────────────────────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────────────────┐
│                   External Services                         │
│  - Azure OpenAI API                                         │
│  - Azure Blob Storage                                       │
└─────────────────────────────────────────────────────────────┘
```

## 📦 Projects

### MCPCopilotUI
The main web application providing the chat interface and API endpoints.

**Key Components:**
- `ChatService.cs`: Core chat logic with Semantic Kernel and Azure OpenAI integration
- `BlobKnowledgeService.cs`: RAG implementation with Azure Blob Storage
- `ChatHub.cs`: SignalR hub for real-time messaging
- `ChatController.cs`: REST API endpoints
- Razor Pages for the web UI

**Technologies:**
- ASP.NET Core 9.0
- SignalR for real-time communication
- Swagger/OpenAPI for API documentation
- Azure.AI.OpenAI SDK
- Azure.Storage.Blobs SDK

### MCP
The Model Context Protocol server providing extensible tools.

**Key Components:**
- `Program.cs`: MCP server initialization with stdio transport
- `Tools.cs`: Custom MCP tools (Echo, ReverseEcho, PlotSortedCsvFromBlob)

**Technologies:**
- .NET 9.0
- ModelContextProtocol SDK
- SixLabors.ImageSharp for image generation
- Azure.Storage.Blobs for blob operations

### MCPClient
A console client for testing MCP server functionality.

**Purpose:**
- Test MCP tool invocations
- Debug MCP server behavior
- Validate tool responses

## 🔧 Prerequisites

- **.NET 9.0 SDK** or later
- **Azure subscription** with:
  - Azure OpenAI service
  - Azure Blob Storage account
- **Visual Studio 2022** or **VS Code** (recommended)
- **Git** for version control

## 🚀 Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/dbruun/MCP-LLM-Learning.git
cd MCP-LLM-Learning
```

### 2. Configure Azure Services

Create an `appsettings.Development.json` in the `MCPCopilotUI` project:

```json
{
  "OpenAI": {
    "Endpoint": "https://your-openai-resource.openai.azure.com/",
    "ApiKey": "your-api-key-here",
    "DeploymentName": "gpt-4"
  },
  "BlobStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=...",
    "ContainerName": "knowledge-base",
    "KnowledgeBlobName": "knowledge.txt"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### 3. Restore Dependencies

```bash
dotnet restore
```

### 4. Build the Solution

```bash
dotnet build
```

### 5. Run the Application

```bash
dotnet run --project MCPCopilotUI/MCPCopilotUI.csproj
```

The application will start on:
- **HTTPS**: `https://localhost:5173`
- **HTTP**: `http://localhost:5172`
- **Swagger**: `https://localhost:5173/swagger`

## ⚙️ Configuration

### Environment Variables

You can also configure the application using environment variables:

```bash
# Azure OpenAI
export OpenAI__Endpoint="https://your-openai-resource.openai.azure.com/"
export OpenAI__ApiKey="your-api-key"
export OpenAI__DeploymentName="gpt-4"

# Azure Blob Storage
export BlobStorage__ConnectionString="DefaultEndpointsProtocol=https;..."
export BlobStorage__ContainerName="knowledge-base"
export BlobStorage__KnowledgeBlobName="knowledge.txt"
```

### MCP Server Configuration

The MCP server runs as a separate process and communicates via stdio. It's automatically started by the `ChatService` when initialized.

## 💬 Usage

### Web Interface

1. Navigate to `https://localhost:5173`
2. Type your message in the chat input
3. Press Enter or click Send
4. View real-time responses powered by Azure OpenAI

### REST API

**Endpoint:** `POST /api/chat`

**Request:**
```json
{
  "message": "Can I write a ticket for a van going 12 over?"
}
```

**Response:**
```json
{
  "response": "Yes, you can issue a speeding ticket for a van going 12 mph over the posted speed limit..."
}
```

### Swagger UI

Access interactive API documentation at `https://localhost:5173/swagger`

### Predefined Responses

The chatbot includes predefined responses for specific queries:

**Example:**
- **Query**: "Can I write a ticket for a van going 12 over?"
- **Response**: "Yes, you can issue a speeding ticket for a van going 12 mph over the posted speed limit. Speeding laws apply uniformly to all standard vehicles unless there is a specific exemption (e.g., emergency or authorized service vehicles). While some officers exercise discretion for minor excess speeds, 12 mph over typically qualifies for enforcement under most traffic codes."

## 🚢 Deployment

The project includes comprehensive GitHub Actions workflows for automated deployment to Azure. See [Workflows README](.github/workflows/README.md) for detailed documentation.

### Quick Deploy

1. **Set up GitHub Secrets:**
   - `AZURE_CLIENT_ID`
   - `AZURE_TENANT_ID`
   - `AZURE_SUBSCRIPTION_ID`
   - `AZURE_RESOURCE_GROUP`
   - `AZURE_WEBAPP_NAME`
   - `AZURE_STORAGE_NAME`

2. **Push to main branch** or **manually trigger** the workflow

3. **Monitor deployment** in the GitHub Actions tab

### Manual Deployment

```bash
# Publish the application
dotnet publish MCPCopilotUI/MCPCopilotUI.csproj -c Release -o ./publish

# Deploy to Azure Web App (using Azure CLI)
az webapp deployment source config-zip \
  --resource-group <your-rg> \
  --name <your-webapp> \
  --src ./publish.zip
```

## 🔄 GitHub Workflows

This project includes several GitHub Actions workflows:

- **`deploy.yml`**: Main deployment pipeline with Azure storage provisioning
- **`orchestrator.yml`**: Reusable orchestrator for multi-environment deployments
- **`deploymentTemplate.yml`**: Reusable deployment template
- **Environment-specific templates**: `template-deploy-dev.yml`, `template-deploy-staging.yml`, `template-deploy-prod.yml`

See the [Workflows README](.github/workflows/README.md) for comprehensive documentation.

## 🧪 Testing

### Running Unit Tests

```bash
dotnet test
```

### Manual Testing with MCPClient

```bash
dotnet run --project MCPClient/MCPClient.csproj
```

## 📚 Learn More

- [Model Context Protocol Documentation](https://modelcontextprotocol.io/)
- [Microsoft Semantic Kernel](https://learn.microsoft.com/en-us/semantic-kernel/)
- [Azure OpenAI Service](https://learn.microsoft.com/en-us/azure/ai-services/openai/)
- [SignalR Documentation](https://learn.microsoft.com/en-us/aspnet/core/signalr/)

## 🤝 Contributing

Contributions are welcome! This is a learning repository, so feel free to:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📝 License

This project is for demonstration and learning purposes. Feel free to use it as a reference for your own projects.

## 🙏 Acknowledgments

- Microsoft Semantic Kernel team
- Azure OpenAI team
- Model Context Protocol community
- .NET community

---

**Note**: This is a demonstration repository showcasing various features and learnings. Always follow security best practices and never commit sensitive credentials to version control.
