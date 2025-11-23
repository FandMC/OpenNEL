<template>
  <div class="servers-page">
    <div class="header">
      <div class="title">网络服务器</div>
      <div class="search">
        <input class="input" v-model="keyword" placeholder="搜索服务器" @input="onInput" />
      </div>
    </div>
    <div v-if="notlogin" class="hint">未登录</div>
    <div v-else>
      <div v-if="servers.length === 0" class="empty-top">暂无服务器</div>
      <div class="grid">
        <div v-for="s in servers" :key="s.entityId" class="card">
          <div class="name">{{ s.name }}</div>
          <div class="id">id: {{ s.entityId }}</div>
          <button class="btn join" @click="openJoin(s)">加入服务器</button>
        </div>
      </div>
    </div>
    <Modal v-model="showJoin" title="加入服务器">
      <div class="join-body">
        <div class="section">
          <div class="section-title">选择账号</div>
          <Dropdown v-model="selectedAccountId" :items="accountItems" placeholder="请选择账号" @update:modelValue="onSelectAccount" />
        </div>
        <div class="section">
          <div class="section-title">选择角色</div>
          <Dropdown v-model="selectedRoleId" :items="roleItems" placeholder="请选择角色" />
        </div>
      </div>
      <template #actions>
        <button class="btn" @click="showAddRole = true">添加角色</button>
        <button class="btn btn-primary" @click="startJoin">启动</button>
      </template>
    </Modal>
    <Modal v-model="showAddRole" title="添加角色">
      <div class="form">
        <input v-model="newRoleName" class="input" placeholder="输入角色名称" />
        <div class="row-actions">
          <div class="row-actions-inner">
            <button class="btn" @click="randomRoleName('网易')">随机名字/网易</button>
            <button class="btn" @click="randomRoleName('离线')">随机名字/离线</button>
            <button class="btn" @click="randomRoleName('中文')">随机名字/中文</button>
          </div>
          <button class="btn btn-primary" @click="createRole">添加</button>
        </div>
      </div>
      <template #actions>
        <button class="btn" @click="showAddRole = false">关闭</button>
      </template>
    </Modal>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted, computed } from 'vue'
import appConfig from '../config/app.js'
import Modal from '../components/Modal.vue'
import Dropdown from '../components/Dropdown.vue'
import { RandomName } from '../utils/random_name.js'
const servers = ref([])
const connected = ref(false)
const notlogin = ref(false)
let socket
const keyword = ref('')
let timer = null
const showJoin = ref(false)
const showAddRole = ref(false)
const accounts = ref([])
const roles = ref([])
const selectedAccountId = ref('')
const selectedRoleId = ref('')
const joinServerId = ref('')
const joinServerName = ref('')
const newRoleName = ref('')
const accountItems = computed(() => accounts.value.map(a => ({ label: a.entityId, description: a.channel, value: a.entityId })))
const roleItems = computed(() => roles.value.map(r => ({ label: r.name, description: '', value: r.id })))
function onSelectAccount(id) { selectAccount(id) }
function openJoin(s) {
  if (!socket || socket.readyState !== 1) return
  joinServerId.value = s.entityId
  joinServerName.value = s.name
  showJoin.value = true
  
  roleOpen.value = false
  try { socket.send(JSON.stringify({ type: 'list_accounts' })) } catch {}
  try { socket.send(JSON.stringify({ type: 'open_server', serverId: s.entityId, serverName: s.name })) } catch {}
}
function selectAccount(id) {
  if (!socket || socket.readyState !== 1) return
  selectedAccountId.value = id
  try { socket.send(JSON.stringify({ type: 'select_account', entityId: id })) } catch {}
  try { socket.send(JSON.stringify({ type: 'open_server', serverId: joinServerId.value, serverName: joinServerName.value })) } catch {}
  
}
function startJoin() {
  if (!socket || socket.readyState !== 1) return
  const rid = selectedRoleId.value
  if (!rid) return
  try { socket.send(JSON.stringify({ type: 'start_proxy', serverId: joinServerId.value, serverName: joinServerName.value, roleId: rid })) } catch {}
  showJoin.value = false
}
async function randomRoleName(type) {
  switch (type) {
    case '网易':
      newRoleName.value = (await RandomName.official()) ?? RandomName.offline()
      break
    case '离线':
      newRoleName.value = RandomName.offline()
      break
    case '中文':
      newRoleName.value = RandomName.gb2312()
      break
    default:
      newRoleName.value = RandomName.offline()
      break
  }
}
function createRole() {
  const name = (newRoleName.value || '').trim()
  if (!name || !socket || socket.readyState !== 1) return
  try { socket.send(JSON.stringify({ type: 'create_role_named', serverId: joinServerId.value, name })) } catch {}
  showAddRole.value = false
}
function doSearch() {
  if (!socket || socket.readyState !== 1) return
  const q = (keyword.value || '').trim()
  if (q) {
    try { socket.send(JSON.stringify({ type: 'search_servers', keyword: q })) } catch {}
  } else {
    try { socket.send(JSON.stringify({ type: 'list_servers' })) } catch {}
  }
}
function onInput() {
  if (timer) clearTimeout(timer)
  timer = setTimeout(doSearch, 300)
}
onMounted(() => {
  try {
    socket = new WebSocket(appConfig.getWsUrl())
    socket.onopen = () => {
      connected.value = true
      try { socket.send(JSON.stringify({ type: 'list_accounts' })) } catch {}
      try { socket.send(JSON.stringify({ type: 'list_servers' })) } catch {}
    }
    socket.onmessage = (e) => {
      let msg
      try { msg = JSON.parse(e.data) } catch { msg = null }
      if (!msg || !msg.type) return
      if (msg.type === 'servers' && Array.isArray(msg.items)) {
        servers.value = msg.items
      } else if (msg.type === 'notlogin') {
        notlogin.value = true
      } else if (msg.type === 'servers_error') {
      } else if (msg.type === 'accounts' && Array.isArray(msg.items)) {
        accounts.value = msg.items
      } else if (msg.type === 'server_roles' && Array.isArray(msg.items)) {
        roles.value = msg.items
        if (msg.createdName) {
          const found = roles.value.find(r => r.name === msg.createdName)
          if (found) selectedRoleId.value = found.id
        }
      }
    }
    socket.onclose = () => { connected.value = false }
    socket.onerror = () => {}
  } catch {}
})
onUnmounted(() => {
  try { if (socket && socket.readyState === 1) socket.close() } catch {}
})
</script>

<style scoped>
.servers-page { display: flex; flex-direction: column; gap: 12px; width: 100%; align-self: flex-start; margin-right: auto; }
.header { display: flex; align-items: center; justify-content: space-between; }
.title { font-size: 16px; font-weight: 600; }
.search { width: 240px; }
.input { padding: 8px 10px; border: 1px solid var(--glass-border); border-radius: 8px; background: var(--glass-surface); color: var(--color-text); width: 100%; transition: border-color 200ms ease, box-shadow 200ms ease; backdrop-filter: blur(var(--glass-blur)); }
.input:focus { outline: none; border-color: #10b981; box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.25); }
.empty-top { padding: 10px 12px; opacity: 0.7; }
.hint { padding: 12px; border: 1px solid var(--glass-border); border-radius: 8px; background: var(--glass-surface); backdrop-filter: blur(var(--glass-blur)); }
.grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 12px; }
.card { border: 1px solid var(--glass-border); border-radius: 12px; padding: 12px; background: var(--glass-surface); color: var(--color-text); display: flex; flex-direction: column; gap: 8px; backdrop-filter: blur(var(--glass-blur)); }
.name { font-size: 14px; font-weight: 600; }
.id { font-size: 12px; opacity: 0.7; }
.btn.join { padding: 8px 12px; border: 1px solid var(--glass-border); background: var(--glass-surface); color: var(--color-text); border-radius: 8px; cursor: pointer; transition: opacity 200ms ease, transform 100ms ease; backdrop-filter: blur(var(--glass-blur)); }
.btn.join:hover { opacity: 0.9; }
.btn.join:active { transform: scale(0.98); }
.empty { opacity: 0.6; }

.btn { padding: 8px 12px; border: 1px solid var(--glass-border); background: var(--glass-surface); color: var(--color-text); border-radius: 8px; cursor: pointer; transition: opacity 200ms ease, transform 100ms ease; backdrop-filter: blur(var(--glass-blur)); }
.btn:hover { opacity: 0.9; }
.btn:active { transform: scale(0.98); }
.btn-primary { border-color: #10b981; box-shadow: 0 0 0 2px rgba(16, 185, 129, 0.2); }

.join-body { display: grid; gap: 16px; }
.section { display: grid; gap: 8px; }
.section-title { font-size: 14px; font-weight: 600; }
.empty-tip { opacity: 0.7; }
.form { display: grid; gap: 8px; }
.row-actions { display: flex; gap: 8px; flex-direction: column; }
.row-actions-inner { display: flex; gap: 8px; flex-direction: row; justify-content: space-between; }
</style>