<script setup lang="ts">
import type { GiftCardStatus } from '~/types/giftCard'

interface Props {
    status: GiftCardStatus
}

const props = defineProps<Props>()

const statusConfig = {
    Created: {
        label: 'Búið til',
        class: 'bg-gray-100 text-gray-800 border-gray-300'
    },
    Sold: {
        label: 'Selt',
        class: 'bg-blue-100 text-blue-800 border-blue-300'
    },
    Redeemed: {
        label: 'Notað',
        class: 'bg-green-100 text-green-800 border-green-300'
    },
    Expired: {
        label: 'Útrunnið',
        class: 'bg-red-100 text-red-800 border-red-300'
    }
}

const config = statusConfig[props.status] || statusConfig.Created
</script>

<template>
    <span
        :class="[
            'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium border',
            config.class
        ]"
    >
        {{ config.label }}
    </span>
</template>



