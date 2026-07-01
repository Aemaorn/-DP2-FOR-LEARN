<script setup lang="ts">
import type { Option } from '@/models/shared/option';
import { BadgeStatus } from '@/components';
import { ButtonApprove, ButtonConfirm, ButtonConfirmAssign, ButtonSave, ButtonSendApprove, ButtonSendEdit, ButtonNotAgree } from '@/components/Button';
import { TitleHeader } from '@/components/cosmetic';
import { Button } from 'primevue';
import { Form } from 'vee-validate';
import { computed, defineAsyncComponent, nextTick, ref, watch, onMounted } from 'vue';
import { AccordAcceptor, AccordAssignee } from '@/components/Accordions';
import { AcceptorType, AssigneeType } from '@/enums/participants';
import { usePurchaseOrder } from '../../stores/PP007/PP007Store';
import { usePPDetailStore } from '../../../../stores/PP/ppStore';
import { PurchaseOrderStatus } from '../../enums/pp007';
import ToastHelper from '@/helpers/toast';
import purchaseOrderHelper from '@/helpers/purchaseOrder';
import { showActivityDialog, showConfirmDialogAsync, showSaveOptionDialogAsync } from '@/helpers/dialog';
import { ConfirmDialogType } from '@/enums/dialog';
import DocumentMapping from '@/components/DocumentMapping.vue';
import type { PP007Detail } from '../../models/PP007/pp007Model';
import DialogReviewDocument from '@/components/Document/DialogReviewDocument.vue';
import { useMenuStore } from '@/stores/menu';
import { useLoadingStore } from '@/stores/loading';
import ButtonRecall from '@/components/Button/ButtonRecall.vue';
import { ProcurementStatus } from '@/enums/procurement';
import { HttpStatusCode } from 'axios';
import { SectionProcessType } from '@/enums/operations';
import operationService from '@/services/Shared/operations';
import pp007service from '../../services/PP007/PP007Service';

const props = defineProps({ readonly: { type: Boolean, default: false } });

const DetailComponent = defineAsyncComponent(() => import('./components/PP007Detail.vue'));
const jp006Component = defineAsyncComponent(() => import('./components/Jp006Document.vue'));
const winnerComponrt = defineAsyncComponent(() => import('./components/WinnerDocument.vue'));

const jp006DocumentRef = ref<InstanceType<typeof jp006Component> | null>(null);
const winnerDocumentRef = ref<InstanceType<typeof winnerComponrt> | null>(null);

const showReviewDocumentDialog = ref(false);
const reviwDocumentId = ref<string>('');
const currentTab = ref('0');
const currentAccordion = ref<string[]>([]);
const isFormDirty = ref(false);
const isInitialized = ref(false);
const headerItem = ref([
  { label: 'รายละเอียด', value: "0" },
  { label: 'เอกสาร จพ.006', value: "1" },
  { label: 'เอกสารประกาศผู้ชนะ', value: "2" },
] as Option[]);

const { MapStatusColor } = purchaseOrderHelper;

const menuStore = useMenuStore();
const store = usePurchaseOrder();
const procurementStore = usePPDetailStore();
const loadingStore = useLoadingStore();
const isSixtyAndMoreThanOneHundredThousand = computed(() => {
  return procurementStore.procurementDetail.supplyMethodCode == "SMethod002" && procurementStore.procurementDetail.budget > 100000;
});

onMounted(async () => {
  if (procurementStore.procurementDetail.purchaseOrder?.id) {

    if (isSixtyAndMoreThanOneHundredThousand.value) {
      headerItem.value = [{ label: 'รายละเอียด', value: "0" }];
    }

    await store.getDefaultData(procurementStore.procurementDetail.purchaseOrder.id);

  } else {
    await store.getDefaultData();

    if (store.body.acceptors.filter(x => x.acceptorType == AcceptorType.Approver).length == 0 && store.isSixtyMorethan100k) {
      await store.getDefaultSegmentAsync();
    }
  }

  await nextTick();
  isInitialized.value = true;
});

watch(
  () => store.body,
  () => {
    if (isInitialized.value) {
      isFormDirty.value = true;
    }
  },
  { deep: true }
);

const saveDocumentFirst = async (): Promise<void> => {
  if (currentTab.value === '1' && jp006DocumentRef.value && 'saveDocumentFirst' in jp006DocumentRef.value) {
    await jp006DocumentRef.value.saveDocumentFirst();
  } else if (currentTab.value === '2' && winnerDocumentRef.value && 'saveDocumentFirst' in winnerDocumentRef.value) {
    await winnerDocumentRef.value.saveDocumentFirst();
  }
};

const onSaveDocument = async () => {
  jp006DocumentRef.value && await jp006DocumentRef.value.saveDocumentFirst();
  winnerDocumentRef.value && await winnerDocumentRef.value.saveDocumentFirst();
}

const saveDocument = async () => {
  loadingStore.setIsLoading(true);
  try {
    await saveDocumentFirst();
  } finally {
    loadingStore.setIsLoading(false);
  }
  await onSubmitAsync();
};

const handleRestoreJp006Version = async (): Promise<void> => {
  const jp006Id = store.body.jp006Id;
  const procurementId = procurementStore.procurementDetail.id;
  if (!jp006Id || !procurementId) {
    ToastHelper.error('เกิดข้อผิดพลาด', 'ไม่สามารถรีเซ็ตเอกสารได้ ข้อมูลไม่ครบถ้วน');
    return;
  }
  const { status } = await pp007service.resetDocumentAsync(procurementId, jp006Id, 'Jp006');
  if (status === HttpStatusCode.Ok) {
    ToastHelper.success('สำเร็จ', 'ทำการรีเซ็ตเอกสารสำเร็จ');
    await store.getJp006ByIdAsync();
  }
};

const handleRestoreWinnerVersion = async (): Promise<void> => {
  const jp006Id = store.body.jp006Id;
  const procurementId = procurementStore.procurementDetail.id;
  if (!jp006Id || !procurementId) {
    ToastHelper.error('เกิดข้อผิดพลาด', 'ไม่สามารถรีเซ็ตเอกสารได้ ข้อมูลไม่ครบถ้วน');
    return;
  }
  const { status } = await pp007service.resetDocumentAsync(procurementId, jp006Id, 'Winner');
  if (status === HttpStatusCode.Ok) {
    ToastHelper.success('สำเร็จ', 'ทำการรีเซ็ตเอกสารสำเร็จ');
    await store.getJp006ByIdAsync();
  }
};

const onSubmitAsync = async () => {
  loadingStore.setIsLoading(true);
  try {
    await onSaveDocument();
  } finally {
    loadingStore.setIsLoading(false);
  }

  if (store.body.jp006Id) {
    if (isFormDirty.value
      && (store.body.jp006DocumentId || store.body.winnerDocumentId) && [PurchaseOrderStatus.Draft, PurchaseOrderStatus.Edit, PurchaseOrderStatus.Rejected].includes(store.body.status)) {
      const saveOption = await showSaveOptionDialogAsync();
      if (saveOption === undefined) return;
      store.body.isJp006DocumentIdReplaced = saveOption;
      store.body.isWinnerDocumentIdReplaced = saveOption;
    }

    isInitialized.value = false;
    await store.onUpdateJp006Async();

    store.body.isJp006DocumentIdReplaced = false;
    store.body.isWinnerDocumentIdReplaced = false;
    isFormDirty.value = false;
    await nextTick();
    isInitialized.value = true;
    if (!isSixtyAndMoreThanOneHundredThousand.value) {
      setCurrentTab();
    }
    return;
  }

  isInitialized.value = false;
  await store.onCreateJp006Async();
  isFormDirty.value = false;
  await nextTick();
  isInitialized.value = true;
  if (!isSixtyAndMoreThanOneHundredThousand.value) {
    setCurrentTab();
  }
};

const setCurrentTab = async () => {
  if (store.body.jp006DocumentId) {
    currentTab.value = '1';
  } else if (store.body.winnerDocumentId) {
    currentTab.value = '2';
  }
};

// Helpers for validation to reduce complexity
const hasWinner = (b: PP007Detail): boolean => b.entrepreneurs.some((x) => x.isWinner);
const isDueDiligenceMissingForAll = (b: PP007Detail): boolean =>
  b.entrepreneurs.every(
    (e) =>
      e.coi?.result == null ||
      e.egp?.result == null ||
      e.watchlist?.result == null
  );
const hasCommitteeAcceptor = (b: PP007Detail): boolean =>
  b.acceptors.some((s) => s.acceptorType === AcceptorType.ProcurementCommittee);
const hasAnyBidding = (b: PP007Detail): boolean => b.entrepreneurs.some((x) => x.isBidding);
const hasApprover = (b: PP007Detail): boolean => b.acceptors.some((s) => s.acceptorType === AcceptorType.Approver);
const hasAssignee = (b: PP007Detail): boolean => b.assignees.some((s) => s.assigneeType === AssigneeType.Assignee);
const hasAssigneeRemarks = (b: PP007Detail): boolean => b.assignees.filter((x) => x.remark).length > 0;

const validatePurchaseOrder = (
  status: PurchaseOrderStatus,
  isUnderDirectorDepartment: boolean,
  body: PP007Detail
): boolean => {
  // Always require at least one winner
  if (!hasWinner(body)) {
    ToastHelper.winnerAtLeastMessageToast();
    return false;
  }

  const checks: Array<() => boolean> = [];

  if (status === PurchaseOrderStatus.WaitingCommitteeApproval) {
    checks.push(
      () => {
        if (isDueDiligenceMissingForAll(body)) {
          ToastHelper.warning('ข้อมูลไม่ถูกต้อง', 'กรุณาตรวจข้อมูล COI, Watchlist, ผู้ทิ้งงาน (e-GP) ให้ครบถ้วน');
          return false;
        }
        return true;
      },
      () => {
        if (!store.isSixtyMorethan100k && !hasCommitteeAcceptor(body)) {
          ToastHelper.committeeAtLeastMessageToast();
          return false;
        }
        return true;
      },
      () => {
        if (!hasAnyBidding(body)) {
          ToastHelper.errorDescription('กรุณาบันทึกรายละเอียดราคา');
          return false;
        }
        return true;
      },
      () => {
        if (!store.isSixtyMorethan100k && !isUnderDirectorDepartment && !hasApprover(body)) {
          ToastHelper.departmentAtLeastMessageToast();
          return false;
        }
        return true;
      },
    );
  }

  if (status === PurchaseOrderStatus.WaitingComment && isUnderDirectorDepartment) {
    checks.push(
      () => {
        if (!hasAssignee(body)) {
          ToastHelper.assignAtLeastMessageToast();
          return false;
        }
        return true;
      },
      () => {
        if (!hasApprover(body)) {
          ToastHelper.approvalAtLeastMessageToast();
          return false;
        }
        return true;
      },
    );
  }

  if (status === PurchaseOrderStatus.WaitingApproval) {
    checks.push(
      () => {
        if (!store.isSixtyMorethan100k && isUnderDirectorDepartment && !hasAssigneeRemarks(body)) {
          ToastHelper.assignneeCommentAtLeastMessageToast();
          return false;
        }
        return true;
      },
      () => {
        if (!store.isSixtyMorethan100k && !isUnderDirectorDepartment && !hasApprover(body)) {
          ToastHelper.approvalAtLeastMessageToast();
          return false;
        }
        return true;
      },
    );
  }

  for (const check of checks) {
    if (!check()) return false;
  }

  return true;
};

const onActionByStatusAsync = async (status: PurchaseOrderStatus.WaitingCommitteeApproval | PurchaseOrderStatus.WaitingApproval | PurchaseOrderStatus.WaitingComment | PurchaseOrderStatus.Edit) => {
  const { body, onUpdateJp006Async, onCreateJp006Async } = store;

  if (!validatePurchaseOrder(status, isUnderDirectorDepartment.value, body)) {
    return;
  }

  if ([PurchaseOrderStatus.WaitingCommitteeApproval, PurchaseOrderStatus.WaitingApproval].includes(status) && !await showConfirmDialogAsync(ConfirmDialogType.SendApproveConfirm)) return;

  if (status === PurchaseOrderStatus.Edit && !await showConfirmDialogAsync(ConfirmDialogType.Edit)) return;

  if (PurchaseOrderStatus.WaitingComment === status && !await showConfirmDialogAsync(ConfirmDialogType.Assigned)) return;

  loadingStore.setIsLoading(true);
  try {
    if (body.jp006Id) {

      store.body.isJp006DocumentIdReplaced = false;
      store.body.isWinnerDocumentIdReplaced = false;

      // Use dedicated SendApprove endpoint for WaitingComment → WaitingApproval
      if (status === PurchaseOrderStatus.WaitingApproval && body.status === PurchaseOrderStatus.WaitingComment) {
        await onSaveDocument();
        await store.onSendApproveAsync();
        return;
      }

      // Use dedicated RecallToComment endpoint for WaitingApproval → WaitingComment
      if (status === PurchaseOrderStatus.WaitingComment && body.status === PurchaseOrderStatus.WaitingApproval) {
        await store.onRecallToCommentAsync();
        return;
      }

      if (status === PurchaseOrderStatus.WaitingComment) {
        await store.getDefaultAcceptorWithCondition();
      }

      await onUpdateJp006Async(status);

      return;
    }

    await onCreateJp006Async(status as PurchaseOrderStatus.WaitingCommitteeApproval | PurchaseOrderStatus.WaitingApproval);
  } finally {
    loadingStore.setIsLoading(false);
  }
};

const totalWinnerPrice = computed(() => {
  return store.body.entrepreneurs
    .filter(x => x.isWinner)
    .reduce((sum, x) => {
      const price = x.priceDetails?.reduce((s, p) => s + (p.agreedPrice * p.parcelQuantity || 0), 0) || 0;
      return sum + price;
    }, 0);
});

watch(totalWinnerPrice, async () => {

  if (store.body.acceptors.filter(x => x.acceptorType == AcceptorType.Approver).length == 0) {
    await store.getDefaultAcceptor();
  }
});

const setDocumentReviewId = async (id: string): Promise<void> => {
  isInitialized.value = false;

  let documentType = '';
  if (currentTab.value === '1') {
    documentType = 'Jp006';
  } else if (currentTab.value === '2') {
    documentType = 'Winner';
  }

  if (documentType === 'Jp006') {
    store.body.jp006DocumentId = id;
  } else if (documentType === 'Winner') {
    store.body.winnerDocumentId = id;
  }

  await nextTick();
  isInitialized.value = true;
};

const canEditDocument = computed(() => {
  return store.body.status == PurchaseOrderStatus.Draft
    || store.body.status == PurchaseOrderStatus.Edit
    || store.body.status == PurchaseOrderStatus.Rejected
});

const defaultDocumentStatuses = [
  PurchaseOrderStatus.WaitingCommitteeApproval,
  PurchaseOrderStatus.WaitingAssign,
  PurchaseOrderStatus.WaitingComment,
  PurchaseOrderStatus.WaitingApproval,
  PurchaseOrderStatus.Approved,
  PurchaseOrderStatus.RejectToAssignee,
];

watch(() => store.body.status, (newStatus: PurchaseOrderStatus) => {
  if (defaultDocumentStatuses.includes(newStatus) && !isSixtyAndMoreThanOneHundredThousand.value) {
    currentTab.value = '1';
  }

  if ([PurchaseOrderStatus.Approved, PurchaseOrderStatus.Cancelled].includes(newStatus)) {
    currentAccordion.value = ['0', '1', '2', '3'];
  }

  if (store.body.isBp) {
    if ([PurchaseOrderStatus.Draft, PurchaseOrderStatus.Edit, PurchaseOrderStatus.Rejected, PurchaseOrderStatus.WaitingCommitteeApproval].includes(newStatus)) {
      currentAccordion.value = ['0'];
    }

    if ([PurchaseOrderStatus.WaitingAssign, PurchaseOrderStatus.WaitingComment, PurchaseOrderStatus.RejectToAssignee].includes(newStatus)) {
      currentAccordion.value = ['2', '3'];
    }

    if ([PurchaseOrderStatus.WaitingApproval].includes(newStatus)) {
      currentAccordion.value = ['3'];
    }

    return;
  }

  if (store.isSixtyMorethan100k) {
    currentAccordion.value = ['0'];

    return;
  }

  if ([PurchaseOrderStatus.Draft, PurchaseOrderStatus.Edit, PurchaseOrderStatus.Rejected].includes(newStatus)) {
    currentAccordion.value = ['0', '1'];
  }

  if ([PurchaseOrderStatus.WaitingCommitteeApproval].includes(newStatus)) {
    currentAccordion.value = ['0'];
  }

  if ([PurchaseOrderStatus.WaitingAssign, PurchaseOrderStatus.RejectToAssignee].includes(newStatus)) {
    currentAccordion.value = ['2'];
  }

  if ([PurchaseOrderStatus.WaitingComment, PurchaseOrderStatus.RejectToAssignee].includes(newStatus)) {

    currentAccordion.value = ['2', '3'];
  }

  if ([PurchaseOrderStatus.WaitingApproval].includes(newStatus)) {
    currentAccordion.value = ['1', '3'];
  }

}, { immediate: true });

const isUnderDirectorDepartment = ref(false);

const setUnderDirectorDepartment = async (budget: number): Promise<void> => {

  if (!budget) {
    return;
  }

  let processType: SectionProcessType = SectionProcessType.PurchaseOrder;

  const detail = procurementStore.procurementDetail;
  if (detail.isCommercialMaterial) {
    processType = SectionProcessType.PurchaseOrderCommercialParcel;
  }

  const lastAssignee = store.body.operators.length > 0
    ? store.body.operators.reduce((prev, curr) => curr.sequence > prev.sequence ? curr : prev, store.body.operators[0])
    : undefined;

  const list = store.body.acceptors.filter(x =>
    x.departmentCode === detail.departmentCode &&
    x.acceptorType === AcceptorType.ProcurementCommittee
  );

  const lastAcceptorsCommittee =
    list.length > 0
      ? list.reduce((a, b) => a.sequence > b.sequence ? a : b, list[0])
      : undefined;

  const operationUserId = procurementStore.procurementDetail.isCommercialMaterial ?
    !lastAcceptorsCommittee
      ? lastAssignee?.userId
      : lastAcceptorsCommittee.userId
    : lastAssignee?.userId;

  if (!operationUserId) return;

  const { data, status } =
    await operationService.getOperationsDefaultAcceptorAsync(
      {
        budget: budget,
        processType,
        supplyMethodCode: procurementStore.procurementDetail.supplyMethodCode,
        supplyMethodSpecialTypeCode: procurementStore.procurementDetail.supplyMethodSpecialTypeCode,
        userId: operationUserId,
        skipCurrentEmployee: false,
      },
      true
    );

  if (status === HttpStatusCode.Ok) {

    const hasLevel300 = data.some(
      x => x.organizationLevel === 300
    );

    isUnderDirectorDepartment.value = hasLevel300;

    if (hasLevel300 && store.body.assignees.length === 0) {
      await store.getDefaultJorporAsync();
    }

    if (!hasLevel300) {
      store.body.assignees = [];
    }
  }
};

const onAssigneeComment = async (e: string) => {
  await onSaveDocument();

  await store.onCommentAsync(e);
}

watch(
  totalWinnerPrice,
  (price) => {

    setUnderDirectorDepartment(price);
  },
  { immediate: true }
);
</script>

<template>
  <Form @submit="onSubmitAsync()" v-slot="{ handleSubmit }">
    <div class="mt-4">
      <div class="my-2">
        <TitleHeader label="จัดทำรายงานผลการพิจารณาและขออนุมัติสั่งซื้อ/สั่งจ้าง ( จพ.006 )">
          <template #action>
            <BadgeStatus :label="MapStatusColor(store.body.status, store.body.isBp).label"
              :color="MapStatusColor(store.body.status, store.body.isBp).color" />
            <Button label="ประวัติการใช้งาน" icon="pi pi-refresh" variant="outlined" severity="primary"
              v-if="store.body.jp006Id" class="bg-white! hover:bg-red-50!"
              @click="() => showActivityDialog(store.body.jp006Id!)" />
          </template>
        </TitleHeader>
      </div>
      <div class="grid grid-cols-1 lg:grid-cols-7 gap-2">
        <div class="lg:col-span-5">
          <div>
            <Tabs :value="currentTab" unstyled @update:value="(tab) => currentTab = tab.toString()">
              <TabHeader :items="headerItem.filter((f, i) => (store.body.jp006DocumentId || store.body.winnerDocumentId) ? f : i === 0)"
                class="sticky top-14 z-3 pt-2 bg-[#F7F7F7]" />
              <TabPanels>
                <TabPanel value="0">
                  <DetailComponent :readonly="props.readonly" />
                </TabPanel>
                <TabPanel value="1">
                  <jp006Component
                    :readonly="props.readonly || !menuStore.hasManage || (!store.canEdit && !canEditDocument && !store.canComment)"
                    ref="jp006DocumentRef" :save="saveDocument" :versions="store.body.jp006DocumentVersions"
                    :canRestoreVersion="store.canEdit && menuStore.hasManage && canEditDocument"
                    @restore-version="handleRestoreJp006Version" />
                </TabPanel>
                <TabPanel value="2">
                  <winnerComponrt :readonly="props.readonly || !store.canEdit || !menuStore.hasManage || !canEditDocument"
                    ref="winnerDocumentRef" :save="saveDocument" :versions="store.body.winnerDocumentVersions"
                    :canRestoreVersion="store.canEdit && menuStore.hasManage && canEditDocument"
                    @restore-version="handleRestoreWinnerVersion" />
                </TabPanel>
              </TabPanels>
            </Tabs>
            <div class="mt-4">
              <div class="lg:col-span-4 mb-5 my-1">
                <UploadFileGroup v-model="procurementStore.procurementDetail.attachments"
                  @upload="procurementStore.onUpsertAttachments" @remove-file="procurementStore.onUpsertAttachments"
                  @remove-group="procurementStore.onUpsertAttachments" @reorder="procurementStore.onUpsertAttachments"
                  :disabled="procurementStore.procurementDetail.status === ProcurementStatus.Completed || !menuStore.hasManage || props.readonly"
                  :isShowActivityDialog="true"
                  :isShowLinkFileAll="true"
                  :id="procurementStore.procurementDetail.id"/>
              </div>
            </div>
          </div>
        </div>
        <div class="relative lg:col-span-2">
          <div class="flex flex-col gap-4 ml-3 sticky top-[100px]">
            <div v-if="menuStore.hasManage && !props.readonly">
              <div id="save-section" class="flex items-center gap-2 justify-end">
                <ButtonSave v-if="store.canEdit" type="submit" />
                <ButtonSendApprove v-if="store.canEdit && store.body.jp006Id"
                  @click="handleSubmit(() => onActionByStatusAsync(store.isSixtyMorethan100k ? PurchaseOrderStatus.WaitingApproval : PurchaseOrderStatus.WaitingCommitteeApproval))" />
              </div>

              <div id="committee-section" class="flex items-center gap-2 justify-end" v-if="store.isCommitteeApproval">
                <ButtonRecall v-if="store.canCommitteeRecall"
                  @click="onActionByStatusAsync(PurchaseOrderStatus.Edit)" />
                <ButtonNotAgree @click="handleSubmit(() => store.onRejectAsync(true))"
                  v-if="store.isCommitteeCurrentApproval" />
                <ButtonApprove @click="store.onApproveAsync()"
                  v-if="store.isCommitteeCurrentApproval" />
              </div>

              <div id="jotpor-section" class="flex items-center gap-2 justify-end">
                <ButtonSendEdit @click="store.assigneeRejectAsync()" v-if="store.jorporCanAssign" />
                <ButtonSave v-if="store.isJorPorAssigned" type="submit" />
                <ButtonConfirmAssign v-if="store.jorporCanAssign"
                  @click="handleSubmit(() => onActionByStatusAsync(PurchaseOrderStatus.WaitingComment))" />
                <ButtonSendApprove v-if="store.canComment"
                  @click="handleSubmit(() => onActionByStatusAsync(PurchaseOrderStatus.WaitingApproval))" />
              </div>

              <div id="acceptor-approve-section" class="flex items-center justify-end gap-2">
                <ButtonRecall v-if="store.isCanRecall"
                  @click="onActionByStatusAsync(PurchaseOrderStatus.WaitingComment)" />

                <ButtonSendEdit v-if="store.isCurrentAcceptorApproval && store.isAcceptorApproval"
                  @click="store.onRejectAsync()" />

                <ButtonConfirm v-if="store.isCurrentAcceptorApproval && store.isLastAcceptorApproval"
                  @click="store.onApproveAsync()" />

                <ButtonApprove v-if="store.isCurrentAcceptorApproval && !store.isLastAcceptorApproval"
                  @click="store.onApproveAsync()" />

              </div>
            </div>

            <Accordion v-model:value="currentAccordion" unstyled multiple>
              <AccordionPanel value="999" class="mb-4" v-if="currentTab !== '0'">
                <AccordHeader label="Dictionary" />
                <AccordionContent>
                  <div></div>
                  <Card class="rounded-none overflow-auto h-[800px]">
                    <template #content>
                      <!-- Dictionary mapping for JP006 and Winner documents -->
                      <DocumentMapping pathToGet="procurement/jp006" @on-click-select="
                        (text, hint) => currentTab === '1' ? jp006DocumentRef?.setPlaceholderInDocument(text, hint) : winnerDocumentRef?.setPlaceholderInDocument(text, hint)
                      " />
                    </template>
                  </Card>
                </AccordionContent>
              </AccordionPanel>
              <div v-if="store.isSixtyMorethan100k">
                <AccordionPanel value="0" class="mt-5">
                  <AccordAcceptor title="หัวหน้าส่วน" v-model="store.body.acceptors"
                    @set-default="() => store.getDefaultSegmentAsync()" isApprove :acceptor-type="AcceptorType.Approver"
                    v-if="store.body.acceptors" isShowCheckBoxAll isManage
                    :is-disable="(!store.isCommitteeApproval && !store.canEdit) || !menuStore.hasManage || !store.body.jp006Id || props.readonly" />
                </AccordionPanel>
              </div>
              <div v-if="!store.isSixtyMorethan100k">
                <AccordionPanel value="0">
                  <AccordAcceptor title="ผู้จัดซื้อจัดจ้าง/คณะกรรมการจัดซื้อจัดจ้าง" :model-value="store.body.acceptors"
                    @set-is-unable-to-perform-duties="(status: boolean, id: string, remark?: string) => store.updateDutiesStatusAsync(id, status, remark)"
                    :is-disable="(!store.isCommitteeApproval && !store.canEdit) || !menuStore.hasManage || !store.body.jp006Id || props.readonly"
                    :acceptor-type="AcceptorType.ProcurementCommittee" v-if="store.body.acceptors" isApprove />
                </AccordionPanel>
                <AccordionPanel value="1" class="mt-5" v-if="totalWinnerPrice > 0 && !isUnderDirectorDepartment">
                  <AccordAcceptor title="ผู้มีอำนาจเห็นชอบ/อนุมัติ" v-model="store.body.acceptors"
                    @set-default="store.getDefaultAcceptorWithCondition()" isSetDefault
                    :acceptor-type="AcceptorType.Approver" isManage :is-disable="!store.canEdit || !menuStore.hasManage || props.readonly"
                    is-approve v-if="store.body.acceptors" />
                </AccordionPanel>
                <AccordionPanel value="2" class="mt-5"
                  v-if="totalWinnerPrice > 0 && store.body.assignees && isUnderDirectorDepartment">
                  <AccordAssignee title="เจ้าหน้าที่พัสดุให้ความเห็น" v-model="store.body.assignees"
                    :disabled="!store.jorporCanAssign || !menuStore.hasManage || props.readonly" :isComment="store.canComment"
                    v-if="store.body.assignees" @on-comment="(e) => onAssigneeComment(e.reason)" />
                </AccordionPanel>
                <AccordionPanel value="3" class="mt-5" v-if="totalWinnerPrice > 0 && isUnderDirectorDepartment">
                  <AccordAcceptor title="ผู้มีอำนาจเห็นชอบ/อนุมัติ" v-model="store.body.acceptors"
                    :acceptor-type="AcceptorType.Approver" isManage
                    @set-default="store.getDefaultAcceptorWithCondition()"
                    :is-disable="!store.isJorPorAssigned || !menuStore.hasManage || props.readonly" is-approve
                    :is-set-default="store.isJorPorAssigned && menuStore.hasManage && !props.readonly" v-if="store.body.acceptors" />
                </AccordionPanel>
              </div>
            </Accordion>
          </div>
        </div>
      </div>
    </div>
  </Form>
  <DialogReviewDocument v-model="showReviewDocumentDialog" :docId="reviwDocumentId"
    :docName="`pp007-${new Date().toISOString()}-${reviwDocumentId}`" @on-click-use-document="setDocumentReviewId" />
</template>
