<template>
  <div class="flex flex-col h-full">
    <!-- Toolbar -->
    <div class="flex items-center gap-2 p-2 border-b border-gray-300 bg-gray-50 rounded-t">
      <button
        @click="formatText('bold')"
        type="button"
        class="p-2 hover:bg-gray-200 rounded"
        title="Feitletra"
      >
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 4h8a4 4 0 014 4 4 4 0 01-4 4H6z" />
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 12h9a4 4 0 014 4 4 4 0 01-4 4H6z" />
        </svg>
      </button>
      <button
        @click="formatText('italic')"
        type="button"
        class="p-2 hover:bg-gray-200 rounded"
        title="Skáletra"
      >
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 20l4-16m4 4l4 4-4 4M6 16l-4-4 4-4" />
        </svg>
      </button>
      <button
        @click="formatText('underline')"
        type="button"
        class="p-2 hover:bg-gray-200 rounded"
        title="Undirstrika"
      >
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 5h14M5 9h14M5 13h14M5 17h14" />
        </svg>
      </button>
      <div class="w-px h-6 bg-gray-300"></div>
      <button
        @click="insertBulletList"
        type="button"
        class="p-2 hover:bg-gray-200 rounded"
        title="Punktalisti"
      >
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" />
        </svg>
      </button>
      <button
        @click="insertNumberedList"
        type="button"
        class="p-2 hover:bg-gray-200 rounded"
        title="Númeraður listi"
      >
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 20l4-16m2 16l4-16M6 9h14M4 15h14" />
        </svg>
      </button>
    </div>

    <!-- Editor -->
    <div
      ref="editorRef"
      contenteditable="true"
      class="flex-1 p-4 border border-gray-300 border-t-0 rounded-b overflow-y-auto focus:outline-none focus:ring-2 focus:ring-indigo-500"
      :class="editorClass"
      @input="handleInput"
      @paste="handlePaste"
    ></div>
  </div>
</template>

<script setup lang="ts">
const props = defineProps<{
  modelValue: string
  placeholder?: string
  editorClass?: string
}>()

const emit = defineEmits<{
  'update:modelValue': [value: string]
}>()

const editorRef = ref<HTMLElement | null>(null)

const handleInput = () => {
  if (editorRef.value) {
    emit('update:modelValue', editorRef.value.innerHTML)
  }
}

const handlePaste = (e: ClipboardEvent) => {
  e.preventDefault()
  const text = e.clipboardData?.getData('text/plain') || ''
  document.execCommand('insertText', false, text)
}

const formatText = (command: string) => {
  document.execCommand(command, false)
  editorRef.value?.focus()
}

const insertBulletList = () => {
  document.execCommand('insertUnorderedList', false)
  editorRef.value?.focus()
}

const insertNumberedList = () => {
  document.execCommand('insertOrderedList', false)
  editorRef.value?.focus()
}

const setContent = (html: string) => {
  if (editorRef.value) {
    editorRef.value.innerHTML = html
    emit('update:modelValue', html)
  }
}

const getContent = (): string => {
  return editorRef.value?.innerHTML || ''
}

const clear = () => {
  if (editorRef.value) {
    editorRef.value.innerHTML = ''
  }
}

// Watch for external changes to modelValue
watch(() => props.modelValue, (newValue) => {
  if (editorRef.value && editorRef.value.innerHTML !== newValue) {
    editorRef.value.innerHTML = newValue
  }
}, { immediate: true })

onMounted(() => {
  if (editorRef.value && props.modelValue) {
    editorRef.value.innerHTML = props.modelValue
  }
})

defineExpose({
  setContent,
  getContent,
  clear
})
</script>

<style scoped>
[contenteditable="true"]:empty:before {
  content: attr(data-placeholder);
  color: #9ca3af;
  pointer-events: none;
}
</style>

