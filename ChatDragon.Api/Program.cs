using Azure.AI.OpenAI;
using ChatDragon.Api.Options;
using ChatDragon.Api.Utilities;
using ChatDragon.Shared;
using ChatDragon.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddOptions<AzureOpenAIOptions>()
    .Bind(builder.Configuration.GetSection(nameof(AzureOpenAIOptions)))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<IChatCompletionService>(sp =>
{
    AzureOpenAIOptions options = sp.GetRequiredService<IOptions<AzureOpenAIOptions>>().Value;

    return new AzureOpenAIChatCompletionService(options.DeploymentName, options.Endpoint, options.ApiKey);
});

Kernel tempKernel = new();
string npcGenerateQuickTemplate = EmbeddedResource.Read("NpcGenerateQuick.yaml");
KernelFunction npcGenerateQuickFunction = tempKernel.CreateFunctionFromPromptYaml(npcGenerateQuickTemplate);
KernelPlugin npcGeneratePlugin = tempKernel.Plugins.AddFromFunctions("NpcGeneratePlugin", [npcGenerateQuickFunction]);
builder.Services.AddKeyedSingleton<KernelPlugin>("NpcGeneratePlugin", npcGeneratePlugin);

string worldGenerateTownTemplate = EmbeddedResource.Read("WorldGenerateTown.yaml");
KernelFunction worldGenerateTownFunction = tempKernel.CreateFunctionFromPromptYaml(worldGenerateTownTemplate);
KernelPlugin worldGeneratePlugin = tempKernel.Plugins.AddFromFunctions("WorldGeneratePlugin", [worldGenerateTownFunction]);
builder.Services.AddKeyedSingleton<KernelPlugin>("WorldGeneratePlugin", worldGeneratePlugin);

builder.Services.AddTransient<Kernel>(sp => 
{
    KernelPluginCollection plugins =
    [
        sp.GetRequiredKeyedService<KernelPlugin>("NpcGeneratePlugin"),
        sp.GetRequiredKeyedService<KernelPlugin>("WorldGeneratePlugin"),
    ];

    return new Kernel(sp, plugins);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var apiGroup = app.MapGroup("/api");
apiGroup.MapPost("getintent", GetIntentAsync);
apiGroup.MapPost("npc/generatequick", NpcGenerateQuickAsync);

app.Run();

async Task<string> GetIntentAsync(
    [FromBody] GetIntentRequest request,
    [FromServices] Kernel kernel)
{
    IChatCompletionService chatCompletionService = 
        kernel.GetRequiredService<IChatCompletionService>();

    OpenAIPromptExecutionSettings promptExecutionSettings = new()
    {
        ToolCallBehavior = ToolCallBehavior.EnableKernelFunctions
    };

    ChatMessageContent chatMessages = 
        await chatCompletionService.GetChatMessageContentAsync(
            new ChatHistory(request.Ask), promptExecutionSettings, kernel);

    ChatCompletionsFunctionToolCall functionCall = 
        ((OpenAIChatMessageContent)chatMessages).ToolCalls
        .OfType<ChatCompletionsFunctionToolCall>()
        .First();

    string intent = string.Empty;
    if (functionCall != null)
    {
        kernel.Plugins.TryGetFunctionAndArguments(
            functionCall, 
            out KernelFunction? function, 
            out KernelArguments? arguments);
        FunctionResult result = await function.InvokeAsync(kernel, arguments);
        intent = result.GetValue<string>()!;
    }

    return intent;
}

async Task<NpcGenerateResponse> NpcGenerateQuickAsync(
    [FromBody] NpcGenerateRequest request, 
    [FromServices] Kernel kernel)
{
    kernel.GetRequiredService<IChatCompletionService>();

    FunctionResult result = await kernel.InvokeAsync(
        "NpcGeneratePlugin", "NpcGenerateQuick", new() { { "input", request.Ask } });

    string resultString = result.GetValue<string>()!;
    if (string.IsNullOrWhiteSpace(resultString))
        throw new InvalidOperationException("Failed to generate NPC.");

    NonPlayerCharacter? npc = JsonSerializer.Deserialize<NonPlayerCharacter>(
        resultString, 
        new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

    return new NpcGenerateResponse(npc);
}