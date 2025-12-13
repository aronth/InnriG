<template>
  <div class="min-h-screen bg-slate-100 font-sans text-slate-900 selection:bg-indigo-100 selection:text-indigo-700">
    <AppNav />

    <div class="py-6">
      <main>
        <div class="mx-auto px-4">
          <slot />
        </div>
      </main>
    </div>
  </div>
</template>
