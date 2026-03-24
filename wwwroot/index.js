// Danh sách avatars (emoji và icons)
const avatars = [
    { emoji: "😀", name: "Vui vẻ", icon: "😀" },
    { emoji: "😎", name: "Ngầu", icon: "😎" },
    { emoji: "🥳", name: "Hạnh phúc", icon: "🥳" },
    { emoji: "🦄", name: "Kỳ lân", icon: "🦄" },
    { emoji: "🐱", name: "Mèo", icon: "🐱" },
    { emoji: "🐶", name: "Chó", icon: "🐶" },
    { emoji: "🦊", name: "Cáo", icon: "🦊" },
    { emoji: "🐼", name: "Gấu trúc", icon: "🐼" },
    { emoji: "🐸", name: "Ếch", icon: "🐸" },
    { emoji: "🐧", name: "Cánh cụt", icon: "🐧" },
    { emoji: "🤖", name: "Robot", icon: "🤖" },
    { emoji: "👻", name: "Ma", icon: "👻" }
];

let connection = null;
let currentRoomId = null;
let currentUser = null;
let currentAvatar = null;

// Khởi tạo khi trang load
document.addEventListener('DOMContentLoaded', () => {
    initAvatars();
    setupEventListeners();
});

// Hiển thị danh sách avatar
function initAvatars() {
    const avatarsContainer = document.getElementById('avatars');
    avatars.forEach((avatar, index) => {
        const avatarDiv = document.createElement('div');
        avatarDiv.className = 'avatar-item';
        if (index === 0) avatarDiv.classList.add('selected');
        avatarDiv.innerHTML = `
            <div style="font-size: 48px;">${avatar.icon}</div>
            <span>${avatar.name}</span>
        `;
        avatarDiv.onclick = () => selectAvatar(avatar, avatarDiv);
        avatarsContainer.appendChild(avatarDiv);
    });

    // Chọn avatar mặc định
    currentAvatar = avatars[0];
}

// Chọn avatar
function selectAvatar(avatar, element) {
    document.querySelectorAll('.avatar-item').forEach(item => {
        item.classList.remove('selected');
    });
    element.classList.add('selected');
    currentAvatar = avatar;
}

// Setup event listeners
function setupEventListeners() {
    document.getElementById('createRoomBtn').onclick = createRoom;
    document.getElementById('joinRoomBtn').onclick = joinRoom;
    document.getElementById('sendBtn').onclick = sendMessage;
    document.getElementById('leaveRoomBtn').onclick = leaveRoom;
    document.getElementById('messageInput').onkeypress = (e) => {
        if (e.key === 'Enter') sendMessage();
    };
}

// Tạo phòng mới
async function createRoom() {
    const userName = document.getElementById('userName').value.trim();
    if (!userName) {
        alert('Vui lòng nhập tên hiển thị!');
        return;
    }

    if (!currentAvatar) {
        alert('Vui lòng chọn avatar!');
        return;
    }

    const roomId = generateRoomId();
    currentUser = { userName, avatar: currentAvatar.icon };
    currentRoomId = roomId;

    await connectToRoom(roomId, userName, currentAvatar.icon, true);
}

// Tham gia phòng
async function joinRoom() {
    const userName = document.getElementById('userName').value.trim();
    const roomId = document.getElementById('joinRoomId').value.trim();

    if (!userName) {
        alert('Vui lòng nhập tên hiển thị!');
        return;
    }

    if (!roomId) {
        alert('Vui lòng nhập mã phòng!');
        return;
    }

    if (!currentAvatar) {
        alert('Vui lòng chọn avatar!');
        return;
    }

    currentUser = { userName, avatar: currentAvatar.icon };
    currentRoomId = roomId;

    await connectToRoom(roomId, userName, currentAvatar.icon, false);
}

// Kết nối đến phòng
async function connectToRoom(roomId, userName, avatar, isNewRoom) {
    try {
        // Khởi tạo kết nối SignalR
        connection = new signalR.HubConnectionBuilder()
            .withUrl("http://localhost:5298/chathub")
            .withAutomaticReconnect()
            .build();

        // Đăng ký các sự kiện
        connection.on("ReceiveMessage", (message) => {
            addMessageToChat(message);
        });

        connection.on("UserJoined", (data) => {
            updateMembersList(data.users);
            addSystemMessage(`${data.userName} đã tham gia phòng!`);
        });

        connection.on("UserLeft", (connectionId) => {
            // Xử lý khi có người rời
            console.log("User left:", connectionId);
        });

        await connection.start();

        let response;
        if (isNewRoom) {
            response = await connection.invoke("CreateRoom", roomId, userName, avatar);
        } else {
            response = await connection.invoke("JoinRoom", { roomId, userName, avatar });
        }

        if (response.success) {
            // Hiển thị lịch sử tin nhắn
            if (response.messages) {
                response.messages.forEach(msg => {
                    if (msg.type === 'join' || msg.type === 'leave') {
                        addSystemMessage(msg.content);
                    } else {
                        addMessageToChat(msg);
                    }
                });
            }

            // Cập nhật danh sách thành viên
            if (response.users) {
                updateMembersList(response.users);
            }

            // Chuyển sang màn hình chat
            document.getElementById('joinScreen').style.display = 'none';
            document.getElementById('chatScreen').style.display = 'flex';
            document.getElementById('roomIdDisplay').textContent = roomId;

            addSystemMessage(`✨ Chào mừng bạn đến với phòng ${roomId}! ✨`);
        } else {
            alert(response.message || 'Không thể tham gia phòng!');
            await connection.stop();
        }
    } catch (error) {
        console.error('Lỗi kết nối:', error);
        alert('Không thể kết nối đến server!');
    }
}

// Gửi tin nhắn
async function sendMessage() {
    const input = document.getElementById('messageInput');
    const content = input.value.trim();

    if (!content) return;
    if (!connection || connection.state !== signalR.HubConnectionState.Connected) {
        alert('Chưa kết nối đến server!');
        return;
    }

    try {
        await connection.invoke("SendMessage", { roomId: currentRoomId, content });
        input.value = '';
        input.focus();
    } catch (error) {
        console.error('Lỗi gửi tin nhắn:', error);
        alert('Không thể gửi tin nhắn!');
    }
}

// Thêm tin nhắn vào chat
function addMessageToChat(message) {
    const messagesContainer = document.getElementById('messagesContainer');
    const messageDiv = document.createElement('div');
    messageDiv.className = 'message';

    const time = new Date(message.timestamp).toLocaleTimeString('vi-VN');

    messageDiv.innerHTML = `
        <div class="message-avatar">
            <div style="font-size: 40px;">${message.avatar}</div>
        </div>
        <div class="message-content">
            <div class="message-header">
                <span class="message-username">${escapeHtml(message.userName)}</span>
                <span class="message-time">${time}</span>
            </div>
            <div class="message-text">${escapeHtml(message.content)}</div>
        </div>
    `;

    messagesContainer.appendChild(messageDiv);
    scrollToBottom();
}

// Thêm tin nhắn hệ thống
function addSystemMessage(content) {
    const messagesContainer = document.getElementById('messagesContainer');
    const systemDiv = document.createElement('div');
    systemDiv.className = 'system-message';
    systemDiv.textContent = content;
    messagesContainer.appendChild(systemDiv);
    scrollToBottom();
}

// Cập nhật danh sách thành viên
function updateMembersList(users) {
    const membersContainer = document.getElementById('membersContainer');
    const memberCount = document.getElementById('memberCount');

    membersContainer.innerHTML = '';
    memberCount.textContent = users.length;

    users.forEach(user => {
        const memberDiv = document.createElement('div');
        memberDiv.className = 'member-item';
        memberDiv.innerHTML = `
            <div style="font-size: 32px;">${user.avatar}</div>
            <div class="member-info">
                <div class="member-name">${escapeHtml(user.userName)}</div>
            </div>
        `;
        membersContainer.appendChild(memberDiv);
    });
}

// Rời phòng
async function leaveRoom() {
    if (connection) {
        await connection.stop();
    }

    // Reset UI
    document.getElementById('joinScreen').style.display = 'block';
    document.getElementById('chatScreen').style.display = 'none';
    document.getElementById('userName').value = '';
    document.getElementById('joinRoomId').value = '';
    document.getElementById('messagesContainer').innerHTML = '';

    currentRoomId = null;
    currentUser = null;
}

// Helper functions
function generateRoomId() {
    return Math.random().toString(36).substring(2, 8).toUpperCase();
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function scrollToBottom() {
    const messagesContainer = document.getElementById('messagesContainer');
    messagesContainer.scrollTop = messagesContainer.scrollHeight;
}