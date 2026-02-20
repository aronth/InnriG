<template>
  <div class="w-full px-4 sm:px-6 lg:px-8 py-8">
    <div class="mb-8 flex items-center justify-between">
      <div>
        <h1 class="text-3xl font-bold text-gray-900">Matseðlar</h1>
        <p class="mt-2 text-sm text-gray-700">
          Stjórna matseðlum og matseðilsatriðum
        </p>
      </div>
      <button
        @click="showCreateModal = true"
        class="px-4 py-2 bg-indigo-600 text-white rounded-md hover:bg-indigo-700 transition-colors font-medium"
      >
        + Nýr matseðill
      </button>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="text-center py-12">
      <div class="inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600"></div>
      <p class="mt-4 text-gray-600">Sæki matseðla...</p>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="bg-red-50 border border-red-200 rounded-lg p-6 text-center">
      <p class="text-red-800 font-semibold">Villa við að sækja matseðla</p>
      <p class="text-red-600 mt-2">{{ error }}</p>
      <button
        @click="loadMenus"
        class="mt-4 px-4 py-2 bg-red-600 text-white rounded-md hover:bg-red-700 transition-colors"
      >
        Reyna aftur
      </button>
    </div>

    <!-- Menus Grid -->
    <div v-else-if="menus.length > 0" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      <div
        v-for="menu in menus"
        :key="menu.id"
        class="bg-white rounded-lg shadow-md overflow-hidden hover:shadow-lg transition-shadow"
      >
        <div class="p-6">
          <div class="flex items-start justify-between mb-4">
            <div class="flex-1">
              <h3 class="text-xl font-semibold text-gray-900 mb-1">{{ menu.name }}</h3>
              <p class="text-sm text-gray-600">{{ menu.forWho }}</p>
            </div>
            <span
              class="px-2 py-1 text-xs font-medium rounded-full"
              :class="menu.isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'"
            >
              {{ menu.isActive ? 'Virkur' : 'Óvirkur' }}
            </span>
          </div>

          <p v-if="menu.description" class="text-sm text-gray-600 mb-4 line-clamp-2">
            {{ menu.description }}
          </p>

          <div class="flex items-center justify-between text-sm text-gray-500 mb-4">
            <span>{{ menu.menuItems.length }} atriði</span>
            <span>{{ formatDate(menu.updatedAt) }}</span>
          </div>

          <div class="flex gap-2">
            <NuxtLink
              :to="`/menus/${menu.id}`"
              class="flex-1 px-3 py-2 bg-indigo-50 text-indigo-700 rounded-md hover:bg-indigo-100 transition-colors text-center text-sm font-medium"
            >
              Skoða
            </NuxtLink>
            <button
              @click="editMenu(menu)"
              class="px-3 py-2 bg-gray-100 text-gray-700 rounded-md hover:bg-gray-200 transition-colors text-sm font-medium"
            >
              Breyta
            </button>
            <button
              @click="deleteMenu(menu.id)"
              class="px-3 py-2 bg-red-50 text-red-700 rounded-md hover:bg-red-100 transition-colors text-sm font-medium"
            >
              Eyða
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Empty State -->
    <div v-else class="bg-white rounded-lg shadow p-12 text-center">
      <p class="text-gray-500 mb-4">Engir matseðlar skráðir</p>
      <button
        @click="showCreateModal = true"
        class="px-4 py-2 bg-indigo-600 text-white rounded-md hover:bg-indigo-700 transition-colors"
      >
        Búa til nýjan matseðil
      </button>
    </div>

    <!-- Create/Edit Modal -->
    <div
      v-if="showCreateModal || editingMenu"
      class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50"
      @click.self="closeModal"
    >
      <div class="bg-white rounded-lg shadow-xl max-w-2xl w-full mx-4 max-h-[90vh] overflow-y-auto">
        <div class="px-6 py-4 border-b border-gray-200">
          <h2 class="text-xl font-semibold text-gray-900">
            {{ editingMenu ? 'Breyta matseðli' : 'Nýr matseðill' }}
          </h2>
        </div>

        <form @submit.prevent="saveMenu" class="px-6 py-4">
          <div class="mb-4">
            <label class="block text-sm font-medium text-gray-700 mb-1">
              Nafn <span class="text-red-500">*</span>
            </label>
            <input
              v-model="form.name"
              type="text"
              required
              class="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            />
          </div>

          <div class="mb-4">
            <label class="block text-sm font-medium text-gray-700 mb-1">
              Fyrir hvern <span class="text-red-500">*</span>
            </label>
            <input
              v-model="form.forWho"
              type="text"
              placeholder="T.d. Fyrirtæki, Einstaklingar, Allir"
              required
              class="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            />
          </div>

          <div class="mb-4">
            <label class="block text-sm font-medium text-gray-700 mb-1">Lýsing</label>
            <textarea
              v-model="form.description"
              rows="3"
              class="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            />
          </div>

          <div class="mb-4">
            <label class="flex items-center">
              <input
                v-model="form.isActive"
                type="checkbox"
                class="rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
              />
              <span class="ml-2 text-sm text-gray-700">Virkur</span>
            </label>
          </div>

          <div class="flex justify-end gap-3 pt-4 border-t border-gray-200">
            <button
              type="button"
              @click="closeModal"
              class="px-4 py-2 border border-gray-300 rounded-md text-gray-700 hover:bg-gray-50 transition-colors"
            >
              Hætta við
            </button>
            <button
              type="submit"
              :disabled="saving"
              class="px-4 py-2 bg-indigo-600 text-white rounded-md hover:bg-indigo-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {{ saving ? 'Vista...' : 'Vista' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useMenus } from '~/composables/useMenus'
import type { MenuDto, CreateMenuDto, UpdateMenuDto } from '~/types/menu'

const { getAllMenus, createMenu, updateMenu, deleteMenu: deleteMenuApi } = useMenus()

const loading = ref(false)
const error = ref<string | null>(null)
const menus = ref<MenuDto[]>([])
const showCreateModal = ref(false)
const editingMenu = ref<MenuDto | null>(null)
const saving = ref(false)

const form = ref<CreateMenuDto>({
  name: '',
  forWho: '',
  description: '',
  isActive: true
})

const loadMenus = async () => {
  loading.value = true
  error.value = null

  try {
    menus.value = await getAllMenus()
  } catch (err: any) {
    error.value = err.message || 'Óþekkt villa'
    console.error('Error loading menus:', err)
  } finally {
    loading.value = false
  }
}

const saveMenu = async () => {
  saving.value = true

  try {
    if (editingMenu.value) {
      const updateDto: UpdateMenuDto = {
        name: form.value.name,
        forWho: form.value.forWho,
        description: form.value.description || undefined,
        isActive: form.value.isActive
      }
      await updateMenu(editingMenu.value.id, updateDto)
    } else {
      await createMenu(form.value)
    }

    closeModal()
    await loadMenus()
  } catch (err: any) {
    error.value = err.message || 'Villa við að vista matseðil'
    console.error('Error saving menu:', err)
  } finally {
    saving.value = false
  }
}

const editMenu = (menu: MenuDto) => {
  editingMenu.value = menu
  form.value = {
    name: menu.name,
    forWho: menu.forWho,
    description: menu.description || '',
    isActive: menu.isActive
  }
  showCreateModal.value = true
}

const deleteMenu = async (id: string) => {
  if (!confirm('Ertu viss um að þú viljir eyða þessum matseðli?')) {
    return
  }

  try {
    await deleteMenuApi(id)
    await loadMenus()
  } catch (err: any) {
    error.value = err.message || 'Villa við að eyða matseðli'
    console.error('Error deleting menu:', err)
  }
}

const closeModal = () => {
  showCreateModal.value = false
  editingMenu.value = null
  form.value = {
    name: '',
    forWho: '',
    description: '',
    isActive: true
  }
}

const formatDate = (dateStr: string): string => {
  try {
    const date = new Date(dateStr)
    return date.toLocaleDateString('is-IS', { day: 'numeric', month: 'short', year: 'numeric' })
  } catch {
    return dateStr
  }
}

onMounted(() => {
  loadMenus()
})
</script>

