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
          Yfirlit
        </NuxtLink>
        
        <NuxtLink 
          to="/products/import" 
          class="whitespace-nowrap pb-2 px-1 border-b-2 font-medium text-sm transition-colors duration-200"
          active-class="border-indigo-500 text-indigo-600"
          :class="[$route.path === '/products/import' ? '' : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300']"
        >
          Lesa inn reikning
        </NuxtLink>
        
        <NuxtLink 
          to="/products/invoices" 
          class="whitespace-nowrap pb-2 px-1 border-b-2 font-medium text-sm transition-colors duration-200"
          active-class="border-indigo-500 text-indigo-600"
          :class="[$route.path.startsWith('/products/invoices') ? '' : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300']"
        >
          Reikningar
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
          to="/products/verdproun" 
          class="whitespace-nowrap pb-2 px-1 border-b-2 font-medium text-sm transition-colors duration-200"
          active-class="border-indigo-500 text-indigo-600"
          :class="[$route.path === '/products/verdproun' ? '' : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300']"
        >
          Verðþróun
        </NuxtLink>
        
        <NuxtLink 
          to="/products/suppliers" 
          class="whitespace-nowrap pb-2 px-1 border-b-2 font-medium text-sm transition-colors duration-200"
          active-class="border-indigo-500 text-indigo-600"
          :class="[$route.path === '/products/suppliers' ? '' : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300']"
        >
          Birgjar
        </NuxtLink>
        
        <NuxtLink 
          to="/products/most-ordered" 
          class="whitespace-nowrap pb-2 px-1 border-b-2 font-medium text-sm transition-colors duration-200"
          active-class="border-indigo-500 text-indigo-600"
          :class="[$route.path === '/products/most-ordered' ? '' : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300']"
        >
          Mest pantaðar
        </NuxtLink>
        
        <NuxtLink 
          to="/products/haesta-innkaup" 
          class="whitespace-nowrap pb-2 px-1 border-b-2 font-medium text-sm transition-colors duration-200"
          active-class="border-indigo-500 text-indigo-600"
          :class="[$route.path === '/products/haesta-innkaup' ? '' : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300']"
        >
          Hæsta innkaup
        </NuxtLink>
      </div>
    </div>
  </div>
</template>
