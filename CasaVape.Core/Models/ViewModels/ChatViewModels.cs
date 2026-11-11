using System.Text.Json.Serialization;

namespace CasaVape.Core.Models.ViewModels
{
    public sealed class ChatMessageDto
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "user";

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    public sealed class ChatRequest
    {
        [JsonPropertyName("messages")]
        public List<ChatMessageDto> Messages { get; set; } = new();
    }

    public sealed class ChatReply
    {
        [JsonPropertyName("reply")]
        public string Reply { get; set; } = string.Empty;

        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;
    }
}
