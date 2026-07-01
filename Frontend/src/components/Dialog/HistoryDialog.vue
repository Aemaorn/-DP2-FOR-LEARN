<script setup lang="ts">
import type { ActivityHistory } from '@/models/shared/dialog';
import { Dialog } from 'primevue';
import { ToDateTime } from '@/helpers/dateTime';
import { onMounted, ref, watch } from 'vue';
import { useEventListener } from '@vueuse/core';
import { HttpStatusCode } from 'axios';
import { useRoute } from 'vue-router';
import ActivitylogService from '@/services/Shared/activitylog';

const isShow = ref(false);
const route = useRoute();
const programName = ref<string | undefined>(undefined);
const dialogTitle = ref('ประวัติการใช้งาน');

const item = ref<Array<ActivityHistory>>([]);

onMounted(() => {
  useEventListener(document, 'onShowActivityDialog', async (event: CustomEvent<{ id: string; programName?: string; title?: string }>): Promise<void> => {
    const { data, status } = await ActivitylogService.onGetActivityLogByIdAsync(event.detail.id, event.detail.programName);

    if (status === HttpStatusCode.Ok) {
      item.value = data;
    }

    programName.value = event.detail.programName;
    dialogTitle.value = event.detail.title ?? 'ประวัติการใช้งาน';
    isShow.value = true;
  });
});

const onAfterClose = () => {
  item.value = [];
  programName.value = undefined;
};

const showHideSub = (index: number) => {
  item.value[index].showExpand = !item.value[index].showExpand;
};

watch(() => route.path, () => {
  isShow.value = false;
});
</script>

<template>
  <Dialog v-model:visible="isShow" modal :draggable="false" :style="{ width: '75vw' }"
    :breakpoints="{ '1199px': '80vw', '575px': '90vw' }" maximizable :close-on-escape="false" @after-hide="onAfterClose"
    @keydown.enter.prevent>
    <template #container="{ closeCallback, maximizeCallback }">
      <div class="h-full overflow-y-auto hide-scrollbar">
        <div class="sticky top-0 bg-white z-10 rounded-lg">
          <div class="p-4">
            <div class="flex gap-2 justify-between items-center">
              <div class="flex gap-2 md:gap-4 items-center w-full">
                <div class="h-4 md:h-7 w-3 md:w-6 bg-primary transform -skew-x-12" />
                <h6 class="font-bold">{{ dialogTitle }}{{ programName ? ` - ${programName}` : '' }}</h6>
                <div class="h-px bg-gray-300 flex-1" />
              </div>
              <div class="flex items-center gap-2">
                <span
                  class="material-symbols-outlined text-gray-500 border-[0.5px] border-gray-500 rounded-md cursor-pointer"
                  @click="maximizeCallback">
                  expand_content
                </span>
                <span class="material-symbols-outlined cursor-pointer" @click="closeCallback">
                  close
                </span>
              </div>
            </div>
          </div>
        </div>
        <div class="p-4 px-5">
          <div class="grid grid-cols-12">
            <p class="header col-span-1" />
            <p class="header col-span-2">วันที่</p>
            <p class="header col-span-3">ผู้ดำเนินการ</p>
            <p class="header col-span-3">การดำเนินการ</p>
            <p class="header col-span-3">หมายเหตุ</p>
          </div>
          <Divider />
          <div v-if="item.length > 0">
            <div v-for="(data, index) in item" :key="data.groupName">
              <div class="grid grid-cols-12 items-center">
                <i class="text-center col-span-1 cursor-pointer"
                  :class="data.showExpand ? 'pi pi-chevron-down' : 'pi pi-chevron-right'"
                  @click="() => showHideSub(index)" v-if="data.activityLogs && data.activityLogs.length > 1" />
                <p v-else class="col-span-1" />
                <p class="text-center col-span-2">{{ ToDateTime(data.lastedActivity.createdAt) }}</p>
                <p class="col-span-3">{{ data.lastedActivity.createdByName }}</p>
                <p class="col-span-3">{{ data.lastedActivity.activityType }}</p>
                <p class="col-span-3">{{ data.lastedActivity.activityRemark }}</p>
              </div>
              <Divider />
              <div v-if="data.showExpand">
                <div v-for="(sub, index) in data.activityLogs.filter(a => a.id != data.lastedActivity.id)" :key="sub.id">
                  <div class="grid grid-cols-12 items-center">
                    <p class="text-center col-span-1" />
                    <p class="text-center col-span-2">{{ ToDateTime(sub.createdAt) }}</p>
                    <p class="col-span-3">{{ sub.createdByName }}</p>
                    <p class="col-span-3">{{ sub.activityType }}</p>
                    <p class="col-span-3">{{ sub.activityRemark }}</p>
                  </div>
                  <Divider v-if="index === data.activityLogs.filter(a => a.id != data.lastedActivity.id).length - 1" />
                  <div v-else class="my-4" />
                </div>
              </div>
            </div>
          </div>
          <p class="text-center" v-else>ไม่พบข้อมูล</p>
        </div>
      </div>
    </template>
  </Dialog>
</template>

<style scoped lang="scss">
.header {
  text-align: center;
  color: var(--color-primary);
  font-weight: bold;
}
</style>
