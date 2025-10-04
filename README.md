# Blazor Starter Application

This template contains an example .NET 7 [Blazor WebAssembly](https://docs.microsoft.com/aspnet/core/blazor/?view=aspnetcore-6.0#blazor-webassembly) client application, a .NET 7 C# [Azure Functions](https://docs.microsoft.com/azure/azure-functions/functions-overview), and a C# class library with shared code.

> Note: Azure Functions only supports .NET 7 in the isolated process execution model

## Getting Started

1. Create a repository from the [GitHub template](https://docs.github.com/en/enterprise/2.22/user/github/creating-cloning-and-archiving-repositories/creating-a-repository-from-a-template) and then clone it locally to your machine.

1. In the **ApiIsolated** folder, copy `local.settings.example.json` to `local.settings.json`

1. Continue using either Visual Studio or Visual Studio Code.

### Visual Studio 2022

Once you clone the project, open the solution in the latest release of [Visual Studio 2022](https://visualstudio.microsoft.com/vs/) with the Azure workload installed, and follow these steps:

1. Right-click on the solution and select **Set Startup Projects...**.

1. Select **Multiple startup projects** and set the following actions for each project:
    - *Api* - **Start**
    - *Client* - **Start**
    - *Shared* - None

1. Press **F5** to launch both the client application and the Functions API app.

### Visual Studio Code with Azure Static Web Apps CLI for a better development experience (Optional)

1. Install the [Azure Static Web Apps CLI](https://www.npmjs.com/package/@azure/static-web-apps-cli) and [Azure Functions Core Tools CLI](https://www.npmjs.com/package/azure-functions-core-tools).

1. Open the folder in Visual Studio Code.

1. Delete file `Client/wwwroot/appsettings.Development.json`

1. In the VS Code terminal, run the following command to start the Static Web Apps CLI, along with the Blazor WebAssembly client application and the Functions API app:

    ```bash
    swa start http://localhost:5000 --api-location http://localhost:7071
    ```

    The Static Web Apps CLI (`swa`) starts a proxy on port 4280 that will forward static site requests to the Blazor server on port 5000 and requests to the `/api` endpoint to the Functions server. 

1. Open a browser and navigate to the Static Web Apps CLI's address at `http://localhost:4280`. You'll be able to access both the client application and the Functions API app in this single address. When you navigate to the "Fetch Data" page, you'll see the data returned by the Functions API app.

1. Enter Ctrl-C to stop the Static Web Apps CLI.

## Template Structure

- **Client**: The Blazor WebAssembly sample application
- **Api**: A C# Azure Functions API, which the Blazor application will call
- **Shared**: A C# class library with a shared data model between the Blazor and Functions application

## Google Firestore Configuration

This application uses Google Cloud Firestore as the production database. To test with real Firestore data locally:

### Local Development Setup

1. **Obtain Google Service Account Credentials**
   - Download the service account JSON file from Google Cloud Console
   - Save it to a secure location (e.g., `D:\Privat\GIT\Google keys\supergnisten-shoppinglist-eb82277057ad.json`)

2. **Set Environment Variable**
   ```powershell
   # Option 1: Set file path (for local development)
   $env:GOOGLE_CREDENTIALS = "D:\Privat\GIT\Google keys\supergnisten-shoppinglist-eb82277057ad.json"
   
   # Option 2: Set JSON content directly (for production/cloud deployment)
   $env:GOOGLE_CREDENTIALS = Get-Content "D:\Privat\GIT\Google keys\supergnisten-shoppinglist-eb82277057ad.json" -Raw
   ```

3. **Smart Credential Handling**
   The `GoogleDbContext` automatically detects whether the environment variable contains:
   - **File path**: Uses `Path.IsPathFullyQualified()` to detect and reads the JSON content
   - **JSON content**: Uses the value directly
   
   This allows seamless switching between local development (file path) and cloud deployment (JSON content).

### Code Implementation
```csharp
var json = Environment.GetEnvironmentVariable("GOOGLE_CREDENTIALS");
if(Path.IsPathFullyQualified(json)) // Check if env var is a file path
{
    json = File.ReadAllText(json);   // Read JSON from file
}
// Use json content for Firestore authentication
```

### Debug vs Production Data
- **Debug mode**: Uses `MemoryGenericRepository` with in-memory test data
- **Production mode**: Uses `GoogleFireBaseGenericRepository` with live Firestore data

Switch between modes by changing the build configuration in `Api/Program.cs`.

## Deploy to Azure Static Web Apps

This application can be deployed to [Azure Static Web Apps](https://docs.microsoft.com/azure/static-web-apps), to learn how, check out [our quickstart guide](https://aka.ms/blazor-swa/quickstart).
