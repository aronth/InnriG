<template>
  <div class="container mx-auto p-6">
    <h1 class="text-2xl font-bold mb-4">Greifinn Orders Test</h1>
    
    <div class="bg-white rounded-lg shadow p-6 mb-6">
      <h2 class="text-xl font-semibold mb-4">Filters</h2>
      <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <label class="block text-sm font-medium mb-1">Phone Number</label>
          <input
            v-model="filters.phoneNumber"
            type="text"
            class="w-full px-3 py-2 border rounded"
            placeholder="8439396"
          />
        </div>
        <div>
          <label class="block text-sm font-medium mb-1">Customer Name</label>
          <input
            v-model="filters.customerName"
            type="text"
            class="w-full px-3 py-2 border rounded"
            placeholder="Ronni"
          />
        </div>
        <div>
          <label class="block text-sm font-medium mb-1">From Date</label>
          <input
            v-model="filters.fromDate"
            type="date"
            class="w-full px-3 py-2 border rounded"
          />
        </div>
        <div>
          <label class="block text-sm font-medium mb-1">To Date</label>
          <input
            v-model="filters.toDate"
            type="date"
            class="w-full px-3 py-2 border rounded"
          />
        </div>
        <div>
          <label class="block text-sm font-medium mb-1">Page</label>
          <input
            v-model.number="filters.page"
            type="number"
            min="1"
            class="w-full px-3 py-2 border rounded"
          />
        </div>
        <div>
          <label class="block text-sm font-medium mb-1">Page Size</label>
          <input
            v-model.number="filters.pageSize"
            type="number"
            min="1"
            max="100"
            class="w-full px-3 py-2 border rounded"
          />
        </div>
      </div>
      <button
        @click="fetchOrders"
        :disabled="loading"
        class="mt-4 px-4 py-2 bg-indigo-600 text-white rounded hover:bg-indigo-700 disabled:opacity-50"
      >
        {{ loading ? 'Loading...' : 'Fetch Orders' }}
      </button>
    </div>

    <div v-if="error" class="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-6">
      <p class="font-bold">Error:</p>
      <p>{{ error }}</p>
    </div>

    <div v-if="result" class="bg-white rounded-lg shadow p-6">
      <h2 class="text-xl font-semibold mb-4">Results</h2>
      <div class="mb-4">
        <p><strong>Total Count:</strong> {{ result.totalCount }}</p>
        <p><strong>Page:</strong> {{ result.page }} / {{ Math.ceil(result.totalCount / result.pageSize) }}</p>
        <p><strong>Page Size:</strong> {{ result.pageSize }}</p>
        <p><strong>Has More Pages:</strong> {{ result.hasMorePages ? 'Yes' : 'No' }}</p>
        <p><strong>Orders Found:</strong> {{ result.orders.length }}</p>
      </div>

      <div class="overflow-x-auto">
        <table class="min-w-full divide-y divide-gray-200">
          <thead class="bg-gray-50">
            <tr>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Order ID</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Phone</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Name</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Address</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Date</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Location</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Delivery</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Payment</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Price</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">External ID</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Status</th>
            </tr>
          </thead>
          <tbody class="bg-white divide-y divide-gray-200">
            <tr v-for="order in result.orders" :key="order.orderId" class="hover:bg-gray-50">
              <td class="px-4 py-3 text-sm">
                <button
                  @click="fetchOrderDetail(order.orderId)"
                  class="text-indigo-600 hover:underline font-medium"
                >
                  {{ order.orderId }}
                </button>
              </td>
              <td class="px-4 py-3 text-sm">{{ order.phoneNumber || '-' }}</td>
              <td class="px-4 py-3 text-sm">{{ order.customerName || '-' }}</td>
              <td class="px-4 py-3 text-sm">{{ order.customerAddress || '-' }}</td>
              <td class="px-4 py-3 text-sm">
                {{ order.addedDate ? new Date(order.addedDate).toLocaleString('is-IS') : '-' }}
              </td>
              <td class="px-4 py-3 text-sm">{{ order.locationName || '-' }}</td>
              <td class="px-4 py-3 text-sm">{{ order.deliveryMethodName || '-' }}</td>
              <td class="px-4 py-3 text-sm">{{ order.paymentMethodName || '-' }}</td>
              <td class="px-4 py-3 text-sm">
                {{ order.totalPrice ? new Intl.NumberFormat('is-IS', { style: 'currency', currency: 'ISK' }).format(order.totalPrice) : '-' }}
              </td>
              <td class="px-4 py-3 text-sm">{{ order.externalId || '-' }}</td>
              <td class="px-4 py-3 text-sm">{{ order.statusName || '-' }}</td>
            </tr>
          </tbody>
        </table>
      </div>

      <div v-if="result.orders.length === 0" class="text-center py-8 text-gray-500">
        No orders found
      </div>
    </div>

    <!-- Order Detail Section -->
    <div v-if="orderDetailLoading" class="bg-white rounded-lg shadow p-6 mt-6">
      <div class="text-center py-8">
        <p class="text-gray-600">Loading order details...</p>
      </div>
    </div>

    <div v-if="orderDetailError" class="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mt-6">
      <p class="font-bold">Error loading order details:</p>
      <p>{{ orderDetailError }}</p>
    </div>

    <div v-if="orderDetail" class="bg-white rounded-lg shadow p-6 mt-6">
      <div class="flex items-center justify-between mb-4">
        <h2 class="text-xl font-semibold">Order Details - {{ orderDetail.orderId }}</h2>
        <button
          @click="orderDetail = null"
          class="px-3 py-1 text-sm bg-gray-200 rounded hover:bg-gray-300"
        >
          Close
        </button>
      </div>

      <!-- Customer Info -->
      <div class="grid grid-cols-1 md:grid-cols-2 gap-4 mb-6 pb-6 border-b">
        <div>
          <p class="text-sm text-gray-600">Phone</p>
          <p class="font-medium">{{ orderDetail.phoneNumber || '-' }}</p>
        </div>
        <div>
          <p class="text-sm text-gray-600">Customer Name</p>
          <p class="font-medium">{{ orderDetail.customerName || '-' }}</p>
        </div>
        <div>
          <p class="text-sm text-gray-600">Address</p>
          <p class="font-medium">{{ orderDetail.customerAddress || '-' }}</p>
        </div>
        <div>
          <p class="text-sm text-gray-600">Delivery Method</p>
          <p class="font-medium">{{ orderDetail.deliveryMethod || '-' }}</p>
        </div>
        <div>
          <p class="text-sm text-gray-600">Payment Method</p>
          <p class="font-medium">{{ orderDetail.paymentMethod || '-' }}</p>
        </div>
        <div>
          <p class="text-sm text-gray-600">Total Price</p>
          <p class="font-medium text-lg">
            {{ new Intl.NumberFormat('is-IS', { style: 'currency', currency: 'ISK' }).format(orderDetail.totalPrice) }}
          </p>
        </div>
        <div v-if="orderDetail.addedTime">
          <p class="text-sm text-gray-600">Order Time</p>
          <p class="font-medium">{{ new Date(orderDetail.addedTime).toLocaleString('is-IS') }}</p>
        </div>
        <div v-if="orderDetail.readyTime">
          <p class="text-sm text-gray-600">Ready Time</p>
          <p class="font-medium">{{ new Date(orderDetail.readyTime).toLocaleString('is-IS') }}</p>
        </div>
      </div>

      <!-- Order Items -->
      <div>
        <h3 class="text-lg font-semibold mb-4">Items</h3>
        <div class="space-y-6">
          <div
            v-for="(item, index) in orderDetail.items"
            :key="index"
            class="border rounded-lg p-4"
          >
            <div class="flex items-start justify-between mb-2">
              <div>
                <p class="font-semibold text-lg">{{ item.name }}</p>
                <p v-if="item.itemId" class="text-xs text-gray-500">Item ID: {{ item.itemId }}</p>
              </div>
              <div class="text-right">
                <p class="text-sm text-gray-600">Quantity: {{ item.quantity }}</p>
                <p class="text-sm text-gray-600">
                  Unit Price: {{ new Intl.NumberFormat('is-IS', { style: 'currency', currency: 'ISK' }).format(item.unitPrice) }}
                </p>
                <p class="font-semibold text-lg">
                  {{ new Intl.NumberFormat('is-IS', { style: 'currency', currency: 'ISK' }).format(item.totalPrice) }}
                </p>
              </div>
            </div>

            <!-- Options -->
            <div v-if="item.options && item.options.length > 0" class="mt-3 pl-4 border-l-2 border-gray-200">
              <p class="text-sm font-medium text-gray-700 mb-2">Options:</p>
              <div class="space-y-1">
                <div
                  v-for="(option, optIndex) in item.options"
                  :key="optIndex"
                  class="flex items-center justify-between text-sm"
                >
                  <span class="text-gray-700">{{ option.name }}</span>
                  <span class="text-gray-600">
                    {{ new Intl.NumberFormat('is-IS', { style: 'currency', currency: 'ISK' }).format(option.price) }}
                  </span>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div v-if="orderDetail.items.length === 0" class="text-center py-8 text-gray-500">
          No items found
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
const config = useRuntimeConfig()
const loading = ref(false)
const error = ref<string | null>(null)
const result = ref<any>(null)
const orderDetail = ref<any>(null)
const orderDetailLoading = ref(false)
const orderDetailError = ref<string | null>(null)

const filters = ref({
  phoneNumber: '8439396',
  customerName: '',
  customerAddress: '',
  fromDate: '',
  toDate: '',
  locationId: null as number | null,
  deliveryMethodId: null as number | null,
  paymentMethodId: null as number | null,
  totalPrice: null as number | null,
  externalId: '',
  statusId: null as number | null,
  page: 1,
  pageSize: 50
})

const fetchOrders = async () => {
  loading.value = true
  error.value = null
  result.value = null

  try {
    const queryParams = new URLSearchParams()
    
    if (filters.value.phoneNumber) queryParams.append('phoneNumber', filters.value.phoneNumber)
    if (filters.value.customerName) queryParams.append('customerName', filters.value.customerName)
    if (filters.value.customerAddress) queryParams.append('customerAddress', filters.value.customerAddress)
    if (filters.value.fromDate) queryParams.append('fromDate', filters.value.fromDate)
    if (filters.value.toDate) queryParams.append('toDate', filters.value.toDate)
    if (filters.value.locationId) queryParams.append('locationId', filters.value.locationId.toString())
    if (filters.value.deliveryMethodId) queryParams.append('deliveryMethodId', filters.value.deliveryMethodId.toString())
    if (filters.value.paymentMethodId) queryParams.append('paymentMethodId', filters.value.paymentMethodId.toString())
    if (filters.value.totalPrice) queryParams.append('totalPrice', filters.value.totalPrice.toString())
    if (filters.value.externalId) queryParams.append('externalId', filters.value.externalId)
    if (filters.value.statusId) queryParams.append('statusId', filters.value.statusId.toString())
    queryParams.append('page', filters.value.page.toString())
    queryParams.append('pageSize', filters.value.pageSize.toString())

    const url = `${config.public.apiBase}/api/orders/greifinn?${queryParams.toString()}`
    
    const response = await $fetch(url, {
      method: 'GET',
      credentials: 'include'
    })

    result.value = response
  } catch (err: any) {
    error.value = err.message || 'Failed to fetch orders'
    console.error('Error fetching orders:', err)
  } finally {
    loading.value = false
  }
}

const fetchOrderDetail = async (orderId: string) => {
  orderDetailLoading.value = true
  orderDetailError.value = null
  orderDetail.value = null

  try {
    const url = `${config.public.apiBase}/api/orders/greifinn/${orderId}`
    
    const response = await $fetch(url, {
      method: 'GET',
      credentials: 'include'
    })

    orderDetail.value = response
  } catch (err: any) {
    orderDetailError.value = err.message || 'Failed to fetch order details'
    console.error('Error fetching order details:', err)
  } finally {
    orderDetailLoading.value = false
  }
}
</script>

<style scoped>
/* Additional styles if needed */
</style>

