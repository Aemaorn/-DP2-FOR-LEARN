<script setup lang="ts">
import type { Option } from '@/models/shared/option';
import { Tabs, TabPanels, TabPanel, Button } from 'primevue';
import { TitleHeader, TabHeader } from '@/components/cosmetic';
import { AccordAcceptor } from '@/components/Accordions';
import { BadgeStatus as BadgeStatusComponent } from '@/components';
import { ButtonApprove, ButtonConfirm, ButtonSendEdit, ButtonRecall, ButtonApproveConfirm } from '@/components/Button';
import { computed, defineAsyncComponent, nextTick, onMounted, ref, watch, watchEffect, type PropType } from 'vue';
import { Form as VeeForm } from 'vee-validate';
import { OperatorType, PP005Accordion, PP005Status } from '../../enums/pp005';
import Jp005Helper from '@/helpers/jp005';
import { usePPDetailStore } from '../../../../stores/PP/ppStore';
import { usePP005DetailStore } from '../../stores/PP005/PP005Store';
import { checkIsEighty, checkIsSixty } from '@/helpers/supplyMethod';
import { AcceptorType } from '@/enums/participants';
import ToastHelper from '@/helpers/toast';
import { default as ApprovalChEditor, default as CommandChEditor } from '@/components/Document/ChEditor.vue';
import { showActivityDialog, showSaveOptionDialogAsync } from '@/helpers/dialog';
import DocumentMapping from '@/components/DocumentMapping.vue';
import DialogReviewDocument from '@/components/Document/DialogReviewDocument.vue';
import { useMenuStore } from '@/stores/menu';
import { HttpStatusCode } from 'axios';
import operationService from '@/services/Shared/operations';
import { SectionProcessType } from '@/enums/operations';
import { useAuthenticationStore } from '@/stores/authentication';
import PP005Service from '../../services/PP005/PP005Service';

const props = defineProps({
  procurementId: {
    type: String,
    required: true,
  },
  id: {
    type: Object as PropType<string | undefined>,
  },
  readonly: {
    type: Boolean,
    default: false,
  },
});

const auth = useAuthenticationStore();
const menuStore = useMenuStore();
const { AccordionName, BadgeStatus } = Jp005Helper;
const procurementStore = usePPDetailStore();
const detailStore = usePP005DetailStore();
const approvalDocRef = ref<InstanceType<typeof ApprovalChEditor> | null>(null);
const commandDocRef = ref<InstanceType<typeof CommandChEditor> | null>(null);
const showReviewDocumentDialog = ref(false);
const reviwDocumentId = ref<string>('');
const isSixtyAndMoreThanOneHundredThousand = computed(() => {
  return checkIsSixty(procurementStore.procurementDetail.supplyMethodCode!) && procurementStore.procurementDetail.budget > 100000;
});
const assistantMD = computed(() => checkIsEighty(procurementStore.procurementDetail.supplyMethodCode!) && procurementStore.procurementDetail.budget > 500000);

const setPlaceholderInApprovalDoc = (text: string, hint?: string) => {
  if (approvalDocRef.value) {
    // Assuming the method setPlaceholderInDocument is defined in ChEditor
    // and it takes a document ID as an argument.
    approvalDocRef.value.setPlaceholderInDocument(text, hint);
  }
};

const setPlaceholderInCommandDocument = (text: string, hint?: string) => {
  if (commandDocRef.value) {
    // Assuming the method setPlaceholderInDocument is defined in ChEditor
    // and it takes a document ID as an argument.
    commandDocRef.value.setPlaceholderInDocument(text, hint);
  }
};

// Save document to CHEditor first before API call
const saveDocumentFirst = async (): Promise<void> => {
  // Only save if document is editable (not read-only)
  if (!canEditDocument.value) return;

  if (currentTab.value === '1' && approvalDocRef.value?.saveAndWait) {
    return new Promise((resolve) => {
      approvalDocRef.value!.saveAndWait(() => {
        resolve();
      });
    });
  } else if (currentTab.value === '2' && commandDocRef.value?.saveAndWait) {
    return new Promise((resolve) => {
      commandDocRef.value!.saveAndWait(() => {
        resolve();
      });
    });
  }
};

const handleRestoreApprovalVersion = async (): Promise<void> => {
  if (!procurementStore.procurementDetail.id || !detailStore.body.id) {
    ToastHelper.error('เกิดข้อผิดพลาด', 'ไม่สามารถรีเซ็ตเอกสารได้ ข้อมูลไม่ครบถ้วน');
    return;
  }

  const { status } = await PP005Service.resetDocumentAsync(procurementStore.procurementDetail.id, detailStore.body.id, 'approval');

  if (status === HttpStatusCode.Ok) {
    ToastHelper.success('สำเร็จ', 'ทำการรีเซ็ตเอกสารสำเร็จ');
    await detailStore.fn.onGetByIdAsync(procurementStore.procurementDetail.id, detailStore.body.id);
  }
};

const handleRestoreCommandVersion = async (): Promise<void> => {
  if (!procurementStore.procurementDetail.id || !detailStore.body.id) {
    ToastHelper.error('เกิดข้อผิดพลาด', 'ไม่สามารถรีเซ็ตเอกสารได้ ข้อมูลไม่ครบถ้วน');
    return;
  }

  const { status } = await PP005Service.resetDocumentAsync(procurementStore.procurementDetail.id, detailStore.body.id, 'command');

  if (status === HttpStatusCode.Ok) {
    ToastHelper.success('สำเร็จ', 'ทำการรีเซ็ตเอกสารสำเร็จ');
    await detailStore.fn.onGetByIdAsync(procurementStore.procurementDetail.id, detailStore.body.id);
  }
};

const formRef = ref();

const currentTab = ref("0");
const isFormDirty = ref(false);
const isInitialized = ref(false);
const showJpNumberDialog = ref(false);

const headerItem = ref([
  { label: 'รายละเอียด', value: "0" },
  { label: 'เอกสารขออนุมัติ', value: "1" },
] as Option[]);
const isProcOptionReady = ref(false);

onMounted(async () => {
  await detailStore.fn.fetchPositionInspOptions();
  await detailStore.fn.fetchPositionProcOptions();
  await detailStore.fn.fetchPositionMaOptions();
  await detailStore.fn.fetchPositionSupOptions();
  isProcOptionReady.value = true;
  detailStore.body.jp005.isJp005ApprovalDocumentIdReplaced = false;
  detailStore.body.jp005.isJp005CommandDocumentIdReplaced = false;
});

watch(
  isProcOptionReady,
  async (ready, _, onCleanup) => {
    if (!ready) return;

    onCleanup(() => { });

    await detailStore.fn.onGetByIdAsync(props.procurementId, props.id);

    if (isSixtyAndMoreThanOneHundredThousand.value) {
      headerItem.value = [{ label: 'รายละเอียด', value: "0" }];
    } else if (assistantMD.value || isUnderDirectorDepartment.value) {
      headerItem.value = [
        { label: 'รายละเอียด', value: "0" },
        { label: 'เอกสารขออนุมัติ', value: "1" },
        { label: 'เอกสารคำสั่งธนาคาร', value: "2" }
      ];
    }

    if (!detailStore.body.jp005.acceptors?.length) {
      await detailStore.fn.setDefaultApproverAsync();
    }

    if (detailStore.body.id &&
      (detailStore.body.status === PP005Status.Draft ||
        detailStore.body.status === PP005Status.Edit ||
        detailStore.body.status === PP005Status.Rejected)) {

      if (checkIsEighty(procurementStore.procurementDetail.supplyMethodCode) &&
        detailStore.body.jp005.jp005ApprovalDocumentId &&
        !isSixtyAndMoreThanOneHundredThousand.value) {

        if (detailStore.body.jp005.jp005CommandDocumentId && (
          assistantMD.value || isUnderDirectorDepartment.value)) {
          currentTab.value = '2';
        } else {
          currentTab.value = '1';
        }
      } else if (detailStore.body.jp005.jp005ApprovalDocumentId &&
        !isSixtyAndMoreThanOneHundredThousand.value) {
        currentTab.value = '1';
      }
    }

    await nextTick();
    isInitialized.value = true;
  },
  { once: true }
);

watch(
  () => detailStore.body,
  () => {
    if (isInitialized.value) {
      isFormDirty.value = true;
    }
  },
  { deep: true }
);

const DetailComponent = defineAsyncComponent(() => import('@/views/PP/components/PP005/components/PP005Detail.vue'));
const SaveJpNumberDialog = defineAsyncComponent(() => import('./components/sub/SaveJpNumberDialog.vue'));

const setDocumentReviewId = async (id: string): Promise<void> => {
  isInitialized.value = false;

  let documentType = '';
  if (currentTab.value === '1') {
    documentType = 'approval';
  } else if (currentTab.value === '2') {
    documentType = 'command';
  }

  if (documentType === 'approval') {
    detailStore.body.jp005.jp005ApprovalDocumentId = id;
  } else if (documentType === 'command') {
    detailStore.body.jp005.jp005CommandDocumentId = id;
  }

  await nextTick();
  isInitialized.value = true;
};

const filteredHeaderItems = computed(() => {
  if (!detailStore.body.id) return headerItem.value.filter((_, i) => i === 0);

  return headerItem.value.filter((f, i) =>
    !checkIsEighty(procurementStore.procurementDetail.supplyMethodCode) ? i < 2 : f
  );
});

const canEditDocument = computed(() => {
  return detailStore.body.status == PP005Status.Draft
    || detailStore.body.status == PP005Status.Edit
    || detailStore.body.status == PP005Status.Rejected
});

const defaultDocumentStatuses = [
  PP005Status.WaitingApproval,
  PP005Status.Approved,
];

watch(() =>
  detailStore.body.status,
  (newStatus: PP005Status) => {
    if (defaultDocumentStatuses.includes(newStatus) && !isSixtyAndMoreThanOneHundredThousand.value) {
      currentTab.value = '1';
    }
  }, { immediate: true });

const onSubmitAsync = async (params?: { isJp005ApprovalDocumentIdReplaced?: boolean }) => {
  if (currentTab.value === '1' || currentTab.value === '2') {
    await saveDocumentFirst();
  }

  // Set flags so backend re-replaces document — only when form data changed

  if (isFormDirty.value && detailStore.body.id
    && (detailStore.body.jp005.jp005ApprovalDocumentId
      || detailStore.body.jp005.jp005CommandDocumentId)) {
    const saveOption = await showSaveOptionDialogAsync();
    if (saveOption === undefined) return;
    detailStore.body.jp005.isJp005ApprovalDocumentIdReplaced = saveOption;
    detailStore.body.jp005.isJp005CommandDocumentIdReplaced = saveOption;
  }

  const wasFormDirty = isFormDirty.value;
  isInitialized.value = false;
  await detailStore.fn.onSubmitAsync({ isJp005ApprovalDocumentIdReplaced: params?.isJp005ApprovalDocumentIdReplaced });
  isFormDirty.value = false;
  await nextTick();
  isInitialized.value = true;

  if (currentTab.value === '1' &&
    assistantMD.value &&
    detailStore.body.jp005.jp005CommandDocumentId &&
    wasFormDirty) {
    currentTab.value = '2';
    return;
  }

  const hasApprovalDoc = !isSixtyAndMoreThanOneHundredThousand.value &&
    detailStore.body.jp005.jp005ApprovalDocumentId;

  if (hasApprovalDoc) {
    currentTab.value = '1'
    return;
  }
};

const isCommercialMaterialUnderDirectorDepartment = ref(false);
const isUnderDirectorDepartment = ref(false);

watchEffect(async () => {
  let processType: SectionProcessType =
    SectionProcessType.ApprovePurchaseRequest;

  const isCommercialMaterial =
    procurementStore.procurementDetail.isCommercialMaterial ?? false;

  const isCommercialLike =
    isCommercialMaterial ||
    procurementStore.procurementDetail.departmentOrganizationLevel === '601';

  if (isCommercialLike) {
    processType =
      SectionProcessType.ApprovePurchaseRequestCommercialParcel;
  }

  const assignees =
    detailStore.body?.purchaseRequisition?.operators
      ?.filter(f => f.operatorType === OperatorType.Assignee) ?? [];

  if (assignees.length === 0) return;

  const assignee = assignees.reduce((prev, curr) =>
    curr.sequence > prev.sequence ? curr : prev
    , assignees[0]);

  const { data, status } =
    await operationService.getOperationsDefaultAcceptorAsync(
      {
        budget: procurementStore.procurementDetail.budget,
        processType,
        supplyMethodCode: procurementStore.procurementDetail.supplyMethodCode,
        supplyMethodSpecialTypeCode: procurementStore.procurementDetail.supplyMethodSpecialTypeCode,
        userId: assignee?.userId ?? auth.profile.id,
        skipCurrentEmployee: false,
      },
      true
    );

  if (status === HttpStatusCode.Ok) {
    const hasLevel300 = data.some(
      x => x.organizationLevel === 300
    );

    isUnderDirectorDepartment.value = hasLevel300;

    isCommercialMaterialUnderDirectorDepartment.value =
      (isCommercialMaterial && !hasLevel300) ||
      procurementStore.procurementDetail.departmentOrganizationLevel === '601';
  }
});
</script>

<template>
  <div class="mt-4">
    <div class="my-2">
      <TitleHeader label="รายงานขอซื้อขอจ้าง (จพ.005)">
        <template #action>
          <BadgeStatusComponent
            :label="BadgeStatus(detailStore.body.status, isSixtyAndMoreThanOneHundredThousand).label"
            :color="BadgeStatus(detailStore.body.status, isSixtyAndMoreThanOneHundredThousand).color" />
          <Button label="ประวัติการใช้งาน" icon="pi pi-refresh" variant="outlined" severity="primary"
            v-if="detailStore.body.id" class="bg-white! hover:bg-red-50!"
            @click="() => showActivityDialog(detailStore.body.id!)" />
        </template>
      </TitleHeader>
    </div>
    <VeeForm ref="formRef" @submit="onSubmitAsync" @invalidSubmit="ToastHelper.invalidMessageToast()"
      v-slot="{ handleSubmit }">
      <div class="grid grid-cols-1 lg:grid-cols-7 gap-2">
        <div class="lg:col-span-5">
          <div>
            <Tabs :value="currentTab" unstyled @update:value="(tab) => currentTab = tab.toString()">
              <TabHeader :items="filteredHeaderItems" class="sticky top-14 z-3 pt-2 bg-[#F7F7F7]" />
              <TabPanels>
                <TabPanel value="0">
                  <DetailComponent :isOverPrice="procurementStore.procurementDetail.budget > 100000"
                    :item="detailStore.body.purchaseRequisition" v-model="detailStore.body.jp005"
                    :is-disabled="!detailStore.states.isEdit || readonly" :pJp005Number="detailStore.body.pJp005Number"
                    v-model:document-date="detailStore.body.documentDate"/>
                </TabPanel>
                <TabPanel value="1">
                  <ApprovalChEditor v-if="detailStore.body.jp005.jp005ApprovalDocumentId"
                    :docId="detailStore.body.jp005.jp005ApprovalDocumentId"
                    :docName="`approval-${new Date().toISOString()}`" ref="approvalDocRef"
                    :readonly="!detailStore.states.isEdit || !menuStore.hasManage || !canEditDocument"
                    :key="`${detailStore.body.jp005.jp005ApprovalDocumentId}-${detailStore.body.status}`"
                    :save="onSubmitAsync" :versions="detailStore.body.jp005.approvalDocumentVersions"
                    :canRestoreVersion="detailStore.states.isEdit && menuStore.hasManage && canEditDocument"
                    @restore-version="handleRestoreApprovalVersion" />
                </TabPanel>
                <TabPanel value="2">
                  <CommandChEditor v-if="detailStore.body.jp005.jp005CommandDocumentId"
                    :docId="detailStore.body.jp005.jp005CommandDocumentId" :docName="`bank-${new Date().toISOString()}`"
                    ref="commandDocRef"
                    :readonly="!detailStore.states.isEdit || !menuStore.hasManage || !canEditDocument"
                    :key="`${detailStore.body.jp005.jp005CommandDocumentId}-${detailStore.body.status}`"
                    :save="onSubmitAsync" :versions="detailStore.body.jp005.commandDocumentVersions"
                    :canRestoreVersion="detailStore.states.isEdit && menuStore.hasManage && canEditDocument"
                    @restore-version="handleRestoreCommandVersion" />
                </TabPanel>
              </TabPanels>
            </Tabs>
          </div>
        </div>

        <div class="col-span-2 relative">
          <div class="flex flex-col gap-4 ml-3 sticky top-14 pt-2">
            <div class="flex items-center gap-2 justify-end" v-if="menuStore.hasManage && !readonly">
              <div class="flex items-center gap-2"
                v-if="detailStore.body.hasEditPermission && detailStore.states.isEdit">
                <Button
                  v-if="(detailStore.body.hasEditPermission && detailStore.states.isEdit) && !isCommercialMaterialUnderDirectorDepartment"
                  label="ส่งกลับแก้ไขไปยัง จพ.004" icon="pi pi-user-minus" severity="danger"
                  @click="detailStore.fn.onSendEditToPurchaseRequisitionAsync" />
                <Button label="บันทึก" icon="pi pi-save" severity="success" type="submit" />
                <ButtonApproveConfirm v-if="detailStore.body.id"
                  @click="() => handleSubmit(() => detailStore.fn.onSendReCallOrApprovalAsync(PP005Status.WaitingApproval, isSixtyAndMoreThanOneHundredThousand))" />
              </div>

              <div class="flex items-center gap-2" v-if="detailStore.states.isRecall">
                <ButtonRecall
                  @click="detailStore.fn.onSendReCallOrApprovalAsync(PP005Status.Edit, isSixtyAndMoreThanOneHundredThousand)" />
              </div>

              <div class="flex items-center gap-2" v-if="detailStore.states.isCurrentApproval">
                <ButtonSendEdit v-if="detailStore.states.isCurrentApproval"
                  @click="() => detailStore.fn.onApprovedRejectedAsync('Reject')" />

                <ButtonApprove v-if="detailStore.states.isCurrentApproval && detailStore.states.isSegment"
                  @click="() => detailStore.fn.onApprovedRejectedAsync('Approve')" />

                <ButtonApprove
                  v-if="detailStore.states.isCurrentApproval && !detailStore.states.isSegment && !detailStore.states.isLastApproval"
                  @click="() => detailStore.fn.onApprovedRejectedAsync('Approve')" />

                <ButtonConfirm
                  v-if="detailStore.states.isCurrentApproval && !detailStore.states.isSegment && detailStore.states.isLastApproval"
                  @click="() => detailStore.fn.onApprovedRejectedAsync('Approve')" />
              </div>
              <Button label="บันทึกเลขที่คำสั่ง จพ." icon="pi pi-save" severity="success" v-if="detailStore.body.status === PP005Status.Approved && (assistantMD || isUnderDirectorDepartment)"
                @click="showJpNumberDialog = true"/>
            </div>

            <Accordion :value="Object.entries(PP005Accordion).map(([_, value]) => value)" unstyled multiple>
              <AccordionPanel value="999" class="mb-4" v-if="currentTab !== '0'">
                <AccordHeader label="Dictionary" />
                <AccordionContent>
                  <div></div>
                  <Card class="rounded-none overflow-auto h-[800px]">
                    <template #content>
                      <DocumentMapping pathToGet="procurement/jp005" @on-click-select="
                        (text, hint) => currentTab === '1'
                          ? setPlaceholderInApprovalDoc(text, hint)
                          : setPlaceholderInCommandDocument(text, hint)
                      " />
                    </template>
                  </Card>
                </AccordionContent>
              </AccordionPanel>

              <AccordionPanel :value="PP005Accordion.Acceptor" v-if="!isSixtyAndMoreThanOneHundredThousand">
                <AccordAcceptor :title="AccordionName(PP005Accordion.Acceptor)"
                  v-model="detailStore.body.jp005.acceptors" :acceptor-type="AcceptorType.Approver" isApprove isManage
                  :isSetDefault="detailStore.states.isCanSetDefault && menuStore.hasManage"
                  :is-disable="!detailStore.states.isEdit || !menuStore.hasManage || readonly"
                  @set-default="() => detailStore.fn.setDefaultApproverAsync()" />
              </AccordionPanel>

              <AccordionPanel :value="PP005Accordion.Segment" v-else>
                <AccordAcceptor :title="AccordionName(PP005Accordion.Segment)"
                  v-model="detailStore.body.jp005.acceptors" :acceptor-type="AcceptorType.DepartmentDirectorAgree"
                  isApprove isManage :isSetDefault="detailStore.states.isCanSetDefault && menuStore.hasManage"
                  :is-disable="!detailStore.states.isEdit || !menuStore.hasManage || readonly"
                  @set-default="() => detailStore.fn.setDefaultApproverAsync()" />
              </AccordionPanel>
            </Accordion>
          </div>
        </div>

      </div>
    </VeeForm>
    <DialogReviewDocument v-model="showReviewDocumentDialog" :docId="reviwDocumentId"
      :docName="`pp005-${new Date().toISOString()}-${reviwDocumentId}`" @on-click-use-document="setDocumentReviewId" />
    <SaveJpNumberDialog v-model="showJpNumberDialog" />
  </div>
</template>