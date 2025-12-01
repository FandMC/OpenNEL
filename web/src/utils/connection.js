import { ref, computed } from 'vue'
import appConfig from '../config/app.js'

const connected = ref(false)
const connecting = ref(false)
const socket = ref(null)

const statusText = computed(() => {
  if (connecting.value) return '连接中'
  return connected.value ? '已连接' : '未连接'
})
const statusClass = computed(() => connected.value ? 'connected' : 'disconnected')

function connect() {
  if (socket.value && socket.value.readyState === 1) return socket.value
  const url = appConfig.getWsUrl()
  connecting.value = true
  try {
    const s = new WebSocket(url)
    s.onopen = () => { connected.value = true; connecting.value = false }
    s.onclose = () => { connected.value = false; connecting.value = false }
    s.onerror = () => { connected.value = false; connecting.value = false }
    socket.value = s
    return s
  } catch {
    connected.value = false
    connecting.value = false
    socket.value = null
    return null
  }
}

function disconnect() {
  try {
    if (socket.value && socket.value.readyState === 1) socket.value.close()
  } catch {}
}

function reconnect() {
  disconnect()
  return connect()
}

export default { connected, connecting, socket, statusText, statusClass, connect, disconnect, reconnect }
