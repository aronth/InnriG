<template>
    <div class="min-h-screen bg-gray-50 py-8">
        <div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">
            <!-- Sub Navigation -->
            <GiftCardSubNav />
            
            <!-- Header -->
            <div class="mb-8">
                <div class="flex items-center justify-between">
                    <div>
                        <h1 class="text-3xl font-bold text-gray-900">Tegundir gjafakorta</h1>
                        <p class="mt-2 text-sm text-gray-600">Stjórna tegundum gjafakorta</p>
                    </div>
                    <button
                        @click="showCreateModal = true"
                        class="inline-flex items-center px-4 py-2 border border-transparent rounded-lg shadow-sm text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700"
                    >
                        <svg class="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
                        </svg>
                        Búa til tegund
                    </button>
                </div>
            </div>

            <!-- Templates List -->
            <div class="bg-white rounded-lg shadow overflow-hidden">
                <div v-if="isLoading" class="text-center py-12">
                    <div class="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600"></div>
                    <p class="mt-4 text-sm text-gray-600">Hleður tegundum...</p>
                </div>

                <div v-else-if="templates.length === 0" class="text-center py-12">
                    <p class="text-sm text-gray-500">Engar tegundir skráðar</p>
                </div>

                <div v-else class="divide-y divide-gray-200">
                    <div
                        v-for="template in templates"
                        :key="template.id"
                        class="p-6 hover:bg-gray-50"
                    >
                        <div class="flex items-start justify-between">
                            <div class="flex-1">
                                <div class="flex items-center gap-3 mb-2">
                                    <h3 class="text-lg font-semibold text-gray-900">{{ template.name }}</h3>
                                    <span
                                        :class="[
                                            'px-2 py-1 text-xs font-medium rounded-full',
                                            template.isActive
                                                ? 'bg-green-100 text-green-800'
                                                : 'bg-gray-100 text-gray-800'
                                        ]"
                                    >
                                        {{ template.isActive ? 'Virk' : 'Óvirk' }}
                                    </span>
                                </div>
                                <p v-if="template.description" class="text-sm text-gray-600 mb-2">
                                    {{ template.description }}
                                </p>
                                <div class="flex flex-wrap items-center gap-4 text-sm text-gray-500">
                                    <span v-if="template.defaultAmount">
                                        Upphæð: {{ formatCurrency(template.defaultAmount) }}
                                    </span>
                                    <span v-if="template.restaurantName">
                                        Staður: {{ template.restaurantName }}
                                    </span>
                                    <span v-else-if="!template.restaurantId">
                                        Staður: Báðir staðir
                                    </span>
                                    <span v-if="template.messageTemplate" class="italic">
                                        {{ template.isMonetaryTemplate ? 'Peningagjöf' : 'Vörugjöf' }}
                                    </span>
                                </div>
                            </div>
                            <div class="flex gap-2 ml-4">
                                <button
                                    @click="editTemplate(template)"
                                    class="px-3 py-1 text-sm font-medium text-indigo-600 hover:text-indigo-800"
                                >
                                    Breyta
                                </button>
                                <button
                                    @click="deleteTemplate(template.id)"
                                    class="px-3 py-1 text-sm font-medium text-red-600 hover:text-red-800"
                                >
                                    Eyða
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- Create/Edit Modal -->
        <div
            v-if="showCreateModal || editingTemplate"
            class="fixed z-50 inset-0 overflow-y-auto"
            @click.self="closeModal"
        >
            <div class="flex items-center justify-center min-h-screen pt-4 px-4 pb-20 text-center sm:block sm:p-0">
                <div class="fixed inset-0 bg-gray-900 bg-opacity-75 transition-opacity" @click="closeModal"></div>
                <div class="inline-block align-bottom bg-white rounded-lg text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-lg sm:w-full">
                    <div class="bg-white px-4 pt-5 pb-4 sm:p-6 sm:pb-4">
                        <h3 class="text-lg font-medium text-gray-900 mb-4">
                            {{ editingTemplate ? 'Breyta tegund' : 'Búa til tegund' }}
                        </h3>
                        <form @submit.prevent="handleSubmit" class="space-y-4">
                            <div>
                                <label for="name" class="block text-sm font-medium text-gray-700 mb-2">
                                    Nafn <span class="text-red-500">*</span>
                                </label>
                                <input
                                    id="name"
                                    v-model="form.name"
                                    type="text"
                                    required
                                    class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
                                />
                            </div>
                            <div>
                                <label for="description" class="block text-sm font-medium text-gray-700 mb-2">
                                    Lýsing
                                </label>
                                <textarea
                                    id="description"
                                    v-model="form.description"
                                    rows="3"
                                    class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
                                />
                            </div>
                            <div>
                                <label for="defaultAmount" class="block text-sm font-medium text-gray-700 mb-2">
                                    Sjálfgefin upphæð (kr.)
                                </label>
                                <input
                                    id="defaultAmount"
                                    v-model.number="form.defaultAmount"
                                    type="number"
                                    step="0.01"
                                    min="0"
                                    class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
                                />
                            </div>
                            <div>
                                <label for="messageTemplate" class="block text-sm font-medium text-gray-700 mb-2">
                                    Skilaboð
                                </label>
                                <textarea
                                    id="messageTemplate"
                                    v-model="form.messageTemplate"
                                    rows="2"
                                    placeholder="Þér er boðið að þyggja veitingar að upphæð:"
                                    class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
                                />
                            </div>
                            <div>
                                <label for="amountAsText" class="block text-sm font-medium text-gray-700 mb-2">
                                    Upphæð sem texti
                                </label>
                                <input
                                    id="amountAsText"
                                    v-model="form.amountAsText"
                                    type="text"
                                    placeholder="Fimm þúsund krónur"
                                    class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
                                />
                            </div>
                            <div>
                                <label for="restaurant" class="block text-sm font-medium text-gray-700 mb-2">
                                    Staður
                                </label>
                                <select
                                    id="restaurant"
                                    v-model="form.restaurantId"
                                    class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
                                >
                                    <option :value="undefined">Báðir staðir</option>
                                    <option
                                        v-for="restaurant in restaurants"
                                        :key="restaurant.id"
                                        :value="restaurant.id"
                                    >
                                        {{ restaurant.name }}
                                    </option>
                                </select>
                            </div>
                            <div class="flex items-center">
                                <input
                                    id="isMonetaryTemplate"
                                    v-model="form.isMonetaryTemplate"
                                    type="checkbox"
                                    class="h-4 w-4 text-indigo-600 focus:ring-indigo-500 border-gray-300 rounded"
                                />
                                <label for="isMonetaryTemplate" class="ml-2 block text-sm text-gray-700">
                                    Peningagjöf (ekki vöru/þjónustugjöf)
                                </label>
                            </div>
                            <div class="flex items-center">
                                <input
                                    id="isActive"
                                    v-model="form.isActive"
                                    type="checkbox"
                                    class="h-4 w-4 text-indigo-600 focus:ring-indigo-500 border-gray-300 rounded"
                                />
                                <label for="isActive" class="ml-2 block text-sm text-gray-700">
                                    Virk
                                </label>
                            </div>
                            <div class="flex gap-3 justify-end pt-4 border-t">
                                <button
                                    type="button"
                                    @click="closeModal"
                                    class="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50"
                                >
                                    Hætta við
                                </button>
                                <button
                                    type="submit"
                                    :disabled="isSubmitting"
                                    class="px-4 py-2 text-sm font-medium text-white bg-indigo-600 rounded-lg hover:bg-indigo-700 disabled:opacity-50"
                                >
                                    <span v-if="isSubmitting">Vista...</span>
                                    <span v-else>Vista</span>
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import type { GiftCardTemplate, CreateGiftCardTemplateDto, UpdateGiftCardTemplateDto, Restaurant } from '~/types/giftCard'

const { getTemplates, createTemplate, updateTemplate, deleteTemplate: deleteTemplateApi } = useGiftCardTemplates()
const { getRestaurants } = useRestaurants()

const templates = ref<GiftCardTemplate[]>([])
const restaurants = ref<Restaurant[]>([])
const isLoading = ref(false)
const isSubmitting = ref(false)
const showCreateModal = ref(false)
const editingTemplate = ref<GiftCardTemplate | null>(null)

const form = reactive<CreateGiftCardTemplateDto & { isActive: boolean }>({
    name: '',
    description: '',
    defaultAmount: undefined,
    messageTemplate: '',
    amountAsText: '',
    isMonetaryTemplate: true,
    restaurantId: undefined,
    isActive: true
})

const loadTemplates = async () => {
    isLoading.value = true
    try {
        templates.value = await getTemplates()
    } catch (err: any) {
        console.error('Failed to load templates', err)
    } finally {
        isLoading.value = false
    }
}

const editTemplate = (template: GiftCardTemplate) => {
    editingTemplate.value = template
    form.name = template.name
    form.description = template.description || ''
    form.defaultAmount = template.defaultAmount
    form.messageTemplate = template.messageTemplate || ''
    form.amountAsText = template.amountAsText || ''
    form.isMonetaryTemplate = template.isMonetaryTemplate
    form.restaurantId = template.restaurantId
    form.isActive = template.isActive
}

const closeModal = () => {
    showCreateModal.value = false
    editingTemplate.value = null
    form.name = ''
    form.description = ''
    form.defaultAmount = undefined
    form.messageTemplate = ''
    form.amountAsText = ''
    form.isMonetaryTemplate = true
    form.restaurantId = undefined
    form.isActive = true
}

const handleSubmit = async () => {
    isSubmitting.value = true
    try {
        if (editingTemplate.value) {
            const updateDto: UpdateGiftCardTemplateDto = {
                name: form.name,
                description: form.description || undefined,
                defaultAmount: form.defaultAmount,
                messageTemplate: form.messageTemplate || undefined,
                amountAsText: form.amountAsText || undefined,
                isMonetaryTemplate: form.isMonetaryTemplate,
                restaurantId: form.restaurantId,
                isActive: form.isActive
            }
            await updateTemplate(editingTemplate.value.id, updateDto)
        } else {
            await createTemplate(form)
        }
        await loadTemplates()
        closeModal()
    } catch (err: any) {
        console.error('Failed to save template', err)
        alert('Mistókst að vista tegund')
    } finally {
        isSubmitting.value = false
    }
}

const deleteTemplate = async (id: string) => {
    if (!confirm('Ertu viss um að þú viljir eyða þessari tegund?')) {
        return
    }

    try {
        await deleteTemplateApi(id)
        await loadTemplates()
    } catch (err: any) {
        console.error('Failed to delete template', err)
        alert('Mistókst að eyða tegund. Gæti verið í notkun.')
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

