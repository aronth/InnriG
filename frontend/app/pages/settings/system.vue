<template>
  <div class="min-h-screen py-8">
    <div class="space-y-6">
      <!-- Header -->
      <div class="mb-8">
        <h1 class="text-3xl font-bold text-gray-900">Kerfisstillingar</h1>
        <p class="mt-2 text-sm text-gray-600">Stjórnaðu kerfisstillingum og ruslpóstsíum</p>
      </div>

      <!-- Navigation Tabs -->
      <div class="mb-6 border-b border-gray-200">
        <nav class="-mb-px flex space-x-8">
          <NuxtLink
            to="/settings"
            class="whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm"
            :class="$route.path === '/settings' ? 'border-indigo-500 text-indigo-600' : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'"
          >
            Persónulegar stillingar
          </NuxtLink>
          <NuxtLink
            to="/settings/system"
            class="whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm"
            :class="$route.path === '/settings/system' ? 'border-indigo-500 text-indigo-600' : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'"
          >
            Kerfisstillingar
          </NuxtLink>
          <NuxtLink
            to="/settings/users"
            class="whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm"
            :class="$route.path === '/settings/users' ? 'border-indigo-500 text-indigo-600' : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'"
          >
            Notendur
          </NuxtLink>
        </nav>
      </div>

      <!-- Email Junk Filters Section -->
      <div class="bg-white shadow rounded-lg mb-6">
        <div class="px-6 py-5 border-b border-gray-200">
          <div class="flex items-center justify-between">
            <div>
              <h2 class="text-lg font-medium text-gray-900">Ruslpóstsíur</h2>
              <p class="mt-1 text-sm text-gray-500">
                Stjórnaðu síum sem merkja tölvupósta sem rusl. Tölvupóstar sem passa við síur verða ekki greindir eða vinnslaðir.
              </p>
            </div>
            <button
              @click="showAddFilterModal = true"
              class="inline-flex items-center px-4 py-2 bg-indigo-600 text-white rounded-md hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 text-sm font-medium"
            >
              <svg class="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
              </svg>
              Bæta við síu
            </button>
          </div>
        </div>
        <div class="px-6 py-5">
          <div v-if="loading" class="text-center py-8">
            <svg class="animate-spin h-8 w-8 text-indigo-600 mx-auto" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
            </svg>
            <p class="mt-4 text-sm text-gray-500">Hleður síum...</p>
          </div>
          <div v-else-if="filters.length > 0" class="space-y-3">
            <div
              v-for="filter in filters"
              :key="filter.id"
              class="flex items-center justify-between p-4 border border-gray-200 rounded-lg hover:bg-gray-50"
              :class="{ 'opacity-60': !filter.isActive }"
            >
              <div class="flex-1">
                <div class="flex items-center gap-3">
                  <div class="flex-1">
                    <div class="flex items-center gap-2 mb-1">
                      <span v-if="filter.subject" class="font-medium text-gray-900">
                        {{ filter.subject }}
                      </span>
                      <span v-else class="text-gray-400 italic text-sm">Allir efnisgreinar</span>
                      <span class="text-gray-400">•</span>
                      <span v-if="filter.senderEmail" class="font-medium text-gray-900">
                        {{ filter.senderEmail }}
                      </span>
                      <span v-else class="text-gray-400 italic text-sm">Allir sendendur</span>
                    </div>
                    <div class="flex items-center gap-2 mt-1">
                      <span
                        :class="filter.isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'"
                        class="px-2 py-0.5 text-xs font-semibold rounded-full"
                      >
                        {{ filter.isActive ? 'Virk' : 'Óvirk' }}
                      </span>
                      <span class="text-xs text-gray-500">
                        Búið til {{ formatDate(filter.createdAt) }}
                      </span>
                    </div>
                  </div>
                </div>
              </div>
              <div class="flex items-center gap-2 ml-4">
                <button
                  @click="editFilter(filter)"
                  class="px-3 py-1 text-sm text-indigo-600 hover:text-indigo-800 hover:bg-indigo-50 rounded"
                >
                  Breyta
                </button>
                <button
                  @click="handleDeleteFilter(filter.id)"
                  class="px-3 py-1 text-sm text-red-600 hover:text-red-800 hover:bg-red-50 rounded"
                >
                  Eyða
                </button>
              </div>
            </div>
          </div>
          <div v-else class="text-center py-8 text-gray-500">
            <svg class="w-12 h-12 mx-auto text-gray-400 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 4a1 1 0 011-1h16a1 1 0 011 1v2.586a1 1 0 01-.293.707l-6.414 6.414a1 1 0 00-.293.707V17l-4 4v-6.586a1 1 0 00-.293-.707L3.293 7.293A1 1 0 013 6.586V4z" />
            </svg>
            <p>Engar síur skilgreindar</p>
            <p class="text-sm mt-2">Smelltu á "Bæta við síu" til að búa til nýja síu</p>
          </div>
        </div>
      </div>
    </div>

    <!-- Add/Edit Filter Modal -->
    <div
      v-if="showAddFilterModal || editingFilter"
      class="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50"
      @click.self="closeFilterModal"
    >
      <div class="bg-white rounded-lg shadow-xl w-full max-w-md m-4">
        <div class="px-6 py-4 border-b border-gray-200 flex items-center justify-between">
          <h3 class="text-lg font-bold text-gray-900">
            {{ editingFilter ? 'Breyta síu' : 'Bæta við síu' }}
          </h3>
          <button
            @click="closeFilterModal"
            class="text-gray-400 hover:text-gray-600 focus:outline-none"
          >
            <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        <div class="px-6 py-4">
          <div class="mb-4">
            <label class="block text-sm font-medium text-gray-700 mb-2">
              Efnisgrein (valfrjálst)
            </label>
            <input
              v-model="filterForm.subject"
              type="text"
              class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
              placeholder="T.d. Password Change Notification"
            />
            <p class="mt-1 text-xs text-gray-500">
              Ef efnisgrein er skilgreind, verður sían aðeins virk ef efnisgrein tölvupóstsins inniheldur þessa texta.
            </p>
          </div>

          <div class="mb-4">
            <label class="block text-sm font-medium text-gray-700 mb-2">
              Netfang sendanda (valfrjálst)
            </label>
            <input
              v-model="filterForm.senderEmail"
              type="email"
              class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
              placeholder="T.d. no-reply@example.com"
            />
            <p class="mt-1 text-xs text-gray-500">
              Ef netfang sendanda er skilgreint, verður sían aðeins virk fyrir tölvupósta frá þessu netfangi.
            </p>
          </div>

          <div class="mb-4">
            <label class="flex items-center">
              <input
                v-model="filterForm.isActive"
                type="checkbox"
                class="rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
              />
              <span class="ml-2 text-sm text-gray-700">Virk síu</span>
            </label>
            <p class="mt-1 text-xs text-gray-500">
              Óvirkar síur eru ekki notaðar við vinnslu tölvupósta.
            </p>
          </div>

          <div v-if="filterError" class="rounded-md bg-red-50 p-4 mb-4">
            <p class="text-sm text-red-800">{{ filterError }}</p>
          </div>
        </div>

        <div class="px-6 py-4 border-t border-gray-200 flex items-center justify-end gap-3">
          <button
            @click="closeFilterModal"
            class="px-4 py-2 text-gray-700 bg-gray-100 rounded-md hover:bg-gray-200 focus:outline-none focus:ring-2 focus:ring-gray-500 text-sm font-medium"
          >
            Hætta við
          </button>
          <button
            @click="saveFilter"
            :disabled="savingFilter || (!filterForm.subject?.trim() && !filterForm.senderEmail?.trim())"
            class="px-4 py-2 text-white bg-indigo-600 rounded-md hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 disabled:opacity-50 disabled:cursor-not-allowed text-sm font-medium"
          >
            {{ savingFilter ? 'Vista...' : 'Vista' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { EmailJunkFilterDto, CreateEmailJunkFilterDto, UpdateEmailJunkFilterDto } from '~/composables/useEmailJunkFilters'

const { currentUser, canAccessAdmin } = useAuth()
const { getJunkFilters, createJunkFilter, updateJunkFilter, deleteJunkFilter } = useEmailJunkFilters()

// Check authorization - only Admin and Manager can access system settings
if (process.client && !canAccessAdmin.value) {
  navigateTo('/settings')
}

const filters = ref<EmailJunkFilterDto[]>([])
const loading = ref(false)
const savingFilter = ref(false)
const showAddFilterModal = ref(false)
const editingFilter = ref<EmailJunkFilterDto | null>(null)
const filterError = ref('')

const filterForm = ref<CreateEmailJunkFilterDto>({
  subject: '',
  senderEmail: '',
  isActive: true
})

const formatDate = (dateString: string): string => {
  const date = new Date(dateString)
  return new Intl.DateTimeFormat('is-IS', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  }).format(date)
}

const loadFilters = async () => {
  loading.value = true
  try {
    filters.value = await getJunkFilters()
  } catch (error: any) {
    console.error('Error loading filters:', error)
    alert(error.data?.message || error.message || 'Villa kom upp við að hlaða síum')
  } finally {
    loading.value = false
  }
}

const editFilter = (filter: EmailJunkFilterDto) => {
  editingFilter.value = filter
  filterForm.value = {
    subject: filter.subject || '',
    senderEmail: filter.senderEmail || '',
    isActive: filter.isActive
  }
  showAddFilterModal.value = true
}

const closeFilterModal = () => {
  showAddFilterModal.value = false
  editingFilter.value = null
  filterForm.value = {
    subject: '',
    senderEmail: '',
    isActive: true
  }
  filterError.value = ''
}

const saveFilter = async () => {
  // Validate that at least one field is provided
  if (!filterForm.value.subject?.trim() && !filterForm.value.senderEmail?.trim()) {
    filterError.value = 'Verður að skilgreina annaðhvort efnisgrein eða netfang sendanda'
    return
  }

  savingFilter.value = true
  filterError.value = ''

  try {
    const dto: CreateEmailJunkFilterDto | UpdateEmailJunkFilterDto = {
      subject: filterForm.value.subject?.trim() || null,
      senderEmail: filterForm.value.senderEmail?.trim() || null,
      isActive: filterForm.value.isActive
    }

    if (editingFilter.value) {
      await updateJunkFilter(editingFilter.value.id, dto)
    } else {
      await createJunkFilter(dto)
    }

    await loadFilters()
    closeFilterModal()
  } catch (error: any) {
    filterError.value = error.data?.message || error.message || 'Villa kom upp við að vista síu'
  } finally {
    savingFilter.value = false
  }
}

const handleDeleteFilter = async (id: string) => {
  if (!confirm('Ertu viss um að þú viljir eyða þessari síu?')) return

  try {
    await deleteJunkFilter(id)
    await loadFilters()
  } catch (error: any) {
    alert(error.data?.message || error.message || 'Villa kom upp við að eyða síu')
  }
}

onMounted(() => {
  loadFilters()
})
</script>

