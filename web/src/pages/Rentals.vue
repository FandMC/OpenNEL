<template>
  <div class="rentals-page">
    <div class="header">
      <div class="title">租赁服</div>
    </div>
    <div v-if="notlogin" class="hint">未登录</div>
    <div v-else>
      <div v-if="rentals.length === 0" class="empty-top">暂无租赁服</div>
      <div class="grid">
        <div v-for="s in rentals" :key="s.entityId" class="card">
          <div class="name">{{ s.name }}</div>
          <div class="id">id: {{ s.entityId }}</div>
          <button class="btn join" @click="openJoin(s)">加入租赁服</button>
        </div>
      </div>
    </div>
    <Modal v-model="showJoin" title="加入租赁服">
      <div class="join-body">
        <div class="section">
          <div class="section-title">选择账号</div>
          <Dropdown v-model="selectedAccountId" :items="accountItems" placeholder="请选择账号" @update:modelValue="onSelectAccount" />
        </div>
        <div class="section">
          <div class="section-title">选择角色</div>
          <Dropdown v-model="selectedRoleId" :items="roleItems" placeholder="请选择角色" />
        </div>
        <div class="section">
          <div class="section-title">服务器密码（可选）</div>
          <input v-model="rentalPassword" class="input" placeholder="输入密码（如需）" />
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
    <Modal v-model="showAdd" title="添加账号">
      <div class="type-select">
        <button :class="['seg', newType === 'cookie' ? 'active' : '']" @click="newType = 'cookie'">Cookie</button>
        <button :class="['seg', newType === 'pc4399' ? 'active' : '']" @click="newType = 'pc4399'">PC4399</button>
        <button :class="['seg', newType === 'netease' ? 'active' : '']" @click="newType = 'netease'">网易邮箱</button>
      </div>
      <div class="form" v-if="newType === 'cookie'">
        <label>Cookie</label>
        <input v-model="cookieText" class="input" placeholder="填写Cookie" />
      </div>
      <div class="form" v-else-if="newType === 'pc4399'">
        <label>账号</label>
        <input v-model="pc4399Account" class="input" placeholder="填写账号" />
        <label>密码</label>
        <input v-model="pc4399Password" type="password" class="input" placeholder="填写密码" />
      </div>
      <div class="form" v-else>
        <label>邮箱</label>
        <input v-model="neteaseEmail" class="input" placeholder="填写邮箱" />
        <label>密码</label>
        <input v-model="neteasePassword" type="password" class="input" placeholder="填写密码" />
      </div>
      <template #actions>
        <button class="btn" @click="confirmAdd">确定</button>
        <button class="btn secondary" @click="showAdd = false">取消</button>
      </template>
    </Modal>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted, inject, computed } from 'vue'
import Modal from '../components/Modal.vue'
import Dropdown from '../components/Dropdown.vue'
import appConfig from '../config/app.js'
import { RandomName } from '../utils/random_name.js'
const notify = inject('notify', null)
let socket
const rentals = ref([])
const roles = ref([])
const accounts = ref([])
const showJoin = ref(false)
const showAddRole = ref(false)
const showAdd = ref(false)
const selectedRentalId = ref('')
const selectedRentalName = ref('')
const selectedAccountId = ref('')
const selectedRoleId = ref('')
const newRoleName = ref('')
const rentalPassword = ref('')
const notlogin = ref(false)
const cookieText = ref('')
const newType = ref('cookie')
const pc4399Account = ref('')
const pc4399Password = ref('')
const neteaseEmail = ref('')
const neteasePassword = ref('')
const accountItems = computed(() => accounts.value.map(a => ({ label: a.entityId, description: a.channel, value: a.entityId })))
const roleItems = computed(() => roles.value.map(r => ({ label: r.name, description: '', value: r.id })))

function openJoin(srv) {
  if (!socket || socket.readyState !== 1) return
  selectedRentalId.value = srv.entityId
  selectedRentalName.value = srv.name
  showJoin.value = true
  try { socket.send(JSON.stringify({ type: 'list_accounts' })) } catch {}
  try { socket.send(JSON.stringify({ type: 'open_rental_server', serverId: srv.entityId })) } catch {}
}
function onSelectAccount(id) { selectAccount(id) }
function selectAccount(id) {
  if (!socket || socket.readyState !== 1) return
  selectedAccountId.value = id
  try { socket.send(JSON.stringify({ type: 'select_account', entityId: id })) } catch {}
  try { socket.send(JSON.stringify({ type: 'open_rental_server', serverId: selectedRentalId.value })) } catch {}
}
function startJoin() {
  if (!socket || socket.readyState !== 1) return
  const rid = selectedRoleId.value
  if (!rid) return
  try { socket.send(JSON.stringify({ type: 'start_rental_proxy', serverId: selectedRentalId.value, role: rid, password: rentalPassword.value })) } catch {}
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
  try { socket.send(JSON.stringify({ type: 'create_rental_character', serverId: selectedRentalId.value, name })) } catch {}
  showAddRole.value = false
}
function confirmAdd() {
  if (!socket || socket.readyState !== 1) return
  if (newType.value === 'cookie') {
    const v = cookieText.value && cookieText.value.trim()
    if (!v) return
    try { socket.send(JSON.stringify({ type: 'cookie_login', cookie: v })) } catch {}
  } else if (newType.value === 'pc4399') {
    const acc = pc4399Account.value && pc4399Account.value.trim()
    const pwd = pc4399Password.value && pc4399Password.value.trim()
    if (!acc || !pwd) return
    try { socket.send(JSON.stringify({ type: 'login_4399', account: acc, password: pwd })) } catch {}
  } else {
    const email = neteaseEmail.value && neteaseEmail.value.trim()
    const pwd = neteasePassword.value && neteasePassword.value.trim()
    if (!email || !pwd) return
    try { socket.send(JSON.stringify({ type: 'login_x19', email, password: pwd })) } catch {}
  }
}

onMounted(() => {
  try {
    socket = new WebSocket(appConfig.getWsUrl())
    socket.onopen = () => {
      try { socket.send(JSON.stringify({ type: 'list_rental_servers' })) } catch {}
    }
    socket.onmessage = (e) => {
      let msg
      try { msg = JSON.parse(e.data) } catch { msg = null }
      if (!msg || !msg.type) return
      if (msg.type === 'rentals' && Array.isArray(msg.items)) {
        rentals.value = msg.items
      } else if (msg.type === 'rental_roles') {
        roles.value.push({ id: msg.id, name: msg.name })
      } else if (msg.type === 'accounts' && Array.isArray(msg.items)) {
        accounts.value = msg.items
        if (msg.items.length === 1) {
          const accountId = msg.items[0].entityId
          selectedAccountId.value = accountId
          try { socket.send(JSON.stringify({ type: 'select_account', entityId: accountId })) } catch {}
          try { socket.send(JSON.stringify({ type: 'open_rental_server', serverId: selectedRentalId.value })) } catch {}
        }
      } else if (msg.type === 'channels_updated') {
        showJoin.value = false
      } else if (msg.type === 'notlogin') {
        notlogin.value = true
      } else if (msg.type && msg.type.endsWith('_error')) {
        if (notify) notify('操作失败', msg.message || '失败', 'error')
      } else if (msg.type === 'channels_updated') {
        if (notify) notify('代理已启动', selectedRentalName.value || '', 'ok')
        showJoin.value = false
      }
    }
  } catch {}
})

onUnmounted(() => { try { if (socket && socket.readyState === 1) socket.close() } catch {} })
</script>

<style scoped>
.rentals-page { display: flex; flex-direction: column; gap: 12px; width: 100%; align-self: flex-start; margin-right: auto; }
.header { display: flex; align-items: center; justify-content: space-between; }
.title { font-size: 16px; font-weight: 600; }
.empty-top { padding: 10px 12px; opacity: 0.7; }
.hint { padding: 12px; border: 1px solid var(--glass-border); border-radius: 8px; background: var(--glass-surface); backdrop-filter: blur(var(--glass-blur)); }
.grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 12px; }
.card { border: 1px solid var(--glass-border); border-radius: 12px; padding: 12px; background: var(--glass-surface); color: var(--color-text); display: flex; flex-direction: column; gap: 8px; backdrop-filter: blur(var(--glass-blur)); }
.name { font-size: 14px; font-weight: 600; }
.id { font-size: 12px; opacity: 0.7; }
.btn.join { padding: 8px 12px; border: 1px solid var(--glass-border); background: var(--glass-surface); color: var(--color-text); border-radius: 8px; cursor: pointer; transition: opacity 200ms ease, transform 100ms ease; backdrop-filter: blur(var(--glass-blur)); }
.btn.join:hover { opacity: 0.9; }
.btn.join:active { transform: scale(0.98); }
.btn { padding: 8px 12px; border: 1px solid var(--glass-border); background: var(--glass-surface); color: var(--color-text); border-radius: 8px; cursor: pointer; transition: opacity 200ms ease, transform 100ms ease; backdrop-filter: blur(var(--glass-blur)); }
.btn:hover { opacity: 0.9; }
.btn:active { transform: scale(0.98); }
.btn-primary { border-color: #10b981; box-shadow: 0 0 0 2px rgba(16, 185, 129, 0.2); }
.join-body { display: grid; gap: 16px; }
.section { display: grid; gap: 8px; }
.section-title { font-size: 14px; font-weight: 600; }
.form { display: grid; gap: 8px; }
.row-actions { display: flex; gap: 8px; flex-direction: column; }
.row-actions-inner { display: flex; gap: 8px; flex-direction: row; justify-content: space-between; }
.type-select { display: flex; gap: 8px; padding: 0 0 12px; }
.seg { padding: 6px 10px; border: 1px solid var(--glass-border); background: var(--glass-surface); color: var(--color-text); border-radius: 8px; cursor: pointer; transition: opacity 200ms ease, box-shadow 200ms ease; backdrop-filter: blur(var(--glass-blur)); }
.seg.active { opacity: 0.95; box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.2); }
.input { padding: 8px 10px; border: 1px solid var(--glass-border); border-radius: 8px; background: var(--glass-surface); color: var(--color-text); transition: border-color 200ms ease, box-shadow 200ms ease; backdrop-filter: blur(var(--glass-blur)); }
.input:focus { outline: none; border-color: #10b981; box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.25); }
</style>
