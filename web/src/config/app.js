const getProtocol = () => {
  if (typeof window !== 'undefined' && window.location) {
    return window.location.protocol === 'https:' ? 'wss:' : 'ws:'
  }
  return 'ws:'
}
const getPort = () => {
  const fromStorage = typeof localStorage !== 'undefined' ? localStorage.getItem('NEL_PORT') : null
  const fromEnv = typeof import.meta !== 'undefined' ? import.meta.env && import.meta.env.VITE_NEL_PORT : null
  return fromStorage || fromEnv || null
}
const getHost = () => (typeof window !== 'undefined' && window.location && window.location.hostname) ? window.location.hostname : '127.0.0.1'
const getDefault = () => `${getHost()}:8080`
export const getWsUrl = () => {
  const proto = getProtocol()
  const port = getPort()
  const host = getHost()
  const authority = port ? `${host}:${port}` : getDefault()
  return `${proto}//${authority}/ws`
}
const getPluginListUrl = () => {
  const fromStorage = typeof localStorage !== 'undefined' ? localStorage.getItem('NEL_PLUGIN_LIST_URL') : null
  const fromEnv = typeof import.meta !== 'undefined' ? import.meta.env && import.meta.env.VITE_PLUGIN_LIST_URL : null
  const isDev = typeof import.meta !== 'undefined' && import.meta.env && import.meta.env.DEV
  return fromStorage || fromEnv || (isDev ? '/nel-plugin-list' : '/nel-plugin-list')
}
const getRandomNameUrl = () => {
  const fromStorage = typeof localStorage !== 'undefined' ? localStorage.getItem('NEL_RANDOM_NAME_URL') : null
  const fromEnv = typeof import.meta !== 'undefined' ? import.meta.env && import.meta.env.VITE_RANDOM_NAME_URL : null
  return fromStorage || fromEnv || '/nel-random-name'
}
const getAnnouncementUrl = () => {
  const fromStorage = typeof localStorage !== 'undefined' ? localStorage.getItem('NEL_ANNOUNCEMENT_URL') : null
  const fromEnv = typeof import.meta !== 'undefined' ? import.meta.env && import.meta.env.VITE_ANNOUNCEMENT_URL : null
  const isDev = typeof import.meta !== 'undefined' && import.meta.env && import.meta.env.DEV
  return fromStorage || fromEnv || (isDev ? '/nel-announcement' : 'http://23.140.4.9:3498/announcement/get')
}
const getAnnouncementTTL = () => {
  const fromStorage = typeof localStorage !== 'undefined' ? localStorage.getItem('NEL_ANNOUNCEMENT_TTL') : null
  const fromEnv = typeof import.meta !== 'undefined' ? import.meta.env && import.meta.env.VITE_ANNOUNCEMENT_TTL : null
  const v = fromStorage || fromEnv
  const n = v ? parseInt(v, 10) : NaN
  return Number.isFinite(n) && n > 0 ? n : 300000
}
const getNoticeKey = () => {
  const fromStorage = typeof localStorage !== 'undefined' ? localStorage.getItem('NEL_NOTICE_KEY') : null
  const fromEnv = typeof import.meta !== 'undefined' ? import.meta.env && import.meta.env.VITE_NOTICE_KEY : null
  return fromStorage || fromEnv || 'NEL_NOTICE_ACK'
}
const getNoticeText = () => {
  const def = 'OpenNEL  Copyright (C) 2025 OpenNEL Studio\n本程序是自由软件，你可以重新发布或修改它，但必须：\n- 保留原始版权声明\n- 采用相同许可证分发\n- 提供完整的源代码'
  const fromStorage = typeof localStorage !== 'undefined' ? localStorage.getItem('NEL_NOTICE_TEXT') : null
  const fromEnv = typeof import.meta !== 'undefined' ? import.meta.env && import.meta.env.VITE_NOTICE_TEXT : null
  return fromStorage || fromEnv || def
}
const getAboutCopyright = () => {
  const fromStorage = typeof localStorage !== 'undefined' ? localStorage.getItem('NEL_ABOUT_COPYRIGHT') : null
  const fromEnv = typeof import.meta !== 'undefined' ? import.meta.env && import.meta.env.VITE_ABOUT_COPYRIGHT : null
  const def = getNoticeText()
  return fromStorage || fromEnv || def
}
const getAboutJoin = () => {
  const fromStorage = typeof localStorage !== 'undefined' ? localStorage.getItem('NEL_ABOUT_JOIN') : null
  const fromEnv = typeof import.meta !== 'undefined' ? import.meta.env && import.meta.env.VITE_ABOUT_JOIN : null
  const def = 'QQ群: 574456857\nGitHub: FandMC/OpenNEL'
  return fromStorage || fromEnv || def
}
const getAboutContributors = () => {
  const fromStorage = typeof localStorage !== 'undefined' ? localStorage.getItem('NEL_ABOUT_CONTRIBUTORS') : null
  const fromEnv = typeof import.meta !== 'undefined' ? import.meta.env && import.meta.env.VITE_ABOUT_CONTRIBUTORS : null
  const def = '贡献者(不分先后顺序)\n[开发]20018\n[开发]Samse\n[开发]haohaojisui\n[思路]ca1cdqy\n[dev]caolvchong\n[吉祥物]Malkuth\n[吉祥物]Atri\n[吉祥物]汉堡鸭\n[功能贡献 - 中文名生成]scarlet\n[观众]ASPIRIN\n[啥也不是]LELEXIAOLL\n[绒布球]Liliah\n一直支持我们的各位群友'
  const v = fromStorage || fromEnv || def
  const arr = v.split('\n').map(s => s.trim()).filter(s => s)
  return arr
}
const getThemeMode = () => {
  const v = typeof localStorage !== 'undefined' ? localStorage.getItem('NEL_THEME_MODE') : null
  return v || 'image'
}
const getThemeColor = () => {
  const v = typeof localStorage !== 'undefined' ? localStorage.getItem('NEL_THEME_COLOR') : null
  return v || '#181818'
}
const getThemeImage = () => {
  const v = typeof localStorage !== 'undefined' ? localStorage.getItem('NEL_THEME_IMAGE') : null
  return v || ''
}
const setThemeMode = (v) => { try { if (typeof localStorage !== 'undefined') localStorage.setItem('NEL_THEME_MODE', v || '') } catch {} }
const setThemeColor = (v) => { try { if (typeof localStorage !== 'undefined') localStorage.setItem('NEL_THEME_COLOR', v || '') } catch {} }
const setThemeImage = (v) => { try { if (typeof localStorage !== 'undefined') localStorage.setItem('NEL_THEME_IMAGE', v || '') } catch {} }
export const theme = { getThemeMode, getThemeColor, getThemeImage, setThemeMode, setThemeColor, setThemeImage }
export { getThemeMode, getThemeColor, getThemeImage, setThemeMode, setThemeColor, setThemeImage }
export default { getWsUrl, getPluginListUrl, getRandomNameUrl, getAnnouncementUrl, getAnnouncementTTL, getNoticeKey, getNoticeText, getAboutCopyright, getAboutJoin, getAboutContributors, getThemeMode, getThemeColor, getThemeImage, setThemeMode, setThemeColor, setThemeImage, theme }
