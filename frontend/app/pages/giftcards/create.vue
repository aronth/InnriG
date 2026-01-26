<template>
    <div class="min-h-screen bg-gray-50 py-8">
        <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
            <!-- Sub Navigation -->
            <GiftCardSubNav />
            
            <!-- Header -->
            <div class="mb-8">
                <NuxtLink
                    to="/giftcards"
                    class="inline-flex items-center text-sm text-gray-600 hover:text-gray-900 mb-4"
                >
                    <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
                    </svg>
                    Til baka
                </NuxtLink>
                <h1 class="text-3xl font-bold text-gray-900">Búa til gjafakort</h1>
                <p class="mt-2 text-sm text-gray-600">Búðu til nýtt gjafakort eða margar í einu</p>
            </div>

            <!-- Mode Toggle -->
            <div class="bg-white rounded-lg shadow p-6 mb-6">
                <div class="flex items-center justify-between">
                    <span class="text-sm font-medium text-gray-700">Hamur:</span>
                    <div class="flex items-center gap-3">
                        <span :class="['text-sm', isBatchMode ? 'text-gray-500' : 'text-indigo-600 font-medium']">
                            Eitt gjafakort
                        </span>
                        <button
                            @click="isBatchMode = !isBatchMode"
                            :class="[
                                'relative inline-flex h-6 w-11 flex-shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2',
                                isBatchMode ? 'bg-indigo-600' : 'bg-gray-200'
                            ]"
                        >
                            <span
                                :class="[
                                    'pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow ring-0 transition duration-200 ease-in-out',
                                    isBatchMode ? 'translate-x-5' : 'translate-x-0'
                                ]"
                            />
                        </button>
                        <span :class="['text-sm', isBatchMode ? 'text-indigo-600 font-medium' : 'text-gray-500']">
                            Fjöldi
                        </span>
                    </div>
                </div>
            </div>

            <!-- Single Card Form with Preview -->
            <div v-if="!isBatchMode" class="grid grid-cols-1 lg:grid-cols-2 gap-6">
                <!-- Form -->
                <div class="bg-white rounded-lg shadow p-6">
                    <h2 class="text-lg font-semibold mb-4">Upplýsingar</h2>
                    <GiftCardForm
                        :templates="templates"
                        @submit="handleCreateSingle"
                        @cancel="navigateTo('/giftcards')"
                    />
                </div>
                
                <!-- Preview -->
                <div class="bg-white rounded-lg shadow p-6">
                    <h2 class="text-lg font-semibold mb-4">Forskoðun</h2>
                    <GiftCardPreview
                        :amount="previewData.amount"
                        :message="previewData.message"
                        :restaurant-id="previewData.restaurantId"
                        :restaurant-code="previewData.restaurantCode"
                        :dk-number="previewData.dkNumber"
                        :print-with-background="previewData.printWithBackground"
                    />
                </div>
            </div>

            <!-- Batch Form -->
            <div v-else class="bg-white rounded-lg shadow p-6">
                <form @submit.prevent="handleCreateBatch" class="space-y-6">
                    <div>
                        <label for="batchCount" class="block text-sm font-medium text-gray-700 mb-2">
                            Fjöldi gjafakorta <span class="text-red-500">*</span>
                        </label>
                        <input
                            id="batchCount"
                            v-model.number="batchForm.count"
                            type="number"
                            min="1"
                            max="100"
                            required
                            class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
                        />
                    </div>

                    <GiftCardForm
                        :templates="templates"
                        :gift-card="undefined"
                        @submit="(data) => { batchFormData = data }"
                    />

                    <div class="flex gap-3 justify-end pt-4 border-t">
                        <button
                            type="button"
                            @click="navigateTo('/giftcards')"
                            class="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50"
                        >
                            Hætta við
                        </button>
                        <button
                            type="submit"
                            :disabled="isSubmitting"
                            class="px-4 py-2 text-sm font-medium text-white bg-indigo-600 rounded-lg hover:bg-indigo-700 disabled:opacity-50"
                        >
                            <span v-if="isSubmitting">Býr til...</span>
                            <span v-else>Búa til {{ batchForm.count }} gjafakort</span>
                        </button>
                    </div>
                </form>
            </div>

            <!-- Success Message -->
            <div
                v-if="createdGiftCards.length > 0"
                class="mt-6 bg-green-50 border border-green-200 rounded-lg p-4"
            >
                <p class="text-sm text-green-800">
                    Búið var að búa til {{ createdGiftCards.length }} gjafakort!
                </p>
                <div class="mt-2">
                    <NuxtLink
                        v-for="gc in createdGiftCards"
                        :key="gc.id"
                        :to="`/giftcards/${gc.id}`"
                        class="text-sm text-green-700 hover:text-green-900 mr-4"
                    >
                        {{ gc.number }}
                    </NuxtLink>
                </div>
            </div>

            <!-- Error Message -->
            <div v-if="error" class="mt-6 bg-red-50 border border-red-200 rounded-lg p-4">
                <p class="text-sm text-red-800">{{ error }}</p>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import type { GiftCard, GiftCardTemplate, CreateGiftCardDto, CreateGiftCardBatchDto, Restaurant } from '~/types/giftCard'

const { createGiftCard, createGiftCardsBatch } = useGiftCards()
const { getTemplates } = useGiftCardTemplates()
const { getRestaurants } = useRestaurants()

const templates = ref<GiftCardTemplate[]>([])
const restaurants = ref<Restaurant[]>([])
const isBatchMode = ref(false)
const isSubmitting = ref(false)
const error = ref('')
const createdGiftCards = ref<GiftCard[]>([])
const batchFormData = ref<CreateGiftCardDto | null>(null)

const batchForm = reactive<CreateGiftCardBatchDto>({
    count: 1,
    templateId: undefined,
    restaurantId: undefined,
    amount: undefined,
    message: undefined,
    printWithBackground: false
})

// Preview data computed from form state
const previewData = computed(() => {
    // Try to get preview data from the active form elements
    const formElements = document.querySelectorAll('input, select, textarea')
    let amount = 0
    let message = ''
    let restaurantId = ''
    let dkNumber = ''
    let printWithBackground = false
    
    formElements.forEach((el: any) => {
        if (el.id === 'amount') amount = parseFloat(el.value) || 0
        if (el.id === 'message') message = el.value || ''
        if (el.id === 'restaurant') restaurantId = el.value || ''
        if (el.id === 'dkNumber') dkNumber = el.value || ''
        if (el.id === 'printWithBackground') printWithBackground = el.checked || false
    })
    
    const restaurant = restaurants.value.find(r => r.id === restaurantId)
    
    return {
        amount,
        message,
        restaurantId,
        restaurantCode: restaurant?.code,
        dkNumber,
        printWithBackground
    }
})

const handleCreateSingle = async (data: CreateGiftCardDto) => {
    isSubmitting.value = true
    error.value = ''
    createdGiftCards.value = []
    
    try {
        const giftCard = await createGiftCard(data)
        createdGiftCards.value = [giftCard]
        await navigateTo(`/giftcards/${giftCard.id}`)
    } catch (err: any) {
        error.value = 'Mistókst að búa til gjafakort'
        console.error(err)
    } finally {
        isSubmitting.value = false
    }
}

const handleCreateBatch = async () => {
    if (!batchFormData.value) {
        error.value = 'Vinsamlegast fylltu út allar upplýsingar'
        return
    }

    isSubmitting.value = true
    error.value = ''
    createdGiftCards.value = []

    try {
        const batchDto: CreateGiftCardBatchDto = {
            count: batchForm.count,
            templateId: batchFormData.value.templateId,
            restaurantId: batchFormData.value.restaurantId,
            amount: batchFormData.value.amount,
            message: batchFormData.value.message,
            printWithBackground: batchFormData.value.printWithBackground
        }
        
        const giftCards = await createGiftCardsBatch(batchDto)
        createdGiftCards.value = giftCards
    } catch (err: any) {
        error.value = 'Mistókst að búa til gjafakort'
        console.error(err)
    } finally {
        isSubmitting.value = false
    }
}

const loadTemplates = async () => {
    try {
        templates.value = await getTemplates(true)
    } catch (err: any) {
        console.error('Failed to load templates', err)
    }
}

const loadRestaurants = async () => {
    try {
        restaurants.value = await getRestaurants()
    } catch (err: any) {
        console.error('Failed to load restaurants', err)
    }
}

onMounted(() => {
    loadTemplates()
    loadRestaurants()
})
</script>

