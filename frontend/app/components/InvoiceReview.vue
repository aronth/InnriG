<template>
  <div class="bg-white shadow rounded-lg p-6">
    <div class="flex justify-between items-center mb-6">
      <h2 class="text-xl font-bold text-gray-800">Yfirferð Reiknings</h2>
      <div v-if="localInvoice">
        <span class="text-gray-500 text-sm">Birgir: </span>
        <span class="font-semibold">{{ localInvoice.supplierName }}</span>
        <span class="mx-2">|</span>
        <span class="text-gray-500 text-sm">Dagsetning: </span>
        <span class="font-semibold">{{ formatDate(localInvoice.invoiceDate) }}</span>
      </div>
    </div>

    <div v-if="localInvoice && localInvoice.items" class="overflow-x-auto">
      <table class="min-w-full divide-y divide-gray-200">
        <thead class="bg-gray-50">
          <tr>
            <th class="px-3 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider w-64">Vara</th>
            <th class="px-3 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider w-24">Magn</th>
            <th class="px-3 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider w-32">Listaverð</th>
            <th class="px-3 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider w-24">Afsl. %</th>
            <th class="px-3 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider w-32">Nettó Verð</th>
            <th class="px-3 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider w-24">VSK</th>
            <th class="px-3 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider w-32">Samtals (Nettó)</th>
            <th class="px-3 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider w-32">Samtals (Brúttó)</th>
          </tr>
        </thead>
        <tbody class="bg-white divide-y divide-gray-200">
          <tr v-for="(item, index) in localInvoice.items" :key="item.id || index">
            <td class="px-3 py-2">
                <input v-model="item.itemName" class="w-full text-sm border-gray-300 rounded-md focus:ring-blue-500 focus:border-blue-500 px-2 py-1 border" />
            </td>
            <td class="px-3 py-2">
                <div class="flex items-center justify-end gap-1">
                    <input type="number" step="0.01" v-model.number="item.quantity" class="w-16 text-sm text-right border-gray-300 rounded-md focus:ring-blue-500 focus:border-blue-500 px-2 py-1 border" />
                    <span class="text-xs text-gray-500">{{ item.unit }}</span>
                </div>
            </td>
            <td class="px-3 py-2">
                <input type="number" step="1" v-model.number="item.listPrice" class="w-full text-sm text-right border-gray-300 rounded-md focus:ring-blue-500 focus:border-blue-500 px-2 py-1 border" />
            </td>
            <td class="px-3 py-2">
                <input type="number" step="0.1" v-model.number="item.discount" class="w-full text-sm text-right border-gray-300 rounded-md focus:ring-blue-500 focus:border-blue-500 px-2 py-1 border" />
            </td>
            <td class="px-3 py-2">
                <input type="number" step="1" v-model.number="item.unitPrice" class="w-full text-sm text-right border-gray-300 rounded-md focus:ring-blue-500 focus:border-blue-500 px-2 py-1 border" />
            </td>
            <td class="px-3 py-2">
                <input v-model="item.vatCode" class="w-full text-sm text-right border-gray-300 rounded-md focus:ring-blue-500 focus:border-blue-500 px-2 py-1 border" />
            </td>
            <td class="px-3 py-4 text-sm text-right text-gray-900">
                {{ formatCurrency(item.quantity * item.unitPrice) }}
            </td>
             <td class="px-3 py-4 text-sm text-right font-bold text-gray-900">
                {{ formatCurrency(item.totalPriceWithVat) }} <!-- Note: This won't auto-calc unless we add logic, but user just wanted editable fields -->
            </td>
          </tr>
        </tbody>
        <tfoot class="bg-gray-50">
           <tr>
             <td colspan="7" class="px-6 py-4 text-right font-bold text-gray-900">Samtals</td>
             <td class="px-3 py-4 text-right font-bold text-gray-900">{{ formatCurrency(localInvoice.totalAmount) }}</td>
           </tr>
        </tfoot>
      </table>
    </div>

    <div class="mt-6 flex justify-end gap-3">
       <button @click="$emit('cancel')" class="px-4 py-2 border border-gray-300 rounded-md text-sm font-medium text-gray-700 hover:bg-gray-50">Hætta við</button>
       <button @click="save" class="px-4 py-2 bg-blue-600 border border-transparent rounded-md text-sm font-medium text-white hover:bg-blue-700">Staðfesta & Vista</button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import type { Invoice } from '~/types/invoice';

const props = defineProps<{
  invoice: Invoice
}>();

const emit = defineEmits<{
  (e: 'confirm', invoice: Invoice): void;
  (e: 'cancel'): void;
}>();

// Create a local reactive copy to edit
const localInvoice = ref<Invoice | null>(null);

// Watch for prop changes to update local state logic
watch(() => props.invoice, (newVal) => {
    if (newVal) {
        // Deep clone to avoid mutating prop directly
        localInvoice.value = JSON.parse(JSON.stringify(newVal));
    }
}, { immediate: true });

// Auto-calculate totals when items change
watch(() => localInvoice.value?.items, (items) => {
    if (!items) return;
    items.forEach(item => {
        // Update line total (Net)
        item.totalPrice = (item.quantity || 0) * (item.unitPrice || 0);
    });
    // Note: We are not recalculating invoice.totalAmount (Gross) because we don't have VatRates on the frontend.
    // The backend should probably re-calculate everything upon receipt.
}, { deep: true });



const save = () => {
    if (localInvoice.value) {
        emit('confirm', localInvoice.value);
    }
};

const formatDate = (dateString: string) => {
  if (!dateString) return '';
  return new Date(dateString).toLocaleDateString('is-IS');
};

const formatCurrency = (val: number) => {
  return new Intl.NumberFormat('is-IS', { style: 'currency', currency: 'ISK' }).format(val);
};
</script>
