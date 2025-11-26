<template>
  <div v-if="modelValue" class="overlay" @click="close">
    <div class="modal" @click.stop>
      <div class="modal-header">
        <div class="modal-title">{{ title }}</div>
        <button class="close-btn" @click="close">×</button>
      </div>
      <div class="modal-body"><slot /></div>
      <div class="modal-actions"><slot name="actions" /></div>
    </div>
  </div>
</template>

<script setup>
import { defineProps, defineEmits } from 'vue'
const props = defineProps({ modelValue: Boolean, title: String })
const emit = defineEmits(['update:modelValue'])
function close() { emit('update:modelValue', false) }
</script>

<style scoped>
.overlay {
  position: fixed;
  inset: 0;
  background: transparent;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 24px;
}
.modal {
  width: 420px;
  background: var(--glass-surface);
  color: var(--color-text);
  border: 1px solid var(--glass-border);
  border-radius: 12px;
  box-shadow: 0 10px 30px rgba(0,0,0,0.2);
  backdrop-filter: blur(var(--glass-blur));
}
.modal-header {
  padding: 16px 20px;
  font-size: 16px;
  font-weight: 600;
  border-bottom: 1px solid var(--color-border);
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.modal-title { line-height: 1; }
.close-btn {
  background: transparent;
  border: none;
  color: var(--color-text);
  font-size: 20px;
  line-height: 1;
  cursor: pointer;
}
.close-btn:hover { opacity: 0.8; }
.modal-body {
  padding: 16px 20px;
}
.modal-actions {
  padding: 12px 20px 16px;
  display: flex;
  gap: 8px;
  justify-content: flex-end;
}
</style>
