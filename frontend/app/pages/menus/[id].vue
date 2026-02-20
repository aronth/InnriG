<template>
  <div class="w-full px-4 sm:px-6 lg:px-8 py-8">
    <div class="mb-8">
      <NuxtLink
        to="/menus"
        class="inline-flex items-center text-sm text-gray-600 hover:text-gray-900 mb-4"
      >
        <svg class="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
        </svg>
        Til baka
      </NuxtLink>
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-3xl font-bold text-gray-900">{{ menu?.name || 'Matseðill' }}</h1>
          <p v-if="menu" class="mt-2 text-sm text-gray-700">
            {{ menu.forWho }}
          </p>
        </div>
        <button
          @click="showCreateItemModal = true"
          class="px-4 py-2 bg-indigo-600 text-white rounded-md hover:bg-indigo-700 transition-colors font-medium"
        >
          + Nýtt atriði
        </button>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="text-center py-12">
      <div class="inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600"></div>
      <p class="mt-4 text-gray-600">Sæki matseðil...</p>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="bg-red-50 border border-red-200 rounded-lg p-6 text-center">
      <p class="text-red-800 font-semibold">Villa við að sækja matseðil</p>
      <p class="text-red-600 mt-2">{{ error }}</p>
      <NuxtLink
        to="/menus"
        class="mt-4 inline-block px-4 py-2 bg-red-600 text-white rounded-md hover:bg-red-700 transition-colors"
      >
        Til baka
      </NuxtLink>
    </div>

    <!-- Menu Details -->
    <div v-else-if="menu" class="space-y-6">
      <!-- Menu Info -->
      <div class="bg-white rounded-lg shadow p-6">
        <div class="flex items-start justify-between mb-4">
          <div class="flex-1">
            <h2 class="text-xl font-semibold text-gray-900 mb-2">Upplýsingar</h2>
            <p v-if="menu.description" class="text-gray-600 mb-4">{{ menu.description }}</p>
            <div class="flex items-center gap-4 text-sm text-gray-600">
              <span>
                <span class="font-medium">Staða:</span>
                <span
                  class="ml-2 px-2 py-1 rounded-full text-xs"
                  :class="menu.isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'"
                >
                  {{ menu.isActive ? 'Virkur' : 'Óvirkur' }}
                </span>
              </span>
              <span>
                <span class="font-medium">Atriði:</span> {{ menu.menuItems.length }}
              </span>
            </div>
          </div>
          <button
            @click="showEditMenuModal = true"
            class="px-3 py-2 bg-gray-100 text-gray-700 rounded-md hover:bg-gray-200 transition-colors text-sm font-medium"
          >
            Breyta
          </button>
        </div>
      </div>

      <!-- Menu Items -->
      <div class="bg-white rounded-lg shadow overflow-hidden">
        <div class="px-6 py-4 border-b border-gray-200">
          <h2 class="text-xl font-semibold text-gray-900">Matseðilsatriði</h2>
        </div>

        <div v-if="menu.menuItems.length === 0" class="p-12 text-center text-gray-500">
          Engin atriði skráð
        </div>

        <div v-else class="divide-y divide-gray-200">
          <div
            v-for="item in menu.menuItems"
            :key="item.id"
            class="px-6 py-4 hover:bg-gray-50 transition-colors"
          >
            <div class="flex items-start justify-between">
              <div class="flex-1">
                <div class="flex items-center gap-3 mb-1">
                  <h3 class="text-lg font-medium text-gray-900">{{ item.name }}</h3>
                  <span
                    v-if="!item.isActive"
                    class="px-2 py-1 text-xs font-medium rounded-full bg-gray-100 text-gray-800"
                  >
                    Óvirkur
                  </span>
                </div>
                <p v-if="item.description" class="text-sm text-gray-600 mb-2">
                  {{ item.description }}
                </p>
                <p class="text-lg font-semibold text-indigo-600">
                  {{ formatCurrency(item.price) }}
                </p>
              </div>
              <div class="flex gap-2 ml-4">
                <button
                  @click="editMenuItem(item)"
                  class="px-3 py-2 bg-gray-100 text-gray-700 rounded-md hover:bg-gray-200 transition-colors text-sm font-medium"
                >
                  Breyta
                </button>
                <button
                  @click="deleteMenuItem(item.id)"
                  class="px-3 py-2 bg-red-50 text-red-700 rounded-md hover:bg-red-100 transition-colors text-sm font-medium"
                >
                  Eyða
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Edit Menu Modal -->
    <div
      v-if="showEditMenuModal"
      class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50"
      @click.self="showEditMenuModal = false"
    >
      <div class="bg-white rounded-lg shadow-xl max-w-2xl w-full mx-4">
        <div class="px-6 py-4 border-b border-gray-200">
          <h2 class="text-xl font-semibold text-gray-900">Breyta matseðli</h2>
        </div>

        <form @submit.prevent="saveMenu" class="px-6 py-4">
          <div class="mb-4">
            <label class="block text-sm font-medium text-gray-700 mb-1">
              Nafn <span class="text-red-500">*</span>
            </label>
            <input
              v-model="menuForm.name"
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
              v-model="menuForm.forWho"
              type="text"
              required
              class="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            />
          </div>

          <div class="mb-4">
            <label class="block text-sm font-medium text-gray-700 mb-1">Lýsing</label>
            <textarea
              v-model="menuForm.description"
              rows="3"
              class="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            />
          </div>

          <div class="mb-4">
            <label class="flex items-center">
              <input
                v-model="menuForm.isActive"
                type="checkbox"
                class="rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
              />
              <span class="ml-2 text-sm text-gray-700">Virkur</span>
            </label>
          </div>

          <div class="flex justify-end gap-3 pt-4 border-t border-gray-200">
            <button
              type="button"
              @click="showEditMenuModal = false"
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

    <!-- Create/Edit Menu Item Modal -->
    <div
      v-if="showCreateItemModal || editingItem"
      class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50"
      @click.self="closeItemModal"
    >
      <div class="bg-white rounded-lg shadow-xl max-w-lg w-full mx-4">
        <div class="px-6 py-4 border-b border-gray-200">
          <h2 class="text-xl font-semibold text-gray-900">
            {{ editingItem ? 'Breyta atriði' : 'Nýtt atriði' }}
          </h2>
        </div>

        <form @submit.prevent="saveMenuItem" class="px-6 py-4">
          <div class="mb-4">
            <label class="block text-sm font-medium text-gray-700 mb-1">
              Nafn <span class="text-red-500">*</span>
            </label>
            <input
              v-model="itemForm.name"
              type="text"
              required
              class="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            />
          </div>

          <div class="mb-4">
            <label class="block text-sm font-medium text-gray-700 mb-1">Lýsing</label>
            <textarea
              v-model="itemForm.description"
              rows="3"
              class="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            />
          </div>

          <div class="mb-4">
            <label class="block text-sm font-medium text-gray-700 mb-1">
              Verð <span class="text-red-500">*</span>
            </label>
            <input
              v-model.number="itemForm.price"
              type="number"
              step="0.01"
              min="0"
              required
              class="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            />
          </div>

          <div class="mb-4">
            <label class="flex items-center">
              <input
                v-model="itemForm.isActive"
                type="checkbox"
                class="rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
              />
              <span class="ml-2 text-sm text-gray-700">Virkur</span>
            </label>
          </div>

          <div class="flex justify-end gap-3 pt-4 border-t border-gray-200">
            <button
              type="button"
              @click="closeItemModal"
              class="px-4 py-2 border border-gray-300 rounded-md text-gray-700 hover:bg-gray-50 transition-colors"
            >
              Hætta við
            </button>
            <button
              type="submit"
              :disabled="savingItem"
              class="px-4 py-2 bg-indigo-600 text-white rounded-md hover:bg-indigo-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {{ savingItem ? 'Vista...' : 'Vista' }}
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
import type { MenuDto, MenuItemDto, UpdateMenuDto, CreateMenuItemDto, UpdateMenuItemDto } from '~/types/menu'

const route = useRoute()
const menuId = route.params.id as string

const { getMenuById, updateMenu, createMenuItem, updateMenuItem, deleteMenuItem: deleteMenuItemApi } = useMenus()

const loading = ref(false)
const error = ref<string | null>(null)
const menu = ref<MenuDto | null>(null)
const showEditMenuModal = ref(false)
const showCreateItemModal = ref(false)
const editingItem = ref<MenuItemDto | null>(null)
const saving = ref(false)
const savingItem = ref(false)

const menuForm = ref<UpdateMenuDto>({
  name: '',
  forWho: '',
  description: '',
  isActive: true
})

const itemForm = ref<CreateMenuItemDto>({
  menuId: menuId,
  name: '',
  description: '',
  price: 0,
  isActive: true
})

const loadMenu = async () => {
  loading.value = true
  error.value = null

  try {
    menu.value = await getMenuById(menuId)
    if (menu.value) {
      menuForm.value = {
        name: menu.value.name,
        forWho: menu.value.forWho,
        description: menu.value.description || '',
        isActive: menu.value.isActive
      }
    }
  } catch (err: any) {
    error.value = err.message || 'Óþekkt villa'
    console.error('Error loading menu:', err)
  } finally {
    loading.value = false
  }
}

const saveMenu = async () => {
  saving.value = true

  try {
    if (menu.value) {
      await updateMenu(menu.value.id, menuForm.value)
      showEditMenuModal.value = false
      await loadMenu()
    }
  } catch (err: any) {
    error.value = err.message || 'Villa við að vista matseðil'
    console.error('Error saving menu:', err)
  } finally {
    saving.value = false
  }
}

const saveMenuItem = async () => {
  savingItem.value = true

  try {
    if (editingItem.value) {
      const updateDto: UpdateMenuItemDto = {
        name: itemForm.value.name,
        description: itemForm.value.description || undefined,
        price: itemForm.value.price,
        isActive: itemForm.value.isActive
      }
      await updateMenuItem(editingItem.value.id, updateDto)
    } else {
      await createMenuItem(menuId, itemForm.value)
    }

    closeItemModal()
    await loadMenu()
  } catch (err: any) {
    error.value = err.message || 'Villa við að vista atriði'
    console.error('Error saving menu item:', err)
  } finally {
    savingItem.value = false
  }
}

const editMenuItem = (item: MenuItemDto) => {
  editingItem.value = item
  itemForm.value = {
    menuId: menuId,
    name: item.name,
    description: item.description || '',
    price: item.price,
    isActive: item.isActive
  }
  showCreateItemModal.value = true
}

const deleteMenuItem = async (id: string) => {
  if (!confirm('Ertu viss um að þú viljir eyða þessu atriði?')) {
    return
  }

  try {
    await deleteMenuItemApi(id)
    await loadMenu()
  } catch (err: any) {
    error.value = err.message || 'Villa við að eyða atriði'
    console.error('Error deleting menu item:', err)
  }
}

const closeItemModal = () => {
  showCreateItemModal.value = false
  editingItem.value = null
  itemForm.value = {
    menuId: menuId,
    name: '',
    description: '',
    price: 0,
    isActive: true
  }
}

const formatCurrency = (amount: number): string => {
  return new Intl.NumberFormat('is-IS', {
    style: 'currency',
    currency: 'ISK'
  }).format(amount)
}

onMounted(() => {
  loadMenu()
})
</script>

