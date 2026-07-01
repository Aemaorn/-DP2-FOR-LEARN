<script setup lang="ts">
import type { Option } from '@/models/shared/option';
import { ref, computed, nextTick, onBeforeMount, onUnmounted, watch } from 'vue';
import { Form } from 'vee-validate';
import { TitleHeader, TabHeader } from '@/components/cosmetic';
import { BadgeStatus } from '@/components/index';
import { AccordAcceptor, AccordAssignee } from '@/components/Accordions';
import {
  ButtonSave,
  ButtonSendApprove,
  ButtonRecall,
  ButtonApprove,
  ButtonSendEdit,
  ButtonConfirm,
  ButtonConfirmAssign,
  ButtonApproveConfirm,
  ButtonSendChange,
  ButtonSendCancel,
  ButtonNotAgree,
} from '@/components/Button';
import DocumentChEditor from '@/components/Document/ChEditor.vue';
import ReviewChEditor from '@/components/Document/ChEditor.vue';
import { TermOfRefSection } from './components/sub';
import {
  Template01,
  Template02,
  Template04,
  Template05,
  Template06,
  Template07,
  Template08,
  Template09,
  Template10,
  Template11,
  Template12,
  Template13,
  Template14,
} from './components';
import { usePP002DetailStore } from '@/views/PP/stores/PP002/pp002Store';
import { usePPDetailStore } from '../../../../stores/PP/ppStore';
import { PP002DocumentTemplate, PP002Status } from '../../enums/pp002';
import { AcceptorType, AssigneeType } from '@/enums/participants';
import {
  showActivityDialog,
  showConfirmDialogAsync,
  showReasonDialogAsync,
  showSaveOptionDialogAsync,
} from '@/helpers/dialog';
import { ConfirmDialogType, ReasonDialogType } from '@/enums/dialog';
import { useMenuStore } from '@/stores/menu';
import ToastHelper from '@/helpers/toast';
import TorDraftConstant from '@/constants/torDraft';
import DocumentMapping from '@/components/DocumentMapping.vue';
import DialogReviewDocument from '@/components/Document/DialogReviewDocument.vue';
import { storeToRefs } from 'pinia';
import { ProgramHistoryName } from '@/enums/programHistoryName';
import Template15 from './components/Template15.vue';
import SectionNavigator from './components/SectionNavigator.vue';
import { useSectionSpy } from './composables/useSectionSpy';
import PP002Service from '../../services/PP002/PP002Service';
import { HttpStatusCode } from 'axios';

const props = defineProps({
  procurementId: {
    type: String,
    required: true,
  },
  torId: { type: String, default: null },
  readonly: {
    type: Boolean,
    default: false,
  },
});

const { TorDraftStatusColor } = TorDraftConstant;
const menuStore = useMenuStore();
const procurementStore = usePPDetailStore();
const store = usePP002DetailStore();
const { PP002Detail } = storeToRefs(store);

const HEADER_ITEMS: Option[] = [
  { label: 'ขอบเขตงาน', value: 'detail' },
  { label: 'เอกสารขอบเขตของงาน', value: 'document' },
  { label: 'เอกสารขออนุมัติขอบเขตงาน (TOR)', value: 'review' },
];

const showReviewDocumentDialog = ref(false);
const reviwDocumentId = ref<string>('');
const documentRef = ref<InstanceType<typeof DocumentChEditor> | null>(null);
const documentReviewRef = ref<InstanceType<typeof ReviewChEditor> | null>(null);

const currentTab = ref('detail');
const currentAccordion = ref<string[]>([]);
const noCommentIf60AndBelowOneHundredธhousand = ref(false);
const isFormDirty = ref(false);
const isInitialized = ref(false);

const detailContainerRef = ref<HTMLElement | null>(null);
const { sections, activeSectionId, scrollToSection } = useSectionSpy(detailContainerRef);

const savedScrollPositions: Record<string, number> = {};

watch(currentTab, async (newTab, oldTab) => {
  if (oldTab !== undefined) {
    savedScrollPositions[oldTab] = window.scrollY;
  }

  await nextTick();

  if (savedScrollPositions[newTab] !== undefined) {
    window.scrollTo(0, savedScrollPositions[newTab]);
  }
});

const setPlaceholderInDocument = (text: string, hint?: string) => {
  if (documentRef.value && currentTab.value === 'document') {
    documentRef.value.setPlaceholderInDocument(text, hint);
  }

  if (documentReviewRef.value && currentTab.value === 'review') {
    documentReviewRef.value.setPlaceholderInDocument(text, hint);
  }
};

const TEMPLATE_COMPONENTS = () => {
  const code = store.PP002Detail.torDocumentTemplateCode as PP002DocumentTemplate;

  switch (code) {
    case PP002DocumentTemplate.TorBuyRentGt500kGen60:
    case PP002DocumentTemplate.TorBuyRentGt500kGen80:
    case PP002DocumentTemplate.TorBuyRentGt500kIt60:
    case PP002DocumentTemplate.TorBuyRentGt500kIt80:
      return Template01;

    case PP002DocumentTemplate.TorBuyRentLte500k60:
    case PP002DocumentTemplate.TorBuyRentLte500k80:
    case PP002DocumentTemplate.TorHireLte500k60:
    case PP002DocumentTemplate.TorHireLte500k80:
      return Template02;

    case PP002DocumentTemplate.TorHireMaintenance60:
    case PP002DocumentTemplate.TorHireMaintenance80:
      return Template04;

    case PP002DocumentTemplate.TorHireDevelopment60:
    case PP002DocumentTemplate.TorHireDevelopment80:
    case PP002DocumentTemplate.TorHireGt500k60:
    case PP002DocumentTemplate.TorHireGt500k80:
      return Template05;

    case PP002DocumentTemplate.TorBuyWithHire60:
    case PP002DocumentTemplate.TorBuyWithHire80:
      return Template06;

    case PP002DocumentTemplate.TorHireWithHire60:
    case PP002DocumentTemplate.TorHireWithHire80:
      return Template06;

    case PP002DocumentTemplate.TorBuyLicense60:
    case PP002DocumentTemplate.TorBuyLicense80:
      return Template07;

    case PP002DocumentTemplate.TorRentComputer60:
    case PP002DocumentTemplate.TorRentComputer80:
      return Template08;

    case PP002DocumentTemplate.TorHireCleaning60:
    case PP002DocumentTemplate.TorHireCleaning80:
      return Template09;

    case PP002DocumentTemplate.TorHireSecurity60:
    case PP002DocumentTemplate.TorHireSecurity80:
      return Template10;

    case PP002DocumentTemplate.TorRentVehicle60:
    case PP002DocumentTemplate.TorRentVehicle80:
      return Template11;

    case PP002DocumentTemplate.TorRentCommCircuit60:
    case PP002DocumentTemplate.TorRentCommCircuit80:
      return Template12;

    case PP002DocumentTemplate.TorHireConsultant60:
    case PP002DocumentTemplate.TorHireConsultant80:
      return Template13;

    case PP002DocumentTemplate.TorHireRenovate60:
    case PP002DocumentTemplate.TorHireRenovate80:
      return Template14;

    case PP002DocumentTemplate.TorHireConsultantCompany60:
    case PP002DocumentTemplate.TorHireConsultantCompany80:
      return Template15;

    default:
      return null;
  }
};

const currentTemplateComponent = computed(() => {
  if (store.PP002Detail.torDocumentTemplateCode) {
    return TEMPLATE_COMPONENTS();
  }

  return null;
});

onBeforeMount(async () => {
  await onInitAsync();
  await nextTick();
  isInitialized.value = true;
});

watch(
  () => store.PP002Detail,
  () => {
    if (isInitialized.value) {
      isFormDirty.value = true;
    }
  },
  { deep: true }
);

onUnmounted(() => {
  store.onResetStore();
});

const onInitAsync = async (): Promise<void> => {
  if (props.procurementId) {
    store.PP002Detail.procurementId = props.procurementId;
  }

  store.PP002Detail.isStock = procurementStore.procurementDetail.isStock;
  store.PP002Detail.supplyMethodTypeCode = procurementStore.procurementDetail.supplyMethodTypeCode;

  if (props.procurementId && !props.torId) {
    await store.onGetCommitteeAcceptorAsync(props.procurementId);

    if (procurementStore.procurementDetail.hasMd) {
      await Promise.all([store.getDefaultJorporAsync()]);
    }

    await onSetDefaultAcceptorAsync();
  }

  if (props.torId) {
    await store.onGetByIdAsync(props.torId);
  }

  noCommentIf60AndBelowOneHundredธhousand.value =
    procurementStore.procurementDetail.supplyMethodCode === 'SMethod002' &&
    procurementStore.procurementDetail.budget < 100000;
};

const saveDocument = async () => {
  await onSaveDocumentation();

  store.PP002Detail.isTorDraftApprovalDocumentIdReplaced = false;
  store.PP002Detail.isTorDraftDocumentIdReplaced = false;
  isInitialized.value = false;


  if (props.torId) {
    await store.onUpdateAsync(props.torId);
    isFormDirty.value = false;
    await nextTick();
    isInitialized.value = true;
    setCurrentTab();
    return;
  }

  await store.onCreateAsync();
  isFormDirty.value = false;
  await nextTick();
  isInitialized.value = true;
  setCurrentTab();
};

const saveEditorAsync = (editor: any): Promise<void> => {
  if (!editor?.saveAndWait) return Promise.resolve();

  return new Promise<void>((resolve) => {
    const timeout = setTimeout(() => resolve(), 3000);
    editor.saveAndWait(() => {
      clearTimeout(timeout);
      resolve();
    });
  });
};

const onSaveDocumentation = async () => {
  if (!canEditDocument.value) return;

  await Promise.all([
    saveEditorAsync(documentRef.value),
    saveEditorAsync(documentReviewRef.value),
  ]);
}

const onSubmitAsync = async (isSaveDraft: boolean = false) => {

  await onSaveDocumentation();

  if (props.torId) {

    if (isFormDirty.value
      && (store.PP002Detail.torDraftApprovalDocumentId
        || store.PP002Detail.torDraftDocumentId) && [PP002Status.Draft, PP002Status.Edit, PP002Status.Rejected].includes(store.PP002Detail.status)) {

      const saveOption = await showSaveOptionDialogAsync();
      if (saveOption === undefined) return;
      store.PP002Detail.isTorDraftApprovalDocumentIdReplaced = saveOption;
      store.PP002Detail.isTorDraftDocumentIdReplaced = saveOption;
    }

    isInitialized.value = false;
    await store.onUpdateAsync(props.torId, undefined, isSaveDraft);
    isFormDirty.value = false;
    await nextTick();
    isInitialized.value = true;
    setCurrentTab();
    return;
  }

  isInitialized.value = false;
  await store.onCreateAsync(isSaveDraft);
  isFormDirty.value = false;
  await nextTick();
  isInitialized.value = true;
  setCurrentTab();
};

const setCurrentTab = async () => {
  if (store.PP002Detail.torDraftApprovalDocumentId) {
    if (defaultReviewStatuses.includes(store.PP002Detail.status)) {
      currentTab.value = 'review';
    } else {
      currentTab.value = 'document';
    }
  }
};

const onSendApproveAsync = async (): Promise<void> => {
  if (!store.PP002Detail.acceptors) return;

  if (
    store.PP002Detail.acceptors.filter(
      (x) => x.acceptorType === AcceptorType.DepartmentDirectorAgree
    ).length <= 0 &&
    procurementStore.procurementDetail.hasMd
  ) {
    ToastHelper.departmentAtLeastMessageToast();

    return;
  }

  if (
    !store.PP002Detail.acceptors.some((s) => s.acceptorType === AcceptorType.Approver) &&
    !procurementStore.procurementDetail.hasMd
  ) {
    ToastHelper.approvalAtLeastMessageToast();

    return;
  }

  if (!(await showConfirmDialogAsync(ConfirmDialogType.SendApproveConfirm))) return;

  store.PP002Detail.isTorDraftApprovalDocumentIdReplaced = false;
  store.PP002Detail.isTorDraftDocumentIdReplaced = false;

  if (props.torId) {
    await store.onUpdateAsync(props.torId, PP002Status.WaitingCommitteeApproval);
  }
};

const onRecallAsync = async (): Promise<void> => {
  if (!await showConfirmDialogAsync(ConfirmDialogType.Edit)) return;

  store.PP002Detail.isTorDraftApprovalDocumentIdReplaced = false;
  store.PP002Detail.isTorDraftDocumentIdReplaced = false;
  await store.onUpdateAsync(props.torId, store.PP002Detail.status === PP002Status.WaitingCommitteeApproval ? PP002Status.Edit : PP002Status.WaitingComment);
};

const onSetDefaultAcceptorAsync = async (): Promise<void> => {
  await store.onClearDefaultAcceptorAsync();
};

const onChangeAsync = async (): Promise<void> => {
  const res = await showReasonDialogAsync(ReasonDialogType.RequestChange, true);

  if (!res.isConfirm || !res.reason) return;

  await store.onActionAsync({ isChange: true, reason: res.reason });
};

const onCancelAsync = async (): Promise<void> => {
  const res = await showReasonDialogAsync(ReasonDialogType.RequestCancel, true);

  if (!res.isConfirm || !res.reason) return;

  await store.onActionAsync({ isCancel: true, reason: res.reason });
};

const onSaveAssignAsync = async (): Promise<void> => {
  store.PP002Detail.isTorDraftApprovalDocumentIdReplaced = false;
  store.PP002Detail.isTorDraftDocumentIdReplaced = false;

  await store.onUpdateAsync(props.torId, PP002Status.WaitingAssign);

  const acceptors = store.PP002Detail.acceptors ?? [];

  if (!acceptors.some((a) => a.acceptorType === AcceptorType.Approver)) {
    await store.onGetDefaultAcceptorAsync();
  }
};

const onConfirmAssignAsync = async (): Promise<void> => {
  if (!store.PP002Detail.acceptors) return;

  store.PP002Detail.isTorDraftApprovalDocumentIdReplaced = false;
  store.PP002Detail.isTorDraftDocumentIdReplaced = false;

  if (!store.PP002Detail.assignees?.some((x) => x.assigneeType === AssigneeType.Assignee)) {
    ToastHelper.assignAtLeastMessageToast();

    return;
  }

  if (!(await showConfirmDialogAsync(ConfirmDialogType.Assigned))) return;
  await store.onUpdateAsync(props.torId, PP002Status.WaitingComment);

  if (
    !store.PP002Detail.acceptors.some((x) => x.acceptorType === AcceptorType.Approver) &&
    store.PP002Detail.status === PP002Status.WaitingComment
  ) {
    await store.onGetDefaultAcceptorAsync();
  }
};

const onSendMdApproveAsync = async (): Promise<void> => {
  if (!store.PP002Detail.acceptors) return;

  if (
    store.PP002Detail.acceptors.filter((x) => x.acceptorType === AcceptorType.Approver).length <= 0
  ) {
    ToastHelper.approvalAtLeastMessageToast();

    return;
  }

  if (
    !store.PP002Detail.assignees?.some((x) => x.remark) &&
    !(
      procurementStore.procurementDetail.supplyMethodCode === 'SMethod002' &&
      procurementStore.procurementDetail.budget < 100000
    )
  ) {
    ToastHelper.assignneeCommentAtLeastMessageToast();

    return;
  }

  if (!(await showConfirmDialogAsync(ConfirmDialogType.SendApproveConfirm))) return;

  store.PP002Detail.isTorDraftApprovalDocumentIdReplaced = false;
  store.PP002Detail.isTorDraftDocumentIdReplaced = false;

  await store.onUpdateAsync(props.torId, PP002Status.WaitingApproval);
};

const onChangeTemplate = (templateId: string): void => {
  store.PP002Detail.torDocumentTemplateCode = templateId;
};

const setDocumentReviewId = async (id: string): Promise<void> => {
  isInitialized.value = false;

  let documentType = '';
  if (currentTab.value === 'document') {
    documentType = 'Approval';
  } else if (currentTab.value === 'review') {
    documentType = 'Tor';
  }

  if (documentType === 'Approval') {
    store.PP002Detail.torDraftApprovalDocumentId = id;
  } else if (documentType === 'Tor') {
    store.PP002Detail.torDraftDocumentId = id;
  }

  await nextTick();
  isInitialized.value = true;
};

const canEditDocument = computed(() => {
  return (
    store.PP002Detail.status == PP002Status.Draft ||
    store.PP002Detail.status == PP002Status.Edit ||
    store.PP002Detail.status == PP002Status.Rejected ||
    store.PP002Detail.status == PP002Status.WaitingComment
  );
});

const ACCORDION_APPROVED: string[] = ['0', '1', '2', '3', '4'];
const ACCORDION_MD_FLOW: Record<PP002Status, string[] | undefined> = {
  [PP002Status.Draft]: ['0', '2'],
  [PP002Status.Edit]: ['0', '2'],
  [PP002Status.Rejected]: ['0', '2'],
  [PP002Status.WaitingCommitteeApproval]: ['0'],
  [PP002Status.WaitingUnitApproval]: ['2'],
  [PP002Status.WaitingAssign]: ['3', '4'],
  [PP002Status.WaitingComment]: ['3', '4'],
  [PP002Status.RejectToAssignee]: ['3', '4'],
  [PP002Status.WaitingApproval]: ['4'],
  [PP002Status.Approved]: ACCORDION_APPROVED,
} as const;

const ACCORDION_NON_MD_FLOW: Record<PP002Status, string[] | undefined> = {
  [PP002Status.Draft]: ['0', '1'],
  [PP002Status.Edit]: ['0', '1'],
  [PP002Status.Rejected]: ['0', '1'],
  [PP002Status.WaitingCommitteeApproval]: ['0'],
  [PP002Status.WaitingApproval]: ['1'],
  [PP002Status.WaitingAssign]: undefined,
  [PP002Status.WaitingComment]: undefined,
  [PP002Status.RejectToAssignee]: undefined,
  [PP002Status.WaitingUnitApproval]: undefined,
  [PP002Status.Approved]: ACCORDION_APPROVED,
} as const;

const getAccordionsForStatus = (status: PP002Status, hasMd: boolean): string[] => {
  const flow = hasMd ? ACCORDION_MD_FLOW : ACCORDION_NON_MD_FLOW;
  return flow[status] ?? currentAccordion.value;
};

const defaultDocumentStatuses = [
  PP002Status.WaitingCommitteeApproval,
  PP002Status.WaitingUnitApproval,
  PP002Status.RejectToAssignee,
];

const defaultReviewStatuses = [
  PP002Status.WaitingAssign,
  PP002Status.WaitingComment,
  PP002Status.WaitingApproval,
  PP002Status.Approved,
];

watch(
  () => PP002Detail.value.status,
  (newStatus: PP002Status) => {
    currentAccordion.value = getAccordionsForStatus(
      newStatus,
      procurementStore.procurementDetail.hasMd
    );

    if (defaultReviewStatuses.includes(newStatus)) {
      currentTab.value = 'review';
    } else if (defaultDocumentStatuses.includes(newStatus)) {
      currentTab.value = 'document';
    }
  },
  { immediate: true }
);

const handleVersionRestored = async (documentType: string) => {
  if (!store.PP002Detail.id) return;

  const { status } = await PP002Service.resetDocumentAsync(
    store.PP002Detail.id,
    procurementStore.procurementDetail.id!,
    documentType
  );

  if (status === HttpStatusCode.Ok) {
    ToastHelper.success('สำเร็จ', 'ทำการรีเซ็ตเอกสารสำเร็จ');
    await onInitAsync();

    return;
  }

  ToastHelper.error("ไม่สามารถรีเซ็ตเอกสารได้", "เกิดข้อผิดพลาดในการรีเซ็ตเอกสารการตรวจสอบ");
};
</script>

<template>
  <TitleHeader label="ร่างขอบเขตของงาน (TOR)">
    <template #action>
      <div class="flex items-center gap-4">
        <BadgeStatus :color="TorDraftStatusColor(store.PP002Detail.status).color"
          :label="TorDraftStatusColor(store.PP002Detail.status).label" v-if="store.PP002Detail.status" />
        <BadgeStatus v-if="store.PP002Detail.isChange" label="ขอเปลี่ยนแปลง" color="amber" />
        <BadgeStatus v-if="store.PP002Detail.isCancel" label="ขอยกเลิก" color="red" />
      </div>

      <div class="w-px self-stretch min-h-[30px] bg-gray-300 mx-1" v-if="store.PP002Detail.id" />

      <Button label="ประวัติการใช้งาน" icon="pi pi-refresh" variant="outlined" severity="primary"
        v-if="store.PP002Detail.id" class="bg-white! hover:bg-red-50!"
        @click="() => showActivityDialog(store.PP002Detail.id!, ProgramHistoryName.Tor)" />
    </template>
  </TitleHeader>
  <Form @submit="onSubmitAsync(false)" @invalidSubmit="ToastHelper.invalidMessageToast()" v-slot="{ handleSubmit }">
    <div class="grid grid-cols-1 lg:grid-cols-7 gap-2">
      <div class="lg:col-span-5 order-2 lg:order-1">
        <Tabs :value="currentTab" unstyled @update:value="(tab) => (currentTab = tab.toString())">
          <TabHeader :items="HEADER_ITEMS.filter((h, i) => (store.showDocument ? h : i == 0))"
            class="sticky top-14 z-3 bg-[#F7F7F7] pt-2" />
          <TabPanels>
            <TabPanel value="detail">
              <div ref="detailContainerRef">
                <TermOfRefSection v-model="store.PP002Detail" @onChangeTemplate="(value: string) => onChangeTemplate(value)" :readonly="readonly" />
                <component :is="currentTemplateComponent" v-model="store.PP002Detail" v-if="currentTemplateComponent"
                  :key="store.PP002Detail.torDocumentTemplateCode" />
              </div>
            </TabPanel>

            <TabPanel value="document">
              <Card class="mb-4">
                <template #content>
                  <DocumentChEditor :docId="store.PP002Detail.torDraftDocumentId" :docName="new Date().toISOString()"
                    :readonly="!store.status.canEditTor || !menuStore.hasManage || !canEditDocument"
                    ref="documentReviewRef" :versions="store.PP002Detail.torDocumentVersions"
                    :key="`${store.PP002Detail.torDraftDocumentId}-${store.PP002Detail.status}`"
                    v-if="store.PP002Detail.torDraftDocumentId"
                    :canRestoreVersion="store.status.canEditTor && menuStore.hasManage && canEditDocument"
                    @restore-version="handleVersionRestored('Tor')" :save="saveDocument" />
                </template>
              </Card>
            </TabPanel>
            <TabPanel value="review">
              <Card class="mb-4">
                <template #content>
                  <ReviewChEditor :docId="store.PP002Detail.torDraftApprovalDocumentId"
                    :docName="new Date().toISOString()"
                    :readonly="!store.status.canEditTorApproval || !menuStore.hasManage" ref="documentRef"
                    :versions="store.PP002Detail.approvalDocumentVersions"
                    :key="store.PP002Detail.torDraftApprovalDocumentId"
                    v-if="store.PP002Detail.torDraftApprovalDocumentId"
                    :canRestoreVersion="store.status.canEditTor && menuStore.hasManage && canEditDocument"
                    @restore-version="handleVersionRestored('Approval')" :save="saveDocument" />
                </template>
              </Card>
            </TabPanel>
          </TabPanels>
        </Tabs>
      </div>
      <div class="lg:col-span-2 order-1 lg:order-2">
        <div class="flex flex-col gap-4 lg:ml-3 sticky top-[100px]">
          <div class="flex items-center gap-2 justify-end" v-if="menuStore.hasManage && !readonly">
            <ButtonSave label="ยกเลิกคำขอ" variant="outlined" v-if="store.status.conRestoreState" icon="pi pi-undo"
              severity="primary" @click="store.onRestoreStateAsync" />
            <ButtonSave label="บันทึกชั่วคราว" @click="onSubmitAsync(true)" v-if="store.status.canEditTor" />
            <ButtonSave label="ยืนยันบันทึก" type="submit" v-if="store.status.canEditTor" />
            <ButtonSendApprove @click="() => handleSubmit(onSendApproveAsync)"
              v-if="store.status.canEditTor && store.showDocument" />

            <ButtonRecall @click="onRecallAsync" v-if="store.status.canCommitteeRecall" />
            <ButtonNotAgree @click="() => store.onRejectAsync(AcceptorType.TorDraftCommittee)"
              v-if="store.status.canAcceptAndReject" />
            <ButtonApprove @click="() => store.onApproveAsync(AcceptorType.TorDraftCommittee)"
              v-if="store.status.canAcceptAndReject" />

            <ButtonSendEdit @click="() => store.onRejectAsync(AcceptorType.Approver)"
              v-if="store.status.canApproveAndReject" />
            <ButtonApprove @click="() => store.onApproveAsync(AcceptorType.Approver)"
              v-if="store.status.canApproveAndReject && !store.status.isLastApproval" />
            <ButtonConfirm @click="() => store.onApproveAsync(AcceptorType.Approver)"
              v-if="store.status.canApproveAndReject && store.status.isLastApproval" />

            <ButtonSendChange @click="onChangeAsync"
              v-if="store.status.canCancelOrChange && store.status.isAuthDepartment" />
            <ButtonSendCancel @click="onCancelAsync"
              v-if="store.status.canCancelOrChange && store.status.isAuthDepartment" />

            <ButtonRecall @click="onRecallAsync" v-if="store.status.canRecall" />
            <ButtonSendEdit @click="() => store.onRejectAsync(AcceptorType.DepartmentDirectorAgree)"
              v-if="store.status.canApproveAndRejectUnit" />
            <ButtonApprove @click="() => store.onApproveAsync(AcceptorType.DepartmentDirectorAgree)"
              v-if="store.status.canApproveAndRejectUnit" />

            <ButtonSendEdit @click="() => store.onRejectAssigneeAsync()" v-if="store.status.jorporCanAssign" />
            <ButtonSave @click="onSaveAssignAsync" text="บันทึกผู้รับผิดชอบ"
              v-if="store.status.jorporCanAssign || store.status.jorporCanAssignByAssignee" />
            <ButtonSave type="submit" v-if="store.status.assignCanAssign" />
            <ButtonConfirmAssign @click="onConfirmAssignAsync" v-if="store.status.jorporCanAssignByAssignee" />
            <ButtonApproveConfirm @click="onSendMdApproveAsync" v-if="store.status.assignCanAssign" />
          </div>

          <Accordion v-model:value="currentAccordion" unstyled multiple>
            <AccordionPanel value="999" class="mb-4" v-if="currentTab == 'document' || currentTab == 'review'">
              <AccordHeader label="Dictionary" />
              <AccordionContent>
                <div></div>
                <Card class="rounded-none overflow-auto h-[800px]">
                  <template #content>
                    <DocumentMapping pathToGet="procurement/tordraft"
                      @on-click-select="(text, hint) => setPlaceholderInDocument(text, hint)" />
                  </template>
                </Card>
              </AccordionContent>
            </AccordionPanel>
            <AccordionPanel value="0">
              <AccordAcceptor title="บุคคล/คณะกรรมการจัดทำร่างขอบเขตของงาน" v-model="store.PP002Detail.acceptors"
                :acceptor-type="AcceptorType.TorDraftCommittee" @set-is-unable-to-perform-duties="
                  (status: boolean, id: string, remark?: string) =>
                    handleSubmit(() => store.onUpdateDutieStatusAsync(id, status, remark))
                " :is-disable="![
                  PP002Status.Draft,
                  PP002Status.Rejected,
                  PP002Status.WaitingCommitteeApproval,
                ].includes(store.PP002Detail.status) ||
                  !store.PP002Detail.status ||
                  !menuStore.hasManage ||
                  !store.PP002Detail.id ||
                  readonly
                  " v-if="store.PP002Detail.acceptors" is-approve />
            </AccordionPanel>
            <AccordionPanel value="1" class="mt-5" v-if="!procurementStore.procurementDetail.hasMd">
              <AccordAcceptor title="ผู้มีอำนาจเห็นชอบ/อนุมัติ" v-model="store.PP002Detail.acceptors"
                :acceptor-type="AcceptorType.Approver" isManage
                :is-disable="!store.status.canEditTor || !menuStore.hasManage || readonly" @set-default="onSetDefaultAcceptorAsync"
                is-approve v-if="store.PP002Detail.acceptors"
                :is-set-default="store.status.isCanSetDefaultUnitnApprover && menuStore.hasManage" />
            </AccordionPanel>

            <AccordionPanel value="2" class="mt-5" v-if="procurementStore.procurementDetail.hasMd">
              <AccordAcceptor title="สายงานเห็นชอบ" v-model="store.PP002Detail.acceptors"
                :acceptor-type="AcceptorType.DepartmentDirectorAgree" isManage
                :is-set-default="store.status.isCanSetDefaultUnitnApprover && menuStore.hasManage"
                :is-disable="!store.status.canEditTor || !menuStore.hasManage || readonly" @set-default="onSetDefaultAcceptorAsync"
                is-approve v-if="store.PP002Detail.acceptors" />
            </AccordionPanel>

            <AccordionPanel value="3" class="mt-5" v-if="procurementStore.procurementDetail.hasMd">
              <AccordAssignee title="เจ้าหน้าที่พัสดุให้ความเห็น" v-model="store.PP002Detail.assignees" :disabled="(!store.status.jorporCanAssign &&
                !store.status.assignCanAssign &&
                !store.status.jorporCanAssignByAssignee) ||
                !menuStore.hasManage ||
                readonly
                " :is-comment="(store.status.isJorPorComment || !menuStore.hasManage) &&
                  !noCommentIf60AndBelowOneHundredธhousand
                  " @on-comment="(v) => store.onAssigneeCommentAsync(v.reason)" v-if="store.PP002Detail.assignees" />
            </AccordionPanel>
            <AccordionPanel value="4" class="mt-5" v-if="procurementStore.procurementDetail.hasMd">
              <AccordAcceptor title="ผู้มีอำนาจเห็นชอบ/อนุมัติ" v-model="store.PP002Detail.acceptors"
                :acceptor-type="AcceptorType.Approver" isManage :is-disable="(!store.status.assignCanAssign && !store.status.jorporCanAssign) ||
                  !menuStore.hasManage ||
                  readonly
                  " @set-default="store.onGetDefaultAcceptorAsync()" is-approve v-if="store.PP002Detail.acceptors"
                :is-set-default="(store.status.assignCanAssign || store.status.jorporCanAssign) &&
                  menuStore.hasManage
                  " />
            </AccordionPanel>
          </Accordion>
        </div>
      </div>
    </div>
  </Form>
  <SectionNavigator v-if="currentTab === 'detail' && sections.length > 0" :sections="sections"
    :active-section-id="activeSectionId" @navigate="scrollToSection" />
  <DialogReviewDocument v-model="showReviewDocumentDialog" :docId="reviwDocumentId"
    :docName="`pp002-${new Date().toISOString()}-${reviwDocumentId}`" @on-click-use-document="setDocumentReviewId" />
</template>
