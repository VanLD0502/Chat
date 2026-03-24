using System.Collections.Concurrent;
using ChatAPI.Models;

namespace ChatAPI.Services
{
    public class RoomManager
    {
        private readonly ConcurrentDictionary<string, ChatRoom> _rooms = new();
        private readonly ConcurrentDictionary<string, string> _userConnections = new();

        public ChatRoom CreateRoom(string roomId, string roomName = "")
        {
            var room = new ChatRoom
            {
                RoomId = roomId,
                RoomName = string.IsNullOrEmpty(roomName) ? $"Phòng {roomId}" : roomName
            };
            
            _rooms.TryAdd(roomId, room);
            return room;
        }

        public ChatRoom? GetRoom(string roomId)
        {
            _rooms.TryGetValue(roomId, out var room);
            return room;
        }

        public bool RoomExists(string roomId)
        {
            return _rooms.ContainsKey(roomId);
        }

        public bool JoinRoom(string roomId, User user)
        {
            if (!_rooms.TryGetValue(roomId, out var room))
                return false;

            if (!room.Users.ContainsKey(user.ConnectionId))
            {
                // Thay vì: room.Users[user.ConnectionId] = user;
                room.Users.TryAdd(user.ConnectionId, user);  // ← dùng TryAdd
                _userConnections.TryAdd(user.ConnectionId, roomId);

                room.Messages.Add(new ChatMessage
                {
                    UserName = user.UserName,
                    Avatar = user.Avatar,
                    Content = $"{user.UserName} đã tham gia phòng",
                    Type = "join"
                });
            }
            return true;
        } 

        public bool LeaveRoom(string connectionId)
        {
            if (_userConnections.TryRemove(connectionId, out var roomId)) // ConcurrentDictionary có TryRemove
            {
                if (_rooms.TryGetValue(roomId, out var room))
                {
                    if (room.Users.TryRemove(connectionId, out var user)) // Cần sửa Users thành ConcurrentDictionary
                    {
                        room.Messages.Add(new ChatMessage
                        {
                            UserName = user.UserName,
                            Avatar = user.Avatar,
                            Content = $"{user.UserName} đã rời phòng",
                            Type = "leave"
                        });
                    }

                    // Xóa phòng nếu không còn ai
                    if (room.Users.Count == 0)
                    {
                        _rooms.TryRemove(roomId, out _);
                    }
                    return true;
                }
            }
            return false;
        }

        public List<User> GetRoomUsers(string roomId)
        {
            if (_rooms.TryGetValue(roomId, out var room))
            {
                return room.Users.Values.ToList();
            }
            return new List<User>();
        }

        
        public List<ChatMessage> GetRoomMessages(string roomId)
        {
            if (_rooms.TryGetValue(roomId, out var room))
            {
                return room.Messages;
            }
            return new List<ChatMessage>();
        }

        public void AddMessage(string roomId, ChatMessage message)
        {
            if (_rooms.TryGetValue(roomId, out var room))
            {
                room.Messages.Add(message);
                // Giới hạn số lượng tin nhắn
                if (room.Messages.Count > 100)
                {
                    room.Messages.RemoveAt(0);
                }
            }
        }
    }
}