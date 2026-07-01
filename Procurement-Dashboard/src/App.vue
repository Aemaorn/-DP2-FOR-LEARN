<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import Toast from 'primevue/toast'
import ConfirmDialog from 'primevue/confirmdialog'
import { useProcurementStore } from '@/stores/procurement'

const router = useRouter()
const route = useRoute()
const store = useProcurementStore()

const navItems = [
  { label: 'Overview', icon: 'pi pi-home', route: '/' },
  { label: 'ติดตามสถานะ', icon: 'pi pi-list-check', route: '/tracking' },
]

onMounted(() => store.fetchAll())
</script>

<template>
  <div class="min-h-screen bg-stone-50">
    <Toast position="top-right" />
    <ConfirmDialog />

    <header class="bg-white border-b border-stone-200">
      <div class="max-w-[1400px] mx-auto px-6 py-3 flex items-center justify-between">
        <div class="flex items-center gap-3">
          <div class="w-8 h-8 bg-amber-500 rounded-lg flex items-center justify-center">
            <i class="pi pi-building text-white text-sm"></i>
          </div>
          <div>
            <h1 class="text-sm font-semibold text-stone-800 leading-tight">GHB Procurement</h1>
            <p class="text-[11px] text-stone-400">ระบบติดตามสถานะการจัดซื้อจัดจ้าง</p>
          </div>
        </div>
        <nav class="flex gap-1">
          <button
            v-for="item in navItems"
            :key="item.route"
            :class="[
              'px-3 py-1.5 rounded-md text-xs font-medium transition-colors',
              route.path === item.route
                ? 'bg-amber-500 text-white'
                : 'text-stone-500 hover:bg-amber-50'
            ]"
            @click="router.push(item.route)"
          >
            <i :class="[item.icon, 'sm:mr-1.5 text-[11px] sm:text-[11px] text-sm']"></i>
            <span class="hidden sm:inline">{{ item.label }}</span>
          </button>
        </nav>
      </div>
    </header>

    <main>
      <!-- Loading -->
      <div v-if="store.loading" class="flex items-center justify-center py-20">
        <div class="text-center">
          <i class="pi pi-spin pi-spinner text-2xl text-amber-500 mb-3"></i>
          <p class="text-sm text-stone-400">กำลังโหลดข้อมูล...</p>
        </div>
      </div>

      <!-- Error -->
      <div v-else-if="store.error" class="flex items-center justify-center py-20">
        <div class="text-center max-w-md">
          <i class="pi pi-exclamation-triangle text-3xl text-rose-400 mb-3"></i>
          <p class="text-sm text-stone-600 mb-4">{{ store.error }}</p>
          <button
            class="px-4 py-2 bg-amber-500 text-white text-xs font-medium rounded-md hover:bg-amber-600 transition-colors"
            @click="store.fetchAll()"
          >
            ลองใหม่อีกครั้ง
          </button>
        </div>
      </div>

      <!-- Content -->
      <router-view v-else />
    </main>
  </div>
</template>
