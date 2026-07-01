<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { onMounted, ref, computed } from 'vue';
import { useMappingDocumentDialogStore } from '@/stores/Shared/mappingDocumentDialog';

const searchText = ref('');

const emit = defineEmits<{
  (e: 'onClickSelect', text: string, hint?: string): void;
}>();

const props = defineProps<{
  pathToGet: string;
}>();

const mappingDocumentStore = useMappingDocumentDialogStore();

onMounted(async () => {
  await mappingDocumentStore.onGetMappingDocumentAsync(props.pathToGet);
});

const onClickText = (text: string, hint?: string) => {
  emit('onClickSelect', text, hint);
};

const filteredData = computed(() => {
  const allData = [
    ...(mappingDocumentStore.criteria.single || []).map(item => ({ ...item, type: 'เดี่ยว' })),
    ...(mappingDocumentStore.criteria.multiple || []).map(item => ({ ...item, type: 'หลายรายการ' }))
  ];

  if (!searchText.value) {
    return allData;
  }

  const searchLower = searchText.value.toLowerCase();
  return allData.filter(item =>
    item.key?.toLowerCase().includes(searchLower) ||
    item.description?.toLowerCase().includes(searchLower) ||
    item.type.toLowerCase().includes(searchLower)
  );
});
</script>

<template>
  <TitleHeader label="ข้อมูลเอกสาร"> </TitleHeader>

  <DataTable
    :value="filteredData"
    showGridlines
    stripedRows
    tableStyle="min-width: 50rem"
    class="overflow-auto text-[24px]"
  >
    <template #header>
      <div class="flex justify-between">
        <b class="text-[24px]">ข้อมูลเอกสาร</b>
        <IconField>
          <InputIcon>
            <i class="pi pi-search" />
          </InputIcon>
          <InputText v-model="searchText" placeholder="ค้นหา" />
        </IconField>
      </div>
    </template>
    <template #empty> ไม่เจอข้อมูล. </template>
    <template #loading> โหลดข้อมูล โปรดรอสักครู่. </template>
    <Column field="type" header="ประเภท" style="width: 120px">
      <template #body="slotProps">
        <Tag
          :value="slotProps.data.type"
          :severity="slotProps.data.type === 'เดี่ยว' ? 'success' : 'info'"
        />
      </template>
    </Column>
    <Column field="key" header="Key">
      <template #body="slotProps">
        <a
          class="cursor-pointer underline"
          @click="onClickText(slotProps.data.key, slotProps.data.description)"
          >{{ slotProps.data.key }}</a
        >
      </template>
    </Column>
    <Column field="description" header="Description">
      <template #body="slotProps">
        <div>
          {{ slotProps.data.description }}
        </div>
      </template>
    </Column>
  </DataTable>
</template>
