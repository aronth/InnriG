<script setup lang="ts">
import type { GiftCard, GiftCardTemplate, CreateGiftCardDto, Restaurant } from '~/types/giftCard'

interface Props {
    giftCard?: GiftCard
    templates?: GiftCardTemplate[]
}

interface Emits {
    (e: 'submit', data: CreateGiftCardDto): void
    (e: 'cancel'): void
}

const props = withDefaults(defineProps<Props>(), {
    giftCard: undefined,
    templates: () => []
})

const emit = defineEmits<Emits>()

const { getRestaurants } = useRestaurants()
const restaurants = ref<Restaurant[]>([])

const form = reactive<CreateGiftCardDto>({
    templateId: props.giftCard?.templateId,
    restaurantId: props.giftCard?.restaurantId,
    amount: props.giftCard?.amount ?? 0,
    message: props.giftCard?.message ?? '',
    dkNumber: props.giftCard?.dkNumber ?? '',
    printWithBackground: props.giftCard?.printWithBackground ?? false
})

const isSubmitting = ref(false)

const selectedTemplate = computed(() => {
    if (!form.templateId) return null
    return props.templates.find(t => t.id === form.templateId)
})

watch(() => form.templateId, (newTemplateId) => {
    if (newTemplateId && props.templates.length > 0) {
        const template = props.templates.find(t => t.id === newTemplateId)
        if (template) {
            // Auto-fill from template
            if (template.defaultAmount) {
                form.amount = template.defaultAmount
            }
            if (template.restaurantId) {
                form.restaurantId = template.restaurantId
            }
            // Build message from template
            if (template.messageTemplate) {
                form.message = template.messageTemplate
                if (template.isMonetaryTemplate && template.amountAsText) {
                    form.message += '\n' + template.amountAsText
                }
            }
        }
    }
})

const handleSubmit = async () => {
    isSubmitting.value = true
    try {
        const submitData: CreateGiftCardDto = {
            templateId: form.templateId || undefined,
            restaurantId: form.restaurantId || undefined,
            amount: form.amount,
            message: form.message || undefined,
            dkNumber: form.dkNumber || undefined,
            printWithBackground: form.printWithBackground
        }
        emit('submit', submitData)
    } finally {
        isSubmitting.value = false
    }
}

const handleCancel = () => {
    emit('cancel')
}

const loadRestaurants = async () => {
    try {
        restaurants.value = await getRestaurants()
    } catch (err: any) {
        console.error('Failed to load restaurants', err)
    }
}

onMounted(() => {
    loadRestaurants()
})
</script>

<template>
    <form @submit.prevent="handleSubmit" class="space-y-6">
        <!-- Template Selection -->
        <div>
            <label for="template" class="block text-sm font-medium text-gray-700 mb-2">
                Tegund gjafakorts (valfrjálst)
            </label>
            <select
                id="template"
                v-model="form.templateId"
                class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            >
                <option :value="undefined">Engin tegund</option>
                <option
                    v-for="template in templates"
                    :key="template.id"
                    :value="template.id"
                >
                    {{ template.name }}
                </option>
            </select>
            <p v-if="selectedTemplate?.description" class="mt-1 text-sm text-gray-500">
                {{ selectedTemplate.description }}
            </p>
        </div>

        <!-- Restaurant Selection -->
        <div>
            <label for="restaurant" class="block text-sm font-medium text-gray-700 mb-2">
                Staður (valfrjálst)
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

        <!-- Amount -->
        <div>
            <label for="amount" class="block text-sm font-medium text-gray-700 mb-2">
                Upphæð (kr.) <span class="text-red-500">*</span>
            </label>
            <input
                id="amount"
                v-model.number="form.amount"
                type="number"
                step="0.01"
                min="0"
                required
                class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            />
        </div>

        <!-- Message -->
        <div>
            <label for="message" class="block text-sm font-medium text-gray-700 mb-2">
                Skilaboð (valfrjálst)
            </label>
            <textarea
                id="message"
                v-model="form.message"
                rows="3"
                class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            />
        </div>

        <!-- DK Number -->
        <div>
            <label for="dkNumber" class="block text-sm font-medium text-gray-700 mb-2">
                DK númer (valfrjálst)
            </label>
            <input
                id="dkNumber"
                v-model="form.dkNumber"
                type="text"
                placeholder="Tómt fyrir forprentaða stafla"
                class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            />
        </div>

        <!-- Print with Background -->
        <div class="flex items-center">
            <input
                id="printWithBackground"
                v-model="form.printWithBackground"
                type="checkbox"
                class="h-4 w-4 text-indigo-600 focus:ring-indigo-500 border-gray-300 rounded"
            />
            <label for="printWithBackground" class="ml-2 block text-sm text-gray-700">
                Prenta með bakgrunni (fyrir tölvupóst)
            </label>
        </div>

        <!-- Actions -->
        <div class="flex gap-3 justify-end pt-4 border-t">
            <button
                type="button"
                @click="handleCancel"
                class="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500"
            >
                Hætta við
            </button>
            <button
                type="submit"
                :disabled="isSubmitting"
                class="px-4 py-2 text-sm font-medium text-white bg-indigo-600 rounded-lg hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50 disabled:cursor-not-allowed"
            >
                <span v-if="isSubmitting">Vista...</span>
                <span v-else>{{ giftCard ? 'Uppfæra' : 'Búa til' }}</span>
            </button>
        </div>
    </form>
</template>



