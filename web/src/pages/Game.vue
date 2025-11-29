<template>
  <div class="game-page">
    <div class="header">
      <div class="title">游戏</div>
    </div>
    <div v-if="sessions.length === 0" class="empty-top">暂无会话</div>
    <div class="list" v-else>
      <div v-for="s in sessions" :key="s.id" class="row">
        <div class="info">
          <div class="server">{{ s.serverName }}</div>
          <div class="player">{{ s.characterName }} · {{ s.type }}</div>
          <div class="ip">{{ s.statusText }}</div>
        </div>
        <div class="actions">
          <button class="btn" @click="copyIp(s.localAddress)">复制地址</button>
          <button class="btn danger" @click="shutdown(s.identifier)">关闭通道</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted, inject } from 'vue'
import appConfig from '../config/app.js'
const sessions = ref([])
let socket
const notify = inject('notify', null)
const prevIds = ref(new Set())
function requestSessions() {
  if (!socket || socket.readyState !== 1) return
  try { socket.send(JSON.stringify({ type: 'query_game_session' })) } catch {}
}
function copyIp(text) {
  if (!text) return
  if (navigator && navigator.clipboard && navigator.clipboard.writeText) {
    navigator.clipboard.writeText(text).catch(() => {})
  }
}
function shutdown(identifier) {
  if (!socket || socket.readyState !== 1) return
  try { socket.send(JSON.stringify({ type: 'shutdown_game', identifiers: [identifier] })) } catch {}
}
onMounted(() => {
  try {
    socket = new WebSocket(appConfig.getWsUrl())
    socket.onopen = () => { requestSessions() }
    socket.onmessage = (e) => {
      let msg
      try { msg = JSON.parse(e.data) } catch { msg = null }
      if (!msg || !msg.type) return
      if (msg.type === 'query_game_session' && Array.isArray(msg.items)) {
        const mapped = (msg.items || []).map(it => ({
          id: it.id ?? it.Id ?? '',
          serverName: it.server_name ?? it.ServerName ?? it.serverName ?? '',
          characterName: it.character_name ?? it.CharacterName ?? it.characterName ?? '',
          type: it.type ?? it.Type ?? '',
          statusText: it.status_text ?? it.StatusText ?? it.statusText ?? '',
          progressValue:
            typeof it.progress_value === 'number'
              ? it.progress_value
              : (typeof it.ProgressValue === 'number' ? it.ProgressValue : (parseInt(it.progressValue) || 0)),
          localAddress: it.local_address ?? it.LocalAddress ?? it.localAddress ?? '',
          identifier: it.guid ?? it.Guid ?? it.Identifier ?? it.identifier ?? ''
        }))
        const old = prevIds.value
        const newly = mapped.filter(s => !old.has(s.id))
        sessions.value = mapped
        prevIds.value = new Set(mapped.map(s => s.id))
        if (notify) {
          for (const s of newly) notify('游戏启动成功', `${s.characterName} · ${s.serverName}`, 'ok')
        }
      } else if (msg.type === 'channels_updated' || msg.type === 'shutdown_ack') {
        requestSessions()
      }
    }
  } catch {}
})
onUnmounted(() => { try { if (socket && socket.readyState === 1) socket.close() } catch {} })
</script>

<style scoped>
.game-page { display: flex; flex-direction: column; gap: 12px; width: 100%; align-self: flex-start; margin-right: auto; }
.header { display: flex; align-items: center; justify-content: space-between; }
.title { font-size: 16px; font-weight: 600; }
.empty-top { padding: 10px 12px; opacity: 0.7; }
.list { display: grid; grid-template-columns: 1fr; gap: 8px; }
.row { display: flex; align-items: center; justify-content: space-between; border: 1px solid var(--glass-border); border-radius: 12px; padding: 12px; background: var(--glass-surface); color: var(--color-text); backdrop-filter: blur(var(--glass-blur)); }
.server { font-size: 14px; font-weight: 600; }
.player { font-size: 12px; opacity: 0.7; }
.ip { font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", "Courier New", monospace; font-size: 12px; }
.actions { display: flex; gap: 8px; }
.btn { padding: 8px 12px; border: 1px solid var(--glass-border); background: var(--glass-surface); color: var(--color-text); border-radius: 8px; cursor: pointer; transition: opacity 200ms ease, transform 100ms ease; backdrop-filter: blur(var(--glass-blur)); }
.btn:hover { opacity: 0.9; }
.btn:active { transform: scale(0.98); }
.btn.danger { color: #ef4444; }
</style>
