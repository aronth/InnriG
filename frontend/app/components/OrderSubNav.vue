<template>
  <div class="bg-white border-gray-200 border mb-6 pt-2 rounded">
    <div class="px-4">
      <div class="flex space-x-8">
        <NuxtLink 
          to="/orders" 
          class="whitespace-nowrap pb-2 px-1 border-b-2 font-medium text-sm transition-colors duration-200"
          active-class="border-indigo-500 text-indigo-600"
          :class="[$route.path === '/orders' ? '' : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300']"
        >
          Yfirlit
        </NuxtLink>
        
        <NuxtLink 
          to="/orders/upload" 
          class="whitespace-nowrap pb-2 px-1 border-b-2 font-medium text-sm transition-colors duration-200"
          active-class="border-indigo-500 text-indigo-600"
          :class="[$route.path === '/orders/upload' ? '' : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300']"
        >
          Hlaða upp
        </NuxtLink>
        
        <NuxtLink 
          to="/orders/manage" 
          class="whitespace-nowrap pb-2 px-1 border-b-2 font-medium text-sm transition-colors duration-200"
          active-class="border-indigo-500 text-indigo-600"
          :class="[$route.path === '/orders/manage' ? '' : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300']"
        >
          Stjórna
        </NuxtLink>
        
        <NuxtLink 
          to="/orders/reports" 
          class="whitespace-nowrap pb-2 px-1 border-b-2 font-medium text-sm transition-colors duration-200"
          active-class="border-indigo-500 text-indigo-600"
          :class="[$route.path.startsWith('/orders/reports') ? '' : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300']"
        >
          Skýrslur
        </NuxtLink>
      </div>
    </div>
  </div>
</template>

