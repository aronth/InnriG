<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <h1 class="text-2xl font-bold text-gray-900">Tímaskýrsla</h1>
    </div>

    <!-- File Upload Section -->
    <div v-if="!parseResult" class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
      <h2 class="text-lg font-semibold text-gray-900 mb-4">Hlaða upp tímaskýrslu</h2>
      <div
        class="flex flex-col items-center justify-center w-full h-64 border-2 border-gray-300 border-dashed rounded-lg cursor-pointer bg-gray-50 hover:bg-gray-100 transition-colors"
        @dragover.prevent
        @drop.prevent="handleDrop"
        @click="triggerFileInput"
      >
        <div class="flex flex-col items-center justify-center pt-5 pb-6">
          <svg class="w-10 h-10 mb-3 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12"></path>
          </svg>
          <p class="mb-2 text-sm text-gray-500">
            <span class="font-semibold">Smelltu til að hlaða upp</span> eða dragðu skjal hingað
          </p>
          <p class="text-xs text-gray-500">Excel skjal (.xlsx)</p>
        </div>
        <input ref="fileInput" type="file" class="hidden" accept=".xlsx" @change="handleFileChange" />
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
      <div class="flex items-center justify-center py-12">
        <div class="animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600"></div>
        <span class="ml-4 text-gray-600">Að vinna úr skjalinu...</span>
      </div>
    </div>

    <!-- Error State -->
    <div v-if="error" class="bg-red-50 border border-red-200 rounded-xl p-4">
      <div class="flex items-center">
        <svg class="w-5 h-5 text-red-600 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
        <p class="text-red-800">{{ error }}</p>
      </div>
    </div>

    <!-- Results Section -->
    <div v-if="parseResult && !loading" class="space-y-6">
      <!-- Summary Card -->
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
        <h2 class="text-lg font-semibold text-gray-900 mb-4">Yfirlit</h2>
        <div class="grid grid-cols-1 md:grid-cols-4 gap-4">
          <div class="bg-indigo-50 rounded-lg p-4">
            <p class="text-sm text-indigo-600 font-medium">Starfsmenn</p>
            <p class="text-2xl font-bold text-indigo-900">{{ parseResult.employees.length }}</p>
          </div>
          <div class="bg-purple-50 rounded-lg p-4">
            <p class="text-sm text-purple-600 font-medium">Vaktir</p>
            <p class="text-2xl font-bold text-purple-900">{{ parseResult.totalShifts }}</p>
          </div>
          <div class="bg-emerald-50 rounded-lg p-4">
            <p class="text-sm text-emerald-600 font-medium">Frá</p>
            <p class="text-lg font-semibold text-emerald-900">
              {{ formatDate(parseResult.reportStartDate) }}
            </p>
          </div>
          <div class="bg-teal-50 rounded-lg p-4">
            <p class="text-sm text-teal-600 font-medium">Til</p>
            <p class="text-lg font-semibold text-teal-900">
              {{ formatDate(parseResult.reportEndDate) }}
            </p>
          </div>
        </div>
      </div>

      <!-- Employees List -->
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        <div class="px-6 py-4 border-b border-gray-200">
          <h2 class="text-lg font-semibold text-gray-900">Starfsmenn</h2>
        </div>
        <div class="divide-y divide-gray-200">
          <div
            v-for="employee in parseResult.employees"
            :key="employee.kennitala"
            class="p-6 hover:bg-gray-50 transition-colors"
          >
            <div class="flex items-start justify-between mb-4">
              <div>
                <h3 class="text-lg font-semibold text-gray-900">{{ employee.name }}</h3>
                <p class="text-sm text-gray-600">KT: {{ employee.kennitala }}</p>
              </div>
              <div class="text-right">
                <p class="text-sm text-gray-600">Vaktir</p>
                <p class="text-xl font-bold text-gray-900">{{ employee.totalShifts }}</p>
                <p v-if="employee.totalHours" class="text-sm text-indigo-600 font-medium mt-1">
                  {{ formatHours(employee.totalHours) }}
                </p>
              </div>
            </div>

            <!-- Shifts Table -->
            <div class="mt-4 overflow-x-auto">
              <table class="min-w-full divide-y divide-gray-200">
                <thead class="bg-gray-50">
                  <tr>
                    <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Dagur
                    </th>
                    <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Frá
                    </th>
                    <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Til
                    </th>
                    <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Lengd
                    </th>
                    <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Verk
                    </th>
                    <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Tegund
                    </th>
                    <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Samþykkt
                    </th>
                    <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Ath á stimplun
                    </th>
                    <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Ath dags
                    </th>
                  </tr>
                </thead>
                <tbody class="bg-white divide-y divide-gray-200">
                  <tr v-for="(shift, index) in employee.shifts" :key="index" class="hover:bg-gray-50">
                    <td class="px-4 py-3 whitespace-nowrap text-sm text-gray-900">
                      {{ formatDate(shift.startTime) }}
                    </td>
                    <td class="px-4 py-3 whitespace-nowrap text-sm text-gray-900">
                      {{ formatTime(shift.startTime) }}
                    </td>
                    <td class="px-4 py-3 whitespace-nowrap text-sm text-gray-900">
                      {{ formatTime(shift.endTime) }}
                    </td>
                    <td class="px-4 py-3 whitespace-nowrap text-sm text-gray-900">
                      {{ formatDuration(shift.duration) }}
                    </td>
                    <td class="px-4 py-3 text-sm text-gray-900">
                      {{ shift.workLocation || '-' }}
                    </td>
                    <td class="px-4 py-3 text-sm text-gray-900">
                      {{ shift.type || '-' }}
                    </td>
                    <td class="px-4 py-3 whitespace-nowrap">
                      <span
                        :class="[
                          'px-2 py-1 text-xs rounded',
                          shift.approved === 'Já' || shift.approved === 'Ja'
                            ? 'bg-green-100 text-green-700'
                            : 'bg-yellow-100 text-yellow-700'
                        ]"
                      >
                        {{ shift.approved || 'Nei' }}
                      </span>
                    </td>
                    <td class="px-4 py-3 text-sm text-gray-600">
                      {{ shift.clockInNote || '-' }}
                    </td>
                    <td class="px-4 py-3 text-sm text-gray-600">
                      {{ shift.dayNote || '-' }}
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </div>

      <!-- Upload New File Button -->
      <div class="flex justify-center">
        <button
          @click="resetUpload"
          class="px-6 py-3 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors font-medium"
        >
          Hlaða upp nýrri skýrslu
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { TimeReportParseResult, Shift } from '~/composables/useTimeReports'

const { parseTimeReport } = useTimeReports()
const fileInput = ref<HTMLInputElement | null>(null)
const loading = ref(false)
const error = ref<string | null>(null)
const parseResult = ref<TimeReportParseResult | null>(null)

const triggerFileInput = () => {
  fileInput.value?.click()
}

const handleFileChange = async (event: Event) => {
  const target = event.target as HTMLInputElement
  if (target.files && target.files.length > 0) {
    await processFile(target.files[0])
  }
}

const handleDrop = async (event: DragEvent) => {
  if (event.dataTransfer?.files && event.dataTransfer.files.length > 0) {
    await processFile(event.dataTransfer.files[0])
  }
}

const processFile = async (file: File) => {
  if (!file.name.endsWith('.xlsx')) {
    error.value = 'Aðeins Excel skjöl (.xlsx) eru studd'
    return
  }

  loading.value = true
  error.value = null
  parseResult.value = null

  try {
    const result = await parseTimeReport(file)
    parseResult.value = result
  } catch (err: any) {
    error.value = err.data?.message || err.message || 'Villa kom upp við að vinna úr skjalinu'
    console.error('Error parsing time report:', err)
  } finally {
    loading.value = false
  }
}

const resetUpload = () => {
  parseResult.value = null
  error.value = null
  if (fileInput.value) {
    fileInput.value.value = ''
  }
}

const formatDate = (dateString?: string | null): string => {
  if (!dateString) return '-'
  const date = new Date(dateString)
  return new Intl.DateTimeFormat('is-IS', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit'
  }).format(date)
}

const formatTime = (dateString?: string | null): string => {
  if (!dateString) return '-'
  const date = new Date(dateString)
  return new Intl.DateTimeFormat('is-IS', {
    hour: '2-digit',
    minute: '2-digit'
  }).format(date)
}

const formatDuration = (durationString?: string | null): string => {
  if (!durationString) return '-'
  // Duration comes as "HH:mm:ss" or TimeSpan format
  const parts = durationString.split(':')
  if (parts.length >= 2) {
    const hours = parseInt(parts[0])
    const minutes = parseInt(parts[1])
    return `${hours}t ${minutes}m`
  }
  return durationString
}

const formatHours = (hoursString?: string | null): string => {
  if (!hoursString) return '-'
  // Similar to formatDuration but for total hours
  return formatDuration(hoursString)
}
</script>
