<script setup lang="ts">
import type { MenuItem } from 'primevue/menuitem';
import { TitleHeader } from '@/components/cosmetic';
import { Form } from 'vee-validate';
import { computed, onMounted, onUnmounted, ref, watch } from 'vue';
import { InputField, InputNumber, Select } from '@/components/forms';
import { useSt006DetailStore, useSt006ListStore } from '@/stores/ST/st006';
import { useRoute } from 'vue-router';
import ToastHelper from '@/helpers/toast';

const route = useRoute();
const listStore = useSt006ListStore();
const store = useSt006DetailStore();

const routeItems = ref(
  [
    { label: 'พารามิเตอร์', url: '/st/st006' },
    { label: 'รายละเอียดพารามิเตอร์' },
  ] as MenuItem[],
);

const id = computed((): string => route.params.id as string);

onMounted(async (): Promise<void> => {
  await listStore.onGetGroupDropdownAsync();

  if (id.value) {
    await store.getDetailAsync(id.value);
  }
});

onUnmounted((): void => {
  store.clearData();
});

const onSubmitAsync = async (): Promise<void> => {
  if (id.value) {
    return await store.updateAsync(id.value);
  }

  await store.createAsync();
};

watch((): string => store.body.group, async (groupCode): Promise<void> => {
  const findData = listStore.groupDropdown.find((g): boolean => g.value === groupCode);

  if (findData) {
    await Promise.all([
      listStore.onGetSubGroupDropdownAsync(findData.id),
      listStore.onGetParentDropdownAsync(groupCode, id.value || undefined),
    ]);

    if (!id.value) {
      await store.setDefaultsAsync(findData.id, store.body.parentId);
    }

    return;
  }

  listStore.subGroupDropdown = [];
  listStore.parentDropdown = [];
});

watch((): string | null | undefined => store.body.parentId, async (parentId): Promise<void> => {
  if (id.value) return;

  const findGroup = listStore.groupDropdown.find((g): boolean => g.value === store.body.group);

  if (!findGroup) return;

  await store.setDefaultsAsync(findGroup.id, parentId);
});
</script>

<template>
  <Form @submit="onSubmitAsync" @invalidSubmit="ToastHelper.invalidMessageToast()">
    <TitleHeader label="รายละเอียดพารามิเตอร์ในระบบ" :route-items="routeItems">
      <template #breadcrumbAction>
        <div class="flex gap-2">
          <Button label="บันทึก" icon="pi pi-save" severity="success" type="submit" />
        </div>
      </template>
    </TitleHeader>
    <Card>
      <template #content>
        <div class="grid lg:grid-cols-4 gap-2 gap-y-8 p-2 mt-8">
          <Select :options="listStore.groupDropdown" label="กลุ่ม" rules="required" :disabled="id != ''"
            v-model="store.body.group" />
          <Select :options="listStore.parentDropdown" label="Parameter หลัก" :disabled="!store.body.group"
            v-model="store.body.parentId" />
          <InputNumber class="lg:col-start-1" label="ลำดับ" rules="required" v-model="store.body.sequence" />
          <InputField class="lg:col-start-1" label="Code" rules="required" v-model.trim="store.body.code"
            :disabled="id != ''" />
          <InputField label="Name" rules="required" v-model="store.body.name" />
          <div class="lg:col-start-1">
            <Checkbox label="ใช้งาน" v-model="store.body.isActive" />
          </div>
        </div>
        <Divider />

        <div class="flex items-center justify-end">
          <Button label="เพิ่มค่าพารามิเตอร์" icon="pi pi-plus" severity="warn" class="mb-5" variant="outlined"
            @click="store.addParameterValue" />
        </div>
        <div v-for="(data, index) in store.body.parameters.filter((_, i) => i < 5)" :key="index"
          class="w-full flex gap-2 px-2">
          <div class="flex gap-5 items-center">
            <p class="text-center mb-6">{{ index + 1 }}</p>
            <InputField label="Key" v-model="data.key" class="md:w-[20rem]" rules="required" />
          </div>
          <InputField label="Value" v-model="data.value.value" class="md:w-[20rem]" rules="required" />
          <i class="pi pi-trash mt-3 ml-5 text-red-600 cursor-pointer" @click="() => store.removeParameterValue(index)"
            v-if="index > 0" />
        </div>

      </template>
    </Card>
  </Form>
</template>
