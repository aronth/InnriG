<template>
  <div class="min-h-[80vh]">
    <!-- Two-Sided Layout: Instructions (Left) + Dropzone (Right) -->
    <div class="grid lg:grid-cols-2 gap-8 items-start">
      
      <!-- Left Side: Instructions -->
      <div class="bg-gradient-to-br from-indigo-50 via-purple-50 to-pink-50 rounded-2xl p-8 border border-indigo-100 shadow-lg">
        <div class="flex items-center gap-3 mb-6">
          <div class="w-12 h-12 bg-gradient-to-br from-indigo-500 to-purple-600 rounded-xl flex items-center justify-center shadow-lg">
            <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
          </div>
          <h2 class="text-2xl font-bold text-gray-800">Leiðbeiningar</h2>
        </div>
        
        <div class="prose prose-sm max-w-none">
          <ol class="space-y-4 text-gray-700 leading-relaxed">
            <li class="flex gap-3">
              <span class="flex-shrink-0 w-6 h-6 bg-indigo-500 text-white rounded-full flex items-center justify-center text-xs font-bold">1</span>
              <span>Farðu í <strong>DK</strong> og finndu reikninga.</span>
            </li>
            <li class="flex gap-3">
              <span class="flex-shrink-0 w-6 h-6 bg-indigo-500 text-white rounded-full flex items-center justify-center text-xs font-bold">2</span>
              <span>Veldu þann reikning sem þú vilt taka út með því að ýta á <kbd class="px-2 py-1 bg-gray-800 text-white rounded text-xs font-mono">CTRL+F10</kbd> til að opna reikning í vafra.</span>
            </li>
            <li class="flex gap-3">
              <span class="flex-shrink-0 w-6 h-6 bg-indigo-500 text-white rounded-full flex items-center justify-center text-xs font-bold">3</span>
              <span>Farðu yfir reikninginn og staðfestu að þetta sé reikningurinn sem þú vilt.</span>
            </li>
            <li class="flex gap-3">
              <span class="flex-shrink-0 w-6 h-6 bg-indigo-500 text-white rounded-full flex items-center justify-center text-xs font-bold">4</span>
              <span>Ef þú vilt þennan reikning ýtir þú á <kbd class="px-2 py-1 bg-gray-800 text-white rounded text-xs font-mono">CTRL+S</kbd> til að vista reikninginn. Mátt vista hann hvar sem er.</span>
            </li>
            <li class="flex gap-3">
              <span class="flex-shrink-0 w-6 h-6 bg-indigo-500 text-white rounded-full flex items-center justify-center text-xs font-bold">5</span>
              <span>Verður svo að velja hann innan DK hýsingarinnar og ýta þá á <kbd class="px-2 py-1 bg-gray-800 text-white rounded text-xs font-mono">CTRL+C</kbd> til að afrita.</span>
            </li>
            <li class="flex gap-3">
              <span class="flex-shrink-0 w-6 h-6 bg-indigo-500 text-white rounded-full flex items-center justify-center text-xs font-bold">6</span>
              <span>Farðu svo á skjáborðið á þinni tölvu og ýtti á <kbd class="px-2 py-1 bg-gray-800 text-white rounded text-xs font-mono">CTRL+V</kbd> til að líma skjalið.</span>
            </li>
            <li class="flex gap-3">
              <span class="flex-shrink-0 w-6 h-6 bg-indigo-500 text-white rounded-full flex items-center justify-center text-xs font-bold">7</span>
              <span>Það skjal getur þú annaðhvort dregið hingað eða valið með því að smella á formið hér hægra megin. ➡️</span>
            </li>
          </ol>
        </div>

        <div class="mt-8 p-4 bg-white/70 backdrop-blur-sm rounded-xl border border-indigo-200">
          <div class="flex gap-2 items-start">
            <svg class="w-5 h-5 text-indigo-600 flex-shrink-0 mt-0.5" fill="currentColor" viewBox="0 0 20 20">
              <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd" />
            </svg>
            <p class="text-sm text-gray-600">
              <strong class="text-indigo-700">Ábending:</strong> Kerfið les sjálfkrafa HTML skrár og dregur út vöruupplýsingar til yfirferðar.
            </p>
          </div>
        </div>
      </div>

      <!-- Right Side: Dropzone -->
      <div class="lg:sticky lg:top-24">
        <div class="bg-white rounded-2xl border-2 border-dashed border-gray-200 shadow-xl overflow-hidden transition-all duration-300 hover:border-indigo-300 hover:shadow-2xl">
          
          <!-- Mode Toggle -->
          <div class="p-4 bg-gray-50 border-b border-gray-200">
            <div class="flex items-center justify-between">
              <span class="text-sm font-medium text-gray-700">Upphleðsluhamur:</span>
              <div class="flex items-center gap-3">
                <span :class="['text-sm', isBulkMode ? 'text-gray-500' : 'text-indigo-600 font-medium']">
                  Einn reikningur með yfirferð
                </span>
                <button
                  @click="isBulkMode = !isBulkMode"
                  :class="[
                    'relative inline-flex h-6 w-11 flex-shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2',
                    isBulkMode ? 'bg-indigo-600' : 'bg-gray-200'
                  ]"
                >
                  <span
                    :class="[
                      'pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow ring-0 transition duration-200 ease-in-out',
                      isBulkMode ? 'translate-x-5' : 'translate-x-0'
                    ]"
                  />
                </button>
                <span :class="['text-sm', isBulkMode ? 'text-indigo-600 font-medium' : 'text-gray-500']">
                  Fjöldaupphleðsla
                </span>
              </div>
            </div>
          </div>

          <!-- Loading State Overlay -->
          <div v-if="isLoading" class="relative bg-gradient-to-br from-indigo-500 to-purple-600 p-12">
            <div class="flex flex-col items-center justify-center space-y-6">
              <!-- Animated spinner -->
              <div class="relative">
                <div class="w-20 h-20 border-4 border-white/30 rounded-full"></div>
                <div class="w-20 h-20 border-4 border-white border-t-transparent rounded-full animate-spin absolute top-0"></div>
              </div>
              
              <div class="text-center">
                <h3 class="text-2xl font-bold text-white mb-2">
                  {{ isBulkMode ? 'Vinn úr reikningum...' : 'Vinn úr reikningi...' }}
                </h3>
                <p class="text-indigo-100">
                  {{ isBulkMode ? 'Augnablik, við erum að lesa gögnin' : 'Augnablik, við erum að lesa gögnin' }}
                </p>
              </div>

              <!-- Progress dots -->
              <div class="flex gap-2">
                <div class="w-3 h-3 bg-white rounded-full animate-pulse"></div>
                <div class="w-3 h-3 bg-white rounded-full animate-pulse" style="animation-delay: 0.2s"></div>
                <div class="w-3 h-3 bg-white rounded-full animate-pulse" style="animation-delay: 0.4s"></div>
              </div>
            </div>
          </div>

          <!-- Error State (Single Mode) -->
          <div v-else-if="errorMsg && !isBulkMode" class="bg-red-50 border-2 border-red-200 p-8">
            <div class="flex flex-col items-center text-center space-y-4">
              <div class="w-16 h-16 bg-red-100 rounded-full flex items-center justify-center">
                <svg class="w-8 h-8 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </div>
              <div>
                <h3 class="text-xl font-bold text-red-900 mb-2">Villa kom upp</h3>
                <p class="text-red-700">{{ errorMsg }}</p>
              </div>
              <button 
                @click="errorMsg = ''" 
                class="mt-4 px-6 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors font-medium"
              >
                Reyna aftur
              </button>
            </div>
          </div>

          <!-- Bulk Upload Result -->
          <div v-else-if="bulkUploadResult" class="p-6">
            <div 
              :class="[
                'rounded-xl p-6 border-2',
                isBulkResultValid ? 'bg-green-50 border-green-200' : 'bg-orange-50 border-orange-200'
              ]"
            >
              <div class="flex items-start gap-4">
                <div 
                  :class="[
                    'w-10 h-10 rounded-full flex items-center justify-center flex-shrink-0',
                    isBulkResultValid ? 'bg-green-100' : 'bg-orange-100'
                  ]"
                >
                  <svg 
                    v-if="isBulkResultValid"
                    class="w-5 h-5 text-green-600" 
                    fill="none" 
                    stroke="currentColor" 
                    viewBox="0 0 24 24"
                  >
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                  </svg>
                  <svg 
                    v-else
                    class="w-5 h-5 text-orange-600" 
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
                      isBulkResultValid ? 'text-green-900' : 'text-orange-900'
                    ]"
                  >
                    {{ isBulkResultValid ? 'Upphleðsla lokið' : 'Viðvörun' }}
                  </h3>
                  <div 
                    :class="[
                      'text-sm space-y-1',
                      isBulkResultValid ? 'text-green-800' : 'text-orange-800'
                    ]"
                  >
                    <p><strong>Samtals:</strong> {{ bulkUploadResult.total }} reikningar</p>
                    <p><strong>Gekk vel:</strong> {{ bulkUploadResult.successful }} reikningar</p>
                    <p><strong>Sleppt (þegar til):</strong> {{ bulkUploadResult.skipped }} reikningar</p>
                    <p><strong>Mistókst:</strong> {{ bulkUploadResult.failed }} reikningar</p>
                    
                    <div v-if="bulkUploadResult.errors && bulkUploadResult.errors.length > 0" class="mt-3">
                      <p class="font-semibold mb-1">Villur:</p>
                      <ul class="list-disc list-inside space-y-1 text-xs">
                        <li v-for="(error, index) in bulkUploadResult.errors" :key="index">
                          {{ error }}
                        </li>
                      </ul>
                    </div>
                  </div>
                  <button
                    @click="bulkUploadResult = null; selectedFiles = []"
                    class="mt-4 px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors text-sm font-medium"
                  >
                    Hlaða upp fleiri
                  </button>
                </div>
              </div>
            </div>
          </div>

          <!-- Normal State: Single File Dropzone -->
          <div v-else-if="!isBulkMode" class="p-12">
            <FileDropzone @file-selected="onFileSelected" />
          </div>

          <!-- Normal State: Bulk Upload Dropzone -->
          <div v-else class="p-12">
            <div
              class="flex flex-col items-center justify-center w-full min-h-64 border-2 border-gray-300 border-dashed rounded-lg cursor-pointer bg-gray-50 hover:bg-gray-100 transition-colors"
              @dragover.prevent
              @drop.prevent="handleBulkDrop"
              @click="triggerBulkFileInput"
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
                ref="bulkFileInput" 
                type="file" 
                class="hidden" 
                accept=".html" 
                multiple
                @change="handleBulkFileChange" 
              />
            </div>

            <!-- Selected Files List -->
            <div v-if="selectedFiles.length > 0" class="mt-6">
              <h3 class="text-sm font-medium text-gray-700 mb-3">Valdar skrár:</h3>
              <div class="space-y-2 max-h-48 overflow-y-auto">
                <div 
                  v-for="(file, index) in selectedFiles" 
                  :key="index"
                  class="flex items-center justify-between p-3 bg-gray-50 rounded-lg"
                >
                  <span class="text-sm text-gray-700">{{ file.name }}</span>
                  <button
                    @click.stop="removeBulkFile(index)"
                    class="text-red-600 hover:text-red-800"
                  >
                    <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                    </svg>
                  </button>
                </div>
              </div>
              <button
                @click="handleBulkUpload"
                :disabled="isLoading"
                class="mt-4 w-full px-6 py-3 bg-gradient-to-r from-indigo-500 to-purple-600 text-white rounded-lg hover:from-indigo-600 hover:to-purple-700 transition-all duration-200 shadow-md hover:shadow-lg font-medium disabled:opacity-50 disabled:cursor-not-allowed"
              >
                Hlaða upp {{ selectedFiles.length }} {{ selectedFiles.length === 1 ? 'reikningi' : 'reikningum' }}
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Review Modal -->
    <div v-if="showReview && invoiceToReview" class="fixed z-50 inset-0 overflow-y-auto" aria-labelledby="modal-title" role="dialog" aria-modal="true">
      <div class="flex items-end justify-center min-h-screen pt-4 px-4 pb-20 text-center sm:block sm:p-0">
        <div class="fixed inset-0 bg-gray-900 bg-opacity-75 transition-opacity backdrop-blur-sm" aria-hidden="true" @click="showReview = false"></div>
        <span class="hidden sm:inline-block sm:align-middle sm:h-screen" aria-hidden="true">&#8203;</span>
        <div class="inline-block align-bottom bg-white rounded-2xl text-left overflow-hidden shadow-2xl transform transition-all sm:my-8 sm:align-middle sm:max-w-5xl sm:w-full">
            <InvoiceReview :invoice="invoiceToReview" @confirm="onConfirmInvoice" @cancel="showReview = false" />
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
const config = useRuntimeConfig();
const apiBase = config.public.apiBase;
const { bulkUploadInvoices } = useInvoices();

const showReview = ref(false);
const invoiceToReview = ref(null);
const isLoading = ref(false);
const errorMsg = ref('');
const isBulkMode = ref(false);
const selectedFiles = ref<File[]>([]);
const bulkFileInput = ref<HTMLInputElement | null>(null);
const bulkUploadResult = ref<BulkUploadResult | null>(null);

const isBulkResultValid = computed(() => {
  if (!bulkUploadResult.value) return false;
  const { total, successful, skipped, failed } = bulkUploadResult.value;
  return (successful + skipped + failed) === total;
});

const onFileSelected = async (file: File) => {
  isLoading.value = true;
  errorMsg.value = '';
  const formData = new FormData();
  formData.append('file', file);

  try {
    const data = await $fetch(`${apiBase}/api/invoices/upload`, {
      method: 'POST',
      body: formData
    });
    invoiceToReview.value = data;
    showReview.value = true;
  } catch (err) {
    errorMsg.value = 'Mistókst að hlaða upp reikningi. Vinsamlegast reyndu aftur.';
    console.error(err);
  } finally {
    isLoading.value = false;
  }
};

const onConfirmInvoice = async () => {
  if (!invoiceToReview.value) return;
  isLoading.value = true;
  errorMsg.value = '';
  
  try {
    await $fetch(`${apiBase}/api/invoices/confirm`, {
      method: 'POST',
      body: invoiceToReview.value
    });
    showReview.value = false;
    invoiceToReview.value = null;
    isLoading.value = false;
    alert('Reikningur vistaður!');
  } catch (err: any) {
    isLoading.value = false;
    
    // Handle duplicate invoice (409 Conflict)
    if (err.status === 409 || err.statusCode === 409) {
      const errorData = err.data || err;
      const message = errorData.message || `Reikningur með númeri ${invoiceToReview.value?.invoiceNumber} frá ${invoiceToReview.value?.supplierName} er þegar til í kerfinu.`;
      errorMsg.value = message;
      
      // Show alert with link to existing invoice if available
      if (errorData.invoiceId) {
        const existingInvoiceUrl = `${window.location.origin}/invoices/${errorData.invoiceId}`;
        if (confirm(`${message}\n\nViltu skoða fyrirliggjandi reikning?`)) {
          window.open(existingInvoiceUrl, '_blank');
        }
      } else {
        alert(message);
      }
    } else {
      errorMsg.value = err.data?.message || err.message || 'Mistókst að vista reikning.';
      alert(errorMsg.value);
    }
    console.error(err);
  }
};

// Bulk upload handlers
const triggerBulkFileInput = () => {
  bulkFileInput.value?.click();
};

const handleBulkFileChange = (event: Event) => {
  const target = event.target as HTMLInputElement;
  if (target.files && target.files.length > 0) {
    selectedFiles.value = Array.from(target.files);
  }
};

const handleBulkDrop = (event: DragEvent) => {
  if (event.dataTransfer?.files && event.dataTransfer.files.length > 0) {
    selectedFiles.value = Array.from(event.dataTransfer.files);
  }
};

const removeBulkFile = (index: number) => {
  selectedFiles.value.splice(index, 1);
};

const handleBulkUpload = async () => {
  if (selectedFiles.value.length === 0) return;

  isLoading.value = true;
  errorMsg.value = '';
  bulkUploadResult.value = null;

  try {
    const result = await bulkUploadInvoices(selectedFiles.value);
    bulkUploadResult.value = result;
    
    // Clear selected files after successful upload
    if (result.failed === 0 || isBulkResultValid.value) {
      selectedFiles.value = [];
      if (bulkFileInput.value) {
        bulkFileInput.value.value = '';
      }
    }
  } catch (err: any) {
    console.error('Bulk upload error:', err);
    bulkUploadResult.value = {
      total: selectedFiles.value.length,
      successful: 0,
      skipped: 0,
      failed: selectedFiles.value.length,
      errors: [err.message || 'Óþekkt villa kom upp við upphleðslu']
    };
  } finally {
    isLoading.value = false;
  }
};

// Reset bulk mode state when switching modes
watch(isBulkMode, (newValue) => {
  if (!newValue) {
    // Switching to single mode - clear bulk state
    selectedFiles.value = [];
    bulkUploadResult.value = null;
    errorMsg.value = '';
  } else {
    // Switching to bulk mode - clear single mode state
    showReview.value = false;
    invoiceToReview.value = null;
    errorMsg.value = '';
  }
});
</script>

<style scoped>
kbd {
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

@keyframes pulse {
  0%, 100% {
    opacity: 1;
  }
  50% {
    opacity: 0.5;
  }
}
</style>
