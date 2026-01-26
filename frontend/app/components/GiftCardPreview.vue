<template>
    <div class="gift-card-preview-container">
        <div class="gift-card-preview" :class="{ 'with-background': printWithBackground }">
            <!-- Background image (if applicable) -->
            <div v-if="printWithBackground && restaurantCode" class="background-overlay" :style="backgroundStyle"></div>
            
            <!-- Content -->
            <div class="content">
                <!-- Header -->
                <h1 class="title">Gjafakort</h1>
                
                <!-- Gift Card Number -->
                <p class="number">Númer: PREVIEW-001</p>
                
                <!-- Message -->
                <div v-if="message" class="message">
                    {{ message }}
                </div>
                
                <!-- Amount -->
                <div class="amount">
                    {{ formatCurrency(amount) }}
                </div>
                
                <!-- Expiration Date - Always empty line for pre-printed -->
                <div class="field">
                    <span class="label">Gildir til:</span>
                    <span class="underline">_______________</span>
                </div>
                
                <!-- DK Number -->
                <div class="field">
                    <span class="label">DK númer:</span>
                    <span v-if="dkNumber" class="value">{{ dkNumber }}</span>
                    <span v-else class="underline">_______________</span>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
interface Props {
    amount: number
    message?: string
    restaurantId?: string
    restaurantCode?: string
    dkNumber?: string
    printWithBackground?: boolean
}

const props = withDefaults(defineProps<Props>(), {
    printWithBackground: false
})

const backgroundStyle = computed(() => {
    if (!props.printWithBackground || !props.restaurantCode) return {}
    
    // Map restaurant code to background image
    const backgroundMap: Record<string, string> = {
        'GRE': '/backgrounds/G_Blank.jpg',
        'SPR': '/backgrounds/S_Blank.jpg',
        'default': '/backgrounds/GS_Blank.jpg'
    }
    
    const backgroundImage = backgroundMap[props.restaurantCode] || backgroundMap.default
    
    return {
        backgroundImage: `url('${backgroundImage}')`,
        backgroundSize: 'cover',
        backgroundPosition: 'center'
    }
})

const formatCurrency = (amount: number): string => {
    // Icelandic format: 5.000 kr.
    return new Intl.NumberFormat('is-IS', {
        minimumFractionDigits: 0,
        maximumFractionDigits: 0
    }).format(amount) + ' kr.'
}
</script>

<style scoped>
.gift-card-preview-container {
    display: flex;
    justify-content: center;
    align-items: center;
    padding: 20px;
}

.gift-card-preview {
    position: relative;
    width: 100%;
    max-width: 400px;
    aspect-ratio: 148 / 210; /* A5 proportions */
    background: white;
    border: 1px solid #e5e7eb;
    border-radius: 8px;
    overflow: hidden;
    box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06);
}

.gift-card-preview.with-background {
    border: none;
}

.background-overlay {
    position: absolute;
    inset: 0;
    z-index: 0;
}

.content {
    position: relative;
    z-index: 1;
    height: 100%;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 40px 20px;
    text-align: center;
}

.title {
    font-size: 2rem;
    font-weight: bold;
    color: #1e40af;
    margin-bottom: 20px;
}

.number {
    font-size: 1.125rem;
    font-weight: 600;
    margin-bottom: 15px;
    color: #374151;
}

.message {
    font-size: 0.875rem;
    font-style: italic;
    margin-bottom: 20px;
    max-width: 80%;
    color: #6b7280;
    white-space: pre-line;
}

.amount {
    font-size: 1.5rem;
    font-weight: bold;
    color: #059669;
    margin-bottom: 30px;
}

.field {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 8px;
    margin-bottom: 12px;
    font-size: 0.75rem;
    color: #374151;
}

.label {
    font-weight: 500;
}

.value {
    font-weight: 600;
}

.underline {
    font-weight: bold;
    color: #000;
    letter-spacing: 0.1em;
}
</style>

