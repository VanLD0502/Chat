using Microsoft.AspNetCore.SignalR;
using ChatAPI.Models;
using ChatAPI.Services;

namespace ChatAPI.Hubs
{
    public class ChatHub : Hub
    {
        private readonly RoomManager _roomManager;

        public ChatHub(RoomManager roomManager)
        {
            _roomManager = roomManager;
        }

        // Tạo phòng mới
        public async Task<object> CreateRoom(string roomId, string userName, string avatar)
        {
            if (_roomManager.RoomExists(roomId))
            {
                return new { success = false, message = "Phòng đã tồn tại!" };
            }

            var room = _roomManager.CreateRoom(roomId);
            var user = new User
            {
                ConnectionId = Context.ConnectionId,
                UserName = userName,
                Avatar = avatar,
                RoomId = roomId
            };

            _roomManager.JoinRoom(roomId, user);
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

            var users = _roomManager.GetRoomUsers(roomId);
            var messages = _roomManager.GetRoomMessages(roomId);

            return new
            {
                success = true,
                roomId = room.RoomId,
                users = users.Select(u => new { u.UserName, u.Avatar, u.ConnectionId }),
                messages = messages.Select(m => new { m.UserName, m.Avatar, m.Content, m.Timestamp, m.Type })
            };
        }

        // Tham gia phòng
        public async Task<object> JoinRoom(JoinRoomRequest request)
        {
            if (!_roomManager.RoomExists(request.RoomId))
            {
                return new { success = false, message = "Phòng không tồn tại!" };
            }

            var user = new User
            {
                ConnectionId = Context.ConnectionId,
                UserName = request.UserName,
                Avatar = request.Avatar,
                RoomId = request.RoomId
            };

            _roomManager.JoinRoom(request.RoomId, user);
            await Groups.AddToGroupAsync(Context.ConnectionId, request.RoomId);

            // Gửi thông báo cho tất cả mọi người trong phòng
            await Clients.Group(request.RoomId).SendAsync("UserJoined", new
            {
                userName = user.UserName,
                avatar = user.Avatar,
                users = _roomManager.GetRoomUsers(request.RoomId).Select(u => new { u.UserName, u.Avatar, u.ConnectionId })
            });

            var messages = _roomManager.GetRoomMessages(request.RoomId);

            return new
            {
                success = true,
                messages = messages.Select(m => new { m.UserName, m.Avatar, m.Content, m.Timestamp, m.Type })
            };
        }

        // Gửi tin nhắn
        public async Task SendMessage(SendMessageRequest request)
        {
            var room = _roomManager.GetRoom(request.RoomId);
            if (room == null) return;

            if (!room.Users.TryGetValue(Context.ConnectionId, out var user))
                return;

            var message = new ChatMessage
            {
                UserName = user.UserName,
                Avatar = user.Avatar,
                Content = request.Content,
                Type = "message"
            };

            _roomManager.AddMessage(request.RoomId, message);

            await Clients.Group(request.RoomId).SendAsync("ReceiveMessage", new
            {
                message.UserName,
                message.Avatar,
                message.Content,
                message.Timestamp,
                message.Type
            });
        }

        // Ngắt kết nối
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_roomManager.LeaveRoom(Context.ConnectionId))
            {
                // Thông báo cho tất cả các phòng
                await Clients.All.SendAsync("UserLeft", Context.ConnectionId);
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}