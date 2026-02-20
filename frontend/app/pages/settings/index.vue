<template>
  <div class="min-h-screen py-8">
    <div class="space-y-6">
      <!-- Header -->
      <div class="mb-8">
        <h1 class="text-3xl font-bold text-gray-900">Stillingar</h1>
        <p class="mt-2 text-sm text-gray-600">Stjórnaðu persónulegum upplýsingum og öryggisstillingum þínum</p>
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
            v-if="canAccessAdmin"
            to="/settings/system"
            class="whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm"
            :class="$route.path === '/settings/system' ? 'border-indigo-500 text-indigo-600' : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'"
          >
            Kerfisstillingar
          </NuxtLink>
          <NuxtLink
            v-if="canAccessAdmin"
            to="/settings/users"
            class="whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm"
            :class="$route.path === '/settings/users' ? 'border-indigo-500 text-indigo-600' : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'"
          >
            Notendur
          </NuxtLink>
          <NuxtLink
            to="/settings/email-classifications"
            class="whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm"
            :class="$route.path.startsWith('/settings/email-classifications') ? 'border-indigo-500 text-indigo-600' : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'"
          >
            Tölvupóstflokkanir
          </NuxtLink>
          <NuxtLink
            to="/settings/workflow-definitions"
            class="whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm"
            :class="$route.path.startsWith('/settings/workflow-definitions') ? 'border-indigo-500 text-indigo-600' : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'"
          >
            Ferlar
          </NuxtLink>
        </nav>
      </div>

      <!-- Profile Section -->
      <div class="bg-white shadow rounded-lg mb-6">
        <div class="px-6 py-5 border-b border-gray-200">
          <h2 class="text-lg font-medium text-gray-900">Persónuupplýsingar</h2>
          <p class="mt-1 text-sm text-gray-500">Grundvallarupplýsingar um notandann</p>
        </div>
        <div class="px-6 py-5">
          <div v-if="currentUser" class="space-y-6">
            <!-- User Avatar and Info -->
            <div class="flex items-center gap-4">
              <div class="h-20 w-20 rounded-full bg-gradient-to-br from-indigo-500 to-purple-600 border-4 border-white shadow-lg flex items-center justify-center text-white font-bold text-2xl">
                {{ getUserInitials(currentUser.name) }}
              </div>
              <div>
                <h3 class="text-xl font-semibold text-gray-900">{{ currentUser.name }}</h3>
                <p class="text-sm text-gray-500">{{ currentUser.username }}</p>
                <div v-if="currentUser.roles && currentUser.roles.length > 0" class="mt-2 flex flex-wrap gap-2">
                  <span
                    v-for="role in currentUser.roles"
                    :key="role"
                    class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-indigo-100 text-indigo-800"
                  >
                    {{ role }}
                  </span>
                </div>
              </div>
            </div>

            <!-- Profile Info Grid -->
            <dl class="grid grid-cols-1 gap-6 sm:grid-cols-2">
              <div>
                <dt class="text-sm font-medium text-gray-500">Notandanafn</dt>
                <dd class="mt-1 text-sm text-gray-900">{{ currentUser.username }}</dd>
              </div>
              <div>
                <dt class="text-sm font-medium text-gray-500">Skráður</dt>
                <dd class="mt-1 text-sm text-gray-900">
                  {{ formatDate(currentUser.createdAt) }}
                </dd>
              </div>
            </dl>
          </div>
        </div>
      </div>

      <!-- Update Name Section -->
      <div class="bg-white shadow rounded-lg mb-6">
        <div class="px-6 py-5 border-b border-gray-200">
          <h2 class="text-lg font-medium text-gray-900">Breyta nafni</h2>
          <p class="mt-1 text-sm text-gray-500">Uppfærðu nafnið þitt</p>
        </div>
        <div class="px-6 py-5">
          <form @submit.prevent="handleUpdateName" class="space-y-4">
            <div>
              <label for="name" class="block text-sm font-medium text-gray-700 mb-2">
                Nafn
              </label>
              <input
                id="name"
                v-model="nameForm.name"
                type="text"
                required
                class="block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                placeholder="Nafn"
              />
            </div>
            <div v-if="nameError" class="rounded-md bg-red-50 p-4">
              <p class="text-sm text-red-800">{{ nameError }}</p>
            </div>
            <div v-if="nameSuccess" class="rounded-md bg-green-50 p-4">
              <p class="text-sm text-green-800">{{ nameSuccess }}</p>
            </div>
            <div class="flex justify-end">
              <button
                type="submit"
                :disabled="isUpdatingName"
                class="inline-flex justify-center rounded-md border border-transparent bg-indigo-600 py-2 px-4 text-sm font-medium text-white shadow-sm hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                <span v-if="isUpdatingName">Vista...</span>
                <span v-else>Vista breytingar</span>
              </button>
            </div>
          </form>
        </div>
      </div>

      <!-- Change Password Section -->
      <div class="bg-white shadow rounded-lg mb-6">
        <div class="px-6 py-5 border-b border-gray-200">
          <h2 class="text-lg font-medium text-gray-900">Breyta lykilorði</h2>
          <p class="mt-1 text-sm text-gray-500">Uppfærðu lykilorðið þitt</p>
        </div>
        <div class="px-6 py-5">
          <form @submit.prevent="handleChangePassword" class="space-y-4">
            <div>
              <label for="currentPassword" class="block text-sm font-medium text-gray-700 mb-2">
                Núverandi lykilorð
              </label>
              <input
                id="currentPassword"
                v-model="passwordForm.currentPassword"
                type="password"
                required
                class="block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                placeholder="Núverandi lykilorð"
              />
            </div>
            <div>
              <label for="newPassword" class="block text-sm font-medium text-gray-700 mb-2">
                Nýtt lykilorð
              </label>
              <input
                id="newPassword"
                v-model="passwordForm.newPassword"
                type="password"
                required
                minlength="4"
                class="block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                placeholder="Nýtt lykilorð"
              />
            </div>
            <div>
              <label for="confirmPassword" class="block text-sm font-medium text-gray-700 mb-2">
                Staðfesta nýtt lykilorð
              </label>
              <input
                id="confirmPassword"
                v-model="passwordForm.confirmPassword"
                type="password"
                required
                minlength="4"
                class="block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                placeholder="Staðfesta nýtt lykilorð"
              />
            </div>
            <div v-if="passwordError" class="rounded-md bg-red-50 p-4">
              <p class="text-sm text-red-800">{{ passwordError }}</p>
            </div>
            <div v-if="passwordSuccess" class="rounded-md bg-green-50 p-4">
              <p class="text-sm text-green-800">{{ passwordSuccess }}</p>
            </div>
            <div class="flex justify-end">
              <button
                type="submit"
                :disabled="isChangingPassword || !isPasswordFormValid"
                class="inline-flex justify-center rounded-md border border-transparent bg-indigo-600 py-2 px-4 text-sm font-medium text-white shadow-sm hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                <span v-if="isChangingPassword">Breyta...</span>
                <span v-else>Breyta lykilorði</span>
              </button>
            </div>
          </form>
        </div>
      </div>

      <!-- Email Mappings Section -->
      <div class="bg-white shadow rounded-lg mb-6">
        <div class="px-6 py-5 border-b border-gray-200">
          <div class="flex items-center justify-between">
            <div>
              <h2 class="text-lg font-medium text-gray-900">Tengd netföng</h2>
              <p class="mt-1 text-sm text-gray-500">Stjórnaðu netföngum sem tengjast reikningnum</p>
            </div>
            <button
              @click="showAddEmailModal = true"
              class="inline-flex items-center px-4 py-2 bg-indigo-600 text-white rounded-md hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 text-sm font-medium"
            >
              <svg class="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
              </svg>
              Bæta við netfangi
            </button>
          </div>
        </div>
        <div class="px-6 py-5">
          <div v-if="emailMappings.length > 0" class="space-y-3">
            <div
              v-for="mapping in emailMappings"
              :key="mapping.id"
              class="flex items-center justify-between p-4 border border-gray-200 rounded-lg hover:bg-gray-50"
            >
              <div class="flex-1">
                <div class="flex items-center gap-2">
                  <span class="font-medium text-gray-900">
                    {{ mapping.displayName || mapping.emailAddress }}
                  </span>
                  <span
                    v-if="mapping.isDefault"
                    class="px-2 py-0.5 text-xs font-semibold bg-indigo-100 text-indigo-800 rounded-full"
                  >
                    Sjálfgefið
                  </span>
                </div>
                <div class="text-sm text-gray-600 mt-1">{{ mapping.emailAddress }}</div>
              </div>
              <div class="flex items-center gap-2">
                <button
                  @click="editEmailMapping(mapping)"
                  class="px-3 py-1 text-sm text-indigo-600 hover:text-indigo-800 hover:bg-indigo-50 rounded"
                >
                  Breyta
                </button>
                <button
                  @click="handleDeleteEmailMapping(mapping.id)"
                  class="px-3 py-1 text-sm text-red-600 hover:text-red-800 hover:bg-red-50 rounded"
                >
                  Eyða
                </button>
              </div>
            </div>
          </div>
          <div v-else class="text-center py-8 text-gray-500">
            <p>Engin netföng tengd við reikninginn</p>
            <p class="text-sm mt-2">Smelltu á "Bæta við netfangi" til að tengja netfang</p>
          </div>
        </div>
      </div>

      <!-- Email Signature Section -->
      <div class="bg-white shadow rounded-lg mb-6">
        <div class="px-6 py-5 border-b border-gray-200">
          <h2 class="text-lg font-medium text-gray-900">Tölvupóstundirskrift</h2>
          <p class="mt-1 text-sm text-gray-500">Undirskriftin verður sjálfkrafa bætt við í lok tölvupósta sem þú sendir</p>
        </div>
        <div class="px-6 py-5">
          <div class="mb-4">
            <label class="block text-sm font-medium text-gray-700 mb-2">
              Undirskrift
            </label>
            <div class="border border-gray-300 rounded-md" style="min-height: 200px;">
              <EmailEditor
                v-model="emailSignature"
                :editor-class="'min-h-[200px]'"
              />
            </div>
          </div>
          <div v-if="signatureError" class="rounded-md bg-red-50 p-4 mb-4">
            <p class="text-sm text-red-800">{{ signatureError }}</p>
          </div>
          <div v-if="signatureSuccess" class="rounded-md bg-green-50 p-4 mb-4">
            <p class="text-sm text-green-800">{{ signatureSuccess }}</p>
          </div>
          <div class="flex items-center justify-end gap-3">
            <button
              @click="loadEmailSignature"
              class="px-4 py-2 text-gray-700 bg-gray-100 rounded-md hover:bg-gray-200 focus:outline-none focus:ring-2 focus:ring-gray-500 text-sm font-medium"
            >
              Hætta við
            </button>
            <button
              @click="saveEmailSignature"
              :disabled="savingSignature"
              class="inline-flex items-center px-4 py-2 text-white bg-indigo-600 rounded-md hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 disabled:opacity-50 disabled:cursor-not-allowed text-sm font-medium"
            >
              <svg
                v-if="savingSignature"
                class="animate-spin h-5 w-5 mr-2"
                xmlns="http://www.w3.org/2000/svg"
                fill="none"
                viewBox="0 0 24 24"
              >
                <circle
                  class="opacity-25"
                  cx="12"
                  cy="12"
                  r="10"
                  stroke="currentColor"
                  stroke-width="4"
                ></circle>
                <path
                  class="opacity-75"
                  fill="currentColor"
                  d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                ></path>
              </svg>
              {{ savingSignature ? 'Vista...' : 'Vista' }}
            </button>
          </div>
        </div>
      </div>

      <!-- Debug Section -->
      <div class="bg-white shadow rounded-lg mb-6">
        <div class="px-6 py-5 border-b border-gray-200">
          <h2 class="text-lg font-medium text-gray-900">Debug - Authentication Claims</h2>
          <p class="mt-1 text-sm text-gray-500">Test authentication claims in cookie</p>
        </div>
        <div class="px-6 py-5">
          <button
            @click="testClaims"
            :disabled="loadingClaims"
            class="px-4 py-2 bg-yellow-600 text-white rounded-md hover:bg-yellow-700 focus:outline-none focus:ring-2 focus:ring-yellow-500 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {{ loadingClaims ? 'Hleður...' : 'Prófa Claims' }}
          </button>
          
          <div v-if="claimsResult" class="mt-4">
            <h3 class="text-sm font-medium text-gray-900 mb-2">Results:</h3>
            <pre class="bg-gray-100 p-4 rounded-md text-xs overflow-auto max-h-96">{{ JSON.stringify(claimsResult, null, 2) }}</pre>
          </div>
          
          <div v-if="claimsError" class="mt-4 p-3 bg-red-50 border border-red-200 rounded-md">
            <p class="text-sm text-red-800">{{ claimsError }}</p>
          </div>
        </div>
      </div>
    </div>

    <!-- Add/Edit Email Modal -->
    <div
      v-if="showAddEmailModal || editingMapping"
      class="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50"
      @click.self="closeEmailModal"
    >
      <div class="bg-white rounded-lg shadow-xl w-full max-w-md m-4">
        <div class="px-6 py-4 border-b border-gray-200 flex items-center justify-between">
          <h3 class="text-lg font-bold text-gray-900">
            {{ editingMapping ? 'Breyta netfangi' : 'Bæta við netfangi' }}
          </h3>
          <button
            @click="closeEmailModal"
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
              Netfang *
            </label>
            <input
              v-model="emailForm.emailAddress"
              type="email"
              :disabled="editingMapping !== null"
              class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500 disabled:bg-gray-50 disabled:text-gray-600"
              placeholder="nafn@example.com"
            />
          </div>

          <div class="mb-4">
            <label class="block text-sm font-medium text-gray-700 mb-2">
              Birtingarnafn
            </label>
            <input
              v-model="emailForm.displayName"
              type="text"
              class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
              placeholder="Jón Jónsson"
            />
          </div>

          <div class="mb-4">
            <label class="flex items-center">
              <input
                v-model="emailForm.isDefault"
                type="checkbox"
                class="rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
              />
              <span class="ml-2 text-sm text-gray-700">Gera að sjálfgefnu netfangi</span>
            </label>
          </div>
        </div>

        <div class="px-6 py-4 border-t border-gray-200 flex items-center justify-end gap-3">
          <button
            @click="closeEmailModal"
            class="px-4 py-2 text-gray-700 bg-gray-100 rounded-md hover:bg-gray-200 focus:outline-none focus:ring-2 focus:ring-gray-500 text-sm font-medium"
          >
            Hætta við
          </button>
          <button
            @click="saveEmailMapping"
            :disabled="savingEmail || !emailForm.emailAddress.trim()"
            class="px-4 py-2 text-white bg-indigo-600 rounded-md hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 disabled:opacity-50 disabled:cursor-not-allowed text-sm font-medium"
          >
            {{ savingEmail ? 'Vista...' : 'Vista' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { UserEmailMappingDto } from '~/types/userEmailSettings'

const { currentUser, updateProfile, changePassword, getCurrentUser, canAccessAdmin, canAccessSystemAdmin } = useAuth()
const { getEmailMappings, createEmailMapping, updateEmailMapping, deleteEmailMapping, getEmailSignature, updateEmailSignature } = useUserEmailSettings()

const nameForm = ref({
  name: currentUser.value?.name || ''
})

const passwordForm = ref({
  currentPassword: '',
  newPassword: '',
  confirmPassword: ''
})

const isUpdatingName = ref(false)
const isChangingPassword = ref(false)
const nameError = ref('')
const nameSuccess = ref('')
const passwordError = ref('')
const passwordSuccess = ref('')

// Email settings
const emailMappings = ref<UserEmailMappingDto[]>([])
const emailSignature = ref<string>('')
const savingSignature = ref(false)
const savingEmail = ref(false)
const showAddEmailModal = ref(false)
const editingMapping = ref<UserEmailMappingDto | null>(null)
const signatureError = ref('')
const signatureSuccess = ref('')

const emailForm = ref({
  emailAddress: '',
  displayName: '',
  isDefault: false
})

// Debug claims
const loadingClaims = ref(false)
const claimsResult = ref<any>(null)
const claimsError = ref('')

// Initialize name form when user loads
watch(currentUser, (user) => {
  if (user) {
    nameForm.value.name = user.name
  }
}, { immediate: true })

const testClaims = async () => {
  loadingClaims.value = true
  claimsError.value = ''
  claimsResult.value = null
  
  try {
    const config = useRuntimeConfig()
    const apiBase = config.public.apiBase
    const { apiFetch } = useApi()
    
    const result = await apiFetch<any>(`${apiBase}/api/auth/debug/claims`)
    claimsResult.value = result
  } catch (error: any) {
    claimsError.value = error.data?.message || error.message || 'Mistókst að hlaða claims'
    console.error('Error fetching claims:', error)
  } finally {
    loadingClaims.value = false
  }
}

const getUserInitials = (name: string): string => {
  if (!name) return '?'
  const parts = name.trim().split(' ').filter(p => p.length > 0)
  if (parts.length === 0) return '?'
  if (parts.length === 1) {
    return parts[0]?.charAt(0).toUpperCase() || '?'
  }
  const first = parts[0]?.charAt(0) || ''
  const last = parts[parts.length - 1]?.charAt(0) || ''
  return (first + last).toUpperCase()
}

const formatDate = (dateString: string): string => {
  const date = new Date(dateString)
  return new Intl.DateTimeFormat('is-IS', {
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  }).format(date)
}

const isPasswordFormValid = computed(() => {
  return passwordForm.value.newPassword.length >= 4 &&
         passwordForm.value.newPassword === passwordForm.value.confirmPassword &&
         passwordForm.value.currentPassword.length > 0
})

const handleUpdateName = async () => {
  if (!currentUser.value) return

  isUpdatingName.value = true
  nameError.value = ''
  nameSuccess.value = ''

  try {
    await updateProfile(nameForm.value.name)
    nameSuccess.value = 'Nafn hefur verið uppfært'
    // Refresh user data
    await getCurrentUser()
    setTimeout(() => {
      nameSuccess.value = ''
    }, 3000)
  } catch (error: any) {
    nameError.value = error.data?.message || error.message || 'Mistókst að uppfæra nafn'
  } finally {
    isUpdatingName.value = false
  }
}

const handleChangePassword = async () => {
  if (!isPasswordFormValid.value) {
    passwordError.value = 'Lykilorð verða að vera að minnsta kosti 4 stafir og verða að passa'
    return
  }

  isChangingPassword.value = true
  passwordError.value = ''
  passwordSuccess.value = ''

  try {
    await changePassword(passwordForm.value.currentPassword, passwordForm.value.newPassword)
    passwordSuccess.value = 'Lykilorð hefur verið breytt'
    passwordForm.value = {
      currentPassword: '',
      newPassword: '',
      confirmPassword: ''
    }
    setTimeout(() => {
      passwordSuccess.value = ''
    }, 3000)
  } catch (error: any) {
    passwordError.value = error.data?.message || error.message || 'Mistókst að breyta lykilorði'
  } finally {
    isChangingPassword.value = false
  }
}

// Email settings functions
const loadEmailMappings = async () => {
  try {
    emailMappings.value = await getEmailMappings()
  } catch (error) {
    console.error('Error loading email mappings:', error)
  }
}

const loadEmailSignature = async () => {
  try {
    emailSignature.value = await getEmailSignature()
  } catch (error) {
    console.error('Error loading email signature:', error)
  }
}

const saveEmailSignature = async () => {
  savingSignature.value = true
  signatureError.value = ''
  signatureSuccess.value = ''
  
  try {
    await updateEmailSignature(emailSignature.value)
    signatureSuccess.value = 'Undirskrift hefur verið vistuð'
    setTimeout(() => {
      signatureSuccess.value = ''
    }, 3000)
  } catch (error: any) {
    signatureError.value = error.data?.message || error.message || 'Villa kom upp við að vista undirskrift'
  } finally {
    savingSignature.value = false
  }
}

const editEmailMapping = (mapping: UserEmailMappingDto) => {
  editingMapping.value = mapping
  emailForm.value = {
    emailAddress: mapping.emailAddress,
    displayName: mapping.displayName || '',
    isDefault: mapping.isDefault
  }
  showAddEmailModal.value = true
}

const closeEmailModal = () => {
  showAddEmailModal.value = false
  editingMapping.value = null
  emailForm.value = {
    emailAddress: '',
    displayName: '',
    isDefault: false
  }
}

const saveEmailMapping = async () => {
  if (!emailForm.value.emailAddress.trim()) return

  savingEmail.value = true
  try {
    if (editingMapping.value) {
      await updateEmailMapping(editingMapping.value.id, {
        displayName: emailForm.value.displayName || undefined,
        isDefault: emailForm.value.isDefault
      })
    } else {
      await createEmailMapping({
        emailAddress: emailForm.value.emailAddress,
        displayName: emailForm.value.displayName || undefined,
        isDefault: emailForm.value.isDefault
      })
    }
    await loadEmailMappings()
    closeEmailModal()
  } catch (error: any) {
    alert(error.data?.message || error.message || 'Villa kom upp við að vista netfang')
  } finally {
    savingEmail.value = false
  }
}

const handleDeleteEmailMapping = async (id: string) => {
  if (!confirm('Ertu viss um að þú viljir eyða þessu netfangi?')) return

  try {
    await deleteEmailMapping(id)
    await loadEmailMappings()
  } catch (error: any) {
    alert(error.data?.message || error.message || 'Villa kom upp við að eyða netfangi')
  }
}

onMounted(async () => {
  await Promise.all([loadEmailMappings(), loadEmailSignature()])
})
</script>

