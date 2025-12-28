package main

import (
	"bufio"
	"bytes"
	"encoding/json"
	"fmt"
	"io"
	"net"
	"net/http"
	"sync"
	"time"
)

const (
	v4BaseURL = "https://api.fandmc.cn/v4"
	apiKey    = "fejjdfwh2y97er9o"
	tcpPort   = ":9527"
)

// V4 验证响应
type UserInfo struct {
	ID        int64  `json:"id"`
	Username  string `json:"username"`
	Token     string `json:"token"`
	Hwid      string `json:"hwid"`
	CreatedAt string `json:"createdAt"`
	LastLogin string `json:"lastLogin"`
}

// 玩家信息
type PlayerInfo struct {
	Username   string
	PlayerName string
}

// 客户端连接
type Client struct {
	conn       net.Conn
	serverId   string
	playerName string
	username   string
}

// 连接管理器
type ConnectionManager struct {
	mu      sync.RWMutex
	clients map[net.Conn]*Client
	servers map[string]map[net.Conn]*Client // serverId -> connections
}

func NewConnectionManager() *ConnectionManager {
	return &ConnectionManager{
		clients: make(map[net.Conn]*Client),
		servers: make(map[string]map[net.Conn]*Client),
	}
}

func (m *ConnectionManager) Register(conn net.Conn, serverId, playerName, username string) {
	m.mu.Lock()
	defer m.mu.Unlock()

	client := &Client{
		conn:       conn,
		serverId:   serverId,
		playerName: playerName,
		username:   username,
	}

	m.clients[conn] = client

	if m.servers[serverId] == nil {
		m.servers[serverId] = make(map[net.Conn]*Client)
	}
	m.servers[serverId][conn] = client
}

func (m *ConnectionManager) Unregister(conn net.Conn) {
	m.mu.Lock()
	defer m.mu.Unlock()

	client := m.clients[conn]
	if client != nil {
		if m.servers[client.serverId] != nil {
			delete(m.servers[client.serverId], conn)
		}
		delete(m.clients, conn)
	}
}

func (m *ConnectionManager) Broadcast(serverId, message string) {
	m.mu.RLock()
	defer m.mu.RUnlock()

	clients := m.servers[serverId]
	for _, client := range clients {
		client.conn.Write([]byte(message + "\n"))
	}
}

func (m *ConnectionManager) GetPlayers(serverId string) []PlayerInfo {
	m.mu.RLock()
	defer m.mu.RUnlock()

	clients := m.servers[serverId]
	result := make([]PlayerInfo, 0, len(clients))
	for _, client := range clients {
		result = append(result, PlayerInfo{Username: client.username, PlayerName: client.playerName})
	}
	return result
}

// V4 验证
func verifyToken(token, hwid string) (*UserInfo, error) {
	payload := map[string]string{"keys": apiKey, "token": token}
	body, _ := json.Marshal(payload)

	resp, err := http.Post(v4BaseURL+"/user/by-token", "application/json", bytes.NewReader(body))
	if err != nil {
		return nil, err
	}
	// 在 verifyToken 函数中添加调试
	respBody, _ := io.ReadAll(resp.Body)
	fmt.Printf("V4 Response: %d %s\n", resp.StatusCode, string(respBody))
	defer resp.Body.Close()

	if resp.StatusCode != 200 {
		return nil, fmt.Errorf("invalid token")
	}

	var user UserInfo
	if err := json.NewDecoder(resp.Body).Decode(&user); err != nil {
		return nil, err
	}

	if user.Hwid != hwid {
		return nil, fmt.Errorf("hwid mismatch")
	}

	return &user, nil
}

// 协议格式:
// REPORT|token|hwid|serverId|playerName\n  - 注册到服务器
// GET|token|hwid|serverId|\n               - 获取服务器玩家列表
// DEL|token|hwid|serverId|playerName\n     - 从服务器删除玩家
// CHAT|token|hwid|serverId|playerName|message\n - 发送聊天消息
//
// 广播格式:
// CHAT|playerName|message\n

func handleConnection(conn net.Conn, manager *ConnectionManager) {
	defer func() {
		manager.Unregister(conn)
		conn.Close()
	}()

	reader := bufio.NewReader(conn)
	for {
		conn.SetDeadline(time.Now().Add(5 * time.Minute))
		line, err := reader.ReadString('\n')
		if err != nil {
			if err != io.EOF {
				fmt.Printf("Read error: %v\n", err)
			}
			return
		}

		response := processCommand(conn, line, manager)
		if response != "" {
			conn.Write([]byte(response + "\n"))
		}
	}
}

func processCommand(conn net.Conn, line string, manager *ConnectionManager) string {
	line = line[:len(line)-1] // 去掉 \n
	parts := splitCommand(line)

	if len(parts) < 4 {
		return "ERR|invalid_format"
	}

	cmd := parts[0]
	token := parts[1]
	hwid := parts[2]
	serverId := parts[3]

	// 验证
	user, err := verifyToken(token, hwid)
	if err != nil {
		return "ERR|" + err.Error()
	}

	switch cmd {
	case "REPORT":
		if len(parts) < 5 {
			return "ERR|missing_player_name"
		}
		playerName := parts[4]
		manager.Register(conn, serverId, playerName, user.Username)
		return "OK|reported"

	case "GET":
		players := manager.GetPlayers(serverId)
		data, _ := json.Marshal(players)
		return "OK|" + string(data)

	case "DEL":
		if len(parts) < 5 {
			return "ERR|missing_player_name"
		}
		manager.Unregister(conn)
		return "OK|deleted"

	case "CHAT":
		if len(parts) < 6 {
			return "ERR|missing_message"
		}
		playerName := parts[4]
		message := parts[5]
		// 广播给同服务器所有客户端
		chatMsg := fmt.Sprintf("CHAT|%s|%s", playerName, message)
		manager.Broadcast(serverId, chatMsg)
		return "" // 不返回响应，广播已发送

	default:
		return "ERR|unknown_command"
	}
}

func splitCommand(s string) []string {
	var result []string
	var current []byte
	for i := 0; i < len(s); i++ {
		if s[i] == '|' {
			result = append(result, string(current))
			current = nil
		} else {
			current = append(current, s[i])
		}
	}
	result = append(result, string(current))
	return result
}

func main() {
	manager := NewConnectionManager()

	listener, err := net.Listen("tcp", tcpPort)
	if err != nil {
		fmt.Printf("Failed to listen on %s: %v\n", tcpPort, err)
		return
	}
	defer listener.Close()

	fmt.Printf("IRC Server listening on %s\n", tcpPort)

	for {
		conn, err := listener.Accept()
		if err != nil {
			fmt.Printf("Accept error: %v\n", err)
			continue
		}
		go handleConnection(conn, manager)
	}
}
