import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { ProcurementItem } from '@/types/procurement'
import { seedBank, seedOther } from '@/data/seed'

export const useProcurementStore = defineStore('procurement', () => {
  const bankItems = ref<ProcurementItem[]>([])
  const otherItems = ref<ProcurementItem[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  function fetchAll() {
    if (bankItems.value.length === 0) bankItems.value = seedBank
    if (otherItems.value.length === 0) otherItems.value = seedOther
  }

  function nextId() {
    const all = [...bankItems.value, ...otherItems.value]
    return all.length === 0 ? 1 : Math.max(...all.map(i => i.id)) + 1
  }

  function addBankItem(item: Omit<ProcurementItem, 'id'>) {
    bankItems.value.push({ ...item, id: nextId() })
  }

  function addOtherItem(item: Omit<ProcurementItem, 'id'>) {
    otherItems.value.push({ ...item, id: nextId() })
  }

  function updateBankItem(item: ProcurementItem) {
    const idx = bankItems.value.findIndex(i => i.id === item.id)
    if (idx !== -1) bankItems.value.splice(idx, 1, { ...item })
  }

  function updateOtherItem(item: ProcurementItem) {
    const idx = otherItems.value.findIndex(i => i.id === item.id)
    if (idx !== -1) otherItems.value.splice(idx, 1, { ...item })
  }

  function removeBankItem(id: number) {
    bankItems.value = bankItems.value.filter(i => i.id !== id)
  }

  function removeOtherItem(id: number) {
    otherItems.value = otherItems.value.filter(i => i.id !== id)
  }

  return {
    bankItems,
    otherItems,
    loading,
    error,
    fetchAll,
    addBankItem,
    addOtherItem,
    updateBankItem,
    updateOtherItem,
    removeBankItem,
    removeOtherItem,
  }
}, {
  persist: true,
})
