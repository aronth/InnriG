<template>
  <div
    class="flex flex-col items-center justify-center w-full h-64 border-2 border-gray-300 border-dashed rounded-lg transition-colors"
    :class="disabled ? 'bg-gray-100 cursor-not-allowed opacity-50' : 'bg-gray-50 hover:bg-gray-100 cursor-pointer'"
    @dragover="handleDragOver"
    @drop="handleDrop"
    @click="triggerFileInput"
  >
    <div class="flex flex-col items-center justify-center pt-5 pb-6">
      <svg class="w-10 h-10 mb-3 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path
          stroke-linecap="round"
          stroke-linejoin="round"
          stroke-width="2"
          d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12"
        />
      </svg>
      <p class="mb-2 text-sm text-gray-500">
        <span class="font-semibold">Smelltu til að hlaða upp</span> eða dragðu skjal hingað
      </p>
      <p class="text-xs text-gray-500">Excel pöntunarskrá (.xlsx)</p>
      <p v-if="selectedName" class="mt-3 text-sm font-medium text-indigo-600">
        {{ selectedName }}
      </p>
    </div>
    <input ref="fileInput" type="file" class="hidden" accept=".xlsx" @change="handleFileChange" />
  </div>
</template>

<script setup lang="ts">
const props = defineProps<{
  disabled?: boolean
}>()

const emit = defineEmits<{
  (e: 'file-selected', file: File): void
}>()

const fileInput = ref<HTMLInputElement | null>(null)
const selectedName = ref<string | null>(null)

const handleDragOver = (event: DragEvent) => {
  if (props.disabled) return
  event.preventDefault()
  event.stopPropagation()
}

const triggerFileInput = () => {
  if (props.disabled) return
  fileInput.value?.click()
}

const handleFileChange = (event: Event) => {
  if (props.disabled) return
  const target = event.target as HTMLInputElement
  const file = target.files?.item(0)
  if (file) {
    selectedName.value = file.name
    emit('file-selected', file)
  }
}

const handleDrop = (event: DragEvent) => {
  if (props.disabled) return
  event.preventDefault()
  event.stopPropagation()
  const file = event.dataTransfer?.files?.item(0)
  if (file) {
    selectedName.value = file.name
    emit('file-selected', file)
  }
}
</script>


