<script setup lang="ts">
import type { Option } from '@/models/shared/option';
import { computed, defineAsyncComponent, nextTick, onMounted, ref, watch } from 'vue';
import { TabPanel, TabPanels } from 'primevue';
import { Form } from 'vee-validate';
import type { MenuItem } from 'primevue/menuitem';
import TitleHeader from '@/components/cosmetic/TitleHeader.vue';
import { useRoute } from 'vue-router';
import { usePcm004DetailStore } from '@/stores/PCM/pcm004';
import { showConfirmDialogAsync, showReasonDialogAsync, showActivityDialog, showSaveOptionDialogAsync } from '@/helpers/dialog';
import { Pcm004Action, Pcm004Status } from '@/enums/pcm004';
import router from '@/router';
import { ConfirmDialogType, ReasonDialogType } from '@/enums/dialog';
import ButtonApprove from '@/components/Button/ButtonApprove.vue';
import Pcm004Constant from '@/constants/pcm004';
import UploadFile from './components/UploadFile.vue';
import AccordAcceptor from '@/components/Accordions/AccordAcceptor.vue';
import ButtonRecall from '@/components/Button/ButtonRecall.vue';
import ButtonSendApprove from '@/components/Button/ButtonSendApprove.vue';
import ButtonConfirmAssign from '@/components/Button/ButtonConfirmAssign.vue';
import AccordAssignee from '@/components/Accordions/AccordAssignee.vue';
import ButtonSave from '@/components/Button/ButtonSave.vue';
import { AcceptorType, AssigneeType } from '@/enums/participants';
import { useAuthenticationStore } from '@/stores/authentication';
import { useMenuStore } from '@/stores/menu';
import ToastHelper from '@/helpers/toast';
import { ButtonNotAgree } from '@/components/Button';
import { BadgeStatus } from '@/components';
import DialogReviewDocument from '@/components/Document/DialogReviewDocument.vue';
import DocumentMapping from '@/components/DocumentMapping.vue';
import Pcm004Service from '@/services/PCM/PCM004';
import { HttpStatusCode } from 'axios';
import { DepartmentId } from '@/enums/businessUnit.ts';
import Datepicker from '@/components/forms/Datepicker.vue';
import InputArea from '@/components/forms/InputArea.vue';

const currentTab = ref('0');
const isFormDirty = ref(false);
const isInitialized = ref(false);
const Detail = defineAsyncComponent(() => import('./components/Detail.vue'));
const ApprovalDocument = defineAsyncComponent(() => import('./components/ApprovalDocument.vue'));

const approvalDocumentRef = ref<InstanceType<typeof ApprovalDocument> | null>(null);

const pcm004Store = usePcm004DetailStore();
const route = useRoute();
const { BadgeStatusColor } = Pcm004Constant;

const hasDirector = computed(() =>
  pcm004Store.detail.assignees?.some(a => a.assigneeType === AssigneeType.Director) ?? false
);

const canSelectAssignee = computed(() => {
  const isEditableAndNotAutoAssigned =
    pcm004Store.isEdit &&
    pcm004Store.detail.pettyCaseDepartmentCode !== DepartmentId.JorPor;

  const isWaitingForAssignmentWithoutDirector =
    pcm004Store.detail.status === Pcm004Status.WaitingForAssignment && !hasDirector.value;

  return isEditableAndNotAutoAssigned || isWaitingForAssignmentWithoutDirector;
});
const authenStore = useAuthenticationStore();
const menuStore = useMenuStore();
const showReviewDocumentDialog = ref(false);
const reviewDocumentId = ref<string>('');
const currentAccordion = ref<string[]>(['0']);

const showConfirmDisbursementDialog = ref(false);
const confirmDisbursementRemark = ref<string>('');

const id = ref(route.params?.id);

onMounted(async () => {
  await onInitPageAsync();

  pcm004Store.detail.departmentCode = authenStore.profile.departmentCode;
  await Promise.all([
    pcm004Store.getDepartmentDDLAsync(),
    pcm004Store.getSupplyMethodDDLAsync(),
    pcm004Store.getSupplyMethodTypeDDLAsync(),
    pcm004Store.getBankDDLAsync(),
    pcm004Store.getExpenseItemW119DDLAsync(),
    pcm004Store.getPaymentMethodDDLAsync(),
    pcm004Store.getVatTypeDDLAsync(),
    pcm004Store.getUnitOfMeasureDDLAsync(),
    pcm004Store.getInvoiceDocumentTypeDDLAsync(),
    pcm004Store.getSolIdDDLAsync(),
    pcm004Store.getBudgetTypeDDLAsync(),
    pcm004Store.getGlAccountDDLAsync(),
    pcm004Store.getPettyCashStandardTypeAsync(),
    pcm004Store.getPettyCashConvenienceTypeAsync(),
    pcm004Store.getPettyCashWithoutForm001TypeAsync(),
    pcm004Store.getDeliveryPeriodTypeOptionsAsync(),
    pcm004Store.getDeliveryConditionOptionAsync(),
    pcm004Store.getPositionInspOptions(),
    pcm004Store.getPositionProcOptions(),
  ]);

  await nextTick();
  isInitialized.value = true;

  if (pcm004Store.detail.status === Pcm004Status.WaitingForInspector) {
    await nextTick();
    document.getElementById('petty-cash-detail-section')
      ?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  if (pcm004Store.detail.status === Pcm004Status.WaitingForCompletion) {
    await nextTick();
    document.getElementById('petty-cash-disbursement-section')
      ?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }
});

watch(
  () => pcm004Store.detail,
  () => {
    if (isInitialized.value) {
      isFormDirty.value = true;
    }
  },
  { deep: true }
);

watch(() => pcm004Store.detail.supplyMethodCode, async (newValue) => {
  if (newValue) {
    return await pcm004Store.getSupplyMethodSpecialTypeDDLAsync(newValue);
  }
});

const onInitPageAsync = async (): Promise<void> => {
  if (id.value) {
    await pcm004Store.getByIdAsync(id.value.toString());

  } else {
    pcm004Store.detail.subject = 'ขอความเห็นชอบการจัดซื้อหรือจัดจ้าง '
  }
};

const saveDocumentFirst = async (): Promise<void> => {
  if (currentTab.value === '1' && approvalDocumentRef.value && 'saveDocumentFirst' in approvalDocumentRef.value) {
    await approvalDocumentRef.value.saveDocumentFirst();
  }
};

const saveDocument = async () => {
  await saveDocumentFirst();
  await onSubmit();
};

const handleRestoreApprovalVersion = async (): Promise<void> => {
  const pcm004Id = pcm004Store.detail.id;
  if (!pcm004Id) {
    ToastHelper.error('เกิดข้อผิดพลาด', 'ไม่สามารถรีเซ็ตเอกสารได้ ข้อมูลไม่ครบถ้วน');
    return;
  }
  const { status } = await Pcm004Service.resetDocumentAsync(pcm004Id);
  if (status === HttpStatusCode.Ok) {
    ToastHelper.success('สำเร็จ', 'ทำการรีเซ็ตเอกสารสำเร็จ');
    await pcm004Store.getByIdAsync(pcm004Id);
  }
};

const onSubmit = async (status?: Pcm004Status): Promise<void> => {
  if (pcm004Store.invalidParentCategoryCodes.length > 0) {
    ToastHelper.warning('เลือกรายการไม่ครบ', 'กรุณาเลือกรายการย่อยอย่างน้อย 1 รายการในรายการเงินสดย่อยที่เลือก');
    return;
  }

  // Save ChEditor content first before calling API
  await saveDocumentFirst();

  // Set flag so backend re-replaces document — only when form data changed
  if (id.value) {

    if (isFormDirty.value && pcm004Store.detail.approvalRequestDocumentId && !status && pcm004Store.detail.status !== Pcm004Status.WaitingForInspector) {
      const saveOption = await showSaveOptionDialogAsync();
      if (saveOption === undefined) return;
      pcm004Store.detail.isApprovalRequestDocumentReplace = saveOption;
    }

    isInitialized.value = false;
    await pcm004Store.updateAsync(id.value.toString(), status);
    isFormDirty.value = false;
    await nextTick();
    isInitialized.value = true;
    setCurrentTab();
    return;
  }

  isInitialized.value = false;
  const newId = await pcm004Store.createAsync();
  isFormDirty.value = false;
  setCurrentTab();

  if (newId) {
    id.value = newId;

    router.replace({ name: 'pcm004Detail', params: { id: newId } });
  }
  await nextTick();
  isInitialized.value = true;
};

const setCurrentTab = async () => {
  if (pcm004Store.detail.approvalRequestDocumentId) {
    currentTab.value = '1';
  }
};

const onRecallAsync = async (): Promise<void> => {
  const res = await showConfirmDialogAsync(ConfirmDialogType.Edit);

  if (res && pcm004Store.detail.id) {
    await pcm004Store.actionAsync(
      pcm004Store.detail.id,
      { action: Pcm004Action.Edit },
      'เรียกคืนแก้ไข',
      'เรียกคืนแก้ไขสำเร็จ',
    );
  }
};

const sendApproveAsync = async (): Promise<void> => {
  if (!pcm004Store.detail.acceptors) return;

  if (!pcm004Store.detail.acceptors.some(x => x.acceptorType === AcceptorType.DepartmentDirectorAgree)) {
    return ToastHelper.errorDescription("กรุณาเพิ่มผู้อนุมัติเงินสดย่อยอย่างน้อย 1 คน");
  }

  if (pcm004Store.isNotFromJorPor001
    && !pcm004Store.detail.assignees?.some(x => x.assigneeType === AssigneeType.Assignee)) {
    return ToastHelper.errorDescription("กรุณาระบุผู้ถือเงินสดย่อย");
  }

  if (!(await showConfirmDialogAsync(ConfirmDialogType.SendApprove))) return;

  if (pcm004Store.detail.id) {
    await pcm004Store.updateAsync(pcm004Store.detail.id, Pcm004Status.WaitingApproval);
  }
};

const onDirectorAgreeApproveAsync = async (): Promise<void> => {
  const res = await showReasonDialogAsync(ReasonDialogType.Accepted);

  if (res.isConfirm && pcm004Store.detail.id) {

    await pcm004Store.actionAsync(
      pcm004Store.detail.id,
      { action: Pcm004Action.DirectorAgreeApproved, remark: res.reason },
      'เห็นชอบ',
      'เห็นชอบสำเร็จ',
    );
  }
};

const onDirectorAgreeRejectAsync = async (): Promise<void> => {
  const res = await showReasonDialogAsync(ReasonDialogType.NotAgree, true);

  if (res.isConfirm && pcm004Store.detail.id) {

    await pcm004Store.actionAsync(
      pcm004Store.detail.id,
      { action: Pcm004Action.DirectorAgreeRejected, remark: res.reason },
      'ส่งกลับแก้ไข',
      'ส่งกลับแก้ไขสำเร็จ',
    );
  }
};

const onInspectionCommitteeApproveAsync = async (): Promise<void> => {

  if (pcm004Store.detail.status === Pcm004Status.WaitingForInspector && pcm004Store.detail.categories.length < 1) {
    return ToastHelper.errorDescription("กรุณากรอกข้อมูลประเภทเงินสด หรือ ประเภทเงินสดย่อย-สะดวกใช้ อย่างน้อง 1 รายการ")
  }

  const res = await showReasonDialogAsync(ReasonDialogType.Accepted);

  if (res.isConfirm && pcm004Store.detail.id) {
    await onApproveUpdateAsync();

    await pcm004Store.actionAsync(
      pcm004Store.detail.id,
      { action: Pcm004Action.InspectionCommitteeApproved, remark: res.reason },
      'เห็นชอบ',
      'เห็นชอบสำเร็จ',
    );
  }
};

const onApproveUpdateAsync = async (status?: Pcm004Status): Promise<void> => {

  if (id.value) {
    await pcm004Store.onApproveUpdateAsync(id.value.toString(), status);
  }

};

const onInspectionCommitteeRejectAsync = async (): Promise<void> => {
  const res = await showReasonDialogAsync(ReasonDialogType.NotAgree, true);

  if (res.isConfirm && pcm004Store.detail.id) {

    await pcm004Store.actionAsync(
      pcm004Store.detail.id,
      { action: Pcm004Action.InspectionCommitteeRejected, remark: res.reason },
      'ไม่เห็นชอบ',
      'ไม่เห็นชอบสำเร็จ',
    );
  }
};

const onAssignAsync = async (): Promise<void> => {
  if (pcm004Store.detail.id) {

    await pcm004Store.actionAsync(
      pcm004Store.detail.id,
      { action: Pcm004Action.Assignment, assignees: pcm004Store.detail.assignees },
      'แก้ไขข้อมูล',
      'บันทึกการอัปเดตแก้ไขข้อมูลสำเร็จ',
    );
  }
};

const onConfirmAssignmentAsync = async (): Promise<void> => {
  if (!pcm004Store.detail.assignees.some(x => x.assigneeType == AssigneeType.Assignee)) {
    return ToastHelper.assignAtLeastMessageToast();
  }

  if (!await showConfirmDialogAsync(ConfirmDialogType.Assigned)) return;

  if (pcm004Store.detail.id) {

    await pcm004Store.actionAsync(
      pcm004Store.detail.id,
      { action: Pcm004Action.ConfirmAssignment, assignees: pcm004Store.detail.assignees, },
      'มอบหมาย',
      'มอบหมายสำเร็จ',
    );
  }
};

const onConfirmCompletedAsync = async (): Promise<void> => {
  confirmDisbursementRemark.value = '';
  showConfirmDisbursementDialog.value = true;
};

const onAssigneeRejectAsync = async (): Promise<void> => {
  const res = await showReasonDialogAsync(ReasonDialogType.Reject, true);

  if (res.isConfirm && pcm004Store.detail.id) {
    await pcm004Store.actionAsync(
      pcm004Store.detail.id,
      { action: Pcm004Action.AssigneeRejected, remark: res.reason },
      'ส่งกลับแก้ไข',
      'ส่งกลับแก้ไขสำเร็จ',
    );
  }
};

const onSubmitConfirmDisbursementAsync = async (): Promise<void> => {
  if (!pcm004Store.detail.id || !pcm004Store.detail.disbursementDate) return;

  await pcm004Store.actionAsync(
    pcm004Store.detail.id,
    {
      action: Pcm004Action.ConfirmCompleted,
      disbursementDate: pcm004Store.detail.disbursementDate,
      assignees: pcm004Store.detail.assignees,
      remark: confirmDisbursementRemark.value,
    },
    'ยืนยันเบิกจ่าย',
    'ยืนยันเบิกจ่ายสำเร็จ',
  );

  showConfirmDisbursementDialog.value = false;
};

const canEditDocument = computed(() => {

  return pcm004Store.detail.status === Pcm004Status.Edit
    || pcm004Store.detail.status === Pcm004Status.Draft
    || pcm004Store.detail.status === Pcm004Status.Rejected
});

const getDefaultDepartmentDirectorAsync = async (): Promise<void> => {
  if (!pcm004Store.detail.pettyCaseDepartmentCode) {
    return;
  }

  return await pcm004Store.getDefaultDepartmentDirectorAsync(pcm004Store.detail.pettyCaseDepartmentCode);
};

const setDocumentReviewId = async (id: string): Promise<void> => {
  isInitialized.value = false;

  if (currentTab.value === '1') {
    pcm004Store.detail.approvalRequestDocumentId = id;
  }

  await nextTick();
  isInitialized.value = true;
};

const HeaderItem = ref([
  {
    label: 'รายละเอียด',
    value: '0',
  },
  {
    label: 'เอกสารรายงานขอความเห็นชอบ',
    value: '1',
  }
] as Option[]);

const routeItems = ref(
  [
    { label: 'รายการจัดซื้อจัดจ้าง Petty Cash', url: '/pcm/pcm004' },
    { label: 'ข้อมูลจัดซื้อจัดจ้าง', },
  ] as MenuItem[]);

const AccordionStep: Record<Pcm004Status, string[]> = {
  [Pcm004Status.Draft]: ['0'],
  [Pcm004Status.Edit]: ['0'],
  [Pcm004Status.Rejected]: ['0'],
  [Pcm004Status.WaitingApproval]: ['0'],
  [Pcm004Status.WaitingForInspector]: ['1'],
  [Pcm004Status.WaitingForAssignment]: ['2'],
  [Pcm004Status.WaitingForCompletion]: ['2'],
  [Pcm004Status.Completed]: ['0', '1', '2'],
  [Pcm004Status.All]: [],
};

const defaultDocumentStatuses = [
  Pcm004Status.WaitingApproval,
  Pcm004Status.WaitingForAssignment,
  Pcm004Status.Completed,
];

watch(
  [() => pcm004Store.detail.status, () => pcm004Store.detail.isFromJorPor001],
  ([newStatus, isFromJorPor001]) => {
    if (!newStatus) return;

    currentAccordion.value =
      isFromJorPor001 === false && pcm004Store.isEdit
        ? ['0', '2']
        : AccordionStep[newStatus];

    if (
      defaultDocumentStatuses.includes(newStatus) &&
      !pcm004Store.isJPApproval
    ) {
      currentTab.value = '1';
    }
  },
  { immediate: true }
);
</script>

<template>
  <Form @submit="onSubmit()" v-slot="{ handleSubmit }" @invalid-submit="() => ToastHelper.invalidMessageToast()">
    <TitleHeader label="ข้อมูลรายการจัดซื้อจัดจ้าง Petty Cash" :route-items="routeItems">
      <template #action>
        <div class="flex items-center gap-2">
          <p class="text-sm">สถานะ :</p>
          <BadgeStatus :label="BadgeStatusColor(pcm004Store.detail.status).label"
            :color="BadgeStatusColor(pcm004Store.detail.status).color" />
        </div>
        <Button label="ประวัติการใช้งาน" icon="pi pi-refresh" severity="primary" variant="outlined"
          @click="() => showActivityDialog(pcm004Store.detail.id!)" />
      </template>
    </TitleHeader>
    <div class="grid lg:grid-cols-7 gap-2">
      <div class="lg:col-span-5 order-2 lg:order-1">
        <Tabs :value="currentTab" unstyled @update:value="(tab) => currentTab = tab.toString()">
          <TabHeader :items="pcm004Store.detail.approvalRequestDocumentId ? HeaderItem : HeaderItem.splice(1, 0)"
            class="sticky top-[58px] z-99 bg-[#F7F7F7]" />
          <TabPanels>
            <TabPanel value="0">
              <Detail />
            </TabPanel>
            <TabPanel value="1">
              <Card>
                <template #content>
                  <ApprovalDocument v-model="pcm004Store.detail.approvalRequestDocumentId" :readonly="!canEditDocument"
                    ref="approvalDocumentRef" :save="saveDocument"
                    :versions="pcm004Store.detail.approvalRequestDocumentVersions"
                    :canRestoreVersion="pcm004Store.isEdit && menuStore.hasManage && canEditDocument"
                    @restore-version="handleRestoreApprovalVersion" />
                </template>
              </Card>
            </TabPanel>
          </TabPanels>
          <UploadFile />
        </Tabs>
      </div>

      <div class="relative lg:col-span-2 order-1 lg:order-2">
        <div class="flex flex-col gap-4 ml-3 sticky top-[100px]">
          <div class="flex items-center justify-end gap-2" v-if="menuStore.hasManage">
            <div id="creator" class="flex items-center gap-2">
              <ButtonSave type="submit" v-if="pcm004Store.isEdit" />
              <ButtonSendApprove @click="handleSubmit(sendApproveAsync)"
                v-if="pcm004Store.isEdit && pcm004Store.detail.id" />
              <ButtonRecall @click="onRecallAsync" v-if="pcm004Store.canReCall" />
            </div>
            <div id="directorAgree-approve" class="flex items-center gap-2" v-if="pcm004Store.isWaitingApproval">
              <ButtonNotAgree @click="onDirectorAgreeRejectAsync" v-if="pcm004Store.isWaitingApproval" />
              <ButtonApprove @click="onDirectorAgreeApproveAsync" v-if="pcm004Store.isWaitingApproval" />
            </div>
            <div id="inspector-committee-approve" class="flex items-center gap-2" v-if="pcm004Store.isJPApproval">
              <ButtonSave type="submit" v-if="pcm004Store.isJPApproval" />
              <ButtonNotAgree @click="onInspectionCommitteeRejectAsync" v-if="pcm004Store.isJPApproval" />
              <ButtonApprove @click="onInspectionCommitteeApproveAsync()" v-if="pcm004Store.isJPApproval" />
            </div>
            <div id="director-assign" class="flex items-center gap-2" v-if="pcm004Store.isWaitingForAssignment">
              <ButtonSave @click="onAssignAsync" v-if="pcm004Store.isWaitingForAssignment" />
              <ButtonConfirmAssign @click="onConfirmAssignmentAsync" v-if="pcm004Store.isWaitingForAssignment" />
            </div>
            <div id="assignee" class="flex items-center gap-2" v-if="pcm004Store.isWaitingForCompletion">
              <Button label="ส่งกลับแก้ไข" icon="pi pi-undo" severity="danger" @click="onAssigneeRejectAsync" />
              <ButtonConfirmAssign label="ยืนยันเบิกจ่าย" @click="onConfirmCompletedAsync" />
            </div>
          </div>
          <Accordion v-model:value="currentAccordion" unstyled multiple>
            <AccordionPanel value="999" class="mb-4" v-if="currentTab !== '0'">
              <AccordHeader label="Dictionary" />
              <AccordionContent>
                <div></div>
                <Card class="rounded-none overflow-auto h-[800px]">
                  <template #content>
                    <DocumentMapping pathToGet="pPettyCash" @on-click-select="
                      (text, hint) => approvalDocumentRef?.setPlaceholderInDocument(text, hint)
                    " />
                  </template>
                </Card>
              </AccordionContent>
            </AccordionPanel>

            <AccordionPanel value="0">
              <AccordAcceptor title="ผู้อนุมัติเงินสดย่อย" v-model="pcm004Store.detail.acceptors"
                :is-set-default="false"
                @set-default="() => getDefaultDepartmentDirectorAsync()"
                :is-manage="pcm004Store.isEdit"
                :is-disable="!pcm004Store.isEdit"
                :acceptor-type="AcceptorType.DepartmentDirectorAgree" v-if="pcm004Store.detail.acceptors" is-approve />
            </AccordionPanel>

            <AccordionPanel class="mt-4" value="1" v-if="!pcm004Store.isNotFromJorPor001">
              <AccordAcceptor title="ผู้ตรวจรับพัสดุ" v-model="pcm004Store.detail.acceptors"
                :is-disable="!pcm004Store.isEdit" :acceptor-type="AcceptorType.InspectionCommittee"
                :disabled="!pcm004Store.isWaitingApproval" v-if="pcm004Store.detail.acceptors" is-approve />
            </AccordionPanel>

            <AccordionPanel value="2" class="mt-4">
              <AccordAssignee v-model="pcm004Store.detail.assignees"
                title="ผู้ถือเงินสดย่อย"
                :disabled="!(pcm004Store.isWaitingForAssignment || canSelectAssignee || (pcm004Store.isNotFromJorPor001 && pcm004Store.isEdit))"
                @on-change="onAssignAsync"
                v-if="pcm004Store.detail.assignees" />
            </AccordionPanel>
          </Accordion>
        </div>
      </div>
    </div>
  </Form>
  <DialogReviewDocument v-model="showReviewDocumentDialog" :docId="reviewDocumentId"
    :docName="`pcm004-${new Date().toISOString()}-${reviewDocumentId}`" @on-click-use-document="setDocumentReviewId" />
  <Dialog v-model:visible="showConfirmDisbursementDialog" modal :draggable="false" :style="{ width: '40vw' }"
    :breakpoints="{ '1199px': '75vw', '575px': '90vw' }" :closable="false">
    <template #header>
      <h6 class="font-bold">ยืนยันเบิกจ่าย</h6>
    </template>
    <Form @submit="onSubmitConfirmDisbursementAsync"
      @invalid-submit="() => ToastHelper.invalidMessageToast()">
      <div class="grid grid-cols-1 gap-4 mt-6">
        <Datepicker label="วันที่เบิกเงินสดย่อย" v-model="pcm004Store.detail.disbursementDate" rules="required" />
        <InputArea label="หมายเหตุ" v-model="confirmDisbursementRemark" />
      </div>
      <div class="flex items-center justify-end gap-4 mt-6">
        <Button severity="secondary" variant="outlined" label="ยกเลิก"
          @click="() => showConfirmDisbursementDialog = false" />
        <Button icon="pi pi-check" label="ยืนยันเบิกจ่าย" type="submit" severity="success" />
      </div>
    </Form>
  </Dialog>
</template>