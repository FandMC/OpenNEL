import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    vue(),
    vueDevTools(),
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    },
  },
  server: {
    proxy: (() => {
      const a = process.env.VITE_ANNOUNCEMENT_URL || 'http://23.140.4.9:3498/announcement/get'
      const au = new URL(a)
      const aOrigin = `${au.protocol}//${au.host}`
      const aPath = `${au.pathname}${au.search || ''}`

      const r = process.env.VITE_RANDOM_NAME_URL || 'https://wapi.wangyupu.com/api/nng'
      const ru = new URL(r)
      const rOrigin = `${ru.protocol}//${ru.host}`
      const rPath = `${ru.pathname}${ru.search || ''}`

      const p = process.env.VITE_PLUGIN_LIST_URL || 'https://api.opennel.top/v1/get/pluginlist'
      const pu = new URL(p)
      const pOrigin = `${pu.protocol}//${pu.host}`
      const pPath = `${pu.pathname}${pu.search || ''}`

      return {
        '/nel-announcement': {
          target: aOrigin,
          changeOrigin: true,
          rewrite: () => aPath,
        },
        '/nel-random-name': {
          target: rOrigin,
          changeOrigin: true,
          rewrite: () => rPath,
        },
        '/nel-plugin-list': {
          target: pOrigin,
          changeOrigin: true,
          rewrite: () => pPath,
        },
      }
    })()
  }
})
