<template>
  <div class="bg-white border-gray-200 border mb-6 pt-2 rounded">
    <div class="px-4">
      <div class="flex space-x-8">
        <NuxtLink 
          to="/giftcards" 
          class="whitespace-nowrap pb-2 px-1 border-b-2 font-medium text-sm transition-colors duration-200"
          active-class="border-indigo-500 text-indigo-600"
          :class="[$route.path === '/giftcards' ? '' : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300']"
        >
          Gjafakort
        </NuxtLink>
        
        <NuxtLink 
          to="/giftcards/templates" 
          class="whitespace-nowrap pb-2 px-1 border-b-2 font-medium text-sm transition-colors duration-200"
          active-class="border-indigo-500 text-indigo-600"
          :class="[$route.path.startsWith('/giftcards/templates') ? '' : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300']"
        >
          Tegundir
        </NuxtLink>
      </div>
    </div>
  </div>
</template>



