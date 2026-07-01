<script setup lang="ts">
import type { Option } from '@/models/shared/option';
import { Tabs } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import { defineAsyncComponent, ref } from 'vue';
import { usePcm005PrincipleStore } from '@/stores/PCM/PCM005/principle';
import { useMenuStore } from '@/stores/menu';

const ResultInformation = defineAsyncComponent(() => import('./ResultInformation.vue'));
const ProjectReturn = defineAsyncComponent(() => import('./ProjectReturn.vue'));

const menuStore = useMenuStore();
const store = usePcm005PrincipleStore();
const { onImportConsiderationsAsync, onExportConsiderationAsync } = store;

const fileRef = ref();

const HeaderItem = ref([
  {
    label: 'ข้อมูลประกอบผลการดำเนินงาน',
    value: '0',
  },
  {
    label: 'ผลตอบแทนโครงการ',
    value: '1',
  },
] as Option[]);


const onSelectedFile = async (event: HTMLInputElement) => {
  if (event.files?.length === 0) return;
  const fileListType = event.files as FileList;

  const selectedFile = Array.from(fileListType)[0];

  if (selectedFile) {
    await onImportConsiderationsAsync(selectedFile);
  }

  event.value = '';
};
</script>

<template>
  <Card>
    <template #content>
      <TitleHeader label="รายละเอียดข้อมูลประกอบการพิจารณา">
        <template #action v-if="store.status.canEdit && menuStore.hasManage">
          <Button label="Export" icon="pi pi-file-export" severity="danger" variant="outlined"
            @click="onExportConsiderationAsync(store.body.id)" />
          <Button label="Import" icon="pi pi-file-import" severity="warn" @click="fileRef.click()" />
          <input type="file" class="hidden" ref="fileRef"
            accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            @change="(e) => onSelectedFile(e.target as HTMLInputElement)" />
        </template>
      </TitleHeader>
      <Tabs value="0" unstyled>
        <TabHeader :items="HeaderItem" />
        <TabPanels>
          <TabPanel value="0">
            <ResultInformation />
          </TabPanel>
          <TabPanel value="1">
            <ProjectReturn />
          </TabPanel>
        </TabPanels>
      </Tabs>
    </template>
  </Card>
</template>

<style lang="scss" scoped>
:deep(th),
:deep(td) {
  background-color: oklch(96.7% 0.003 264.542);
}
</style>
