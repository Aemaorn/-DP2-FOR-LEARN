<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { UploadFileGroup } from '@/components/forms';
import { Cam01Status } from '@/enums/CAM/CAM01/cam01';
import { useCam01DetailStore } from '@/stores/CAM/CAM01/cam01.detail';
import { useMenuStore } from '@/stores/menu';
import { storeToRefs } from 'pinia';
import { defineAsyncComponent } from 'vue';

const menuStore = useMenuStore();
const store = useCam01DetailStore();
const { routeItems, body } = storeToRefs(store);
const { onUpsertAttachments } = store;

const Detail = defineAsyncComponent(() => import('@/views/CAM/CAM01/components/Detail.vue'));
</script>

<template>
  <TitleHeader label="จัดการบันทึกต่อท้ายสัญญา" :routeItems />
  <Detail />
  <div class="grid grid-cols-1 lg:grid-cols-6 gap-2 z-50">
    <div class="lg:col-span-4 mb-5 my-1">
      <UploadFileGroup v-if="body.id" v-model="body.attachments" @upload="onUpsertAttachments"
        @remove-file="onUpsertAttachments" @remove-group="onUpsertAttachments" @reorder="onUpsertAttachments"
        :disabled="body.status === Cam01Status.Completed || !menuStore.hasManage" />
    </div>
  </div>
</template>