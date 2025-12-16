<template>
  <div class="min-h-[80vh]">
    <!-- Header -->
    <div class="bg-gradient-to-br from-indigo-50 via-purple-50 to-pink-50 rounded-2xl p-8 border border-indigo-100 shadow-lg mb-6">
      <div class="flex items-center gap-3">
        <div class="w-12 h-12 bg-gradient-to-br from-indigo-500 to-purple-600 rounded-xl flex items-center justify-center shadow-lg">
          <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
          </svg>
        </div>
        <div>
          <h1 class="text-2xl font-bold text-gray-800">Fjöldaupphleðsla reikninga</h1>
          <p class="text-sm text-gray-600">Hlaða upp mörgum reikningum í einu</p>
        </div>
      </div>
    </div>

    <!-- File Upload Area -->
    <div class="bg-white rounded-2xl shadow-lg border border-gray-100 p-8 mb-6">
      <div
        class="flex flex-col items-center justify-center w-full h-64 border-2 border-gray-300 border-dashed rounded-lg cursor-pointer bg-gray-50 hover:bg-gray-100 transition-colors"
        @dragover.prevent
        @drop.prevent="handleDrop"
        @click="triggerFileInput"
      >
        <div class="flex flex-col items-center justify-center pt-5 pb-6">
          <svg class="w-10 h-10 mb-3 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12"></path>
          </svg>
          <p class="mb-2 text-sm text-gray-500">
            <span class="font-semibold">Smelltu til að velja skrár</span> eða dragðu skrár hingað
          </p>
          <p class="text-xs text-gray-500">HTML Reikningar (margar skrár í einu)</p>
          <p v-if="selectedFiles.length > 0" class="mt-4 text-sm font-medium text-indigo-600">
            {{ selectedFiles.length }} {{ selectedFiles.length === 1 ? 'skrá valin' : 'skrár valdar' }}
          </p>
        </div>
        <input 
          ref="fileInput" 
          type="file" 
          class="hidden" 
          accept=".html" 
          multiple
          @change="handleFileChange" 
        />
      </div>

      <!-- Selected Files List -->
      <div v-if="selectedFiles.length > 0" class="mt-6">
        <h3 class="text-sm font-medium text-gray-700 mb-3">Valdar skrár:</h3>
        <div class="space-y-2 max-h-64 overflow-y-auto">
          <div 
            v-for="(file, index) in selectedFiles" 
            :key="index"
            class="flex items-center justify-between p-3 bg-gray-50 rounded-lg"
          >
            <span class="text-sm text-gray-700">{{ file.name }}</span>
            <button
              @click.stop="removeFile(index)"
              class="text-red-600 hover:text-red-800"
            >
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
        </div>
      </div>

      <!-- Upload Button -->
      <div v-if="selectedFiles.length > 0" class="mt-6 flex justify-end">
        <button
          @click="handleBulkUpload"
          :disabled="isUploading"
          class="px-6 py-3 bg-gradient-to-r from-indigo-500 to-purple-600 text-white rounded-lg hover:from-indigo-600 hover:to-purple-700 transition-all duration-200 shadow-md hover:shadow-lg font-medium disabled:opacity-50 disabled:cursor-not-allowed"
        >
          <span v-if="isUploading" class="flex items-center gap-2">
            <svg class="animate-spin h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
            </svg>
            Hleður upp...
          </span>
          <span v-else>Hlaða upp {{ selectedFiles.length }} {{ selectedFiles.length === 1 ? 'reikningi' : 'reikningum' }}</span>
        </button>
      </div>
    </div>

    <!-- Result Message -->
    <div 
      v-if="uploadResult"
      :class="[
        'rounded-2xl p-6 shadow-lg border-2 mb-6',
        isResultValid ? 'bg-green-50 border-green-200' : 'bg-orange-50 border-orange-200'
      ]"
    >
      <div class="flex items-start gap-4">
        <div 
          :class="[
            'w-12 h-12 rounded-full flex items-center justify-center flex-shrink-0',
            isResultValid ? 'bg-green-100' : 'bg-orange-100'
          ]"
        >
          <svg 
            v-if="isResultValid"
            class="w-6 h-6 text-green-600" 
            fill="none" 
            stroke="currentColor" 
            viewBox="0 0 24 24"
          >
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
          </svg>
          <svg 
            v-else
            class="w-6 h-6 text-orange-600" 
            fill="none" 
            stroke="currentColor" 
            viewBox="0 0 24 24"
          >
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
          </svg>
        </div>
        <div class="flex-1">
          <h3 
            :class="[
              'text-lg font-bold mb-2',
              isResultValid ? 'text-green-900' : 'text-orange-900'
            ]"
          >
            {{ isResultValid ? 'Upphleðsla lokið' : 'Viðvörun' }}
          </h3>
          <div 
            :class="[
              'text-sm space-y-1',
              isResultValid ? 'text-green-800' : 'text-orange-800'
            ]"
          >
            <p><strong>Samtals:</strong> {{ uploadResult.total }} reikningar</p>
            <p><strong>Gekk vel:</strong> {{ uploadResult.successful }} reikningar</p>
            <p><strong>Sleppt (þegar til):</strong> {{ uploadResult.skipped }} reikningar</p>
            <p><strong>Mistókst:</strong> {{ uploadResult.failed }} reikningar</p>
            
            <div v-if="uploadResult.errors && uploadResult.errors.length > 0" class="mt-4">
              <p class="font-semibold mb-2">Villur:</p>
              <ul class="list-disc list-inside space-y-1">
                <li v-for="(error, index) in uploadResult.errors" :key="index" class="text-xs">
                  {{ error }}
                </li>
              </ul>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
const { bulkUploadInvoices } = useInvoices()

const fileInput = ref<HTMLInputElement | null>(null)
const selectedFiles = ref<File[]>([])
const isUploading = ref(false)
const uploadResult = ref<BulkUploadResult | null>(null)

const triggerFileInput = () => {
  fileInput.value?.click()
}

const handleFileChange = (event: Event) => {
  const target = event.target as HTMLInputElement
  if (target.files && target.files.length > 0) {
    selectedFiles.value = Array.from(target.files)
  }
}

const handleDrop = (event: DragEvent) => {
  if (event.dataTransfer?.files && event.dataTransfer.files.length > 0) {
    selectedFiles.value = Array.from(event.dataTransfer.files)
  }
}

const removeFile = (index: number) => {
  selectedFiles.value.splice(index, 1)
}

const isResultValid = computed(() => {
  if (!uploadResult.value) return false
  const { total, successful, skipped, failed } = uploadResult.value
  return (successful + skipped + failed) === total
})

const handleBulkUpload = async () => {
  if (selectedFiles.value.length === 0) return

  isUploading.value = true
  uploadResult.value = null

  try {
    const result = await bulkUploadInvoices(selectedFiles.value)
    uploadResult.value = result
    
    // Clear selected files after successful upload
    if (result.failed === 0 || isResultValid.value) {
      selectedFiles.value = []
      if (fileInput.value) {
        fileInput.value.value = ''
      }
    }
  } catch (err: any) {
    console.error('Bulk upload error:', err)
    uploadResult.value = {
      total: selectedFiles.value.length,
      successful: 0,
      skipped: 0,
      failed: selectedFiles.value.length,
      errors: [err.message || 'Óþekkt villa kom upp við upphleðslu']
    }
  } finally {
    isUploading.value = false
  }
}
</script>

<style scoped>
@keyframes spin {
  from {
    transform: rotate(0deg);
  }
  to {
    transform: rotate(360deg);
  }
}
</style>

