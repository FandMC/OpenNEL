<script setup>
import { ref, onMounted, provide } from 'vue'
import connection from './utils/connection.js'
import Sidebar from './components/Sidebar.vue'
import ContentArea from './components/ContentArea.vue'
import Notifications from './components/Notifications.vue'
import menuItems from './config/menu.js'
const currentPage = ref(menuItems[0].key)
const notes = ref([])
function push(title, text, level = 'info') {
  const id = Math.random().toString(36).slice(2)
  notes.value.unshift({ id, title, text, level })
  setTimeout(() => { notes.value = notes.value.filter(n => n.id !== id) }, 6000)
}
function close(id) { notes.value = notes.value.filter(n => n.id !== id) }
onMounted(() => { push('通知', '通知系统已加载', 'ok') })
provide('notify', (title, text, level = 'info') => push(title, text, level))
provide('connection', connection)
</script>

<template>
  <div class="layout">
    <aside class="sidebar">
      <Sidebar v-model="currentPage" :items="menuItems" />
    </aside>
    <main class="content">
      <ContentArea :currentPage="currentPage" />
    </main>
    <Notifications :items="notes" position="top-right" @close="close" />
  </div>
</template>
