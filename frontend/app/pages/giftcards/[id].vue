<template>
    <div class="min-h-screen bg-gray-50 py-8">
        <div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">
            <!-- Sub Navigation -->
            <GiftCardSubNav />
            
            <!-- Loading State -->
            <div v-if="isLoading" class="text-center py-12">
                <div class="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600"></div>
                <p class="mt-4 text-sm text-gray-600">Hleður gjafakorti...</p>
            </div>

            <!-- Error State -->
            <div v-else-if="error" class="bg-red-50 border border-red-200 rounded-lg p-6">
                <p class="text-sm text-red-800">{{ error }}</p>
                <NuxtLink
                    to="/giftcards"
                    class="inline-block mt-4 text-sm text-red-600 hover:text-red-800"
                >
                    Til baka
                </NuxtLink>
            </div>

            <!-- Gift Card Detail -->
            <div v-else-if="giftCard">
                <!-- Back Button -->
                <NuxtLink
                    to="/giftcards"
                    class="inline-flex items-center text-sm text-gray-600 hover:text-gray-900 mb-6"
                >
                    <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
                    </svg>
                    Til baka
                </NuxtLink>

                <!-- Header Card -->
                <div class="bg-gradient-to-br from-indigo-50 via-purple-50 to-pink-50 rounded-2xl p-8 border border-indigo-100 shadow-lg mb-6">
                    <div class="flex items-start justify-between">
                        <div>
                            <h1 class="text-3xl font-bold text-gray-800 mb-2">Gjafakort {{ giftCard.number }}</h1>
                            <div class="flex items-center gap-3 mb-4">
                                <GiftCardStatusBadge :status="giftCard.status" />
                                <span class="text-2xl font-bold text-indigo-600">
                                    {{ formatCurrency(giftCard.amount) }}
                                </span>
                            </div>
                            <p v-if="giftCard.templateName" class="text-sm text-gray-600">
                                Tegund: {{ giftCard.templateName }}
                            </p>
                        </div>
                        <div class="flex gap-2">
                            <button
                                @click="downloadPdf(giftCard.id, false)"
                                class="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50"
                            >
                                <svg class="w-5 h-5 inline mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                                </svg>
                                PDF (án bakgrunns)
                            </button>
                            <button
                                @click="downloadPdf(giftCard.id, true)"
                                class="px-4 py-2 text-sm font-medium text-white bg-indigo-600 rounded-lg hover:bg-indigo-700"
                            >
                                <svg class="w-5 h-5 inline mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                                </svg>
                                PDF (með bakgrunni)
                            </button>
                        </div>
                    </div>
                </div>

                <!-- Details Card -->
                <div class="bg-white rounded-lg shadow p-6 mb-6">
                    <h2 class="text-xl font-bold text-gray-900 mb-4">Upplýsingar</h2>
                    <dl class="grid grid-cols-1 md:grid-cols-2 gap-4">
                        <div>
                            <dt class="text-sm font-medium text-gray-500">Númer</dt>
                            <dd class="mt-1 text-sm text-gray-900">{{ giftCard.number }}</dd>
                        </div>
                        <div>
                            <dt class="text-sm font-medium text-gray-500">Upphæð</dt>
                            <dd class="mt-1 text-sm text-gray-900">{{ formatCurrency(giftCard.amount) }}</dd>
                        </div>
                        <div v-if="giftCard.templateName">
                            <dt class="text-sm font-medium text-gray-500">Tegund</dt>
                            <dd class="mt-1 text-sm text-gray-900">{{ giftCard.templateName }}</dd>
                        </div>
                        <div>
                            <dt class="text-sm font-medium text-gray-500">Staða</dt>
                            <dd class="mt-1">
                                <GiftCardStatusBadge :status="giftCard.status" />
                            </dd>
                        </div>
                        <div v-if="giftCard.expirationDate">
                            <dt class="text-sm font-medium text-gray-500">Gildir til</dt>
                            <dd class="mt-1 text-sm text-gray-900">{{ formatDate(giftCard.expirationDate) }}</dd>
                        </div>
                        <div v-if="giftCard.dkNumber">
                            <dt class="text-sm font-medium text-gray-500">DK númer</dt>
                            <dd class="mt-1 text-sm text-gray-900">{{ giftCard.dkNumber }}</dd>
                        </div>
                        <div>
                            <dt class="text-sm font-medium text-gray-500">Búið til</dt>
                            <dd class="mt-1 text-sm text-gray-900">{{ formatDate(giftCard.createdAt) }}</dd>
                        </div>
                        <div v-if="giftCard.createdByName">
                            <dt class="text-sm font-medium text-gray-500">Búið til af</dt>
                            <dd class="mt-1 text-sm text-gray-900">{{ giftCard.createdByName }}</dd>
                        </div>
                        <div v-if="giftCard.soldAt">
                            <dt class="text-sm font-medium text-gray-500">Selt</dt>
                            <dd class="mt-1 text-sm text-gray-900">{{ formatDate(giftCard.soldAt) }}</dd>
                        </div>
                        <div v-if="giftCard.redeemedAt">
                            <dt class="text-sm font-medium text-gray-500">Notað</dt>
                            <dd class="mt-1 text-sm text-gray-900">{{ formatDate(giftCard.redeemedAt) }}</dd>
                        </div>
                    </dl>
                    <div v-if="giftCard.message" class="mt-4 pt-4 border-t">
                        <dt class="text-sm font-medium text-gray-500 mb-2">Skilaboð</dt>
                        <dd class="text-sm text-gray-900 whitespace-pre-wrap">{{ giftCard.message }}</dd>
                    </div>
                </div>

                <!-- Status Update Card -->
                <div class="bg-white rounded-lg shadow p-6 mb-6">
                    <h2 class="text-xl font-bold text-gray-900 mb-4">Uppfæra staðu</h2>
                    <div class="flex flex-wrap gap-2">
                        <button
                            v-for="status in availableStatuses"
                            :key="status"
                            @click="updateStatus(status)"
                            :disabled="isUpdating || giftCard.status === status"
                            :class="[
                                'px-4 py-2 text-sm font-medium rounded-lg transition-colors',
                                giftCard.status === status
                                    ? 'bg-gray-200 text-gray-600 cursor-not-allowed'
                                    : 'bg-indigo-600 text-white hover:bg-indigo-700',
                                isUpdating ? 'opacity-50 cursor-not-allowed' : ''
                            ]"
                        >
                            {{ getStatusLabel(status) }}
                        </button>
                    </div>
                </div>

                <!-- Edit Card -->
                <div v-if="!isEditing" class="bg-white rounded-lg shadow p-6">
                    <div class="flex items-center justify-between">
                        <h2 class="text-xl font-bold text-gray-900">Breyta gjafakorti</h2>
                        <button
                            @click="isEditing = true"
                            class="px-4 py-2 text-sm font-medium text-indigo-600 bg-indigo-50 rounded-lg hover:bg-indigo-100"
                        >
                            Breyta
                        </button>
                    </div>
                </div>

                <!-- Edit Form -->
                <div v-else class="bg-white rounded-lg shadow p-6">
                    <h2 class="text-xl font-bold text-gray-900 mb-4">Breyta gjafakorti</h2>
                    <GiftCardForm
                        :gift-card="giftCard"
                        :templates="templates"
                        @submit="handleUpdate"
                        @cancel="isEditing = false"
                    />
                </div>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import type { GiftCard, GiftCardTemplate, GiftCardStatus, CreateGiftCardDto } from '~/types/giftCard'

const route = useRoute()
const { getGiftCard, updateGiftCard, updateGiftCardStatus, downloadPdf } = useGiftCards()
const { getTemplates } = useGiftCardTemplates()

const giftCard = ref<GiftCard | null>(null)
const templates = ref<GiftCardTemplate[]>([])
const isLoading = ref(false)
const isUpdating = ref(false)
const isEditing = ref(false)
const error = ref('')

const availableStatuses: GiftCardStatus[] = ['Created', 'Sold', 'Redeemed', 'Expired']

const loadGiftCard = async () => {
    isLoading.value = true
    error.value = ''
    try {
        giftCard.value = await getGiftCard(route.params.id as string)
    } catch (err: any) {
        error.value = 'Mistókst að sækja gjafakort'
        console.error(err)
    } finally {
        isLoading.value = false
    }
}

const loadTemplates = async () => {
    try {
        templates.value = await getTemplates()
    } catch (err: any) {
        console.error('Failed to load templates', err)
    }
}

const updateStatus = async (status: GiftCardStatus) => {
    if (!giftCard.value) return
    
    isUpdating.value = true
    try {
        giftCard.value = await updateGiftCardStatus(giftCard.value.id, status)
    } catch (err: any) {
        error.value = 'Mistókst að uppfæra staðu'
        console.error(err)
    } finally {
        isUpdating.value = false
    }
}

const handleUpdate = async (data: CreateGiftCardDto) => {
    if (!giftCard.value) return
    
    isUpdating.value = true
    try {
        giftCard.value = await updateGiftCard(giftCard.value.id, data)
        isEditing.value = false
    } catch (err: any) {
        error.value = 'Mistókst að uppfæra gjafakort'
        console.error(err)
    } finally {
        isUpdating.value = false
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

const getStatusLabel = (status: GiftCardStatus) => {
    const labels: Record<GiftCardStatus, string> = {
        Created: 'Búið til',
        Sold: 'Selt',
        Redeemed: 'Notað',
        Expired: 'Útrunnið'
    }
    return labels[status]
}

onMounted(() => {
    loadTemplates()
    loadGiftCard()
})
</script>

