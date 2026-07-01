<script setup lang="ts">
import { Accordion, AccordionPanel } from 'primevue';
import { TitleHeader, TabHeader } from '@/components/cosmetic';
import { AccordAcceptor, AccordAssignee } from '@/components/Accordions';
import { ButtonSave, ButtonSendApprove, ButtonSendEdit, ButtonApprove, ButtonConfirmAssign, ButtonConfirm } from '@/components/Button';
import Cm006Constants from '@/constants/CM/cm006';
import { useMenuStore } from '@/stores/menu';
import { storeToRefs } from 'pinia';
import { Form as VeeForm } from 'vee-validate';
import { Cm006AccordionTab, Cm006Status } from '@/enums/CM/cm006';
import { AcceptorType, AssigneeGroup, AssigneeType } from '@/enums/participants';
import { computed, defineAsyncComponent, nextTick, onMounted, ref, watch } from 'vue';
import type { Option } from '@/models/shared/option';
import type { MenuItem } from 'primevue/menuitem';
import ToastHelper from '@/helpers/toast';
import { useCm006DetailStore } from '@/stores/CM/CM006/cm006.detail';
import DocumentMapping from '@/components/DocumentMapping.vue';
import ApprovalCmContractGuaranteeReturnDoc from '@/components/Document/ChEditor.vue';
import ContractGuaranteeReturnResultDoc from '@/components/Document/ChEditor.vue';
import { useRoute } from 'vue-router';
import { showActivityDialog, showSaveOptionDialogAsync } from '@/helpers/dialog';
import { useAuthenticationStore } from '@/stores/authentication';
import DisbursementSection from '@/views/CM/CM006/components/Sub/DisbursementSection.vue';
import SendEmailGuaranteeReturnDialog from './Sub/SendEmailGuaranteeReturnDialog.vue';

interface ChEditorExposed {
  clickSave: () => void;
  saveAndWait: (callback: () => void) => void;
  setPlaceholderInDocument: (text: string, hint?: string) => void;
  resetToCurrentVersion: () => void;
}

const approvalCmContractGuaranteeReturn = ref<ChEditorExposed>();
const contractGuaranteeReturnResult = ref<ChEditorExposed>();
const showReviewDocumentDialog = ref(false);
const reviwDocumentId = ref<string>('');
const route = useRoute();
const contractVendorId = computed<string>((): string => route.params.contractVendorId as string);
const id = computed<string | undefined>((): string | undefined => route.params.id as string | undefined);

const authStore = useAuthenticationStore();

const { Cm006BadgeStatus, Cm006AccordionTabName } = Cm006Constants;
const menuStore = useMenuStore();
const { hasManage } = storeToRefs(menuStore);
const store = useCm006DetailStore();
const { body } = storeToRefs(store);
const { isCanEdit, isCurrentCommiteeApproval, isCommiteeApproval, isCanAssign, isConfirmAssigned, isCurrentApprover, isLastApprover, isCommittee, isCommitteeRecall, isRecall, isAccountingCanAssign, isAccountingApprover, isAccountingCanEdit } = storeToRefs(store);
const { onSubmitAsync, onSendApprovalAsync: _onSendApprovalAsync, onCommitteeApprovedAsync, onRejectedAsync, onConfirmAssignedAsync, onSendAcceptorApproveAsync, onAcceptorApproverAsync, onUpsertAttachments, onSetDuties, onReCallAsync, onAccountingApproveOrRejectAsync, onSetDisbursementDateAsync } = store;

const currentTab = ref<string>('detail');
const showDocumentBTab = ref<boolean>(false);
const showSendEmailDialog = ref(false);
const isFormDirty = ref(false);
const isInitialLoad = ref(true);

onMounted(async () => {
  await nextTick();
  isInitialLoad.value = false;
});
const routeItems = ref<Array<MenuItem>>([
  { label: 'รายการคืนหลักประกันสัญญา', url: '/cm/cm006' },
  { label: 'ข้อมูลคืนหลักประกันสัญญา' },
]);
const tabHeader = ref<Array<Option>>([
  {
    label: 'รายละเอียด',
    value: 'detail',
  },
  {
    label: 'เอกสารผลการพิจารณาคืนหลักประกันสัญญา',
    value: 'documentB',
  },
  {
    label: 'เอกสารขออนุมัติคืนหลักประกัน',
    value: 'documentA',
  },
]);

const filteredTabHeader = computed<Array<Option>>((): Array<Option> => {
  if (!body.value.guaranteeReturn.id) {
    return tabHeader.value.filter((_: Option, index: number): boolean => index === 0);
  }

  const showAllTabsStatuses = [Cm006Status.Assigned, Cm006Status.WaitingAcceptance, Cm006Status.Approved, Cm006Status.WaitingAccountingApproval, Cm006Status.WaitingDisbursementDate, Cm006Status.Paid];
  if (showAllTabsStatuses.includes(body.value.guaranteeReturn.status)) {
    return tabHeader.value;
  }

  if (showDocumentBTab.value) {
    return tabHeader.value.filter((_: Option, index: number): boolean => index === 0 || index === 1);
  }

  return tabHeader.value.filter((_: Option, index: number): boolean => index === 0);
});

const DetailForm = defineAsyncComponent(
  (): Promise<typeof import('@/views/CM/CM006/components/Sub/Detail.vue')> =>
    import('@/views/CM/CM006/components/Sub/Detail.vue')
);
const RequredDoc = defineAsyncComponent(
  (): Promise<typeof import('@/views/CM/CM006/components/Sub/RequiredDoc.vue')> =>
    import('@/views/CM/CM006/components/Sub/RequiredDoc.vue')
);

const onSendApprovalWithDocSave = async () => {
  await saveDocumentFirst();

  if (currentTab.value === 'documentA'
    && store.body.guaranteeReturn.approvalCmContractGuaranteeReturnDocumentId
    && !store.body.guaranteeReturn.isApprovalCmContractGuaranteeReturnDocumentIdReplaced) {
    store.body.guaranteeReturn.isApprovalCmContractGuaranteeReturnDocumentIdReplaced = true;
  } else if (currentTab.value === 'documentB'
    && store.body.guaranteeReturn.contractGuaranteeReturnResultDocumentId
    && !store.body.guaranteeReturn.isContractGuaranteeReturnResultDocumentIdReplaced) {
    store.body.guaranteeReturn.isContractGuaranteeReturnResultDocumentIdReplaced = true;
  }

  await _onSendApprovalAsync();
};

const onTabChange = (tab: string): void => {
  currentTab.value = tab;

  nextTick(() => {
    if (tab === 'documentA') {
      approvalCmContractGuaranteeReturn.value?.resetToCurrentVersion();
    } else if (tab === 'documentB') {
      contractGuaranteeReturnResult.value?.resetToCurrentVersion();
    }
  });
};

// Save document to CHEditor first, then call API
const saveDocumentFirst = (): Promise<void> => {
  return new Promise((resolve) => {
    // Only save if document is editable (not read-only)
    if (!canEditDocument.value) {
      resolve();
      return;
    }

    // Save document A if on that tab
    if (currentTab.value === 'documentA' && approvalCmContractGuaranteeReturn.value?.saveAndWait) {
      approvalCmContractGuaranteeReturn.value.saveAndWait(() => {
        resolve();
      });
    }
    // Save document B if on that tab
    else if (currentTab.value === 'documentB' && contractGuaranteeReturnResult.value?.saveAndWait) {
      contractGuaranteeReturnResult.value.saveAndWait(() => {
        resolve();
      });
    } else {
      resolve();
    }
  });
};

const saveDocument = async () => {
  await saveDocumentFirst();
  await onSubmitAsync();
};

const replaceDocumentBStatuses = [Cm006Status.Draft, Cm006Status.WaitingCommitteeApproval, Cm006Status.WaitingAssigned];

const submitAsync = async () => {
  const status = store.body.guaranteeReturn.status;

  let shouldResetDocumentA = false;
  let skipDocumentAReplace = false;
  if (isFormDirty.value
    && id.value
    && store.body.guaranteeReturn.approvalCmContractGuaranteeReturnDocumentId
    && showDocumentAStatuses.includes(status)
    && status !== Cm006Status.Rejected) {
    const saveOption = await showSaveOptionDialogAsync();
    if (saveOption === undefined) return;
    store.body.guaranteeReturn.isApprovalCmContractGuaranteeReturnDocumentIdReplaced = saveOption;
    shouldResetDocumentA = saveOption;
    skipDocumentAReplace = !saveOption;
  }

  let shouldResetDocumentB = false;
  let skipDocumentBReplace = false;
  if (isFormDirty.value
    && id.value
    && store.body.guaranteeReturn.contractGuaranteeReturnResultDocumentId
    && replaceDocumentBStatuses.includes(status)
    && status !== Cm006Status.Rejected) {
    const saveOption = await showSaveOptionDialogAsync();
    if (saveOption === undefined) return;
    store.body.guaranteeReturn.isContractGuaranteeReturnResultDocumentIdReplaced = saveOption;
    shouldResetDocumentB = saveOption;
    skipDocumentBReplace = !saveOption;
  }

  if (replaceDocumentBStatuses.includes(status)) {
    store.body.guaranteeReturn.isApprovalCmContractGuaranteeReturnDocumentIdReplaced = false;
  } else {
    store.body.guaranteeReturn.isContractGuaranteeReturnResultDocumentIdReplaced = false;
  }

  if ((currentTab.value === 'documentA' && store.body.guaranteeReturn.approvalCmContractGuaranteeReturnDocumentId)
    || (currentTab.value === 'documentB' && store.body.guaranteeReturn.contractGuaranteeReturnResultDocumentId)) {
    await saveDocumentFirst();
  }

  const hadDocumentABeforeSave = !!store.body.guaranteeReturn.approvalCmContractGuaranteeReturnDocumentId;
  const hadDocumentBBeforeSave = !!store.body.guaranteeReturn.contractGuaranteeReturnResultDocumentId;

  await onSubmitAsync();

  const conditions = store.body.guaranteeReturn.conditions;
  const allSatisfied = conditions && conditions.length > 0 && conditions.every(c => c.isSatisfied);
  if (allSatisfied && store.body.guaranteeReturn.contractGuaranteeReturnResultDocumentId) {
    showDocumentBTab.value = true;
  }

  const isShowAllTabs = showDocumentAStatuses.includes(store.body.guaranteeReturn.status);

  if (store.body.guaranteeReturn.approvalCmContractGuaranteeReturnDocumentId
    && isShowAllTabs) {
    currentTab.value = 'documentA';
  }
  else if (showDocumentBTab.value
    && store.body.guaranteeReturn.contractGuaranteeReturnResultDocumentId
    && !store.body.guaranteeReturn.isContractGuaranteeReturnResultDocumentIdReplaced) {
    currentTab.value = 'documentB';
  }

  if (id.value && shouldResetDocumentA && currentTab.value === 'documentA') {
    await getReviewDocumentAsync();
  }
  else if (id.value && currentTab.value === 'documentA'
    && !skipDocumentAReplace
    && !store.body.guaranteeReturn.isApprovalCmContractGuaranteeReturnDocumentIdReplaced
    && !hadDocumentABeforeSave) {
    await getReviewDocumentAsync();
  }
  else if (id.value && shouldResetDocumentB && currentTab.value === 'documentB') {
    await getReviewDocumentAsync();
  }
  else if (id.value && currentTab.value === 'documentB'
    && !skipDocumentBReplace
    && !store.body.guaranteeReturn.isContractGuaranteeReturnResultDocumentIdReplaced
    && !hadDocumentBBeforeSave) {
    await getReviewDocumentAsync();
  }

  isFormDirty.value = false;
  store.body.guaranteeReturn.isApprovalCmContractGuaranteeReturnDocumentIdReplaced = false;
  store.body.guaranteeReturn.isContractGuaranteeReturnResultDocumentIdReplaced = false;
};

const canEditDocument = computed(() => {
  return store.body.guaranteeReturn.status == Cm006Status.Draft
    || store.body.guaranteeReturn.status == Cm006Status.Rejected
    || store.body.guaranteeReturn.status == Cm006Status.WaitingAssigned
    || store.body.guaranteeReturn.status == Cm006Status.Assigned
});

const canEditDocumentA = computed(() => {
  return store.body.guaranteeReturn.status == Cm006Status.WaitingAssigned
    || store.body.guaranteeReturn.status == Cm006Status.Assigned
});

const getReviewDocumentAsync = async (): Promise<void> => {
  if (!id.value) return;

  let documentType = '';
  if (currentTab.value === 'documentA') {
    documentType = 'ApprovalCmContractGuaranteeReturn'
  }

  if (currentTab.value === 'documentB') {
    documentType = 'CmContractGuaranteeReturnResule'
  }

  const idDocument = await store.getReviewDocumentAsync(contractVendorId.value, id.value, documentType);

  reviwDocumentId.value = idDocument;
  setDocumentReviewId(idDocument);
};

const setDocumentReviewId = (id: string): void => {
  if (currentTab.value === 'documentA') {
    store.body.guaranteeReturn.approvalCmContractGuaranteeReturnDocumentId = id;
    store.body.guaranteeReturn.isApprovalCmContractGuaranteeReturnDocumentIdReplaced = true;
  }

  if (currentTab.value === 'documentB') {
    store.body.guaranteeReturn.contractGuaranteeReturnResultDocumentId = id;
    store.body.guaranteeReturn.isContractGuaranteeReturnResultDocumentIdReplaced = true;
  }
};

const onClickSelectMapping = (text: string, hint?: string): void => {
  if (currentTab.value === 'documentA') {
    approvalCmContractGuaranteeReturn.value?.setPlaceholderInDocument(text, hint);
    return;
  }
  if (currentTab.value === 'documentB') {
    contractGuaranteeReturnResult.value?.setPlaceholderInDocument(text, hint);
  }
};

const showDocumentAStatuses = [Cm006Status.Assigned, Cm006Status.WaitingAcceptance, Cm006Status.Approved, Cm006Status.WaitingAccountingApproval, Cm006Status.WaitingDisbursementDate, Cm006Status.Paid];

const showSendEmailButton = computed(() => {
  const approvedOnwardStatuses = [
    Cm006Status.Approved,
    Cm006Status.WaitingAccountingApproval,
    Cm006Status.WaitingDisbursementDate,
    Cm006Status.Paid,
  ];
  return approvedOnwardStatuses.includes(body.value.guaranteeReturn.status);
});

const showDocumentBStatuses = [
  Cm006Status.WaitingCommitteeApproval,
  Cm006Status.WaitingAssigned,
];

const accordionValue = computed(() => {
  const status = body.value.guaranteeReturn.status;

  if ([Cm006Status.Draft, Cm006Status.WaitingCommitteeApproval, Cm006Status.Rejected].includes(status)) {
    return [Cm006AccordionTab.Committee];
  }
  if ([Cm006Status.WaitingAssigned, Cm006Status.AccountingRejected, Cm006Status.Assigned].includes(status)) {
    const hasAssigneeType = body.value.guaranteeReturn.assignees?.some(a => a.assigneeType === AssigneeType.Assignee);
    if (status === Cm006Status.Assigned && hasAssigneeType) {
      return [Cm006AccordionTab.Assignee, Cm006AccordionTab.Acceptor];
    }
    return [Cm006AccordionTab.Assignee];
  }
  if (status === Cm006Status.WaitingAcceptance) {
    return [Cm006AccordionTab.Acceptor];
  }
  if (status === Cm006Status.WaitingAccountingApproval) {
    return [Cm006AccordionTab.Accounting];
  }
  if (status === Cm006Status.WaitingDisbursementDate) {
    return [Cm006AccordionTab.Accounting, Cm006AccordionTab.AccountingConfirmer];
  }

  return Object.entries(Cm006AccordionTab).map(([val]) => val);
});

watch(() =>
  store.body.guaranteeReturn.status,
  (newStatus: Cm006Status) => {
    const conditions = body.value.guaranteeReturn.conditions;
    const allSatisfied = conditions && conditions.length > 0 && conditions.every(c => c.isSatisfied);
    if (allSatisfied && body.value.guaranteeReturn.contractGuaranteeReturnResultDocumentId) {
      showDocumentBTab.value = true;
    }

    if (!body.value.guaranteeReturn.id) {
      currentTab.value = 'detail';
    } else if (newStatus === Cm006Status.Assigned) {
      currentTab.value = 'detail';
      nextTick(() => {
        setTimeout(() => {
          document.getElementById('guarantee-return-detail-section')?.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }, 300);
      });
    } else if (newStatus === Cm006Status.WaitingDisbursementDate) {
      currentTab.value = 'detail';
      nextTick(() => {
        setTimeout(() => {
          document.getElementById('disbursement-section')?.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }, 300);
      });
    } else if (newStatus === Cm006Status.Paid) {
      currentTab.value = 'documentA';
    } else if (showDocumentAStatuses.includes(newStatus)) {
      currentTab.value = 'documentA';
    } else if (showDocumentBStatuses.includes(newStatus)) {
      currentTab.value = 'documentB';
    } else {
      currentTab.value = 'detail';
    }
  }, { immediate: true });

watch(() => body.value.guaranteeReturn.assignees, async () => {
  const assignees = body.value.guaranteeReturn.assignees?.filter(f => f.assigneeType === AssigneeType.Assignee) ?? [];
  const hasExistingApprovers = body.value.guaranteeReturn.acceptors?.some(a => a.acceptorType === AcceptorType.Approver && a.userId);
  if (assignees.length > 0 && !hasExistingApprovers && [Cm006Status.WaitingAssigned, Cm006Status.Assigned].includes(body.value.guaranteeReturn.status)) {
    await store.assigneeDefaultAcceptor();
  }
}, { deep: true, immediate: true });

watch(
  () => store.body,
  () => {
    if (!!id.value && !isInitialLoad.value) {
      isFormDirty.value = true;
    }
  },
  { deep: true }
);
</script>

<template>
  <TitleHeader label="รายละเอียดคืนหลักประกันสัญญา" :routeItems="routeItems">
    <template #action>
      <BadgeStatus :label="Cm006BadgeStatus(body.guaranteeReturn.status).label"
        :color="Cm006BadgeStatus(body.guaranteeReturn.status).color" />
      <Button v-if="id" label="ประวัติการใช้งาน" severity="warn" icon="pi pi-refresh" variant="outlined"
        class="bg-white! hover:bg-red-50!" @click="showActivityDialog(id)" />
    </template>
  </TitleHeader>
  <VeeForm @submit="submitAsync" @invalidSubmit="ToastHelper.invalidMessageToast()" :validateOnMount="false"
    v-slot="{ handleSubmit }">
    <div class="grid lg:grid-cols-7 gap-2">
      <div class="lg:col-span-5 order-2 lg:order-1">
        <Tabs :value="currentTab" unstyled @update:value="(tab) => onTabChange(tab.toString())">
          <TabHeader class="sticky top-[55px] z-3 bg-[#F7F7F7] pt-2" :items="filteredTabHeader" />
          <TabPanels>
            <TabPanel value="detail">
              <DetailForm />
              <RequredDoc v-model="body"
                v-if="body.guaranteeReturn.guaranteeTypeCode === 'PBondType001' && showDocumentAStatuses.includes(body.guaranteeReturn.status)" />
              <DisbursementSection v-if="isAccountingCanEdit || body.guaranteeReturn.status === Cm006Status.Paid" />
            </TabPanel>

            <TabPanel value="documentA">
              <ApprovalCmContractGuaranteeReturnDoc
                :docId="store.body.guaranteeReturn.approvalCmContractGuaranteeReturnDocumentId"
                :docName="new Date().toISOString()" :readonly="!menuStore.hasManage || !canEditDocumentA"
                :save="saveDocument" ref="approvalCmContractGuaranteeReturn"
                v-if="store.body.guaranteeReturn.approvalCmContractGuaranteeReturnDocumentId"
                :key="store.body.guaranteeReturn.approvalCmContractGuaranteeReturnDocumentId"
                :versions="store.body.guaranteeReturn.approvalDocumentVersions ?? []"
                :canRestoreVersion="store.canRestoreVersion"
                @restore-version="() => store.resetDocumentAsync(contractVendorId, id!, 'ApprovalCmContractGuaranteeReturn')" />
            </TabPanel>

            <TabPanel value="documentB">
              <ContractGuaranteeReturnResultDoc
                :docId="store.body.guaranteeReturn.contractGuaranteeReturnResultDocumentId"
                :docName="new Date().toISOString()" :readonly="!menuStore.hasManage || !canEditDocument"
                :save="saveDocument" ref="contractGuaranteeReturnResult"
                v-if="store.body.guaranteeReturn.contractGuaranteeReturnResultDocumentId"
                :key="store.body.guaranteeReturn.contractGuaranteeReturnResultDocumentId"
                :versions="store.body.guaranteeReturn.resultDocumentVersions ?? []"
                :canRestoreVersion="store.canRestoreVersion"
                @restore-version="() => store.resetDocumentAsync(contractVendorId, id!, 'CmContractGuaranteeReturnResule')" />
            </TabPanel>

            <div class="mt-4">
              <UploadFileGroup v-if="body.guaranteeReturn.id" v-model="body.guaranteeReturn.attachments"
                @upload="onUpsertAttachments" @remove-file="onUpsertAttachments" @remove-group="onUpsertAttachments"
                @reorder="onUpsertAttachments" :disabled="!hasManage" />
            </div>
          </TabPanels>
        </Tabs>
      </div>

      <div class="relative lg:col-span-2 order-1 lg:order-2">
        <div class="flex flex-col gap-4 lg:ml-3 sticky top-[55px] pt-2 z-3 bg-[#F7F7F7]">
          <div v-if="hasManage" class="flex items-center justify-end gap-2">

            <div id="send-committee-approval-section" class="flex gap-2" v-if="isCanEdit">
              <ButtonSave type="submit" />
              <ButtonSendApprove v-if="body.guaranteeReturn.id"
                @click="handleSubmit(() => onSendApprovalWithDocSave())" />
            </div>

            <div id="committee-seciton" class="flex gap-2" v-if="isCommiteeApproval">
              <ButtonRecall v-if="isCommitteeRecall" @click="onReCallAsync" />
              <ButtonSendEdit v-if="isCurrentCommiteeApproval && isCommiteeApproval" @click="onRejectedAsync" />
              <ButtonApprove v-if="isCurrentCommiteeApproval && isCommiteeApproval" @click="onCommitteeApprovedAsync" />
            </div>

            <div id="assign-section" class="flex gap-2" v-if="isCanAssign">
              <ButtonSendEdit @click="onRejectedAsync()" />
              <ButtonSave type="submit" />
              <ButtonConfirmAssign @click="onConfirmAssignedAsync()" />
            </div>

            <div id="assign-sendApprove-section" class="flex gap-2" v-if="isConfirmAssigned">
              <ButtonSave type="submit" />
              <ButtonSendApprove
                v-if="body.guaranteeReturn.assignees?.some(a => a.assigneeType === AssigneeType.Assignee && (a.delegateeUserId ? a.delegateeUserId : a.userId) === authStore.profile.id)"
                @click="handleSubmit(() => onSendAcceptorApproveAsync())" />
            </div>

            <div id="approver-section" class="flex gap-2">
              <ButtonRecall v-if="isRecall" @click="onReCallAsync" />
              <ButtonSendEdit @click="onRejectedAsync()" v-if="isCurrentApprover" />
              <ButtonApprove v-if="!isLastApprover && isCurrentApprover"
                @click="onAcceptorApproverAsync()" />
              <ButtonConfirm v-if="isLastApprover && isCurrentApprover"
                @click="onAcceptorApproverAsync()" />
            </div>

            <div id="accounting-section" class="flex gap-2">
              <ButtonSendEdit v-if="isAccountingApprover && isAccountingCanAssign"
                @click="onAccountingApproveOrRejectAsync('Reject')" />
              <ButtonApprove v-if="isAccountingApprover && isAccountingCanAssign"
                @click="onAccountingApproveOrRejectAsync('Approve')" />
            </div>

            <div id="disbursement-section" class="flex gap-2" v-if="isAccountingCanEdit">
              <ButtonSave type="submit" />
              <Button severity="warn" label="บันทึกยืนยันวันที่เบิกจ่าย"
                @click="handleSubmit(() => onSetDisbursementDateAsync())" />
            </div>

            <div class="flex gap-2" v-if="showSendEmailButton && isCommittee">
              <Button :severity="body.guaranteeReturn.isSendMail ? 'success' : 'warn'" label="ส่งอีเมลคืนหลักประกัน"
                icon="pi pi-send" variant="outlined" @click="showSendEmailDialog = true" />
            </div>
          </div>

          <Accordion :value="accordionValue" unstyled multiple>
            <AccordionPanel value="999" class="mb-4" v-if="currentTab !== 'detail'">
              <AccordHeader label="Dictionary" />
              <AccordionContent>
                <div></div>
                <Card class="rounded-none overflow-auto h-[800px]">
                  <template #content>
                    <DocumentMapping pathToGet="contract/contract-guarantee-return"
                      @on-click-select="onClickSelectMapping" />
                  </template>
                </Card>
              </AccordionContent>
            </AccordionPanel>
            <AccordionPanel :value="Cm006AccordionTab.Committee">
              <AccordAcceptor :acceptorType="AcceptorType.AcceptanceCommittee"
                :title="Cm006AccordionTabName(Cm006AccordionTab.Committee)" v-model="body.guaranteeReturn.acceptors"
                :is-disable="!body.guaranteeReturn.id || ![Cm006Status.Draft, Cm006Status.Rejected, Cm006Status.WaitingCommitteeApproval].includes(body.guaranteeReturn.status) || !isCommittee"
                isSetDefault
                @set-is-unable-to-perform-duties="(isUnableDuties: boolean, acceptorId: string, remark?: string) => handleSubmit(() => onSetDuties(acceptorId, isUnableDuties, remark))" />
            </AccordionPanel>

            <AccordionPanel class="mt-4" :value="Cm006AccordionTab.Assignee">
              <AccordAssignee :group="AssigneeGroup.Contract" :title="Cm006AccordionTabName(Cm006AccordionTab.Assignee)"
                v-model="body.guaranteeReturn.assignees" :disabled="!isCanAssign && !isConfirmAssigned" />
            </AccordionPanel>
            <AccordionPanel class="my-4" :value="Cm006AccordionTab.Acceptor">
              <AccordAcceptor :acceptorType="AcceptorType.Approver" @set-default="() => store.assigneeDefaultAcceptor()"
                :title="Cm006AccordionTabName(Cm006AccordionTab.Acceptor)" v-model="body.guaranteeReturn.acceptors"
                isSetDefault :isManage="isCanAssign || isConfirmAssigned"
                :is-disable="!isCanAssign && !isConfirmAssigned" />
            </AccordionPanel>
            <AccordionPanel class="my-4" :value="Cm006AccordionTab.Accounting"
              v-if="body.guaranteeReturn.status === Cm006Status.WaitingAccountingApproval || body.guaranteeReturn.status === Cm006Status.WaitingDisbursementDate || body.guaranteeReturn.status === Cm006Status.Paid">
              <AccordAcceptor :acceptorType="AcceptorType.Accounting"
                @set-default="() => store.getDefaultAccountingAcceptor()"
                :title="Cm006AccordionTabName(Cm006AccordionTab.Accounting)" v-model="body.guaranteeReturn.acceptors"
                isSetDefault :isManage="isCanAssign || isConfirmAssigned"
                :is-disable="(!isCanAssign && !isConfirmAssigned) && !isAccountingCanAssign" isApprove />
            </AccordionPanel>
            <AccordionPanel class="mb-4"
              v-if="[Cm006Status.WaitingDisbursementDate, Cm006Status.Paid].includes(body.guaranteeReturn.status)"
              :value="Cm006AccordionTab.AccountingConfirmer">
              <AccordAcceptor :title="'กลุ่มงานบัญชี'" v-model="body.guaranteeReturn.acceptors"
                :acceptor-type="AcceptorType.AccountingConfirmer" isManage is-disable isApprove />
            </AccordionPanel>
          </Accordion>
        </div>
      </div>
    </div>
  </VeeForm>

  <DialogReviewDocument v-if="!!id" v-model="showReviewDocumentDialog" :docId="reviwDocumentId"
    :docName="`plan-${new Date().toISOString()}-${id.toString()}`" @on-click-use-document="setDocumentReviewId" />

  <SendEmailGuaranteeReturnDialog v-model="showSendEmailDialog" />
</template>