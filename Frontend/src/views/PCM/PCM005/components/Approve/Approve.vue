<script setup lang="ts">
import { BadgeStatus as BadgeStatusComponent } from '@/components';
import { TitleHeader } from '@/components/cosmetic';
import { ButtonSendApprove, ButtonConfirmAssign } from '@/components/Button';
import { AccordAssignee, AccordAcceptor } from '@/components/Accordions';
import { Pcm005ApproveAccordionType } from '@/enums/PCM005/approve';
import PoaConstant from '@/constants/poa';
import ToastHelper from '@/helpers/toast';
import { PcmApproveHelper } from '@/helpers/pcm';
import { usePcm005DetailStore } from '@/stores/PCM/PCM005/pcm005';
import { usePcm005ApproveStore } from '@/stores/PCM/PCM005/approve';
import { Form as VeeForm } from 'vee-validate';
import { defineAsyncComponent, onBeforeMount, onMounted, watch } from 'vue';
import { AcceptorType, AssigneeGroup } from '@/enums/participants';

const props = defineProps({
  readonly: { type: Boolean, default: false },
});
import { showActivityDialog } from '@/helpers/dialog';
import { useMenuStore } from '@/stores/menu';

const Condition = defineAsyncComponent(() => import('./Sub/Condition.vue'));
const ProcurementBudget = defineAsyncComponent(() => import('./Sub/ProcurementBudget.vue'));

const { poaStatusColor } = PoaConstant;
const { AccordionName } = PcmApproveHelper;

const menuStore = useMenuStore();
const pcmStore = usePcm005DetailStore();
const store = usePcm005ApproveStore(pcmStore.body.id!);

onBeforeMount(() => {
  store.fn.resetBody();
})

onMounted(async () => {
  await store.fn.onGetByIdAsync(pcmStore.body.purchaseOrderApproval?.id);

  await store.fn.onSetDefaultEntrepreneursAsync();

  if (!store.body.acceptors || store.body.acceptors.length === 0) {
    await store.fn.onSetDefaultAcceptors();
  }
});

watch(() => store.body.contractType, async (newVal) => {
  if (newVal == "CType001") {
    if (!store.body.assignees || store.body.assignees.length === 0) {
      await store.fn.onGetDefaultSegmentContractManagerAsync();
    }
  }
});
</script>

<template>
  <TitleHeader label="อนุมัติใบสั่งเช่า และแจ้งทำสัญญา">
    <template #action>
      <BadgeStatusComponent :color="poaStatusColor(store.body.status).color"
        :label="poaStatusColor(store.body.status).label" />
      <Button label="ประวัติการใช้งาน" icon="pi pi-refresh" variant="outlined" severity="primary" v-if="store.body.id"
        class="bg-white! hover:bg-red-50!" @click="() => showActivityDialog(store.body.id!)" />
    </template>
  </TitleHeader>

  <VeeForm @submit="store.fn.onSubmitAsync" @invalidSubmit="ToastHelper.invalidMessageToast()"
    v-slot="{ handleSubmit }">
    <div class="grid grid-cols-1 lg:grid-cols-7 gap-2">
      <div class="lg:col-span-5 order-2 lg:order-1">
        <Condition v-model="store.body" :disabled="!store.states.isEdit || props.readonly" />
        <div v-if="store.body.contractBudgetGroups">
          <div v-for="(_, index) in store.body.contractBudgetGroups" :key="index">
            <ProcurementBudget title="วงเงินที่จัดซื้อจัดจ้าง" v-model="store.body.contractBudgetGroups[index]"
              :disabled="!store.states.isEdit || !menuStore.hasManage || props.readonly" />
          </div>
        </div>
      </div>

      <div class="lg:col-span-2 relative order-1 lg:order-2">
        <div class="flex flex-col gap-4 lg:ml-3 sticky top-[100px]">
          <div v-if="menuStore.hasManage && !props.readonly">
            <div class="flex items-center justify-end gap-2" v-if="store.states.isEdit">
              <Button label="บันทึก" icon="pi pi-save" severity="success" type="submit" />

              <ButtonSendApprove v-if="store.body.id && store.states.isEdit"
                @click="() => handleSubmit(() => store.fn.onSendApprovalAsync())" />
            </div>

            <div class="flex items-center justify-end gap-2"
              v-if="store.states.isCurrentApproval || store.states.isRecall">
              <ButtonRecall v-if="store.states.isRecall" @click="() => store.fn.onRecallAsync()" />

              <ButtonSendEdit v-if="store.states.isCurrentApproval || store.states.isPermissionAssign"
                @click="() => store.fn.onApproveRejectAsync('Reject')" />

              <ButtonApprove v-if="store.states.isCurrentApproval && !store.states.isLastApproval"
                @click="() => store.fn.onApproveRejectAsync('Approve')" />

              <ButtonConfirm v-if="store.states.isCurrentApproval && store.states.isLastApproval"
                @click="() => store.fn.onApproveRejectAsync('Approve')" />
            </div>
            <div class="flex items-center justify-end gap-2">
              <ButtonSave :text="'บันทึกผู้รับผิดชอบ'" @click="store.fn.onSubmitAsync"
                v-if="store.states.canAssignedApprove || store.states.isPermissionAssign" />
              <ButtonConfirmAssign @click="() => store.fn.onAssignedAsync()" v-if="store.states.isPermissionAssign" />
            </div>
          </div>

          <Accordion :value="Object.entries(Pcm005ApproveAccordionType).map(([, value]) => value)" unstyled multiple>
            <AccordionPanel class="mb-4" :value="Pcm005ApproveAccordionType.Acceptor">
              <AccordAcceptor :title="AccordionName(Pcm005ApproveAccordionType.Acceptor)" v-model="store.body.acceptors"
                :acceptor-type="AcceptorType.Approver" isApprove :isManage="store.states.isEdit"
                :isDisable="!store.states.isEdit || !menuStore.hasManage || props.readonly" isSetDefault
                @set-default="store.fn.onSetDefaultAcceptors" />
            </AccordionPanel>
            <AccordionPanel class="mb-4" :value="Pcm005ApproveAccordionType.Assignee"
              v-if="store.body.contractType != 'Vendor' && store.body.contractType != 'CType002'">
              <AccordAssignee :title="AccordionName(Pcm005ApproveAccordionType.Assignee)" v-model="store.body.assignees"
                :disabled="(!store.states.isPermissionAssign && !store.states.canAssignedApprove) || !menuStore.hasManage || props.readonly"
                :group="AssigneeGroup.Contract" />
            </AccordionPanel>
          </Accordion>
        </div>
      </div>
    </div>
  </VeeForm>
</template>