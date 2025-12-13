<template>
  <div class="bg-white border-gray-200 border mb-6 pt-2 rounded">
    <div class="px-4">
      <div class="flex space-x-8 ">
        <NuxtLink 
          to="/products" 
          class="whitespace-nowrap pb-2 px-1 border-b-2 font-medium text-sm transition-colors duration-200"
          active-class="border-indigo-500 text-indigo-600"
          :class="[$route.path === '/products' ? '' : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300']"
        >
          Lesa inn reikning
        </NuxtLink>
        
        <NuxtLink 
          to="/products/catalog" 
          class="whitespace-nowrap pb-2 px-1 border-b-2 font-medium text-sm transition-colors duration-200"
          active-class="border-indigo-500 text-indigo-600"
          :class="[$route.path.startsWith('/products/catalog') ? '' : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300']"
        >
          Vörulisti
        </NuxtLink>
        
        <NuxtLink 
          to="/products/compare" 
          class="whitespace-nowrap pb-2 px-1 border-b-2 font-medium text-sm transition-colors duration-200"
          active-class="border-indigo-500 text-indigo-600"
          :class="[$route.path === '/products/compare' ? '' : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300']"
        >
          Verðsamanburður
        </NuxtLink>
        
        <NuxtLink 
          to="/products/suppliers" 
          class="whitespace-nowrap pb-2 px-1 border-b-2 font-medium text-sm transition-colors duration-200"
          active-class="border-indigo-500 text-indigo-600"
          :class="[$route.path === '/products/suppliers' ? '' : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300']"
        >
          Birgjar
        </NuxtLink>
      </div>
    </div>
  </div>
</template>
