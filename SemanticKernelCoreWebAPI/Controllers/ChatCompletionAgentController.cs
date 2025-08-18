using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace SemanticKernelCoreWebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ChatCompletionAgentController : ControllerBase
    {
        private readonly ILogger<ChatCompletionAgentController> _logger;

        private readonly string modelId = "azure-gpt-4o-mini";
        private readonly string endpoint = "https://aips-ai-gateway.ue1.dev.ai-platform.int.wexfabric.com/";
        private readonly string apiKey = "";

        public ChatCompletionAgentController(ILogger<ChatCompletionAgentController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "GetChatResponse")]
        public async Task<IEnumerable<ChatCompletion>> GetAsync([FromQuery] string userInput)
        {
            var builder = Kernel.CreateBuilder();
            builder.AddAzureOpenAIChatCompletion(
                            modelId,
                            endpoint,
                            apiKey
                            );
            var kernel = builder.Build();

            ChatCompletionAgent agent =
                new()
                {
                    Name = "SK-Agent",
                    Instructions = "You are a helpful financial assistant.",
                    Kernel = kernel,
                };

            var chatCompletion = new List<ChatCompletion>();

            await foreach (var response in agent.InvokeAsync(userInput))
            {
                chatCompletion.Add(new ChatCompletion
                {
                    MessageContent = response.Message.Content
                });
            }

            return chatCompletion;
        }
    }
}
