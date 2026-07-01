<script setup lang="ts">
import { computed, defineAsyncComponent, nextTick, onMounted, onUnmounted, ref, watch, watchEffect } from 'vue';
import type { Option } from '@/models/shared/option';
import { Form } from 'vee-validate';
import { Accordion, AccordionPanel, Button, Tabs, TabPanels, TabPanel } from 'primevue';
import { TabHeader, TitleHeader } from '@/components/cosmetic';
import { AccordAcceptor, AccordAssignee } from '@/components/Accordions';
import { ButtonApprove, ButtonConfirmAssign, ButtonSave, ButtonSendApprove, ButtonSendEdit } from '@/components/Button';
import { AcceptorType, AssigneeType } from '@/enums/participants';
import { useAuthenticationStore } from '@/stores/authentication';
import { usePPDetailStore } from '../../../../stores/PP/ppStore';
import { usePP004Store } from '../../stores/PP004/pp004Store';
import { pp004CommitteeType, pp004status } from '@/views/PP/enums/pp004';
import PurchaseRequisionHelper from '@/helpers/purchaseRequisition';
import { showActivityDialog, showConfirmDialogAsync, showSaveOptionDialogAsync } from '@/helpers/dialog';
import { ConfirmDialogType } from '@/enums/dialog';
import ToastHelper from '@/helpers/toast';
import DocumentMapping from '@/components/DocumentMapping.vue';
import { BadgeStatus } from '@/components';
import DialogReviewDocument from '@/components/Document/DialogReviewDocument.vue';
import { useMenuStore } from '@/stores/menu';
import { HttpStatusCode } from 'axios';
import PreProcurementService from '@/services/PP/ppService';
import { SectionProcessType } from '@/enums/operations';
import { AssignDepartmentCodeEnum, OrganizationLevelEnum } from '@/enums/shared';
import { isBranchOrganizationLevel } from '@/helpers/organization';
import operationService from '@/services/Shared/operations';
import PP004Service from '../../services/PP004/pp004Service';
import { isCurrentPendingAcceptor } from '@/helpers/participants';

const pp004Store = usePP004Store();
const procurementStore = usePPDetailStore();
const menuStore = useMenuStore();
const userStore = useAuthenticationStore();

const { StatusName, MapStatusColor } = PurchaseRequisionHelper;

const props = defineProps({
  preProcurementId: {
    type: String,
  },
  budget: {
    type: Number,
    default: 0,
  },
  id: {
    type: String,
  },
  torId: {
    type: String,
  },
  readonly: {
    type: Boolean,
    default: false,
  },
});

const showReviewDocumentDialog = ref(false);
const reviwDocumentId = ref<string>('');
const currentTab = ref('0');
const currentAccordion = ref<string[]>([]);
const isFormDirty = ref(false);
const isInitialized = ref(false);

const HeaderItem = ref([
  { label: 'รายละเอียด', value: '0' },
  { label: 'เอกสารแจ้งข้อมูลเบื้องต้นเพื่อประกอบการจัดทำรายงานขอซื้อหรือขอจ้าง', value: '1' },
] as Option[]);

const PP004FullComponent = defineAsyncComponent(() => import('@/views/PP/components/PP004/components/PP004Full.vue'));
const PP004PartialComponent = defineAsyncComponent(() => import('@/views/PP/components/PP004/components/PP004Partial.vue'));
const PP004DocumentComponent = defineAsyncComponent(() => import('@/views/PP/components/PP004/components/PP004Document.vue'));

const documentRef = ref<InstanceType<typeof PP004DocumentComponent> | null>(null);

const isCommercialMaterialUnderDirectorDepartment = ref(false);

const canEditDocument = computed(() => {
  return pp004Store.body.requisition.status == pp004status.Draft
    || pp004Store.body.requisition.status == pp004status.Edit
    || pp004Store.body.requisition.status == pp004status.Rejected
});

watchEffect(async () => {
  if (pp004Store.body.id && !canEditDocument.value) {
    isCommercialMaterialUnderDirectorDepartment.value =
      pp004Store.body.assignees.length > 0 &&
      !pp004Store.body.assignees.some(a => a.assigneeType === AssigneeType.Director);
    return;
  }

  let processType: SectionProcessType =
    SectionProcessType.ApprovePurchaseRequest;

  const isCommercialMaterial =
    pp004Store.body.isCommercialMaterial ?? false;

  const isCommercialLike = isCommercialMaterial

  let byUserId = userStore.profile.id;

  if (isCommercialLike) {
    processType = SectionProcessType.ApprovePurchaseRequestCommercialParcel;

    const latestAssignee = pp004Store.body.assignees
      .filter(s => s.assigneeType === AssigneeType.Assignee)
      .sort((a, b) => b.sequence - a.sequence)[0];

    byUserId = latestAssignee?.userId ?? userStore.profile.id;
  }

  const { data, status } =
    await operationService.getOperationsDefaultAcceptorAsync(
      {
        budget: procurementStore.procurementDetail.budget,
        processType,
        supplyMethodCode: procurementStore.procurementDetail.supplyMethodCode,
        supplyMethodSpecialTypeCode:
          procurementStore.procurementDetail.supplyMethodSpecialTypeCode,
        userId: byUserId,
        skipCurrentEmployee: false,
      },
      true
    );

  if (status === HttpStatusCode.Ok) {
    const hasLevel300 = data.some(
      x => x.organizationLevel === OrganizationLevelEnum.Line
    );

    isCommercialMaterialUnderDirectorDepartment.value =
      (isCommercialLike ||
        isBranchOrganizationLevel(
          procurementStore.procurementDetail.departmentOrganizationLevel
        )) &&
      !hasLevel300;
  }
});

const getDefaultAcceptor = async () => {
  await pp004Store.getDefaultAcceptor();
};

const validateScopeOfWorkList = () => {
  if (pp004Store.body.scopeOfWorks.length < 1 && !procurementStore.procurementDetail.appoint) {
    ToastHelper.errorDescription('จะต้องมีข้อมูลขอบเขตของงานอย่างน้อย 1 รายการ');
    return false;
  }
  return true;
};

const validatePaymentConditionList = () => {
  if (pp004Store.body.paymentTerms && pp004Store.body.paymentTerms.length < 1) {
    ToastHelper.errorDescription('จะต้องมีข้อมูลเงื่อนไขการชำระเงินอย่างน้อย 1 รายการ');
    return false;
  }
  return true;
}

const validateFineRateList = () => {
  if (pp004Store.body.requisition.hasFineRate && pp004Store.body.fineRates.length < 1) {
    ToastHelper.errorDescription('จะต้องมีข้อมูลอัตราค่าปรับอย่างน้อย 1 รายการ');
    return false;
  }
  return true;
};

const validateAcceptors = (): boolean => {
  if (pp004Store.body.acceptors.length < 1) {
    ToastHelper.approvalAtLeastMessageToast();
    return false;
  }
  return true;
};

const validateProcurementCommittee = (): boolean => {
  const procurementCommittees = filterCommitteeByGroupType(pp004CommitteeType.ProcurementCommittee);

  if (procurementCommittees.length === 0) {
    ToastHelper.errorDescription('จะต้องมีผู้จัดซื้อ/คณะกรรมการจัดซื้อจัดจ้างอย่างน้อย 1 คน');
    return false;
  }

  if (pp004Store.body.isProcurementCommittee &&
    procurementCommittees.filter(f => f.committeePositionsCode === 'PosBoard001').length === 0) {
    ToastHelper.mustHaveLeaderBoardToast();
    return false;
  }

  return true;
};

const validateInspectionCommittee = (): boolean => {
  const inspectionCommittees = filterCommitteeByGroupType(pp004CommitteeType.InspectionCommittee);

  if (inspectionCommittees.length === 0) {
    ToastHelper.errorDescription('จะต้องมีผู้ตรวจรับพัสดุ/คณะกรรมการตรวจรับพัสดุอย่างน้อย 1 คน');
    return false;
  }

  if (pp004Store.body.isInspectCommittee &&
    inspectionCommittees.filter(f => f.committeePositionsCode === 'PosBoard001').length === 0) {
    ToastHelper.mustHaveLeaderBoardToast();
    return false;
  }

  return true;
};

const validateMaintenanceInspectionCommittee = (): boolean => {
  if (!pp004Store.body.requisition.hasInspectionCommittee) {
    return true;
  }

  const maintenanceCommittees = filterCommitteeByGroupType(pp004CommitteeType.MaintenanceInspectionCommittee);

  if (maintenanceCommittees.length === 0) {
    ToastHelper.errorDescription('จะต้องมีคณะกรรมการตรวจรับพัสดุ (งานจ้างบริการบำรุงรักษา) อย่างน้อย 1 คน');
    return false;
  }

  if (pp004Store.body.isMaCommittee &&
    maintenanceCommittees.filter(f => f.committeePositionsCode === 'PosBoard001').length === 0) {
    ToastHelper.mustHaveLeaderBoardToast();
    return false;
  }

  return true;
};

const validateConstructionSupervisor = (): boolean => {
  if (!pp004Store.body.requisition.hasConstructionSupervisor) {
    return true;
  }

  const supervisors = filterCommitteeByGroupType(pp004CommitteeType.ConstructionSupervisor);

  if (supervisors.length === 0) {
    ToastHelper.errorDescription('จะต้องมีผู้ควบคุมงาน (เฉพาะงานก่อสร้าง)อย่างน้อย 1 คน');
    return false;
  }

  if (pp004Store.body.isSupCommittee &&
    supervisors.filter(f => f.committeePositionsCode === 'PosBoard001').length === 0) {
    ToastHelper.mustHaveLeaderBoardToast();
    return false;
  }

  return true;
};

const validateSendApproval = (): boolean => {
  return validateScopeOfWorkList() &&
    validatePaymentConditionList() &&
    validateFineRateList() &&
    validateProcurementCommittee() &&
    validateInspectionCommittee() &&
    validateMaintenanceInspectionCommittee() &&
    validateConstructionSupervisor();
};

const filterCommitteeByGroupType = (groupType: pp004CommitteeType) => {
  return pp004Store.body.committees.filter(f => f.groupType === groupType);
};

// Save document to Collabora first before API call
const saveDocumentFirst = async (): Promise<void> => {
  if (currentTab.value === '1' && documentRef.value && 'saveDocumentFirst' in documentRef.value) {
    await documentRef.value.saveDocumentFirst();
  }
};

const saveDocument = async () => {
  await onSubmit();
};

const onSubmit = async (status?: pp004status, skipSaveOption = false): Promise<void> => {
  await saveDocumentFirst();

  // Set flag so backend re-replaces document — only when form data changed
  if (status === pp004status.WaitingApproval) {
    pp004Store.body.requisition.isPurchaseRequisitionDocumentIdReplaced = false;
  }

  if ((status === pp004status.WaitingApproval || isCommercialMaterialUnderDirectorDepartment.value) && !validateSendApproval()) {
    return;
  }

  if (status === pp004status.WaitingApproval && !validateAcceptors()) {
    return;
  }

  if (status && status === pp004status.WaitingApproval && !await showConfirmDialogAsync(ConfirmDialogType.SendApprove)) return;

  if (status && status === pp004status.Edit && !await showConfirmDialogAsync(ConfirmDialogType.Edit)) return;

  if (pp004Store.body.id) {
    if (!skipSaveOption && isFormDirty.value && pp004Store.body.requisition.purchaseRequisitionDocumentId && !status && [pp004status.Edit, pp004status.Draft, pp004status.Rejected].includes(status ?? pp004Store.body.requisition.status)) {
      const saveOption = await showSaveOptionDialogAsync();
      if (saveOption === undefined) return;
      pp004Store.body.requisition.isPurchaseRequisitionDocumentIdReplaced = saveOption;
    }

    isInitialized.value = false;
    await pp004Store.updatePp004Async(status);
    isFormDirty.value = false;
    await nextTick();
    isInitialized.value = true;
    setCurrentTab();
    return;
  }

  isInitialized.value = false;
  const newId = await pp004Store.onCreateAsync(props.preProcurementId!, props.torId);
  isFormDirty.value = false;
  setCurrentTab();

  if (newId) {
    pp004Store.body.id = newId;


    await procurementStore.onGetProcurementById(props.preProcurementId!);
    await pp004Store.getPp004ByIdAsync(newId);
  }
  await nextTick();
  isInitialized.value = true;
};

const setCurrentTab = async () => {
  if (pp004Store.body.requisition.purchaseRequisitionDocumentId
    && !isCommercialMaterialUnderDirectorDepartment.value) {
    currentTab.value = '1';
  }
};

const onAssignedAsync = async () => {
  if (!procurementStore.procurementDetail.id) return;

  if (!pp004Store.body.assignees.some(s => [AssigneeType.Assignee].includes(s.assigneeType))) {
    return ToastHelper.assignAtLeastMessageToast();
  }

  if (!await showConfirmDialogAsync(ConfirmDialogType.Assigned)) return;

  if (pp004Store.body.id) {
    await pp004Store.updatePp004Async(pp004status.Approved);
    await procurementStore.onGetProcurementById(procurementStore.procurementDetail.id);
  }
};

const onRejectAssignedAsync = async () => {
  if (!await showConfirmDialogAsync(ConfirmDialogType.SendEdit)) return;

  if (pp004Store.body.id) {
    await pp004Store.updatePp004Async(pp004status.Rejected);
  }
}

const currentUserIsFirstPendingAcceptor = computed(() => {
  if (!pp004Store.body.acceptors) return false;
  return pp004Store.body.requisition.status == pp004status.WaitingApproval
    && isCurrentPendingAcceptor(pp004Store.body.acceptors, userStore.profile.id, AcceptorType.Approver);
});

const getDetailAsync = async (preProcurementId: string): Promise<void> => {

  const { data, status } = await PreProcurementService.getProcurementByIdAsync(preProcurementId);

  if (status === HttpStatusCode.Ok) {
    pp004Store.body.isCommercialMaterial = data.isCommercialMaterial;
    pp004Store.body.budget = data.budget;
    pp004Store.body.supplyMethodCode = data.supplyMethodCode;
  }
};

onMounted(async () => {
  if (!props.id && props.preProcurementId && props.torId) {
    await pp004Store.getPp004ByIdAsync();
  }

  if (props.preProcurementId
    && pp004Store.body.budget === undefined
    && pp004Store.body.supplyMethodCode === ''
    && pp004Store.body.isCommercialMaterial === undefined) {
    await getDetailAsync(props.preProcurementId);
  }

  if (pp004Store.body.acceptors.length == 0) {
    await getDefaultAcceptor();
  };

  await Promise.all([
    pp004Store.fetchPositionInspOptions(),
    pp004Store.fetchPositionSupOptions(),
    pp004Store.fetchPositionMaOptions(),
    pp004Store.fetchPositionProcOptions(),
    pp004Store.fetchDateTypeOptions(),
    pp004Store.fetchDeliveryConditionOptions(),
    pp004Store.getAssignDepartmentDDLAsync(),
  ]);

  if (props.id) {
    await pp004Store.getPp004ByIdAsync(props.id);

  };

  if (!props.id) {
    await pp004Store.setDefaultJorPorDirectorAsync(false, isCommercialMaterialUnderDirectorDepartment.value);
  }

  pp004Store.body.requisition.isPurchaseRequisitionDocumentIdReplaced = false;

  await nextTick();
  isInitialized.value = true;
});

watch(
  () => pp004Store.body,
  () => {
    if (isInitialized.value) {
      isFormDirty.value = true;
    }
  },
  { deep: true }
);

watchEffect(() => {
  HeaderItem.value = isCommercialMaterialUnderDirectorDepartment.value
    ? [{ label: 'รายละเอียด', value: '0' }]
    : [
      { label: 'รายละเอียด', value: '0' },
      { label: 'เอกสารแจ้งข้อมูลเบื้องต้นเพื่อประกอบการจัดทำรายงานขอซื้อหรือขอจ้าง', value: '1' },
    ];
});

onUnmounted(async () => {
  pp004Store.clearBody();
})

const setDocumentReviewId = async (id: string): Promise<void> => {
  isInitialized.value = false;
  pp004Store.body.requisition.purchaseRequisitionDocumentId = id;
  await nextTick();
  isInitialized.value = true;
};

// No longer need to call getReviewDocumentAsync() on page load.
// ChEditor now shows version snapshot directly for readonly statuses,
// and working copy for editable statuses (via resolveWopiFileId).
const isReviewDocumentReady = ref(true);

const defaultDocumentStatuses = [
  pp004status.WaitingApproval,
  pp004status.WaitingAssign,
  pp004status.Approved
];

watch(() => pp004Store.body.requisition.status, (newStatus: pp004status) => {
  if ([pp004status.Draft, pp004status.Edit, pp004status.Rejected, pp004status.WaitingApproval].includes(newStatus)) {
    currentAccordion.value = ['0'];
  }

  if ([pp004status.WaitingAssign].includes(newStatus)) {
    currentAccordion.value = ['1'];
  }

  if ([pp004status.Approved, pp004status.Cancelled].includes(newStatus)) {
    currentAccordion.value = ['0', '1'];
  }

  if (defaultDocumentStatuses.includes(newStatus) && !isCommercialMaterialUnderDirectorDepartment.value && pp004Store.body.id) {
    currentTab.value = '1';
  } else if (isCommercialMaterialUnderDirectorDepartment.value || !pp004Store.body.id) {
    currentTab.value = '0';
  }
}, { immediate: true });

watch(isCommercialMaterialUnderDirectorDepartment, (isUnderDirector) => {
  if (isUnderDirector) {
    currentTab.value = '0';
  }
});

const onAssignSegmentApproverAsync = async (assignSegmentCode: string) => {
  switch (assignSegmentCode) {
    case AssignDepartmentCodeEnum.SegmentJorPorOther:
      await pp004Store.getDefaultSegmentOtherManagerApproverAsync();
      break;
    case AssignDepartmentCodeEnum.SegmentJorPorIT:
      await pp004Store.getDefaultSegmentITManagerApproverAsync();
      break;
    default:
      break;
  }
};

const handleRestoreVersion = async (): Promise<void> => {
  if (!procurementStore.procurementDetail.id || !pp004Store.body.id) {
    ToastHelper.error('เกิดข้อผิดพลาด', 'ไม่สามารถรีเซ็ตเอกสารได้ ข้อมูลไม่ครบถ้วน');
    return;
  }

  const { status } = await PP004Service.resetDocumentAsync(procurementStore.procurementDetail.id, pp004Store.body.id);

  if (status === HttpStatusCode.Ok) {
    ToastHelper.success('สำเร็จ', 'ทำการรีเซ็ตเอกสารสำเร็จ');
    await pp004Store.getPp004ByIdAsync(pp004Store.body.id);
  }
};
</script>

<template>
  <div class="mt-4">
    <div class="my-2">
      <TitleHeader label="การแจ้งข้อมูลเบื้องต้น (จพ.004)">
        <template #action>
          <BadgeStatus :color="MapStatusColor(pp004Store.body.requisition.status).color"
            :label="StatusName(pp004Store.body.requisition.status)" v-if="pp004Store.body.requisition.status" />
          <Button label="ประวัติการใช้งาน" icon="pi pi-refresh" variant="outlined" severity="primary"
            v-if="pp004Store.body.id" class="bg-white! hover:bg-red-50!"
            @click="() => showActivityDialog(pp004Store.body.id!)" />
        </template>
      </TitleHeader>
    </div>
    <Form @submit="onSubmit()" @invalidSubmit="ToastHelper.invalidMessageToast()" v-slot="{ handleSubmit }">
      <div class="grid grid-cols-1 lg:grid-cols-7 gap-2">
        <div class="lg:col-span-5 lg:order-1 order-2">
          <div>
            <Tabs :value="currentTab" unstyled @update:value="(tab) => currentTab = tab.toString()">
              <TabHeader :items="HeaderItem.filter((h, i) => pp004Store.body.id ? h : i === 0)"
                class="sticky top-14 z-3 pt-2 bg-[#F7F7F7]" />
              <TabPanels>
                <TabPanel value="0">
                  <PP004FullComponent :pre-procurement-id="props.preProcurementId" :budget="props.budget"
                    v-if="!torId" />
                  <PP004PartialComponent :pre-procurement-id="props.preProcurementId" :budget="props.budget"
                    :tor-id="props.torId" v-else />
                </TabPanel>
                <TabPanel value="1" v-if="!isCommercialMaterialUnderDirectorDepartment">
                  <PP004DocumentComponent
                    v-if="pp004Store.body.requisition.purchaseRequisitionDocumentId && isReviewDocumentReady"
                    v-model="pp004Store.body.requisition.purchaseRequisitionDocumentId"
                    :readonly="!pp004Store.IsEdit || !menuStore.hasManage || !canEditDocument" ref="documentRef"
                    :save="saveDocument" :versions="pp004Store.body.documentVersions"
                    :canRestoreVersion="pp004Store.IsEdit && menuStore.hasManage && canEditDocument"
                    @restore-version="handleRestoreVersion" />
                </TabPanel>
              </TabPanels>
            </Tabs>
          </div>
        </div>

        <div class="relative lg:col-span-2 lg:order-2 order-1">
          <div class="flex flex-col gap-4 lg:ml-3 sticky top-[100px]">
            <div class="flex items-center gap-2 justify-end" v-if="menuStore.hasManage && !readonly">
              <ButtonSendEdit v-if="pp004Store.isJorPorDirectorAssignee"
                @click="onRejectAssignedAsync()" />
              <ButtonSave text="บันทึกผู้รับผิดชอบ" type="button"
                @click="onSubmit(undefined, true)"
                v-if="pp004Store.body.requisition.status == pp004status.Approved && (pp004Store.isJorPorAssignee || pp004Store.isJorPorDirectorAssignee || pp004Store.assigneesCanAssign)" />
              <ButtonSave :text="pp004Store.body.requisition.status == pp004status.WaitingAssign
                  ? 'บันทึกมอบหมาย'
                  : undefined" type="button"
                @click="handleSubmit(() => onSubmit())"
                v-if="pp004Store.body.requisition.status != pp004status.Approved && (pp004Store.IsEdit || pp004Store.isJorPorAssignee || pp004Store.isJorPorDirectorAssignee || pp004Store.assigneesCanAssign)" />
              <ButtonSendApprove @click="handleSubmit(() => onSubmit(pp004status.WaitingApproval))"
                v-if="pp004Store.IsEdit && pp004Store.body.id" />

              <ButtonRecall v-if="pp004Store.canRecall" @click="onSubmit(pp004status.Edit)" />
              <ButtonSendEdit v-if="currentUserIsFirstPendingAcceptor && !pp004Store.IsEdit"
                @click="pp004Store.rejectPp004Async()" />
              <ButtonApprove @click="() => pp004Store.approvePp004Async()" v-if="currentUserIsFirstPendingAcceptor" />

              <ButtonConfirmAssign v-if="pp004Store.isJorPorAssignee" @click="handleSubmit(() => onAssignedAsync())" />
            </div>
            <Accordion v-model:value="currentAccordion" unstyled multiple>
              <AccordionPanel value="999" class="mb-4" v-if="currentTab !== '0'">
                <AccordHeader label="Dictionary" />
                <AccordionContent>
                  <div></div>
                  <Card class="rounded-none overflow-auto h-[800px]">
                    <template #content>
                      <DocumentMapping pathToGet="JorPor04" @on-click-select="
                        (text, hint) => documentRef?.setPlaceholderInDocument(text, hint)
                      " />
                    </template>
                  </Card>
                </AccordionContent>
              </AccordionPanel>
              <AccordionPanel class="mb-4" :value="'0'" v-if="!isCommercialMaterialUnderDirectorDepartment">
                <AccordAcceptor title="ผู้มีอำนาจเห็นชอบ" v-model="pp004Store.body.acceptors"
                  :acceptor-type="AcceptorType.Approver" isManage
                  :is-disable="!pp004Store.IsEdit || !menuStore.hasManage || readonly" @set-default="() => getDefaultAcceptor()"
                  :is-set-default="pp004Store.isCanSetDefaultAcceptor && menuStore.hasManage" />
              </AccordionPanel>
              <AccordionPanel value="1" class="mt-4">
                <AccordAssignee title="มอบหมายผู้รับผิดชอบ" v-model="pp004Store.body.assignees" :isComment="false"
                  :is-dropdown="pp004Store.isJorPorAssignee || pp004Store.isJorPorDirectorAssignee"
                  :dropdown="pp004Store.assignDepartmentDDL" v-on:change="onAssignSegmentApproverAsync"
                  :disabled="(!pp004Store.isJorPorAssignee && !pp004Store.isJorPorDirectorAssignee && !pp004Store.assigneesCanAssign) || !menuStore.hasManage || readonly" />
              </AccordionPanel>
            </Accordion>
          </div>
        </div>
      </div>
    </Form>
    <DialogReviewDocument v-model="showReviewDocumentDialog" :docId="reviwDocumentId"
      :docName="`pp004-${new Date().toISOString()}-${reviwDocumentId}`" @on-click-use-document="setDocumentReviewId" />
  </div>
</template>