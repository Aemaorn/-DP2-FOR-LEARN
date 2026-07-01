<script setup lang="ts">
import { BadgeStatus } from '@/components';
import { AccordAcceptor, AccordAssignee } from '@/components/Accordions';
import { ButtonApprove, ButtonConfirm, ButtonConfirmAssign, ButtonRecall, ButtonSave, ButtonSendApprove, ButtonSendEdit } from '@/components/Button';
import { AccordHeader, TabHeader, TitleHeader } from '@/components/cosmetic';
import DocumentMapping from '@/components/DocumentMapping.vue';
import principleApprovalRentalConstant from '@/constants/PCM005/principleApprovalRental';
import { ConfirmDialogType } from '@/enums/dialog';
import { AcceptorType, AssigneeGroup, AssigneeType } from '@/enums/participants';
import { PrincipleApprovalRentalStatus } from '@/enums/PCM005/principleApprovalRental';
import { showActivityDialog, showConfirmDialogAsync, showSaveOptionDialogAsync } from '@/helpers/dialog';
import ToastHelper from '@/helpers/toast';
import type { Option } from '@/models/shared/option';
import { useMenuStore } from '@/stores/menu';
import { usePcm005DetailStore } from '@/stores/PCM/PCM005/pcm005';
import { usePcm005PrinApproveRentStore } from '@/stores/PCM/PCM005/principleApprovalRental';
import { TabPanel, TabPanels, Tabs } from 'primevue';
import { Form } from 'vee-validate';
import { computed, defineAsyncComponent, nextTick, onMounted, ref, watch } from 'vue';

const props = defineProps({
  readonly: { type: Boolean, default: false },
});

const Condition = defineAsyncComponent(() => import('./Sub/Condition.vue'));
const Detail = defineAsyncComponent(() => import('./Sub/Detail.vue'));
const MedianPrice = defineAsyncComponent(() => import('./Sub/MedianPrice.vue'));
const Budget = defineAsyncComponent(() => import('./Sub/Budget.vue'));
const Entrepreneur = defineAsyncComponent(() => import('./Sub/Entrepreneur.vue'));
const RequestDocument = defineAsyncComponent(() => import('./Sub/RequestDocument.vue'));
const WinnerDocument = defineAsyncComponent(() => import('./Sub/WinnerDocument.vue'));

const requestDocumentRef = ref<InstanceType<typeof RequestDocument> | null>(null);
const winnerDocumentRef = ref<InstanceType<typeof WinnerDocument> | null>(null);

const currentTab = ref('0');
const isFormDirty = ref(false);
const isInitialized = ref(false);

const menuStore = useMenuStore();
const pcmStore = usePcm005DetailStore();
const store = usePcm005PrinApproveRentStore();
const { principleApprovalRentalStatusColor, principleApprovalRentalStatusName } = principleApprovalRentalConstant;

const HeaderItem = ref([
  {
    label: 'รายละเอียด',
    value: '0',
  },
  {
    label: 'เอกสารขออนุมัติเช่า',
    value: '1',
  },
  {
    label: 'เอกสารประกาศผู้ชนะ',
    value: '2',
  },
] as Option[]);

onMounted(() => {
  initAsync();
});

const initAsync = async (): Promise<void> => {
  if (pcmStore.body.id) {
    await pcmStore.getDetailAsync(pcmStore.body.id);
  }

  await Promise.all([
    store.getDepartmentDropdownAsync(),
    store.getBudgetTypeDropdownAsync(),
    store.getAccountCodeDropdownAsync(),
    store.getParcelUnitDDLAsync(),
    store.getVatTypeDDLAsync(),
  ]);

  if (pcmStore.body.principleApprovalRental?.id) {
    await store.getByIdAsync(pcmStore.body.principleApprovalRental.id);
  }

  await nextTick();
  isInitialized.value = true;
};

watch(
  () => store.body,
  () => {
    if (isInitialized.value) {
      isFormDirty.value = true;
    }
  },
  { deep: true }
);

const checkDataBeforeSendApprove = (): { isCancel: boolean; reason: string } => {
  const conditions = [
    {
      check: store.body.acceptors.some(a => a.acceptorType === AcceptorType.RentCommittee),
      reason: 'กรุณาเพิ่มคณะกรรมการจัดเช่าอย่างน้อย 1 คน',
    },
    {
      check: store.body.entrepreneurs && store.body.entrepreneurs.length > 0,
      reason: 'กรุณาเพิ่มผู้ประกอบการอย่างน้อย 1 คน'
    }
  ];

  const failed = conditions.find(c => !c.check);

  return failed ? { isCancel: true, reason: failed.reason } : { isCancel: false, reason: '' };
};

const saveDocument = async () => {
  await onSubmitAsync();
};

const onSubmitAsync = async (): Promise<void> => {
  // Save current document to Collabora first based on current tab
  if (currentTab.value === '1' && requestDocumentRef.value?.saveDocumentFirst && store.body.documentId) {
    await requestDocumentRef.value.saveDocumentFirst();
  } else if (currentTab.value === '2' && winnerDocumentRef.value?.saveDocumentFirst && store.body.winnerDocumentId) {
    await winnerDocumentRef.value.saveDocumentFirst();
  }

  if (store.body.id) {
    if (isFormDirty.value && (store.body.documentId || store.body.winnerDocumentId) && [PrincipleApprovalRentalStatus.Draft, PrincipleApprovalRentalStatus.Edit, PrincipleApprovalRentalStatus.Rejected].includes(store.body.status)) {
      const saveOption = await showSaveOptionDialogAsync();
      if (saveOption === undefined) return;
      store.body.isDocumentReplace = saveOption;
      store.body.isWinnerDocumentReplace = saveOption;
    }

    isInitialized.value = false;
    setCurrentTab();
    await store.updateAsync(store.body.status);
    isFormDirty.value = false;
    await nextTick();
    isInitialized.value = true;
    return;
  }
};

const setCurrentTab = async () => {
  if (store.body.documentId && !store.body.isDocumentIdReplaced) {
    currentTab.value = '1';
  }
  else if (store.body.winnerDocumentId && !store.body.isWinnerDocumentIdReplaced) {
    currentTab.value = '2';
  }
};

const onSaveUpdateAsync = async (): Promise<void> => {
  await store.updateAsync();

  if (!store.body.acceptors.some(a => a.acceptorType === AcceptorType.Approver)) {
    store.getDefaultApproverAsync();
  }
};

const onSendApproveAsync = async (): Promise<void> => {
  const { isCancel, reason } = checkDataBeforeSendApprove();

  if (isCancel) {
    return ToastHelper.errorDescription(reason);
  }

  if (store.body.entrepreneurs?.every(i => !i.coiResultAt || !i.egpResultAt || !i.watchlistResultAt)) {
    return ToastHelper.warning('ข้อมูลไม่ถูกต้อง', 'กรุณาตรวจสอบข้อมูลผู้ประกอบการให้ครบถ้วน');
  }

  if (store.body.entrepreneurs?.some(s => (s.details?.length ?? 0) === 0)) {
    return ToastHelper.warning('ข้อมูลไม่ถูกต้อง', 'ผู้ประกอบการจะต้องบันทึกรายละเอียดราคาอย่างน้อย 1 รายการ');
  }

  if (!await showConfirmDialogAsync(ConfirmDialogType.SendApprove)) return;

  if (store.body.id) {
    return await store.updateAsync(PrincipleApprovalRentalStatus.WaitingCommitteeApproval);
  }
};

const onRecallAsync = async (): Promise<void> => {
  if (!await showConfirmDialogAsync(ConfirmDialogType.Edit)) return;

  await store.updateAsync(store.body.status === PrincipleApprovalRentalStatus.WaitingCommitteeApproval ? PrincipleApprovalRentalStatus.Edit : PrincipleApprovalRentalStatus.WaitingComment);
};

const onConfirmAssignAsync = async (): Promise<void> => {
  if (!store.body.assignees.filter(x => x.assigneeGroup == AssigneeGroup.JorPor).some(x => x.assigneeType == AssigneeType.Assignee)) {
    return ToastHelper.assignAtLeastMessageToast();
  }

  if (!await showConfirmDialogAsync(ConfirmDialogType.Assigned)) return;

  await store.updateAsync(PrincipleApprovalRentalStatus.WaitingComment);

  if (!store.body.acceptors.some(a => a.acceptorType === AcceptorType.Approver)) {
    await store.getDefaultApproverAsync();
  }
};

const onWaitingAcceptanceAsync = async (): Promise<void> => {
  if (!store.body.assignees.filter(x => x.assigneeGroup == AssigneeGroup.JorPor).some(x => x.remark)) {
    return ToastHelper.assignneeCommentAtLeastMessageToast();
  }

  if (!store.body.acceptors.some(a => a.acceptorType === AcceptorType.Approver)) {
    return ToastHelper.approvalAtLeastMessageToast();
  }

  if (!await showConfirmDialogAsync(ConfirmDialogType.SendApprove)) return;

  await store.updateAsync(PrincipleApprovalRentalStatus.WaitingAcceptance);
};

const onSaveContractAssignAsync = async (): Promise<void> => {
  await store.updateAsync(PrincipleApprovalRentalStatus.WaitingContractAssign);
};

const onSaveAssignAsync = async () => {
  await store.updateAsync();
}

const onConfirmContractAssignAsync = async (): Promise<void> => {
  if (!store.body.assignees.filter(x => x.assigneeGroup == AssigneeGroup.Contract).some(x => x.assigneeType == AssigneeType.Assignee)) {
    return ToastHelper.assignAtLeastMessageToast();
  }

  if (!await showConfirmDialogAsync(ConfirmDialogType.Assigned)) return;

  await store.updateAsync(PrincipleApprovalRentalStatus.ContractAssigned);
};

const canEditDocument = computed(() => {
  return [PrincipleApprovalRentalStatus.Edit,
      PrincipleApprovalRentalStatus.Draft,
      PrincipleApprovalRentalStatus.Rejected,
      PrincipleApprovalRentalStatus.WaitingComment].includes(store.body.status);
});

const canEditWinnerDocument = computed(() => {
  return [PrincipleApprovalRentalStatus.Edit,
      PrincipleApprovalRentalStatus.Draft,
      PrincipleApprovalRentalStatus.Rejected].includes(store.body.status);
});
</script>

<template>
  <TitleHeader label="ขออนุมัติเช่า">
    <template #action>
      <BadgeStatus :color="principleApprovalRentalStatusColor(store.body.status).color"
        :label="principleApprovalRentalStatusName(store.body.status)" v-if="store.body.status" />
      <Button label="ประวัติการใช้งาน" icon="pi pi-refresh" variant="outlined" severity="primary" v-if="store.body.id"
        class="bg-white! hover:bg-red-50!" @click="() => showActivityDialog(store.body.id!)" />
    </template>
  </TitleHeader>

  <Form @submit="onSubmitAsync" v-slot="{ handleSubmit }">
    <div class="grid grid-cols-1 lg:grid-cols-7 gap-2">
      <div class="lg:col-span-5 order-2 lg:order-1">
        <Tabs :value="currentTab" unstyled @update:value="(tab) => currentTab = tab.toString()">
          <TabHeader :items="HeaderItem" class="sticky top-[57px] z-99 bg-[#F7F7F7]" />
          <TabPanels>
            <TabPanel value="0">
              <div class="flex flex-col gap-4 mb-5">
                <Condition />
                <Detail />
                <Entrepreneur />
                <!-- <Consideration />
                <AnalysisBuilding /> -->
                <MedianPrice />
                <Budget />
              </div>
            </TabPanel>
            <TabPanel value="1">
              <div class="grid grid-rows-1 gap-2 mb-5" v-if="store.body.documentId">
                <RequestDocument
                  :doc-id="store.body.documentId"
                  ref="requestDocumentRef"
                  :is-disable="!canEditDocument || !menuStore.hasManage"
                  :readonly="!canEditDocument"
                  :save="saveDocument"
                  :versions="store.body.documentVersions ?? []"
                  :canRestoreVersion="store.status.canRestoreDocumentVersion"
                  @restore-version="() => store.resetDocumentAsync('Approval')" />
              </div>
            </TabPanel>
            <TabPanel value="2">
              <div class="grid grid-rows-1 gap-2 mb-5" v-if="store.body.winnerDocumentId">
                <WinnerDocument
                  :doc-id="store.body.winnerDocumentId"
                  ref="winnerDocumentRef"
                  :is-disable="!menuStore.hasManage"
                  :readonly="!canEditWinnerDocument"
                  :save="saveDocument"
                  :versions="store.body.winnerDocumentVersions ?? []"
                  :canRestoreVersion="store.status.canRestoreWinnerDocumentVersion"
                  @restore-version="() => store.resetDocumentAsync('Winner')" />
              </div>
            </TabPanel>
          </TabPanels>
        </Tabs>
      </div>
      <div class="lg:col-span-2 relative order-1 lg:order-2">
        <div class="flex flex-col gap-4 ml-3 sticky top-[100px]">
          <div class="flex items-center justify-end" v-if="menuStore.hasManage && !props.readonly">
            <div class="draft flex gap-2 items-center">
              <ButtonSave type="submit" v-if="store.status.canEdit" />
              <ButtonSendApprove @click="() => handleSubmit(() => onSendApproveAsync())" v-if="store.status.canEdit" />
            </div>
            <div class="waiting-unit flex gap-2 items-center">
              <ButtonRecall @click="onRecallAsync" v-if="store.status.canCommitteeRecall" />
              <ButtonNotAgree @click="() => store.rejectAsync(true)" v-if="store.status.canCommitteeApprove" />
              <ButtonApprove @click="store.committeeApproveAsync" v-if="store.status.canCommitteeApprove" />
            </div>
            <div class="assign flex gap-2 items-center">
              <ButtonSendEdit @click="store.assigneeRejectAsync" v-if="store.status.canDirectorAssign" />
              <ButtonSave @click="onSaveUpdateAsync"
                v-if="store.status.canDirectorAssign" />
              <ButtonSave @click="onSaveUpdateAsync" text="บันทึกผู้รับผิดชอบ"
                v-if="store.status.canAssignAndConfirm || store.status.canComment" />
              <ButtonConfirmAssign @click="onConfirmAssignAsync" v-if="store.status.canAssignAndConfirm" />
              <ButtonSendApprove @click="onWaitingAcceptanceAsync" v-if="store.status.canComment" />
            </div>
            <div class="approve flex gap-2 items-center">
              <ButtonRecall @click="onRecallAsync" v-if="store.status.canRecall" />
              <ButtonSendEdit @click="store.rejectAsync" v-if="store.status.canApprove" />
              <ButtonApprove @click="store.approveAsync"
                v-if="store.status.canApprove && !store.status.isLastApprove" />
              <ButtonConfirm @click="store.approveAsync" v-if="store.status.canApprove && store.status.isLastApprove" />
            </div>
            <div class="assign flex gap-2 items-center">
              <ButtonSave :text="'บันทึกผู้รับผิดชอบ'" @click="onSaveAssignAsync"
                v-if="store.status.canAssignedApprove" />
              <ButtonSave @click="onSaveContractAssignAsync" v-if="store.status.canContractAssign" />
              <ButtonConfirmAssign @click="onConfirmContractAssignAsync" v-if="store.status.canContractAssign" />
            </div>
          </div>

          <Accordion :value="['0', '1', '2', '3']" unstyled multiple>
            <AccordionPanel value="999" class="mb-4" v-if="currentTab !== '0' && currentTab !== '1'">
              <AccordHeader label="Dictionary" />
              <AccordionContent>
                <div></div>
                <Card class="rounded-none overflow-auto h-[800px]">
                  <template #content>
                    <DocumentMapping pathToGet="procurement/principle-approval-rental" @on-click-select="
                      (text, hint) => currentTab == '2' ? requestDocumentRef?.setPlaceholderInDocument(text, hint)
                        : winnerDocumentRef?.setPlaceholderInDocument(text, hint)
                    " />
                  </template>
                </Card>
              </AccordionContent>
            </AccordionPanel>
            <AccordionPanel value="0">
              <AccordAcceptor v-model="store.body.acceptors" title="คณะกรรมการจัดเช่า"
                :acceptor-type="AcceptorType.RentCommittee" is-approve v-if="store.body.acceptors"
                @set-is-unable-to-perform-duties="(status: boolean, id: string, remark?: string) => handleSubmit(() => store.isSetDutiesStatusAsync(status, id, remark))"
                :disabled="store.status.canAssigneesAssign || props.readonly" />
            </AccordionPanel>
            <AccordionPanel value="1" class="mt-4">
              <AccordAssignee v-model="store.body.assignees" title="เจ้าหน้าที่พัสดุให้ความเห็น"
                :disabled="(!store.status.canAssigneesAssign && !store.status.canComment) || !menuStore.hasManage || props.readonly"
                :is-comment="store.status.canComment" @on-comment="(e) => store.commentAsync(e.reason)"
                :group="AssigneeGroup.JorPor" v-if="store.body.assignees" />
            </AccordionPanel>
            <AccordionPanel value="2" class="mt-4">
              <AccordAcceptor v-model="store.body.acceptors" title="ผู้มีอำนาจเห็นชอบ/อนุมัติ"
                :acceptor-type="AcceptorType.Approver" isManage is-approve
                :is-disable="!store.status.canAssignees || !menuStore.hasManage || props.readonly" :is-set-default="true"
                v-if="store.body.acceptors" @set-default="() => store.getDefaultApproverAsync()" />
            </AccordionPanel>
            <AccordionPanel value="3" class="mt-4"
              v-if="[PrincipleApprovalRentalStatus.WaitingContractAssign, PrincipleApprovalRentalStatus.ContractAssigned].includes(store.body.status)">
              <AccordAssignee v-model="store.body.assignees" title="มอบหมายผู้รับผิดชอบใบสั่ง และแจ้งทำสัญญา"
                :disabled="(!store.status.canContractAssign && !store.status.canAssignedApprove) || !menuStore.hasManage || props.readonly"
                v-if="store.body.assignees" :group="AssigneeGroup.Contract" />
            </AccordionPanel>
          </Accordion>
        </div>
      </div>
    </div>
  </Form>
</template>
