<script setup lang="ts">
import { ButtonSave, ButtonClose, ButtonCancelClose } from '@/components/Button';
import { showReasonDialogAsync, showConfirmDialogAsync } from '@/helpers/dialog';
import { ReasonDialogType, ConfirmDialogType } from '@/enums/dialog';
import { TitleHeader } from '@/components/cosmetic';
import { InputArea, InputField, Select } from '@/components/forms';
import { PrincipleStatus } from '@/enums/PCM005/principle';
import { ProcurementStatus } from '@/enums/procurement';
import ToastHelper from '@/helpers/toast';
import { useMenuStore } from '@/stores/menu';
import { usePcm005DetailStore } from '@/stores/PCM/PCM005/pcm005';
import { Button, Card } from 'primevue';
import { Form } from 'vee-validate';
import { computed, defineAsyncComponent, ref } from 'vue';

const EditProcurementDialog = defineAsyncComponent(() => import('./EditProcurementDialog.vue'));

const store = usePcm005DetailStore();
const menuStore = useMenuStore();

const showEditDialog = ref(false);

// แก้ไขข้อมูล Procurement ได้เมื่อมีสิทธิ์จัดการ และยังไม่มีข้อมูลขออนุมัติหลักการ
// หรือขออนุมัติหลักการอยู่ในสถานะ Draft / Edit / Rejected
const canEditProcurement = computed(() => {
  if (!menuStore.hasManage) return false;
  if (store.body.status === ProcurementStatus.Cancelled) return false;

  const principle = store.body.principleApproval;

  if (!principle) return true;

  return [PrincipleStatus.Draft, PrincipleStatus.Edit, PrincipleStatus.Rejected]
    .includes(principle.status as PrincipleStatus);
});

const onSubmitAsync = async (): Promise<void> => {
  if (store.body.id) {
    await store.updateAsync(store.body.id);

    return;
  }

  await store.createAsync();
};

const onConfirmAsync = async () => {
  if (!await showConfirmDialogAsync(ConfirmDialogType.ConfirmData)) return;

  if (store.body.id) {
    await store.updateAsync(store.body.id, ProcurementStatus.InProgress);

    return;
  }

  await store.createAsync(ProcurementStatus.InProgress);
}

const onCloseProcurementAsync = async (): Promise<void> => {
  const res = await showReasonDialogAsync(ReasonDialogType.ClosePlan, true);
  if (res.isConfirm) {
    await store.onCloseAsync(res.reason ?? '', res.attachments);
  }
};

const onCancelCloseProcurementAsync = async (): Promise<void> => {
  const isConfirm = await showConfirmDialogAsync(ConfirmDialogType.CancelClosePlan);
  if (isConfirm) {
    await store.onCancelCloseAsync();
  }
};

const onChangeSMCodeAsync = async (code: string): Promise<void> => {
  store.smSpTypeCodeDDL = [];

  await store.getsmSpTypeCodeDDLAsync(code);


  store.body = {
    ...store.body,
    supplyMethodTypeCode: undefined,
    supplyMethodSpecialTypeCode: undefined,
  };
};
</script>

<template>
  <Card>
    <template #content>
      <Form @submit="onSubmitAsync" @invalidSubmit="ToastHelper.invalidMessageToast()" v-slot="{ handleSubmit }">
        <TitleHeader label="ข้อมูลเช่าพื้นที่/อาคาร/ที่จอดรถ/ป้าย">
          <template #action>
            <div class="flex gap-2 items-center" v-if="store.states.isDraft">
              <ButtonSave type="submit" />
              <Button label="ยืนยัน" icon="pi pi-check" severity="success" variant="outlined"
                @click="() => handleSubmit(onConfirmAsync)" />
            </div>
            <i v-else-if="canEditProcurement" class="pi pi-pencil cursor-pointer text-primary"
              @click="showEditDialog = true"></i>
          </template>
        </TitleHeader>
        <div class="grid grid-cols-1 lg:grid-cols-4 gap-2 gap-y-8 mt-8">
          <InputField label="เลขที่" v-model="store.body.procurementNumber" disabled />
          <Select label="ฝ่าย/ภาคเขต" :options="store.departmentDDL" rules="required"
            v-model="store.body.departmentCode" :disabled="!store.states.isDraft" />
          <InputArea label="เรื่อง" rules="required" class="lg:col-span-3 lg:col-start-1 col-start-auto"
            v-model="store.body.planName" :disabled="!store.states.isDraft" />
          <Select label="วิธีจัดหา" :options="store.smCodeDDL" class="col-start-auto lg:col-start-1"
            v-model="store.body.supplyMethodCode" @on-select="onChangeSMCodeAsync" rules="required" disabled />
          <Select :options="store.smTypeCodeDDL" v-model="store.body.supplyMethodTypeCode" disabled />
          <Select :options="store.smSpTypeCodeDDL" v-model="store.body.supplyMethodSpecialTypeCode" disabled />
        </div>
        <div v-if="store.body.remarkClosed" class="mt-4">
          <InputArea label="หมายเหตุการปิดงาน" v-model="store.body.remarkClosed" disabled />
        </div>
        <div v-if="store.canCloseProcurement" class="flex justify-end mt-4">
          <ButtonClose @click="onCloseProcurementAsync" />
        </div>
        <div v-if="store.canCancelCloseProcurement" class="flex justify-end mt-4">
          <ButtonCancelClose @click="onCancelCloseProcurementAsync" />
        </div>
      </Form>
      <EditProcurementDialog v-model:show="showEditDialog" />
    </template>
  </Card>
</template>
