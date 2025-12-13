<template>
  <div class="bg-gradient-to-br from-yellow-50 to-orange-50 rounded-lg border border-yellow-100 p-4">
    <div class="flex items-start gap-3 mb-3">
      <div class="w-10 h-10 bg-yellow-100 rounded-lg flex items-center justify-center flex-shrink-0">
        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-5 h-5 text-yellow-600">
          <path stroke-linecap="round" stroke-linejoin="round" d="M15.182 15.182a4.5 4.5 0 01-6.364 0M21 12a9 9 0 11-18 0 9 9 0 0118 0zM9.75 9.75c0 .414-.168.75-.375.75S9 10.164 9 9.75 9.168 9 9.375 9s.375.336.375.75zm-.375 0h.008v.015h-.008V9.75zm5.625 0c0 .414-.168.75-.375.75s-.375-.336-.375-.75.168-.75.375-.75.375.336.375.75zm-.375 0h.008v.015h-.008V9.75z" />
        </svg>
      </div>
      <div class="flex-1 min-w-0">
        <h3 class="font-semibold text-yellow-800 mb-1">Pabbabrandari dagsins</h3>
      </div>
    </div>

    <!-- Joke Content -->
    <div class="mb-3">
      <p v-if="pending" class="text-yellow-700/60 text-sm italic">Sæki brandara...</p>
      <p v-else-if="error" class="text-red-500 text-sm">Gat ekki sótt brandara :(</p>
      <p v-else class="text-sm text-gray-700 italic leading-relaxed line-clamp-3">
        "{{ joke }}"
      </p>
    </div>

    <!-- Refresh Button -->
    <div class="flex justify-end">
      <button 
        @click="refresh" 
        class="text-xs font-medium text-yellow-700 hover:text-yellow-900 transition-colors flex items-center gap-1"
        :disabled="pending"
      >
        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-3 h-3" :class="{'animate-spin': pending}">
          <path stroke-linecap="round" stroke-linejoin="round" d="M16.023 9.348h4.992v-.001M2.985 19.644v-4.992m0 0h4.992m-4.993 0l3.181 3.183a8.25 8.25 0 0013.803-3.7M4.031 9.865a8.25 8.25 0 0113.803-3.7l3.181 3.182m0-4.991v4.99" />
        </svg>
        Nýr brandari
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
const joke = ref('');
const pending = ref(true);
const error = ref(false);

const fetchJoke = async () => {
  pending.value = true;
  error.value = false;
  try {
    const data = await $fetch<{joke: string}>('https://icanhazdadjoke.com/', {
      headers: { 'Accept': 'application/json' }
    });
    joke.value = data.joke;
  } catch (e) {
    error.value = true;
  } finally {
    pending.value = false;
  }
};

onMounted(() => {
  fetchJoke();
});

const refresh = () => fetchJoke();
</script>
