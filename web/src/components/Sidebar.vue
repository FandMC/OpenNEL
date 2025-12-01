<template>
  <nav class="menu">
    <div class="menu-header">
      <div class="brand">
        <img class="menu-logo" src="./favicon.ico" alt="logo" />
        <span class="menu-name">OpenNEL</span>
      </div>
      <div class="header-actions">
        <a
          class="icon-btn"
          :href="socialLinks.github"
          target="_blank"
          rel="noopener noreferrer"
          aria-label="GitHub"
        >
          <svg class="icon" viewBox="0 0 16 16" aria-hidden="true">
            <path d="M8 0C3.58 0 0 3.58 0 8c0 3.54 2.29 6.54 5.47 7.59.4.07.55-.17.55-.38 0-.19-.01-.82-.01-1.49-2 .37-2.53-.49-2.69-.94-.09-.23-.48-.94-.82-1.13-.28-.15-.68-.52-.01-.53.63-.01 1.08.58 1.23.82.72 1.21 1.87.87 2.33.66.07-.52.28-.87.51-1.07-1.78-.2-3.64-.89-3.64-3.95 0-.87.31-1.59.82-2.15-.08-.2-.36-1.02.08-2.12 0 0 .67-.21 2.2.82.64-.18 1.32-.27 2-.27s1.36.09 2 .27c1.53-1.04 2.2-.82 2.2-.82.44 1.1.16 1.92.08 2.12.51.56.82 1.27.82 2.15 0 3.07-1.87 3.75-3.65 3.95.29.25.54.73.54 1.48 0 1.07-.01 1.93-.01 2.2 0 .21.15.46.55.38A8.013 8.013 0 0016 8c0-4.42-3.58-8-8-8z"/>
          </svg>
        </a>
        <a
          class="icon-btn"
          :href="socialLinks.qq"
          target="_blank"
          rel="noopener noreferrer"
          aria-label="QQ群"
        >
          <svg xmlns="http://www.w3.org/2000/svg" height="800" width="1200" viewBox="-18.15 -35.9725 157.3 215.835"><path fill="#faab07" d="M60.503 142.237c-12.533 0-24.038-4.195-31.445-10.46-3.762 1.124-8.574 2.932-11.61 5.175-2.6 1.918-2.275 3.874-1.807 4.663 2.056 3.47 35.273 2.216 44.862 1.136zm0 0c12.535 0 24.039-4.195 31.447-10.46 3.76 1.124 8.573 2.932 11.61 5.175 2.598 1.918 2.274 3.874 1.805 4.663-2.056 3.47-35.272 2.216-44.862 1.136zm0 0"/><path d="M60.576 67.119c20.698-.14 37.286-4.147 42.907-5.683 1.34-.367 2.056-1.024 2.056-1.024.005-.189.085-3.37.085-5.01C105.624 27.768 92.58.001 60.5 0 28.42.001 15.375 27.769 15.375 55.401c0 1.642.08 4.822.086 5.01 0 0 .583.615 1.65.913 5.19 1.444 22.09 5.65 43.312 5.795zm56.245 23.02c-1.283-4.129-3.034-8.944-4.808-13.568 0 0-1.02-.126-1.537.023-15.913 4.623-35.202 7.57-49.9 7.392h-.153c-14.616.175-33.774-2.737-49.634-7.315-.606-.175-1.802-.1-1.802-.1-1.774 4.624-3.525 9.44-4.808 13.568-6.119 19.69-4.136 27.838-2.627 28.02 3.239.392 12.606-14.821 12.606-14.821 0 15.459 13.957 39.195 45.918 39.413h.848c31.96-.218 45.917-23.954 45.917-39.413 0 0 9.368 15.213 12.607 14.822 1.508-.183 3.491-8.332-2.627-28.021"/><path fill="#fff" d="M49.085 40.824c-4.352.197-8.07-4.76-8.304-11.063-.236-6.305 3.098-11.576 7.45-11.773 4.347-.195 8.064 4.76 8.3 11.065.238 6.306-3.097 11.577-7.446 11.771m31.133-11.063c-.233 6.302-3.951 11.26-8.303 11.063-4.35-.195-7.684-5.465-7.446-11.77.236-6.305 3.952-11.26 8.3-11.066 4.352.197 7.686 5.468 7.449 11.773"/><path fill="#faab07" d="M87.952 49.725C86.79 47.15 75.077 44.28 60.578 44.28h-.156c-14.5 0-26.212 2.87-27.375 5.446a.863.863 0 00-.085.367.88.88 0 00.16.496c.98 1.427 13.985 8.487 27.3 8.487h.156c13.314 0 26.319-7.058 27.299-8.487a.873.873 0 00.16-.498.856.856 0 00-.085-.365"/><path d="M54.434 29.854c.199 2.49-1.167 4.702-3.046 4.943-1.883.242-3.568-1.58-3.768-4.07-.197-2.492 1.167-4.704 3.043-4.944 1.886-.244 3.574 1.58 3.771 4.07m11.956.833c.385-.689 3.004-4.312 8.427-2.993 1.425.347 2.084.857 2.223 1.057.205.296.262.718.053 1.286-.412 1.126-1.263 1.095-1.734.875-.305-.142-4.082-2.66-7.562 1.097-.24.257-.668.346-1.073.04-.407-.308-.574-.93-.334-1.362"/><path fill="#fff" d="M60.576 83.08h-.153c-9.996.12-22.116-1.204-33.854-3.518-1.004 5.818-1.61 13.132-1.09 21.853 1.316 22.043 14.407 35.9 34.614 36.1h.82c20.208-.2 33.298-14.057 34.616-36.1.52-8.723-.087-16.035-1.092-21.854-11.739 2.315-23.862 3.64-33.86 3.518"/><path fill="#eb1923" d="M32.102 81.235v21.693s9.937 2.004 19.893.616V83.535c-6.307-.357-13.109-1.152-19.893-2.3"/><path fill="#eb1923" d="M105.539 60.412s-19.33 6.102-44.963 6.275h-.153c-25.591-.172-44.896-6.255-44.962-6.275L8.987 76.57c16.193 4.882 36.261 8.028 51.436 7.845h.153c15.175.183 35.242-2.963 51.437-7.845zm0 0"/></svg>
        </a>
      </div>
    </div>
    <ul class="menu-list" ref="listRef">
      <div class="indicator" :style="indicatorStyle"></div>
      <li
        v-for="(item, idx) in items"
        :key="item.key"
        class="menu-item"
        :class="{ active: item.key === modelValue }"
        :ref="el => itemRefs[idx] = el"
        @click="$emit('update:modelValue', item.key)"
      >
        {{ item.label }}
      </li>
    </ul>
    <div class="menu-footer">
      <div class="conn" :class="connClass">{{ connText }}</div>
      <div class="actions">
        <button class="btn" @click="doDisconnect">断开</button>
        <button class="btn" @click="doReconnect">重连</button>
      </div>
    </div>
  </nav>
</template>

<script setup>
import { defineProps, defineEmits, ref, onMounted, watch, nextTick, computed, inject } from 'vue'
const props = defineProps({
  modelValue: String,
  items: Array,
  links: Object
})
defineEmits(['update:modelValue'])
const connection = inject('connection', null)

const listRef = ref(null)
const itemRefs = ref([])
const y = ref(0)
const h = ref(0)
const indicatorStyle = computed(() => ({
  transform: `translateY(${y.value + 3}px)`,
  height: `${Math.max(0, h.value - 6)}px`
}))

const socialLinks = computed(() => ({
  github: (props.links && props.links.github) ? props.links.github : 'https://github.com/FandMC/OpenNEL',
  qq: (props.links && props.links.qq) ? props.links.qq : 'https://qm.qq.com/q/aYMYxiZnhK'
}))


function updateIndicator() {
  nextTick(() => {
    const idx = props.items.findIndex(i => i.key === props.modelValue)
    const el = itemRefs.value[idx]
    if (!el || !listRef.value) return
    const top = el.offsetTop
    const height = el.offsetHeight
    y.value = top
    h.value = height
  })
}

onMounted(updateIndicator)
watch(() => props.modelValue, updateIndicator)

const connText = computed(() => connection ? connection.statusText.value : '未连接')
const connClass = computed(() => connection ? connection.statusClass.value : 'disconnected')
function doDisconnect() { if (connection) connection.disconnect() }
function doReconnect() { if (connection) connection.reconnect() }
</script>

<style scoped>
.menu {
  display: flex;
  flex-direction: column;
  height: 100%;
}
.menu-header {
  padding: 16px;
  font-size: 16px;
  font-weight: 600;
  color: var(--color-text);
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.menu-title {
  line-height: 1;
}
.brand { display: flex; align-items: center; gap: 8px; }
.menu-logo { width: 16px; height: 16px; border-radius: 4px; }
.menu-name { font-size: 16px; font-weight: 600; color: var(--color-text); }
.header-actions {
  display: flex;
  align-items: center;
  gap: 8px;
}
.icon-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  border-radius: 6px;
  color: var(--color-text);
  text-decoration: none;
}
.icon-btn:hover {
  background: var(--glass-surface);
  backdrop-filter: blur(var(--glass-blur));
}
.icon {
  width: 18px;
  height: 18px;
  fill: currentColor;
}
.menu-list {
  list-style: none;
  margin: 0;
  padding: 0 8px;
  position: relative;
}
.menu-item {
  position: relative;
  padding: 10px 8px 10px 14px;
  border-radius: 6px;
  cursor: pointer;
}
.menu-item:hover {
  background: var(--glass-surface);
  backdrop-filter: blur(var(--glass-blur));
}
.indicator {
  position: absolute;
  left: 10px;
  width: 4px;
  background: #10b981;
  border-radius: 2px;
  z-index: 1;
  transition: transform 200ms ease, height 200ms ease;
}
.menu-footer {
  margin-top: auto;
  padding: 12px 16px;
  border-top: 1px solid var(--color-border);
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.conn { font-size: 12px; }
.conn.connected { color: #10b981; }
.conn.disconnected { color: #ef4444; }
.actions { display: flex; gap: 8px; }
.btn { padding: 6px 10px; border: 1px solid var(--glass-border); background: var(--glass-surface); color: var(--color-text); border-radius: 8px; cursor: pointer; }
.btn:hover { opacity: 0.9; }
</style>
