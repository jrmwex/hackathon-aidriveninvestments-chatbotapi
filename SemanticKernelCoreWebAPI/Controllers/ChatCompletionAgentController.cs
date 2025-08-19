using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

namespace SemanticKernelCoreWebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ChatCompletionAgentController : ControllerBase
    {
        private readonly IMemoryCache _memoryCache;
        private const string ChatHistoryCacheKey = "ChatHistory";


        private const string modelId = "azure-gpt-4o-mini";
        private const string endpoint = "https://aips-ai-gateway.ue1.dev.ai-platform.int.wexfabric.com/";
        private const string apiKey = "";

        public ChatCompletionAgentController(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
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

            var localChatHistory = new ChatHistory();
            localChatHistory.AddUserMessage(userInput);
            AddToMemoryCache(localChatHistory);

            var cachedChatHistory = _memoryCache.Get<ChatHistory>(ChatHistoryCacheKey) ?? localChatHistory;

            ChatCompletionAgent agent =
                new()
                {
                    Name = "SK-Agent",
                    Instructions = "You are a helpful financial assistant.",
                    Kernel = kernel,
                };

            var chatCompletion = new List<ChatCompletion>();

            await foreach (var response in agent.InvokeAsync(cachedChatHistory))
            {
                var responseChatHistory = new ChatHistory();
                responseChatHistory.AddAssistantMessage(response.Message.Content);
                AddToMemoryCache(responseChatHistory);

                chatCompletion.Add(new ChatCompletion
                {
                    MessageContent = response.Message.Content
                });
            }

            return chatCompletion;
        }

        private void AddToMemoryCache(ChatHistory chatHistory)
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5)); // Cache for 5 minutes
            if (_memoryCache.TryGetValue(ChatHistoryCacheKey, out ChatHistory cacheChatHistory))
            {
                if (cacheChatHistory != null)
                {
                    ChatMessageContent[] array = new ChatMessageContent[chatHistory.Count];
                    chatHistory.CopyTo(array, 0);
                    cacheChatHistory.AddRange(array);
                    _memoryCache.Set(ChatHistoryCacheKey, cacheChatHistory, cacheEntryOptions);
                }
            }
            else
            {
                _memoryCache.Set(ChatHistoryCacheKey, chatHistory, cacheEntryOptions);
            }
        }
    }
}
