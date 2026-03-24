using System.Collections.Concurrent;
namespace ChatAPI.Models
{
    public class User
    {
        public string ConnectionId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public string RoomId { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; } = DateTime.Now;
    }

    public class ChatRoom
    {
        public string RoomId { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public ConcurrentDictionary<string, User> Users { get; set; } = new(); // ← thay đổi
        public List<ChatMessage> Messages { get; set; } = new();
    }

    public class ChatMessage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserName { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Type { get; set; } = "message"; // message, join, leave
    }

    public class JoinRoomRequest
    {
        public string RoomId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
    }

    public class SendMessageRequest
    {
        public string RoomId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
