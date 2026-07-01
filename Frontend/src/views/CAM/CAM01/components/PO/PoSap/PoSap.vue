<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { BadgeStatus } from '@/components';
import { AccordAcceptor } from '@/components/Accordions';
import { ButtonSave, ButtonSendApprove, ButtonRecall, ButtonSendEdit, ButtonApprove, ButtonConfirm } from '@/components/Button';
import Cam01PoSapConstants from '@/constants/CAM/CAM01/cam01.poSap';
import { useCam01DetailStore } from '@/stores/CAM/CAM01/cam01.detail';
import { useMenuStore } from '@/stores/menu';
import { useCam01PoSapStore } from '@/stores/CAM/CAM01/PO/cam01.poSap';
import { Form } from 'vee-validate';
import { storeToRefs } from 'pinia';
import { defineAsyncComponent, onMounted } from 'vue';
import { AcceptorType } from '@/enums/participants';
import ToastHelper from '@/helpers/toast';
import { showActivityDialog } from '@/helpers/dialog';

const menuStore = useMenuStore();
const amendmentStore = useCam01DetailStore();
const store = useCam01PoSapStore();
const { body, isCanEdit, isCanReCall, isCurrentApprover, isLastApprover } = storeToRefs(store);
const { onGetByIdAsync, onSubmitAsync, onSendApproveAsync, onRecallAsync, onRejectedAsync, onApprovedAsync } = store;

const { Cam01PoSapBadgeStatus } = Cam01PoSapConstants;

const PoSapForm = defineAsyncComponent(() => import('@/views/CAM/CAM01/components/PO/PoSap/Sub/PoSapForm.vue'));

onMounted(async () => {
  if (amendmentStore.body.id) {
    await onGetByIdAsync(amendmentStore.body.id, amendmentStore.body.poSap?.id);
  }
});
</script>

<template>
  <TitleHeader label="ข้อมูลบันทึกต่อท้ายสัญญา">
    <template #action>
      <BadgeStatus :color="Cam01PoSapBadgeStatus(body.status).color" :label="Cam01PoSapBadgeStatus(body.status).label"
        v-if="body.status" />
      <Button v-if="body.id" label="ประวัติการใช้งาน" icon="pi pi-refresh" severity="warn"
        class="hover:bg-yellow-50 bg-white" variant="outlined" @click="() => showActivityDialog(body.id!)" />
    </template>
  </TitleHeader>

  <Form @submit="onSubmitAsync" @invalid-submit="ToastHelper.invalidMessageToast()" v-slot="{ handleSubmit }">
    <div class="grid lg:grid-cols-7 gap-2">
      <div class="lg:col-span-5 order-2 lg:order-1">
        <PoSapForm v-model="body" />
      </div>

      <div class="relative lg:col-span-2 order-1 lg:order-2">
        <div class="flex flex-col gap-4 ml-3 sticky top-[100px]">

          <div id="button-section" class="flex items-center justify-end gap-2" v-if="menuStore.hasManage">

            <div id="edit-section" class="flex items-center gap-2" v-if="isCanEdit">
              <ButtonSave type="submit" />
              <ButtonSendApprove @click="handleSubmit(onSendApproveAsync)" v-if="body.id" />
            </div>

            <div id="approve-section" class="flex items-center gap-2" v-if="isCanReCall || isCurrentApprover">
              <ButtonRecall v-if="isCanReCall" @click="onRecallAsync" />
              <ButtonSendEdit v-if="isCurrentApprover" @click="onRejectedAsync()" />
              <ButtonApprove v-if="!isLastApprover && isCurrentApprover" @click="onApprovedAsync()" />
              <ButtonConfirm v-if="isLastApprover && isCurrentApprover" @click="onApprovedAsync()" />
            </div>

          </div>
          <Accordion :value="['0']" unstyled multiple>
            <AccordionPanel value="0" class="my-5">
              <AccordAcceptor title="ผู้มีอำนาจเห็นชอบ/อนุมัติ" v-model="body.acceptors" is-approve
                :acceptor-type="AcceptorType.Approver" :is-manage="isCanEdit" :is-disable="!isCanEdit" />
            </AccordionPanel>
          </Accordion>
        </div>
      </div>

    </div>
  </Form>
</template>