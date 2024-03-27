using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Plugins.Core;


namespace AOI_Plugins
{
    internal class Program
    {

        static async Task Main(string[] args)
        {
            // Create a new configuration builder
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory) // Set the base path to the current domain's base directory
                .AddJsonFile("appsettings.json") // Add the appsettings.json file
                .AddUserSecrets<Program>() // Add user secrets of the Program class
                .AddEnvironmentVariables() // Add environment variables
                .AddCommandLine(args) // Add command line arguments
                .Build(); // Build the configuration

            if (config["AOI_ENDPOINT"] == null || config["AOI_API_KEY"] == null)
            {
                Console.WriteLine("Please provide the AOI_ENDPOINT and AOI_API_KEY in the appsettings.json file.");
                return;
            }
            // Create a kernel with the Azure OpenAI chat completion service
            var builder = Kernel.CreateBuilder();
            builder.AddAzureOpenAIChatCompletion(
                config["AOI_DEPLOYMODEL"] ?? "gpt-35-turbo",
                config["AOI_ENDPOINT"]!,
                config["AOI_API_KEY"]!);

            #pragma warning disable SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            builder.Plugins.AddFromType<TimePlugin>();


            // Build the kernel
            var kernel = builder.Build();

            // Create chat history
            ChatHistory history = [];
            
            // Get chat completion service
            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            // Enable auto function calling
            OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
            {
                ChatSystemPrompt = @"You're a virtual assistant that helps people find information.",
                Temperature = 0.7,
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            };

            // Start the conversation
            while (true)
            {
                // Get user input
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("User > ");
                history.AddUserMessage(Console.ReadLine()!);


                // Get the response from the AI
                var response = chatCompletionService.GetStreamingChatMessageContentsAsync(
                               history,
                               executionSettings: openAIPromptExecutionSettings,
                               kernel: kernel);


                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("\nAssistant > ");

                string combinedResponse = string.Empty;
                await foreach (var message in response)
                {
                    //Write the response to the console
                    Console.Write(message);
                    combinedResponse += message;
                }

                Console.WriteLine();

                // Add the message from the agent to the chat history
                history.AddAssistantMessage(combinedResponse);
            }
        }
    }
}