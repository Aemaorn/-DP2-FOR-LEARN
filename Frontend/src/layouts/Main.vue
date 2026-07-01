<script setup lang="ts">
import { Navbar } from '@/components/layouts/index';
</script>

<template>
  <Navbar />
  <main class="px-4 relative top-[60px] pt-6">
    <RouterView />
  </main>
</template>
