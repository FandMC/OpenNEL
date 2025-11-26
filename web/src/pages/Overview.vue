<template>
  <div class="overview">
    <div class="card announcement-card">
      <div class="card-title">公告</div>
      <div class="card-body">
        <div class="announcement" :class="announcementClass">{{ announcementText }}</div>
      </div>
    </div>
    

    <div class="card accounts">
      <div class="card-title">
        账号
        <button class="add-btn" @click="showAdd = true">添加账号</button>
      </div>
      <div class="card-body">
        <ul class="account-list">
          <li v-for="(acc, i) in accounts" :key="i" class="account-item">
            <div class="account-info">
              <div class="account-id">{{ acc.entityId || '未分配' }}</div>
              <div class="account-type">{{ acc.channel }}</div>
            </div>
            <button class="del-btn" @click="removeAccount(acc)">删除</button>
          </li>
          <li v-if="accounts.length === 0" class="empty">暂无账号</li>
        </ul>
      </div>
    </div>

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
    <div class="free-row">
      <button class="btn btn-primary" :disabled="freeBusy" @click="getFreeAccount">获取账号</button>
      <div :class="['free-alert', freeLevel, { show: !!freeMessage }]">{{ freeMessage }}</div>
    </div>
    <div class="form" v-if="pc4399CaptchaUrl || pc4399NeedCaptcha">
      <label>验证码</label>
      <input v-model="pc4399Captcha" class="input" placeholder="填写验证码" />
      <img v-if="pc4399CaptchaUrl" :src="pc4399CaptchaUrl" alt="验证码" style="margin-top:8px;border:1px solid var(--color-border);border-radius:8px;max-width:100%" />
    </div>
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
    <Modal v-model="showNotice" title="OpenSDK.NEL">
      <div class="notice-text">
        <div v-for="(line,i) in noticeLines" :key="i" class="line">{{ line }}</div>
      </div>
      <template #actions>
        <button class="btn" @click="confirmNotice">确定</button>
      </template>
    </Modal>
  </div>
</template>

<script setup>
import Modal from '../components/Modal.vue'
import appConfig from '../config/app.js'
import { ref, onMounted, onUnmounted, computed, inject } from 'vue'
const notify = inject('notify', null)
const wsUrl = appConfig.getWsUrl()
const announcementText = ref('加载中...')
const announcementLevel = ref('info')
const announcementClass = computed(() => announcementLevel.value)
const accounts = ref([])
const showAdd = ref(false)
const newType = ref('cookie')
const cookieText = ref('')
const pc4399Account = ref('')
const pc4399Password = ref('')
const pc4399Captcha = ref('')
const pc4399CaptchaUrl = ref('')
const pc4399SessionId = ref('')
const pc4399NeedCaptcha = ref(false)
const pc4399IdCard = ref('')
const pc4399RealName = ref('')
const neteaseEmail = ref('')
const neteasePassword = ref('')
const connected = ref(false)
const connecting = ref(true)
let socket
const freeMessage = ref('')
const freeBusy = ref(false)
const freeLevel = ref('')
function getFreeAccount() {
  if (!socket || socket.readyState !== 1) return
  freeBusy.value = true
  freeMessage.value = ''
  freeLevel.value = ''
  try {
    if (pc4399CaptchaUrl.value && pc4399SessionId.value) {
      const cap = pc4399Captcha.value && pc4399Captcha.value.trim()
      if (!cap) { freeBusy.value = false; return }
      socket.send(JSON.stringify({
        type: 'get_free_account',
        username: pc4399Account.value,
        password: pc4399Password.value,
        idcard: pc4399IdCard.value,
        realname: pc4399RealName.value,
        captchaId: pc4399SessionId.value,
        captcha: cap
      }))
    } else {
      socket.send(JSON.stringify({ type: 'get_free_account' }))
    }
  } catch {}
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
    if (!acc || !pwd) {
      if (notify) notify('账号或密码为空', '请填写账号与密码', 'error')
      return
    }
    if (pc4399CaptchaUrl.value || pc4399NeedCaptcha.value) {
      const cap = pc4399Captcha.value && pc4399Captcha.value.trim()
      if (!cap) { if (notify) notify('验证码为空', '请填写验证码', 'error'); return }
      if (!pc4399SessionId.value) { if (notify) notify('会话缺失', '请刷新验证码后重试', 'warn'); return }
      if (notify) notify('提交验证码', '正在验证...', 'info')
      try { socket.send(JSON.stringify({ type: 'login_4399', account: acc, password: pwd, sessionId: pc4399SessionId.value, captcha: cap })) } catch {}
    } else {
      try { socket.send(JSON.stringify({ type: 'login_4399', account: acc, password: pwd })) } catch {}
    }
  } else {
    const email = neteaseEmail.value && neteaseEmail.value.trim()
    const pwd = neteasePassword.value && neteasePassword.value.trim()
    if (!email || !pwd) return
    try { socket.send(JSON.stringify({ type: 'login_x19', email, password: pwd })) } catch {}
  }
  
}

function removeAccount(acc) {
  if (!socket || socket.readyState !== 1) return
  if (!acc || !acc.entityId) return
  try { socket.send(JSON.stringify({ type: 'delete_account', entityId: acc.entityId })) } catch {}
}

const statusText = computed(() => {
  if (connecting.value) return '连接中'
  return connected.value ? '已连接' : '未连接'
})
const statusClass = computed(() => connected.value ? 'connected' : 'disconnected')

onMounted(() => {
  try {
    socket = new WebSocket(wsUrl)
    socket.onopen = () => {
      connected.value = true
      connecting.value = false
      try { socket.send(JSON.stringify({ type: 'list_accounts' })) } catch {}
    }
    socket.onclose = () => {
      connected.value = false
      connecting.value = false
    }
    socket.onerror = () => {
      connected.value = false
      connecting.value = false
    }
    socket.onmessage = (e) => {
      let msg
      try { msg = JSON.parse(e.data) } catch { msg = null }
      if (!msg || !msg.type) return
      if (msg.type === 'accounts' && Array.isArray(msg.items)) {
        accounts.value = msg.items
      } else if (msg.type === 'Success_login') {
        if (msg.entityId && msg.channel) {
          const exists = accounts.value.some(a => a.entityId === msg.entityId)
          if (!exists) accounts.value.push({ entityId: msg.entityId, channel: msg.channel })
          showAdd.value = false
          cookieText.value = ''
          pc4399Account.value = ''
          pc4399Password.value = ''
          pc4399Captcha.value = ''
          pc4399CaptchaUrl.value = ''
          pc4399SessionId.value = ''
          pc4399NeedCaptcha.value = false
          neteaseEmail.value = ''
          neteasePassword.value = ''
          freeMessage.value = ''
          freeLevel.value = ''
          freeBusy.value = false
          if (notify) notify('账号登录成功', `${msg.entityId} · ${msg.channel}`, 'ok')
        }
      } else if (msg.type === 'login_error') {
        if (notify) notify('账号登录失败', msg.message || '登录失败', 'error')
      } else if (msg.type === 'get_free_account_status') {
        freeMessage.value = msg.message || '获取中...'
        freeLevel.value = 'info'
        freeBusy.value = true
      } else if (msg.type === 'get_free_account_result') {
        freeBusy.value = false
        if (msg.success) {
          pc4399Account.value = msg.username || msg.account || ''
          pc4399Password.value = msg.password || ''
          freeMessage.value = '获取成功！已自动填充。'
          freeLevel.value = 'ok'
        } else {
          freeMessage.value = msg.message || '获取失败'
          freeLevel.value = 'error'
        }
      } else if (msg.type === 'get_free_account_requires_captcha') {
        pc4399Account.value = msg.username || pc4399Account.value || ''
        pc4399Password.value = msg.password || pc4399Password.value || ''
        pc4399CaptchaUrl.value = msg.captchaImageUrl || ''
        pc4399SessionId.value = msg.captchaId || ''
        pc4399IdCard.value = msg.idcard || ''
        pc4399RealName.value = msg.realname || ''
      } else if (msg.type === 'login_error') {
      } else if (msg.type === 'connected') {
      } else if (msg.type === 'channels') {
      } else if (msg.type === 'captcha_required') {
        pc4399Account.value = msg.account || pc4399Account.value || ''
        pc4399Password.value = msg.password || pc4399Password.value || ''
        pc4399CaptchaUrl.value = msg.captchaUrl || msg.captcha_url || ''
        pc4399SessionId.value = msg.sessionId || msg.session_id || ''
        pc4399NeedCaptcha.value = true
        newType.value = 'pc4399'
        showAdd.value = true
        if (notify) notify('需要验证码', '请完成验证码后重试', 'warn')
      }
    }
  } catch {}
  fetchAnnouncement()
  initNotice()
})
onUnmounted(() => {
  try {
    if (socket && socket.readyState === 1) socket.close()
  } catch {}
})

function fetchAnnouncement() {
  const url = appConfig.getAnnouncementUrl()
  try {
    fetch(url, { headers: { 'Accept': 'text/plain, application/json' } })
      .then(r => r.text())
      .then(t => {
        let v = t && t.trim()
        try {
          const j = JSON.parse(t)
          v = typeof j === 'string' ? j : (j.message || j.data || t)
        } catch {}
        announcementText.value = v || '暂无公告'
        announcementLevel.value = 'info'
      })
      .catch(() => {
        announcementText.value = '公告加载失败'
        announcementLevel.value = 'error'
      })
  } catch {
    announcementText.value = '公告不可用'
    announcementLevel.value = 'error'
  }
}

const showNotice = ref(false)
const noticeLines = computed(() => {
  const t = appConfig.getNoticeText()
  return t.split('\n')
})
function initNotice() {
  const key = appConfig.getNoticeKey()
  let ack
  try { ack = localStorage.getItem(key) } catch {}
  showNotice.value = !ack
}
function confirmNotice() {
  const key = appConfig.getNoticeKey()
  try { localStorage.setItem(key, '1') } catch {}
  showNotice.value = false
}
</script>

<style scoped>
.overview {
  display: flex;
  flex-direction: column;
  gap: 16px;
  width: 100%;
  height: 100%;
  align-items: flex-start;
  justify-content: flex-start;
}
.card {
  border: 1px solid var(--glass-border);
  border-radius: 12px;
  background: var(--glass-surface);
  color: var(--color-text);
  backdrop-filter: blur(12px);
}
.announcement-card { width: 100%; }
.status-card { display: none; }
.card-title {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 16px;
  font-size: 16px;
  font-weight: 600;
  border-bottom: 1px solid var(--color-border);
}
.card-body {
  padding: 12px 16px;
}
.row {
  margin-bottom: 8px;
}
.status.connected {
  margin-left: 8px;
  color: #10b981;
}
.status.disconnected {
  margin-left: 8px;
  color: #ef4444;
}
.ws {
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", "Courier New", monospace;
}
.accounts {
  width: 100%;
}
.accounts .add-btn { padding: 6px 10px; font-size: 14px; border: 1px solid var(--glass-border); background: var(--glass-surface); color: var(--color-text); border-radius: 8px; cursor: pointer; transition: opacity 200ms ease, transform 100ms ease; backdrop-filter: blur(var(--glass-blur)); }
.accounts .add-btn:hover { opacity: 0.9; }
.accounts .add-btn:active { transform: scale(0.98); }
.account-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: grid;
  grid-template-columns: 1fr;
  gap: 8px;
}
.account-item { padding: 8px 10px; border: 1px solid var(--glass-border); border-radius: 8px; display: flex; align-items: center; justify-content: space-between; background: var(--glass-surface); backdrop-filter: blur(var(--glass-blur)); }
.account-id {
  font-size: 14px;
  font-weight: 600;
}
.account-type {
  font-size: 12px;
  opacity: 0.7;
}
.del-btn { padding: 6px 10px; border: 1px solid var(--glass-border); background: var(--glass-surface); color: #ef4444; border-radius: 8px; cursor: pointer; transition: opacity 200ms ease, transform 100ms ease; backdrop-filter: blur(var(--glass-blur)); }
.del-btn:hover { opacity: 0.9; }
.del-btn:active { transform: scale(0.98); }
.empty {
  color: var(--color-text);
  opacity: 0.6;
}
.form {
  display: grid;
  gap: 8px;
}
.type-select {
  display: flex;
  gap: 8px;
  padding: 0 0 12px;
}
.seg { padding: 6px 10px; border: 1px solid var(--glass-border); background: var(--glass-surface); color: var(--color-text); border-radius: 8px; cursor: pointer; transition: opacity 200ms ease, box-shadow 200ms ease; backdrop-filter: blur(var(--glass-blur)); }
.seg.active { opacity: 0.95; box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.2); }
.input { padding: 8px 10px; border: 1px solid var(--glass-border); border-radius: 8px; background: var(--glass-surface); color: var(--color-text); transition: border-color 200ms ease, box-shadow 200ms ease; backdrop-filter: blur(var(--glass-blur)); }
.input:focus {
  outline: none;
  border-color: #10b981;
  box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.25);
}
.btn { padding: 8px 12px; border: 1px solid var(--glass-border); background: var(--glass-surface); color: var(--color-text); border-radius: 8px; cursor: pointer; transition: opacity 200ms ease, transform 100ms ease; backdrop-filter: blur(var(--glass-blur)); }
.btn:hover { opacity: 0.9; }
.btn:active {
  transform: scale(0.98);
}
.btn.secondary {
  background: var(--glass-surface);
}

.btn.btn-primary {
  border-color: #10b981;
  box-shadow: 0 0 0 2px rgba(16, 185, 129, 0.2);
}
.free-row {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-top: 8px;
}
.free-alert {
  display: none;
  font-size: 12px;
}
.free-alert.show { display: block; }
.free-alert.info { color: #1e90ff; }
.free-alert.ok { color: #10b981; }
.free-alert.error { color: #ef4444; }
.announcement { font-size: 14px; white-space: pre-wrap; }
.announcement.info { color: var(--color-text); opacity: 0.9; }
.announcement.error { color: #ef4444; }
.notice-text { display: grid; gap: 6px; }
.notice-text .line { white-space: pre-wrap; }
</style>