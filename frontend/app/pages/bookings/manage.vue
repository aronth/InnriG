<template>
  <div class="w-full px-4 sm:px-6 lg:px-8 py-8">
    <div class="mb-8">
      <h1 class="text-3xl font-bold text-gray-900">Stjórna Bókunum</h1>
      <p class="mt-2 text-sm text-gray-700">
        Búðu til og breyttu bókunum í nýja bókunarkerfinu
      </p>
    </div>

    <!-- Filters -->
    <div class="mb-6 bg-white rounded-lg shadow px-6 py-4">
      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">Frá dagsetningu</label>
          <input
            v-model="filters.fromDate"
            type="date"
            class="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            @change="loadBookings"
          />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">Til dagsetningar</label>
          <input
            v-model="filters.toDate"
            type="date"
            class="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            @change="loadBookings"
          />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">Staða</label>
          <select
            v-model="filters.status"
            class="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            @change="loadBookings"
          >
            <option value="">Allar</option>
            <option value="Ný">Ný</option>
            <option value="Staðfest">Staðfest</option>
            <option value="Afbókað">Afbókað</option>
            <option value="Situr">Situr</option>
            <option value="Farinn">Farinn</option>
          </select>
        </div>
        <div class="flex items-end">
          <button
            @click="showCreateModal = true"
            class="w-full px-4 py-2 bg-indigo-600 text-white rounded-md hover:bg-indigo-700 transition-colors font-medium"
          >
            + Ný bókun
          </button>
        </div>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="text-center py-12">
      <div class="inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600"></div>
      <p class="mt-4 text-gray-600">Sæki bókanir...</p>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="bg-red-50 border border-red-200 rounded-lg p-6 text-center">
      <p class="text-red-800 font-semibold">Villa við að sækja bókanir</p>
      <p class="text-red-600 mt-2">{{ error }}</p>
      <button
        @click="loadBookings"
        class="mt-4 px-4 py-2 bg-red-600 text-white rounded-md hover:bg-red-700 transition-colors"
      >
        Reyna aftur
      </button>
    </div>

    <!-- Bookings List -->
    <div v-else-if="bookings.length > 0" class="bg-white rounded-lg shadow overflow-hidden">
      <div class="overflow-x-auto">
        <table class="min-w-full divide-y divide-gray-200">
          <thead class="bg-gray-50">
            <tr>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Dagsetning & Tími
              </th>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Viðskiptavinur
              </th>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Staðsetning
              </th>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Gestir
              </th>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Staða
              </th>
              <th class="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">
                Aðgerðir
              </th>
            </tr>
          </thead>
          <tbody class="bg-white divide-y divide-gray-200">
            <tr v-for="booking in bookings" :key="booking.id" class="hover:bg-gray-50">
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                {{ formatDateTime(booking.bookingDate, booking.startTime) }}
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                {{ booking.customer?.name || '-' }}
                <div v-if="booking.customer?.phone" class="text-xs text-gray-500">
                  {{ booking.customer.phone }}
                </div>
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                {{ booking.locationName || '-' }}
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                {{ booking.adultCount }} fullorðnir
                <span v-if="booking.childCount > 0">, {{ booking.childCount }} börn</span>
              </td>
              <td class="px-6 py-4 whitespace-nowrap">
                <span
                  class="px-2 py-1 text-xs font-medium rounded-full"
                  :class="getStatusBadgeClass(booking.status)"
                >
                  {{ booking.status }}
                </span>
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-center text-sm">
                <button
                  @click="editBooking(booking)"
                  class="text-indigo-600 hover:text-indigo-900 mr-3"
                >
                  Breyta
                </button>
                <button
                  @click="deleteBooking(booking.id)"
                  class="text-red-600 hover:text-red-900"
                >
                  Eyða
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Empty State -->
    <div v-else class="bg-white rounded-lg shadow p-12 text-center">
      <p class="text-gray-500">Engar bókanir fundust</p>
      <button
        @click="showCreateModal = true"
        class="mt-4 px-4 py-2 bg-indigo-600 text-white rounded-md hover:bg-indigo-700 transition-colors"
      >
        Búa til nýja bókun
      </button>
    </div>

    <!-- Create/Edit Modal -->
    <div
      v-if="showCreateModal || editingBooking"
      class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50"
      @click.self="closeModal"
    >
      <div class="bg-white rounded-lg shadow-xl max-w-2xl w-full mx-4 max-h-[90vh] overflow-y-auto">
        <div class="px-6 py-4 border-b border-gray-200">
          <h2 class="text-xl font-semibold text-gray-900">
            {{ editingBooking ? 'Breyta bókun' : 'Ný bókun' }}
          </h2>
        </div>

        <form @submit.prevent="saveBooking" class="px-6 py-4">
          <!-- Customer Selection -->
          <div class="mb-4">
            <label class="block text-sm font-medium text-gray-700 mb-1">
              Viðskiptavinur <span class="text-red-500">*</span>
            </label>
            <div class="flex gap-2">
              <select
                v-model="form.customerId"
                required
                class="flex-1 px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
              >
                <option value="">Veldu viðskiptavin...</option>
                <option v-for="customer in customers" :key="customer.id" :value="customer.id">
                  {{ customer.name }} {{ customer.phone ? `(${customer.phone})` : '' }}
                </option>
              </select>
              <button
                type="button"
                @click="showCustomerModal = true"
                class="px-4 py-2 bg-gray-100 text-gray-700 rounded-md hover:bg-gray-200 transition-colors"
              >
                + Nýr
              </button>
            </div>
          </div>

          <!-- Location -->
          <div class="mb-4">
            <label class="block text-sm font-medium text-gray-700 mb-1">Staðsetning</label>
            <select
              v-model="form.locationId"
              class="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            >
              <option :value="undefined">Engin</option>
              <option v-for="location in locations" :key="location.id" :value="location.id">
                {{ location.name }}
              </option>
            </select>
          </div>

          <!-- Date and Time -->
          <div class="grid grid-cols-2 gap-4 mb-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">
                Dagsetning <span class="text-red-500">*</span>
              </label>
              <input
                v-model="form.bookingDate"
                type="date"
                required
                class="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
              />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">
                Tími <span class="text-red-500">*</span>
              </label>
              <input
                v-model="form.startTime"
                type="time"
                required
                class="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
              />
            </div>
          </div>

          <!-- Guest Count -->
          <div class="grid grid-cols-2 gap-4 mb-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">
                Fullorðnir <span class="text-red-500">*</span>
              </label>
              <input
                v-model.number="form.adultCount"
                type="number"
                min="0"
                required
                class="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
              />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Börn</label>
              <input
                v-model.number="form.childCount"
                type="number"
                min="0"
                class="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
              />
            </div>
          </div>

          <!-- Status -->
          <div class="mb-4">
            <label class="block text-sm font-medium text-gray-700 mb-1">
              Staða <span class="text-red-500">*</span>
            </label>
            <select
              v-model="form.status"
              required
              class="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            >
              <option value="Ný">Ný</option>
              <option value="Staðfest">Staðfest</option>
              <option value="Afbókað">Afbókað</option>
              <option value="Situr">Situr</option>
              <option value="Farinn">Farinn</option>
            </select>
          </div>

          <!-- Notes -->
          <div class="mb-4">
            <label class="block text-sm font-medium text-gray-700 mb-1">Athugasemdir</label>
            <textarea
              v-model="form.notes"
              rows="3"
              class="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            />
          </div>

          <!-- Special Requests -->
          <div class="mb-4">
            <label class="block text-sm font-medium text-gray-700 mb-1">Sérstakar beiðnir</label>
            <textarea
              v-model="form.specialRequests"
              rows="3"
              class="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            />
          </div>

          <!-- Needs Print -->
          <div class="mb-4">
            <label class="flex items-center">
              <input
                v-model="form.needsPrint"
                type="checkbox"
                class="rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
              />
              <span class="ml-2 text-sm text-gray-700">Þarf að prenta</span>
            </label>
          </div>

          <!-- Menu Items -->
          <div class="mb-4">
            <label class="block text-sm font-medium text-gray-700 mb-2">Matseðilsatriði</label>
            
            <!-- Add Menu Item -->
            <div class="mb-3 flex gap-2">
              <select
                v-model="selectedMenuId"
                @change="loadMenuItems"
                class="flex-1 px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
              >
                <option value="">Veldu matseðil...</option>
                <option v-for="m in menus" :key="m.id" :value="m.id">
                  {{ m.name }} ({{ m.forWho }})
                </option>
              </select>
              <select
                v-if="availableMenuItems.length > 0"
                v-model="selectedMenuItemId"
                class="flex-1 px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
              >
                <option value="">Veldu atriði...</option>
                <option v-for="item in availableMenuItems" :key="item.id" :value="item.id">
                  {{ item.name }} - {{ formatCurrency(item.price) }}
                </option>
              </select>
              <button
                type="button"
                @click="addMenuItem"
                :disabled="!selectedMenuItemId"
                class="px-4 py-2 bg-indigo-600 text-white rounded-md hover:bg-indigo-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              >
                Bæta við
              </button>
            </div>

            <!-- Selected Menu Items -->
            <div v-if="form.menuItems.length > 0" class="space-y-2">
              <div
                v-for="(item, index) in form.menuItems"
                :key="index"
                class="flex items-center justify-between p-3 bg-gray-50 rounded-md"
              >
                <div class="flex-1">
                  <div class="font-medium text-gray-900">{{ getMenuItemName(item.menuItemId) }}</div>
                  <div class="text-sm text-gray-600">
                    {{ formatCurrency(item.unitPrice || 0) }} × {{ item.quantity }}
                  </div>
                </div>
                <div class="flex items-center gap-2">
                  <input
                    v-model.number="item.quantity"
                    type="number"
                    min="1"
                    class="w-20 px-2 py-1 text-sm border border-gray-300 rounded-md focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
                  />
                  <button
                    type="button"
                    @click="removeMenuItem(index)"
                    class="px-3 py-1 text-red-600 hover:text-red-800 text-sm font-medium"
                  >
                    Eyða
                  </button>
                </div>
              </div>
            </div>
          </div>

          <!-- Actions -->
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

    <!-- Customer Modal -->
    <div
      v-if="showCustomerModal"
      class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50"
      @click.self="showCustomerModal = false"
    >
      <div class="bg-white rounded-lg shadow-xl max-w-md w-full mx-4">
        <div class="px-6 py-4 border-b border-gray-200">
          <h2 class="text-xl font-semibold text-gray-900">Nýr viðskiptavinur</h2>
        </div>

        <form @submit.prevent="createCustomer" class="px-6 py-4">
          <div class="mb-4">
            <label class="block text-sm font-medium text-gray-700 mb-1">
              Nafn <span class="text-red-500">*</span>
            </label>
            <input
              v-model="customerForm.name"
              type="text"
              required
              class="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            />
          </div>

          <div class="mb-4">
            <label class="block text-sm font-medium text-gray-700 mb-1">Sími</label>
            <input
              v-model="customerForm.phone"
              type="text"
              class="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            />
          </div>

          <div class="mb-4">
            <label class="block text-sm font-medium text-gray-700 mb-1">Netfang</label>
            <input
              v-model="customerForm.email"
              type="email"
              class="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            />
          </div>

          <div class="mb-4">
            <label class="block text-sm font-medium text-gray-700 mb-1">Athugasemdir</label>
            <textarea
              v-model="customerForm.notes"
              rows="3"
              class="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            />
          </div>

          <div class="flex justify-end gap-3 pt-4 border-t border-gray-200">
            <button
              type="button"
              @click="showCustomerModal = false"
              class="px-4 py-2 border border-gray-300 rounded-md text-gray-700 hover:bg-gray-50 transition-colors"
            >
              Hætta við
            </button>
            <button
              type="submit"
              :disabled="savingCustomer"
              class="px-4 py-2 bg-indigo-600 text-white rounded-md hover:bg-indigo-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {{ savingCustomer ? 'Vista...' : 'Vista' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useBookingManagement } from '~/composables/useBookingManagement'
import { useCustomers } from '~/composables/useCustomers'
import { useRestaurants } from '~/composables/useRestaurants'
import type { BookingManagementDto, CreateBookingDto, UpdateBookingDto } from '~/types/booking-management'
import type { CreateCustomerDto } from '~/types/customer'

const { getBookings, createBooking, updateBooking, deleteBooking: deleteBookingApi } = useBookingManagement()
const { getAllCustomers, createCustomer: createCustomerApi } = useCustomers()
const { getRestaurants } = useRestaurants()
const { getAllMenus } = useMenus()

const loading = ref(false)
const error = ref<string | null>(null)
const bookings = ref<BookingManagementDto[]>([])
const customers = ref<any[]>([])
const locations = ref<any[]>([]) // Will be populated from restaurants API
const menus = ref<MenuDto[]>([])
const availableMenuItems = ref<MenuItemDto[]>([])
const selectedMenuId = ref('')
const selectedMenuItemId = ref('')

const showCreateModal = ref(false)
const editingBooking = ref<BookingManagementDto | null>(null)
const showCustomerModal = ref(false)
const saving = ref(false)
const savingCustomer = ref(false)

const filters = ref({
  fromDate: '',
  toDate: '',
  status: ''
})

const form = ref<CreateBookingDto>({
  customerId: '',
  locationId: undefined,
  bookingDate: new Date().toISOString().split('T')[0],
  startTime: '18:00',
  endTime: undefined,
  adultCount: 2,
  childCount: 0,
  status: 'Ný',
  specialRequests: '',
  notes: '',
  needsPrint: false,
  menuItems: []
})

const customerForm = ref<CreateCustomerDto>({
  name: '',
  phone: '',
  email: '',
  notes: ''
})

const loadBookings = async () => {
  loading.value = true
  error.value = null

  try {
    const fromDate = filters.value.fromDate ? new Date(filters.value.fromDate) : undefined
    const toDate = filters.value.toDate ? new Date(filters.value.toDate) : undefined

    bookings.value = await getBookings(
      fromDate,
      toDate,
      undefined,
      undefined,
      filters.value.status || undefined
    )
  } catch (err: any) {
    error.value = err.message || 'Óþekkt villa'
    console.error('Error loading bookings:', err)
  } finally {
    loading.value = false
  }
}

const loadCustomers = async () => {
  try {
    customers.value = await getAllCustomers()
  } catch (err: any) {
    console.error('Error loading customers:', err)
  }
}

const loadLocations = async () => {
  try {
    const restaurants = await getRestaurants()
    locations.value = restaurants.map(r => ({ id: r.id, name: r.name }))
  } catch (err: any) {
    console.error('Error loading locations:', err)
  }
}

const loadMenus = async () => {
  try {
    menus.value = await getAllMenus()
  } catch (err: any) {
    console.error('Error loading menus:', err)
  }
}

const loadMenuItems = async () => {
  if (!selectedMenuId.value) {
    availableMenuItems.value = []
    selectedMenuItemId.value = ''
    return
  }

  try {
    const menu = menus.value.find(m => m.id === selectedMenuId.value)
    if (menu) {
      availableMenuItems.value = menu.menuItems.filter(item => item.isActive)
    }
  } catch (err: any) {
    console.error('Error loading menu items:', err)
  }
}

const addMenuItem = () => {
  if (!selectedMenuItemId.value) return

  const menuItem = availableMenuItems.value.find(item => item.id === selectedMenuItemId.value)
  if (!menuItem) return

  // Check if already added
  const existingIndex = form.value.menuItems.findIndex(item => item.menuItemId === selectedMenuItemId.value)
  if (existingIndex >= 0) {
    // Increase quantity
    form.value.menuItems[existingIndex].quantity++
  } else {
    // Add new item
    form.value.menuItems.push({
      menuItemId: selectedMenuItemId.value,
      quantity: 1,
      unitPrice: menuItem.price,
      notes: undefined
    })
  }

  // Reset selection
  selectedMenuItemId.value = ''
}

const removeMenuItem = (index: number) => {
  form.value.menuItems.splice(index, 1)
}

const getMenuItemName = (menuItemId: string): string => {
  for (const menu of menus.value) {
    const item = menu.menuItems.find(i => i.id === menuItemId)
    if (item) return item.name
  }
  return 'Óþekkt atriði'
}

const formatCurrency = (amount: number): string => {
  return new Intl.NumberFormat('is-IS', {
    style: 'currency',
    currency: 'ISK'
  }).format(amount)
}

const saveBooking = async () => {
  saving.value = true

  try {
    if (editingBooking.value) {
      const updateDto: UpdateBookingDto = {
        ...form.value,
        locationId: form.value.locationId || undefined
      }
      await updateBooking(editingBooking.value.id, updateDto)
    } else {
      await createBooking(form.value)
    }

    closeModal()
    await loadBookings()
  } catch (err: any) {
    error.value = err.message || 'Villa við að vista bókun'
    console.error('Error saving booking:', err)
  } finally {
    saving.value = false
  }
}

const editBooking = (booking: BookingManagementDto) => {
  editingBooking.value = booking
  form.value = {
    customerId: booking.customerId,
    locationId: booking.locationId || undefined,
    bookingDate: booking.bookingDate.split('T')[0],
    startTime: booking.startTime.substring(0, 5), // HH:mm format
    endTime: booking.endTime ? booking.endTime.substring(0, 5) : undefined,
    adultCount: booking.adultCount,
    childCount: booking.childCount,
    status: booking.status,
    specialRequests: booking.specialRequests || '',
    notes: booking.notes || '',
    needsPrint: booking.needsPrint,
    menuItems: booking.menuItems.map(item => ({
      menuItemId: item.menuItemId,
      quantity: item.quantity,
      unitPrice: item.unitPrice,
      notes: item.notes
    }))
  }
  showCreateModal.value = true
}

const deleteBooking = async (id: string) => {
  if (!confirm('Ertu viss um að þú viljir eyða þessari bókun?')) {
    return
  }

  try {
    await deleteBookingApi(id)
    await loadBookings()
  } catch (err: any) {
    error.value = err.message || 'Villa við að eyða bókun'
    console.error('Error deleting booking:', err)
  }
}

const createCustomer = async () => {
  savingCustomer.value = true

  try {
    const customer = await createCustomerApi(customerForm.value)
    customers.value.push(customer)
    form.value.customerId = customer.id
    showCustomerModal.value = false
    customerForm.value = { name: '', phone: '', email: '', notes: '' }
  } catch (err: any) {
    error.value = err.message || 'Villa við að vista viðskiptavin'
    console.error('Error creating customer:', err)
  } finally {
    savingCustomer.value = false
  }
}

const closeModal = () => {
  showCreateModal.value = false
  editingBooking.value = null
  selectedMenuId.value = ''
  selectedMenuItemId.value = ''
  availableMenuItems.value = []
  form.value = {
    customerId: '',
    locationId: undefined,
    bookingDate: new Date().toISOString().split('T')[0],
    startTime: '18:00',
    endTime: undefined,
    adultCount: 2,
    childCount: 0,
    status: 'Ný',
    specialRequests: '',
    notes: '',
    needsPrint: false,
    menuItems: []
  }
}

const formatDateTime = (dateStr: string, timeStr: string): string => {
  try {
    const date = new Date(dateStr)
    const day = date.getDate().toString().padStart(2, '0')
    const month = (date.getMonth() + 1).toString().padStart(2, '0')
    const year = date.getFullYear()
    return `${day}.${month}.${year} ${timeStr}`
  } catch {
    return `${dateStr} ${timeStr}`
  }
}

const getStatusBadgeClass = (status: string): string => {
  switch (status) {
    case 'Ný':
      return 'bg-blue-100 text-blue-800'
    case 'Staðfest':
      return 'bg-green-100 text-green-800'
    case 'Afbókað':
      return 'bg-red-100 text-red-800'
    case 'Situr':
      return 'bg-purple-100 text-purple-800'
    case 'Farinn':
      return 'bg-gray-100 text-gray-800'
    default:
      return 'bg-gray-100 text-gray-800'
  }
}

onMounted(async () => {
  await Promise.all([loadBookings(), loadCustomers(), loadLocations(), loadMenus()])
})
</script>

