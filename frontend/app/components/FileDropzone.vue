<template>
  <div
    class="flex flex-col items-center justify-center w-full h-64 border-2 border-gray-300 border-dashed rounded-lg cursor-pointer bg-gray-50 hover:bg-gray-100 transition-colors"
    @dragover.prevent
    @drop.prevent="handleDrop"
    @click="triggerFileInput"
  >
    <div class="flex flex-col items-center justify-center pt-5 pb-6">
      <svg class="w-10 h-10 mb-3 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12"></path></svg>
      <p class="mb-2 text-sm text-gray-500"><span class="font-semibold">Smelltu til að hlaða upp</span> eða dragðu skjal hingað</p>
      <p class="text-xs text-gray-500">HTML Reikningur</p>
    </div>
    <input ref="fileInput" type="file" class="hidden" accept=".html" @change="handleFileChange" />
  </div>
</template>

<script setup lang="ts">
const emit = defineEmits(['file-selected']);
const fileInput = ref<HTMLInputElement | null>(null);

const triggerFileInput = () => {
  fileInput.value?.click();
};

const handleFileChange = (event: Event) => {
  const target = event.target as HTMLInputElement;
  if (target.files && target.files.length > 0) {
    emit('file-selected', target.files[0]);
  }
};

const handleDrop = (event: DragEvent) => {
  if (event.dataTransfer?.files && event.dataTransfer.files.length > 0) {
    emit('file-selected', event.dataTransfer.files[0]);
  }
};
</script>
