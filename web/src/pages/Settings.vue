<template>
  <div class="settings">
    <div class="card">
      <div class="card-title">设置</div>
      <div class="card-body">
        <div class="row">在此配置客户端选项</div>
        <div class="row">
          <div class="section-title">主题</div>
          <div class="type-select">
            <button :class="['seg', themeMode === 'color' ? 'active' : '']" @click="themeMode = 'color'">纯色</button>
            <button :class="['seg', themeMode === 'image' ? 'active' : '']" @click="themeMode = 'image'">图片</button>
          </div>
          <div v-if="themeMode === 'color'" class="theme-form">
            <label>颜色</label>
            <input type="color" v-model="themeColor" class="input" />
            <input v-model="themeColor" class="input" placeholder="#181818" />
            <div class="actions">
              <button class="btn btn-primary" :disabled="busy" @click="applyTheme">应用</button>
            </div>
          </div>
          <div v-else class="theme-form">
            <label>图片地址</label>
            <input v-model="themeImage" class="input" placeholder="填写图片URL" />
            <label>本地图片</label>
            <input type="file" accept="image/*" class="input" @change="onPickImage" />
            <div class="actions">
              <button class="btn" :disabled="busy" @click="previewImage">预览</button>
              <button class="btn btn-primary" :disabled="busy" @click="applyTheme">应用</button>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, inject } from 'vue'
import appConfig from '../config/app.js'
const notify = inject('notify', null)
const themeMode = ref('image')
const themeColor = ref('#181818')
const themeImage = ref('')
const themeImageData = ref('')
const busy = ref(false)
let socket
function loadThemeFromServer() {
  try {
    if (!socket || socket.readyState !== 1) return
    socket.send(JSON.stringify({ type: 'get_settings' }))
  } catch {}
}
function applyStyle() {
  try {
    if (themeMode.value === 'color') {
      document.documentElement.style.setProperty('--color-background', themeColor.value || '#181818')
      document.body.style.backgroundImage = 'none'
      document.body.style.backgroundColor = themeColor.value || '#181818'
    } else {
      const src = themeImageData.value || themeImage.value || ''
      if (src) {
        document.body.style.backgroundImage = `url('${src}')`
      }
    }
  } catch {}
}
function applyTheme() {
  busy.value = true
  try {
    if (!socket || socket.readyState !== 1) return
    const src = themeMode.value === 'color' ? '' : (themeImageData.value || themeImage.value || '')
    socket.send(JSON.stringify({ type: 'update_settings', mode: themeMode.value, color: themeColor.value, image: src }))
    applyStyle()
  } catch {}
  busy.value = false
}
function onPickImage(e) {
  try {
    const f = e && e.target && e.target.files && e.target.files[0]
    if (!f) return
    const r = new FileReader()
    r.onload = () => { themeImageData.value = typeof r.result === 'string' ? r.result : '' }
    r.readAsDataURL(f)
  } catch {}
}
function previewImage() { applyStyle() }
onMounted(() => {
  try {
    socket = new WebSocket(appConfig.getWsUrl())
    socket.onopen = () => { loadThemeFromServer() }
    socket.onmessage = (e) => {
      let msg
      try { msg = JSON.parse(e.data) } catch { msg = null }
      if (!msg || !msg.type) return
      if (msg.type === 'settings') {
        themeMode.value = msg.mode || 'image'
        themeColor.value = msg.color || '#181818'
        themeImage.value = msg.image || ''
        applyStyle()
      } else if (msg.type === 'update_settings_error') {
        if (notify) notify('更新设置失败', msg.message || '失败', 'error')
      } else if (msg.type === 'settings_updated') {
        if (notify) notify('设置已更新', '已保存到服务器', 'ok')
      }
    }
    socket.onclose = () => {}
    socket.onerror = () => {}
  } catch {}
  applyStyle()
})
</script>

<style scoped>
.settings { display: flex; flex-direction: column; gap: 16px; width: 100%; height: 100%; }
.card { border: 1px solid var(--glass-border); border-radius: 12px; background: var(--glass-surface); color: var(--color-text); backdrop-filter: blur(12px); }
.card-title { display: flex; align-items: center; justify-content: space-between; padding: 12px 16px; font-size: 16px; font-weight: 600; border-bottom: 1px solid var(--color-border); }
.card-body { padding: 12px 16px; }
.row { margin-bottom: 8px; }
.section-title { font-weight: 600; margin-bottom: 8px; }
.type-select { display: flex; gap: 8px; padding: 0 0 12px; }
.seg { padding: 6px 10px; border: 1px solid var(--glass-border); background: var(--glass-surface); color: var(--color-text); border-radius: 8px; cursor: pointer; transition: opacity 200ms ease, box-shadow 200ms ease; backdrop-filter: blur(var(--glass-blur)); }
.seg.active { opacity: 0.95; box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.2); }
.input { padding: 8px 10px; border: 1px solid var(--glass-border); border-radius: 8px; background: var(--glass-surface); color: var(--color-text); transition: border-color 200ms ease, box-shadow 200ms ease; backdrop-filter: blur(var(--glass-blur)); width: 100%; }
.input:focus { outline: none; border-color: #10b981; box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.25); }
.btn { padding: 8px 12px; border: 1px solid var(--glass-border); background: var(--glass-surface); color: var(--color-text); border-radius: 8px; cursor: pointer; transition: opacity 200ms ease, transform 100ms ease; backdrop-filter: blur(var(--glass-blur)); }
.btn:hover { opacity: 0.9; }
.btn:active { transform: scale(0.98); }
.btn.btn-primary { border-color: #10b981; box-shadow: 0 0 0 2px rgba(16, 185, 129, 0.2); }
.actions { display: flex; gap: 8px; margin-top: 8px; }
.theme-form { display: grid; gap: 8px; }
</style>
