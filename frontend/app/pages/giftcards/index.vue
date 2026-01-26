<template>
    <div class="min-h-screen bg-gray-50 py-8">
        <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
            <!-- Sub Navigation -->
            <GiftCardSubNav />
            
            <!-- Header -->
            <div class="mb-8">
                <div class="flex items-center justify-between">
                    <div>
                        <h1 class="text-3xl font-bold text-gray-900">Gjafakort</h1>
                        <p class="mt-2 text-sm text-gray-600">Stjórna og fylgjast með gjafakortum</p>
                    </div>
                    <NuxtLink
                        to="/giftcards/create"
                        class="inline-flex items-center px-4 py-2 border border-transparent rounded-lg shadow-sm text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500"
                    >
                        <svg class="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
                        </svg>
                        Búa til gjafakort
                    </NuxtLink>
                </div>
            </div>

            <!-- Filters -->
            <div class="bg-white rounded-lg shadow p-6 mb-6">
                <div class="grid grid-cols-1 md:grid-cols-4 gap-4">
                    <div>
                        <label for="statusFilter" class="block text-sm font-medium text-gray-700 mb-2">
                            Staða
                        </label>
                        <select
                            id="statusFilter"
                            v-model="filters.status"
                            @change="loadGiftCards"
                            class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
                        >
                            <option :value="undefined">Allar stöður</option>
                            <option value="Created">Búið til</option>
                            <option value="Sold">Selt</option>
                            <option value="Redeemed">Notað</option>
                            <option value="Expired">Útrunnið</option>
                        </select>
                    </div>
                    <div>
                        <label for="templateFilter" class="block text-sm font-medium text-gray-700 mb-2">
                            Tegund
                        </label>
                        <select
                            id="templateFilter"
                            v-model="filters.templateId"
                            @change="loadGiftCards"
                            class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
                        >
                            <option :value="undefined">Allar tegundir</option>
                            <option
                                v-for="template in templates"
                                :key="template.id"
                                :value="template.id"
                            >
                                {{ template.name }}
                            </option>
                        </select>
                    </div>
                    <div>
                        <label for="fromDate" class="block text-sm font-medium text-gray-700 mb-2">
                            Frá
                        </label>
                        <input
                            id="fromDate"
                            v-model="filters.fromDate"
                            type="date"
                            @change="loadGiftCards"
                            class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
                        />
                    </div>
                    <div>
                        <label for="toDate" class="block text-sm font-medium text-gray-700 mb-2">
                            Til
                        </label>
                        <input
                            id="toDate"
                            v-model="filters.toDate"
                            type="date"
                            @change="loadGiftCards"
                            class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
                        />
                    </div>
                </div>
            </div>

            <!-- Loading State -->
            <div v-if="isLoading" class="text-center py-12">
                <div class="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600"></div>
                <p class="mt-4 text-sm text-gray-600">Hleður gjafakortum...</p>
            </div>

            <!-- Error State -->
            <div v-else-if="error" class="bg-red-50 border border-red-200 rounded-lg p-4 mb-6">
                <p class="text-sm text-red-800">{{ error }}</p>
            </div>

            <!-- Gift Cards Table -->
            <div v-else class="bg-white rounded-lg shadow overflow-hidden">
                <div class="overflow-x-auto">
                    <table class="min-w-full divide-y divide-gray-200">
                        <thead class="bg-gray-50">
                            <tr>
                                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                                    Númer
                                </th>
                                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                                    Upphæð
                                </th>
                                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                                    Tegund
                                </th>
                                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                                    Staða
                                </th>
                                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                                    Gildir til
                                </th>
                                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                                    Búið til
                                </th>
                                <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                                    Aðgerðir
                                </th>
                            </tr>
                        </thead>
                        <tbody class="bg-white divide-y divide-gray-200">
                            <tr v-if="giftCards.length === 0">
                                <td colspan="7" class="px-6 py-8 text-center text-sm text-gray-500">
                                    Engin gjafakort fundust
                                </td>
                            </tr>
                            <tr
                                v-for="giftCard in giftCards"
                                :key="giftCard.id"
                                class="hover:bg-gray-50"
                            >
                                <td class="px-6 py-4 whitespace-nowrap">
                                    <div class="text-sm font-medium text-gray-900">{{ giftCard.number }}</div>
                                </td>
                                <td class="px-6 py-4 whitespace-nowrap">
                                    <div class="text-sm text-gray-900">
                                        {{ formatCurrency(giftCard.amount) }}
                                    </div>
                                </td>
                                <td class="px-6 py-4 whitespace-nowrap">
                                    <div class="text-sm text-gray-500">
                                        {{ giftCard.templateName || 'Engin tegund' }}
                                    </div>
                                </td>
                                <td class="px-6 py-4 whitespace-nowrap">
                                    <GiftCardStatusBadge :status="giftCard.status" />
                                </td>
                                <td class="px-6 py-4 whitespace-nowrap">
                                    <div class="text-sm text-gray-500">
                                        {{ giftCard.expirationDate ? formatDate(giftCard.expirationDate) : '-' }}
                                    </div>
                                </td>
                                <td class="px-6 py-4 whitespace-nowrap">
                                    <div class="text-sm text-gray-500">
                                        {{ formatDate(giftCard.createdAt) }}
                                    </div>
                                </td>
                                <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                                    <div class="flex items-center justify-end gap-2">
                                        <NuxtLink
                                            :to="`/giftcards/${giftCard.id}`"
                                            class="text-indigo-600 hover:text-indigo-900"
                                        >
                                            Skoða
                                        </NuxtLink>
                                        <button
                                            @click="downloadPdf(giftCard.id, giftCard.printWithBackground)"
                                            class="text-gray-600 hover:text-gray-900"
                                            title="Sækja PDF"
                                        >
                                            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                                            </svg>
                                        </button>
                                    </div>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import type { GiftCard, GiftCardTemplate, GiftCardStatus } from '~/types/giftCard'

const { getGiftCards, downloadPdf } = useGiftCards()
const { getTemplates } = useGiftCardTemplates()

const giftCards = ref<GiftCard[]>([])
const templates = ref<GiftCardTemplate[]>([])
const isLoading = ref(false)
const error = ref('')

const filters = reactive<{
    status?: GiftCardStatus
    templateId?: string
    fromDate?: string
    toDate?: string
}>({})

const loadGiftCards = async () => {
    isLoading.value = true
    error.value = ''
    try {
        giftCards.value = await getGiftCards(filters)
    } catch (err: any) {
        error.value = 'Mistókst að sækja gjafakort'
        console.error(err)
    } finally {
        isLoading.value = false
    }
}

const loadTemplates = async () => {
    try {
        templates.value = await getTemplates(true)
    } catch (err: any) {
        console.error('Failed to load templates', err)
    }
}

const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('is-IS', {
        style: 'currency',
        currency: 'ISK',
        minimumFractionDigits: 0
    }).format(amount)
}

const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('is-IS')
}

onMounted(() => {
    loadTemplates()
    loadGiftCards()
})
</script>

