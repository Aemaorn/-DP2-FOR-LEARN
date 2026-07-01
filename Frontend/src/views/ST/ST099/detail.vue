<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue';
import { useRoute } from 'vue-router';
import { Form } from 'vee-validate';
import type { MenuItem } from 'primevue/menuitem';
import { InputField, InputNumber, Select } from '@/components/forms';
import { TitleHeader } from '@/components/cosmetic';
import ToastHelper from '@/helpers/toast';
import { useST099Store } from '@/stores/ST/st099';
import draggable from 'vuedraggable';
import type { SuSectionApprover } from '@/models/ST/st099';
import { getSectionProcessTypeOptions, getDefaultSectionProcessType } from '@/helpers/sectionProcessType';

const routeItems = ref([
  { label: 'รายการคำสั่ง', url: '/st/st099' },
  { label: 'ข้อมูลคำสั่ง' },
] as MenuItem[]);

const route = useRoute();

const detailStore = useST099Store();
const sectionProcessTypeOptions = ref(getSectionProcessTypeOptions());

onMounted(async (): Promise<void> => {
  if (route.params.id) {
    await detailStore.onGeByIdAsync(route.params.id as string);
  }
});

onUnmounted((): void => {
  detailStore.onResetBody();
});

const onSubmitAsync = async (): Promise<void> => {
  if (route.params.id) {
    await detailStore.onUpdate(route.params.id.toString());

    return;
  }

  await detailStore.onCreate();
};

const addNewApprover = (): void => {
  const newApprover: SuSectionApprover = {
    processType: getDefaultSectionProcessType(),
    positionName: '',
    shortPositionName: '',
    inRefCode: '',
    budget: 0,
    sectionId: '',
    commandText: '',
  };

  detailStore.body.approvers.push(newApprover);
};

const removeApprover = async (index: number): Promise<void> => {
  detailStore.body.approvers.splice(index, 1);
};
</script>

<template>
  <Form class="pb-10" @submit="onSubmitAsync" @invalidSubmit="ToastHelper.invalidMessageToast()">
    <TitleHeader
      :label="route.params.id ? 'แก้ไขข้อมูลคำสั่ง' : 'เพิ่มข้อมูลคำสั่ง'"
      :route-items="routeItems"
    >
      <template #breadcrumbAction>
        <Button label="บันทึก" icon="pi pi-save" severity="success" type="submit" />
      </template>
    </TitleHeader>
    <Card class="my-4">
      <template #content>
        <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
          <InputField label="รหัส" v-model="detailStore.body.newId" />
          <InputField label="คำสั่ง" v-model="detailStore.body.refBankOrder" />
          <InputNumber label="วงเงิน" v-model="detailStore.body.maximumBudget" :min-fraction-digits="2" grouping/>
          <InputField label="คำอธิบาย" v-model="detailStore.body.remark" />
          <InputField label="เงื่อนไข 1" v-model="detailStore.body.supplyMethodCode" />
          <InputField label="เงื่อนไข 2" v-model="detailStore.body.supplyMethodSpecialTypeCode" />
        </div>
      </template>
    </Card>

    <Card class="my-4">
      <template #content>
        <div class="w-full flex justify-end">
          <Button
            label="เพิ่มรายการ"
            icon="pi pi-plus"
            severity="warn"
            variant="outlined"
            class="hover:bg-yellow-50 bg-white"
            @click="addNewApprover"
          />
        </div>
        <draggable
          v-model="detailStore.body.approvers"
          group="approvers"
          handle=".drag-data"
          itemKey="id"
        >
          <template #item="{ element: data, index }: { element: SuSectionApprover, index: number }">
            <Card class="my-4">
              <template #content>
                <div class="grid grid-cols-1 md:grid-cols-5 gap-4">
                  <Select label="ประเภทกระบวนการ" v-model="data.processType" :options="sectionProcessTypeOptions" />
                  <InputField label="positionName" v-model="data.positionName" />
                  <InputField label="shortPositionName" v-model="data.shortPositionName" />
                  <InputField label="inRefCode" v-model="data.inRefCode" />
                  <div class="flex items-center justify-end">
                    <Button
                      icon="pi pi-trash"
                      class="text-red-600! hover:bg-red-300/20! focus:bg-red-300/20!"
                      size="small"
                      variant="text"
                      @click="removeApprover(index)"
                    />
                    <span class="material-symbols-outlined drag-data cursor-move">drag_indicator</span>
                  </div>
                  <InputNumber class="col-start-1" label="budget" v-model="data.budget" :min-fraction-digits="2" grouping/>
                  <InputField label="sectionId" v-model="detailStore.body.newId" disabled />
                  <InputField label="commandText" v-model="data.commandText" />
                </div>
              </template>
            </Card>
          </template>
        </draggable>
      </template>
    </Card>
  </Form>
</template>

<style scoped>
.center {
  display: flex;
  align-items: center;
  justify-content: center;
}

.underline {
  text-decoration: underline;
}

.pointer {
  cursor: pointer;
}

.grab {
  cursor: grab;
}
</style>
