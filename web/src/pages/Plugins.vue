<template>
  <div class="plugins-page">
    <div class="card">
      <div class="card-title">
        已安装插件
        <div class="actions">
          <button class="btn" @click="restartGateway">重启</button>
        </div>
      </div>
      <div class="card-body">
        <div v-if="installed.length === 0" class="empty">暂无已安装插件</div>
        <div class="list">
          <div v-for="p in installed" :key="p.identifier || p.name" class="row">
            <div class="info">
              <div class="name">{{ p.name }}</div>
              <div class="version">{{ p.version }}</div>
              <div class="status" v-if="p.waitingRestart">已卸载，等待重启</div>
            </div>
            <div class="actions">
              <button v-if="needUpdate(p)" class="btn" @click="updatePlugin(p)">更新</button>
              <button class="btn danger" @click="uninstallPlugin(p.identifier)">卸载</button>
            </div>
          </div>
        </div>
      </div>
    </div>
    <div class="card">
      <div class="card-title">插件列表</div>
      <div class="card-body">
        <div v-if="available.length === 0" class="empty">暂无可用插件</div>
        <div class="list">
          <div v-for="it in available" :key="it.id" class="row">
            <div class="info">
              <div class="name">{{ it.name }}</div>
              <div class="version">{{ it.version || '未标注版本' }}</div>
              <div class="status">{{ it.shortDescription }}</div>
            </div>
            <div class="actions">
              <button class="btn" :disabled="isInstalled(it)" @click="downloadPlugin(it)">{{ isInstalled(it) ? '已安装' : '下载' }}</button>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue'
import appConfig from '../config/app.js'
const installed = ref([])
const available = ref([])
let socket
function requestInstalled() {
  if (!socket || socket.readyState !== 1) return
  try { socket.send(JSON.stringify({ type: 'list_installed_plugins' })) } catch {}
}
function uninstallPlugin(id) {
  if (!id) return
  if (!socket || socket.readyState !== 1) return
  try { socket.send(JSON.stringify({ type: 'uninstall_plugin', pluginId: id })) } catch {}
}
function restartGateway() {
  if (!socket || socket.readyState !== 1) return
  try { socket.send(JSON.stringify({ type: 'restart' })) } catch {}
}
function requestAvailable() {
  if (!socket || socket.readyState !== 1) return
  try { socket.send(JSON.stringify({ type: 'list_available_plugins' })) } catch {}
}
function normalize(v) {
  if (!v) return []
  return String(v).replace(/^v/i, '').split('.').map(n => parseInt(n, 10) || 0)
}
function gtVersion(a, b) {
  const aa = normalize(a), bb = normalize(b)
  const len = Math.max(aa.length, bb.length)
  for (let i = 0; i < len; i++) {
    const x = aa[i] || 0, y = bb[i] || 0
    if (x > y) return true
    if (x < y) return false
  }
  return false
}
function findAvailableById(id) {
  if (!id) return null
  const iid = String(id).toUpperCase()
  return available.value.find(it => it.id === iid) || null
}
function needUpdate(p) {
  const it = findAvailableById(p.identifier)
  if (!it) return false
  return gtVersion(it.version, p.version)
}
function isInstalled(it) {
  if (!it) return false
  const id = String(it.id || '').toUpperCase()
  return installed.value.some(p => String(p.identifier || '').toUpperCase() === id)
}
function downloadPlugin(it) {
  if (!it || !socket || socket.readyState !== 1) return
  const payload = { id: it.id, plugin: it }
  try { socket.send(JSON.stringify({ type: 'install_plugin', info: JSON.stringify(payload) })) } catch {}
}
function updatePlugin(p) {
  if (!p || !socket || socket.readyState !== 1) return
  const it = findAvailableById(p.identifier)
  if (!it) return
  const payload = { id: it.id, plugin: it }
  try { socket.send(JSON.stringify({ type: 'update_plugin', id: it.id, old: p.version || '', info: JSON.stringify(payload) })) } catch {}
}
onMounted(() => {
  try {
    socket = new WebSocket(appConfig.getWsUrl())
    socket.onopen = () => { requestInstalled(); requestAvailable() }
    socket.onmessage = (e) => {
      let msg
      try { msg = JSON.parse(e.data) } catch { msg = null }
      if (!msg || !msg.type) return
      if (msg.type === 'installed_plugins' && Array.isArray(msg.items)) {
        installed.value = (msg.items || []).map(it => ({
          identifier: it.identifier,
          name: it.name,
          version: it.version,
          waitingRestart: !!it.waitingRestart
        }))
      } else if (msg.type === 'installed_plugins_updated') {
        requestInstalled()
      } else if (msg.type === 'available_plugins' && Array.isArray(msg.items)) {
        available.value = (msg.items || []).map(x => ({
          id: (x.id || '').toUpperCase(),
          name: x.name || '',
          version: x.version || '',
          logoUrl: (x.logoUrl || '').replace(/`/g, '').trim(),
          shortDescription: x.shortDescription || '',
          publisher: x.publisher || '',
          downloadUrl: (x.downloadUrl || '').replace(/`/g, '').trim()
        }))
      }
    }
  } catch {}
})
onUnmounted(() => { try { if (socket && socket.readyState === 1) socket.close() } catch {} })
</script>

<style scoped>
.plugins-page { display: flex; flex-direction: column; gap: 16px; width: 100%; align-self: flex-start; margin-right: auto; }
.card { border: 1px solid var(--glass-border); border-radius: 12px; background: var(--glass-surface); color: var(--color-text); backdrop-filter: blur(var(--glass-blur)); }
.card-title { display: flex; align-items: center; justify-content: space-between; padding: 12px 16px; font-size: 16px; font-weight: 600; border-bottom: 1px solid var(--glass-border); }
.card-body { padding: 12px 16px; }
.empty { opacity: 0.7; }
.list { display: grid; grid-template-columns: 1fr; gap: 8px; }
.row { display: flex; align-items: center; justify-content: space-between; border: 1px solid var(--glass-border); border-radius: 12px; padding: 12px; background: var(--glass-surface); backdrop-filter: blur(var(--glass-blur)); }
.actions { display: flex; gap: 8px; }
.btn { padding: 8px 12px; border: 1px solid var(--glass-border); background: var(--glass-surface); color: var(--color-text); border-radius: 8px; cursor: pointer; transition: opacity 200ms ease, transform 100ms ease; backdrop-filter: blur(var(--glass-blur)); }
.btn:hover { opacity: 0.9; }
.btn:active { transform: scale(0.98); }
.btn.danger { color: #ef4444; }
.btn[disabled] { cursor: not-allowed; opacity: 0.6; }
.status { font-size: 12px; color: #f59e0b; }
.info { display: flex; flex-direction: column; gap: 4px; }
.name { font-size: 14px; font-weight: 600; }
.version { font-size: 12px; opacity: 0.7; }
.logo { width: 20px; height: 20px; object-fit: contain; }
</style>