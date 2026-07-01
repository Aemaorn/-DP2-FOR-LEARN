<script setup lang="ts">
import { Paginator, Select, type PageState } from 'primevue';
import { computed, type ComputedRef } from 'vue';

type Props = {
  pageNumber: number;
  pageSize: number;
  totalRecord?: number;
};

const props = defineProps<Props>();

const emit = defineEmits<{
  (event: 'change', pageNumber: number, pageSize: number): void;
}>();

const onPageChange = (event: PageState): void => {
  emit('change', event.page + 1, event.rows);
};

const onNextPage = (
  event: Event,
  page: number,
  pageCount: number | undefined,
  nextPageCallback: (event: Event) => void
): void => {
  if (page < pageCount!) {
    nextPageCallback(event);
  }
};

const onChangePageNumber = (
  value: number,
  page: number,
  changePageCallback: (value: number) => void
): void => {
  if (page != value) {
    changePageCallback(value);
  }
};

const OnPrevPage = (event: Event, page: number, prevPageCallback: (event: Event) => void): void => {
  if (page != 0) {
    prevPageCallback(event);
  }
};

const startPage = (pageCount: number, currentPage: number): ComputedRef<number[]> => {
  return computed(() => {
    const maxVisible = 5;
    const half = Math.floor(maxVisible / 2);
    let start = Math.max(1, currentPage - half);
    let end = start + maxVisible - 1;

    if (end > pageCount) {
      end = pageCount;
      start = Math.max(1, end - maxVisible + 1);
    }

    return Array.from({ length: end - start + 1 }, (_, i) => start + i);
  });
};
</script>

<template>
  <Paginator :rows="props.pageSize" :totalRecords="props.totalRecord ?? 0"
    :first="(props.pageNumber - 1) * props.pageSize" :rowsPerPageOptions="[10, 20, 50, 100]" @page="onPageChange">
    <template #container="{
      rows,
      page,
      pageCount,
      totalRecords,
      firstPageCallback,
      lastPageCallback,
      prevPageCallback,
      nextPageCallback,
      rowChangeCallback,
      changePageCallback,
    }">
      <div class="w-full flex flex-wrap items-center-safe justify-between px-2 my-1 gap-2">
        <div id="start" class="flex items-center-safe gap-2 order-2 lg:order-1 w-full lg:w-auto justify-center lg:justify-start">
          <p class="text-gray-400">แสดง</p>
          <Select class="text-xl" :model-value="rows" @update:model-value="(e: number) => rowChangeCallback(e)"
            :options="[10, 20, 50, 100]" />
          <p class="text-gray-400">รายการ จากทั้งหมด {{ totalRecords }} รายการ</p>
        </div>
        <div class="flex items-center-safe gap-2 order-1 lg:order-2 w-full lg:w-auto justify-center lg:justify-end">
          <i class="pi pi-angle-double-left border-1 rounded-lg border-gray-400 hover:border-primary text-gray-400 hover:text-primary p-1 px-2 text-xl!"
            @click="firstPageCallback" />
          <i class="pi pi-angle-left border-1 rounded-lg border-gray-400 hover:border-primary text-gray-400 hover:text-primary p-1 px-2 text-xl!"
            @click="(e: any) => OnPrevPage(e, page, prevPageCallback)" />

          <template v-if="pageCount">
            <Button v-for="pageNum in startPage(pageCount, page + 1).value" :key="pageNum" class="text-md!"
              :class="`${pageNum == page + 1 ? 'bg-primary! border-primary!' : 'text-gray-400! hover:text-primary! focus:text-primary! hover:bg-secondary! focus:bg-secondary!'}`"
              @click="onChangePageNumber(pageNum - 1, page, changePageCallback)"
              :variant="`${pageNum == page + 1 ? '' : 'text'}`">
              {{ pageNum }}
            </Button>
          </template>

          <i class="pi pi-angle-right border-1 rounded-lg border-gray-400 hover:border-primary text-gray-400 hover:text-primary p-1 px-2 text-xl!"
            @click="(e: any) => onNextPage(e, page + 1, pageCount, nextPageCallback)" />
          <i class="pi pi-angle-double-right border-1 rounded-lg border-gray-400 hover:border-primary text-gray-400 hover:text-primary p-1 px-2 text-xl!"
            @click="lastPageCallback" />
        </div>
      </div>
    </template>
  </Paginator>
</template>
