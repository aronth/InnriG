<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex justify-between items-center">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">Notendastjórnun</h1>
        <p class="mt-1 text-sm text-gray-500">Stjórna notendum kerfisins</p>
      </div>
      <button
        @click="showCreateModal = true"
        class="inline-flex items-center px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500"
      >
        <svg class="-ml-1 mr-2 h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
        </svg>
        Búa til notanda
      </button>
    </div>

    <!-- Users Table -->
    <div class="bg-white shadow rounded-lg overflow-hidden">
      <div v-if="isLoading" class="p-8 text-center">
        <svg class="animate-spin h-8 w-8 text-indigo-600 mx-auto" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
        </svg>
        <p class="mt-2 text-sm text-gray-500">Hleður notendum...</p>
      </div>

      <div v-else-if="errorMsg" class="p-4 bg-red-50 border-l-4 border-red-400">
        <div class="flex">
          <div class="flex-shrink-0">
            <svg class="h-5 w-5 text-red-400" fill="currentColor" viewBox="0 0 20 20">
              <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd" />
            </svg>
          </div>
          <div class="ml-3">
            <p class="text-sm text-red-800">{{ errorMsg }}</p>
          </div>
        </div>
      </div>

      <table v-else class="min-w-full divide-y divide-gray-200">
        <thead class="bg-gray-50">
          <tr>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Notandanafn
            </th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Nafn
            </th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Stöða
            </th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Stofnað
            </th>
            <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
              Aðgerðir
            </th>
          </tr>
        </thead>
        <tbody class="bg-white divide-y divide-gray-200">
          <tr v-for="user in users" :key="user.id">
            <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
              {{ user.username }}
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
              {{ user.name }}
            </td>
            <td class="px-6 py-4 whitespace-nowrap">
              <span
                v-if="user.mustChangePassword"
                class="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-yellow-100 text-yellow-800"
              >
                Verður að breyta lykilorði
              </span>
              <span
                v-else
                class="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-green-100 text-green-800"
              >
                Virkur
              </span>
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
              {{ formatDate(user.createdAt) }}
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
              <button
                @click="editUser(user)"
                class="text-indigo-600 hover:text-indigo-900 mr-4"
              >
                Breyta
              </button>
              <button
                @click="confirmDelete(user)"
                class="text-red-600 hover:text-red-900"
              >
                Eyða
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Create User Modal -->
    <div v-if="showCreateModal" class="fixed z-50 inset-0 overflow-y-auto" @click.self="showCreateModal = false">
      <div class="flex items-center justify-center min-h-screen pt-4 px-4 pb-20 text-center sm:block sm:p-0">
        <div class="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity" @click="showCreateModal = false"></div>
        <div class="inline-block align-bottom bg-white rounded-lg text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-lg sm:w-full">
          <form @submit.prevent="handleCreateUser">
            <div class="bg-white px-4 pt-5 pb-4 sm:p-6 sm:pb-4">
              <div class="sm:flex sm:items-start">
                <div class="mt-3 text-center sm:mt-0 sm:text-left w-full">
                  <h3 class="text-lg leading-6 font-medium text-gray-900 mb-4">Búa til notanda</h3>
                  
                  <div class="space-y-4">
                    <div>
                      <label for="create-username" class="block text-sm font-medium text-gray-700">
                        Notandanafn
                      </label>
                      <input
                        id="create-username"
                        v-model="createForm.username"
                        type="text"
                        required
                        class="mt-1 block w-full border-gray-300 rounded-md shadow-sm focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm px-3 py-2 border"
                        placeholder="notandanafn"
                      />
                    </div>
                    <div>
                      <label for="create-name" class="block text-sm font-medium text-gray-700">
                        Nafn
                      </label>
                      <input
                        id="create-name"
                        v-model="createForm.name"
                        type="text"
                        required
                        class="mt-1 block w-full border-gray-300 rounded-md shadow-sm focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm px-3 py-2 border"
                        placeholder="Fullt nafn"
                      />
                    </div>
                    <div v-if="createError" class="rounded-md bg-red-50 p-3">
                      <p class="text-sm text-red-800">{{ createError }}</p>
                    </div>
                  </div>
                </div>
              </div>
            </div>
            <div class="bg-gray-50 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse">
              <button
                type="submit"
                :disabled="isCreating"
                class="w-full inline-flex justify-center rounded-md border border-transparent shadow-sm px-4 py-2 bg-indigo-600 text-base font-medium text-white hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 sm:ml-3 sm:w-auto sm:text-sm disabled:opacity-50"
              >
                Búa til
              </button>
              <button
                type="button"
                @click="showCreateModal = false"
                class="mt-3 w-full inline-flex justify-center rounded-md border border-gray-300 shadow-sm px-4 py-2 bg-white text-base font-medium text-gray-700 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 sm:mt-0 sm:ml-3 sm:w-auto sm:text-sm"
              >
                Hætta við
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>

    <!-- Edit User Modal -->
    <div v-if="showEditModal && editingUser" class="fixed z-50 inset-0 overflow-y-auto" @click.self="showEditModal = false">
      <div class="flex items-center justify-center min-h-screen pt-4 px-4 pb-20 text-center sm:block sm:p-0">
        <div class="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity" @click="showEditModal = false"></div>
        <div class="inline-block align-bottom bg-white rounded-lg text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-lg sm:w-full">
          <form @submit.prevent="handleUpdateUser">
            <div class="bg-white px-4 pt-5 pb-4 sm:p-6 sm:pb-4">
              <div class="sm:flex sm:items-start">
                <div class="mt-3 text-center sm:mt-0 sm:text-left w-full">
                  <h3 class="text-lg leading-6 font-medium text-gray-900 mb-4">Breyta notanda</h3>
                  
                  <div class="space-y-4">
                    <div>
                      <label class="block text-sm font-medium text-gray-700">
                        Notandanafn
                      </label>
                      <input
                        type="text"
                        :value="editingUser.username"
                        disabled
                        class="mt-1 block w-full border-gray-300 rounded-md shadow-sm bg-gray-50 sm:text-sm px-3 py-2 border"
                      />
                      <p class="mt-1 text-xs text-gray-500">Notandanafn er ekki hægt að breyta</p>
                    </div>
                    <div>
                      <label for="edit-name" class="block text-sm font-medium text-gray-700">
                        Nafn
                      </label>
                      <input
                        id="edit-name"
                        v-model="editForm.name"
                        type="text"
                        required
                        class="mt-1 block w-full border-gray-300 rounded-md shadow-sm focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm px-3 py-2 border"
                        placeholder="Fullt nafn"
                      />
                    </div>
                    <div v-if="editError" class="rounded-md bg-red-50 p-3">
                      <p class="text-sm text-red-800">{{ editError }}</p>
                    </div>
                  </div>
                </div>
              </div>
            </div>
            <div class="bg-gray-50 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse">
              <button
                type="submit"
                :disabled="isUpdating"
                class="w-full inline-flex justify-center rounded-md border border-transparent shadow-sm px-4 py-2 bg-indigo-600 text-base font-medium text-white hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 sm:ml-3 sm:w-auto sm:text-sm disabled:opacity-50"
              >
                Vista
              </button>
              <button
                type="button"
                @click="showEditModal = false"
                class="mt-3 w-full inline-flex justify-center rounded-md border border-gray-300 shadow-sm px-4 py-2 bg-white text-base font-medium text-gray-700 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 sm:mt-0 sm:ml-3 sm:w-auto sm:text-sm"
              >
                Hætta við
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>

    <!-- Delete Confirmation Modal -->
    <div v-if="showDeleteModal && deletingUser" class="fixed z-50 inset-0 overflow-y-auto" @click.self="showDeleteModal = false">
      <div class="flex items-center justify-center min-h-screen pt-4 px-4 pb-20 text-center sm:block sm:p-0">
        <div class="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity" @click="showDeleteModal = false"></div>
        <div class="inline-block align-bottom bg-white rounded-lg text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-lg sm:w-full">
          <div class="bg-white px-4 pt-5 pb-4 sm:p-6 sm:pb-4">
            <div class="sm:flex sm:items-start">
              <div class="mx-auto flex-shrink-0 flex items-center justify-center h-12 w-12 rounded-full bg-red-100 sm:mx-0 sm:h-10 sm:w-10">
                <svg class="h-6 w-6 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
                </svg>
              </div>
              <div class="mt-3 text-center sm:mt-0 sm:ml-4 sm:text-left">
                <h3 class="text-lg leading-6 font-medium text-gray-900">Eyða notanda</h3>
                <div class="mt-2">
                  <p class="text-sm text-gray-500">
                    Ertu viss um að þú viljir eyða notandanum <strong>{{ deletingUser.name }}</strong> ({{ deletingUser.username }})? 
                    Þessi aðgerð er óafturkræf.
                  </p>
                </div>
              </div>
            </div>
          </div>
          <div class="bg-gray-50 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse">
            <button
              @click="handleDeleteUser"
              :disabled="isDeleting"
              class="w-full inline-flex justify-center rounded-md border border-transparent shadow-sm px-4 py-2 bg-red-600 text-base font-medium text-white hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-500 sm:ml-3 sm:w-auto sm:text-sm disabled:opacity-50"
            >
              Eyða
            </button>
            <button
              @click="showDeleteModal = false"
              class="mt-3 w-full inline-flex justify-center rounded-md border border-gray-300 shadow-sm px-4 py-2 bg-white text-base font-medium text-gray-700 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 sm:mt-0 sm:ml-3 sm:w-auto sm:text-sm"
            >
              Hætta við
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { User } from '~/types/user'

const { getAllUsers, updateUser, deleteUser } = useUsers()
const { createUser } = useAuth()

const users = ref<User[]>([])
const isLoading = ref(false)
const errorMsg = ref('')

// Create modal
const showCreateModal = ref(false)
const isCreating = ref(false)
const createError = ref('')
const createForm = ref({
  username: '',
  name: ''
})

// Edit modal
const showEditModal = ref(false)
const isUpdating = ref(false)
const editError = ref('')
const editingUser = ref<User | null>(null)
const editForm = ref({
  name: ''
})

// Delete modal
const showDeleteModal = ref(false)
const isDeleting = ref(false)
const deletingUser = ref<User | null>(null)

const loadUsers = async () => {
  isLoading.value = true
  errorMsg.value = ''
  try {
    users.value = await getAllUsers()
  } catch (error: any) {
    errorMsg.value = error.data?.message || error.message || 'Mistókst að hlaða notendum'
  } finally {
    isLoading.value = false
  }
}

const handleCreateUser = async () => {
  isCreating.value = true
  createError.value = ''
  try {
    await createUser(createForm.value.username, createForm.value.name)
    showCreateModal.value = false
    createForm.value = { username: '', name: '' }
    await loadUsers()
  } catch (error: any) {
    createError.value = error.data?.message || error.message || 'Mistókst að búa til notanda'
  } finally {
    isCreating.value = false
  }
}

const editUser = (user: User) => {
  editingUser.value = user
  editForm.value.name = user.name
  showEditModal.value = true
}

const handleUpdateUser = async () => {
  if (!editingUser.value) return
  
  isUpdating.value = true
  editError.value = ''
  try {
    await updateUser(editingUser.value.id, editForm.value.name)
    showEditModal.value = false
    editingUser.value = null
    await loadUsers()
  } catch (error: any) {
    editError.value = error.data?.message || error.message || 'Mistókst að uppfæra notanda'
  } finally {
    isUpdating.value = false
  }
}

const confirmDelete = (user: User) => {
  deletingUser.value = user
  showDeleteModal.value = true
}

const handleDeleteUser = async () => {
  if (!deletingUser.value) return
  
  isDeleting.value = true
  try {
    await deleteUser(deletingUser.value.id)
    showDeleteModal.value = false
    deletingUser.value = null
    await loadUsers()
  } catch (error: any) {
    alert(error.data?.message || error.message || 'Mistókst að eyða notanda')
  } finally {
    isDeleting.value = false
  }
}

const formatDate = (dateString: string) => {
  return new Date(dateString).toLocaleDateString('is-IS')
}

onMounted(() => {
  loadUsers()
})
</script>

