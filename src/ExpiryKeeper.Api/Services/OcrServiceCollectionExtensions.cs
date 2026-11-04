using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Identity;
using OpenAI.Chat;

namespace MedicineExpiration.Api.Services;

public static class OcrServiceCollectionExtensions
{
    public static IServiceCollection AddOcr(this IServiceCollection services)
    {
        services.AddSingleton<TokenCredential>(provider =>
            provider.GetRequiredService<IHostEnvironment>().IsDevelopment()
                ? new AzureCliCredential()
                : new DefaultAzureCredential());

        services.AddSingleton<ChatClient>(provider =>
        {
            using var activity = OcrService.OcrActivitySource.StartActivity("ocr.client.initialize");
            var config = provider.GetRequiredService<IConfiguration>();
            var endpoint = config["AzureOpenAI:Endpoint"]
                ?? throw new InvalidOperationException("AzureOpenAI:Endpoint not configured");
            var deployment = config["AzureOpenAI:DeploymentName"] ?? "gpt-4o";
            return new AzureOpenAIClient(new Uri(endpoint), provider.GetRequiredService<TokenCredential>())
                .GetChatClient(deployment);
        });

        services.AddScoped<OcrService>();
        return services;
    }
}
