<script setup lang="ts">
import { computed, defineAsyncComponent, nextTick, onMounted, onUnmounted, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { Form, type FieldState, type SubmissionHandler } from 'vee-validate';
import { TabPanel, TabPanels } from 'primevue';
import type { MenuItem } from 'primevue/menuitem';
import TitleHeader from '@/components/cosmetic/TitleHeader.vue';
import { AccordAssignee, AccordAcceptor } from '@/components/Accordions';
import { UploadFileGroup } from '@/components/forms';
import { BadgeStatus as BadgeComponent } from '@/components';
import { PlanAnnouncementConstants } from '@/constants';
import { PlanAnnouncementAction, PlanAnnouncementStatus } from '@/enums/planAnnouncement';
import { AcceptorType, AssigneeGroup, AssigneeType } from '@/enums/participants';
import type { Option } from '@/models/shared/option';
import { usePl002DetailStore } from '@/stores/PL/pl002';
import {
  ButtonApprove,
  ButtonConfirm,
  ButtonConfirmAssign,
  ButtonApproveConfirm,
  ButtonSendEdit,
  ButtonPublish,
  ButtonRecall,
} from '@/components/Button';
import { storeToRefs } from 'pinia';
import { showActivityDialog, showConfirmDialogAsync, showSaveOptionDialogAsync } from '@/helpers/dialog';
import { ConfirmDialogType } from '@/enums/dialog';
import ToastHelper from '@/helpers/toast';
import DocumentMapping from '@/components/DocumentMapping.vue';
import DialogReviewDocument from '@/components/Document/DialogReviewDocument.vue';
import { useMenuStore } from '@/stores/menu';
import { useAuthenticationStore } from '@/stores/authentication';
import { DepartmentId } from '@/enums/businessUnit';
import { AssignDepartmentCodeEnum } from '@/enums/shared';
import PlanAnnouncementService from '@/services/PL/pl002';
import { HttpStatusCode } from 'axios';

const routeItems = ref([
  { label: 'ขออนุมัติเผยแพร่แผนจัดซื้อจัดจ้าง', url: '/pl/pl002' },
  { label: 'ข้อมูลจัดซื้อจัดจ้าง' },
] as MenuItem[]);

const HeaderItem = ref([
  {
    label: 'รายละเอียด',
    value: '0',
  },
  {
    label: 'เอกสารขออนุมัติประกาศเผยแพร่แผนการจัดซื้อจัดจ้าง',
    value: '1',
  },
  {
    label: 'เอกสารประกาศเผยแพร่แผน',
    value: '2',
  },
] as Option[]);

const showReviewDocumentDialog = ref(false);
const reviwDocumentId = ref<string>('');
const route = useRoute();
const router = useRouter();
const detailStore = usePl002DetailStore();
const menuStore = useMenuStore();
const authStore = useAuthenticationStore();
const { canEdit, canRecall, jorPorAssign, canPublish, isAnnouncementVisible, canAcceptAndReject, isLastApproval, AssignCanEdit, canSendApprove, isConfirmAssign } = storeToRefs(detailStore);
const currentAccordion = ref<Array<string>>([]);

const id = ref(route.params?.id);

const { AnnouncementStatusColor, AnnouncementStatusName } = PlanAnnouncementConstants;
const Detail = defineAsyncComponent(() => import('./components/Detail.vue'));
const ApproveProcurment = defineAsyncComponent(() => import('./components/ApproveProcurment.vue'));
const PublishDocument = defineAsyncComponent(() => import('./components/PublishDocument.vue'));

const approveProcurmentDocumentRef = ref<InstanceType<typeof ApproveProcurment> | null>(null);
const publishDocumentRef = ref<InstanceType<typeof PublishDocument> | null>(null);

const currentTab = ref('0');
const isFormDirty = ref(false);
const isInitialized = ref(false);
const { isRequiredPublished } = storeToRefs(detailStore);

const onHandleSubmit = async (
  handleSubmit: (evt: Event | SubmissionHandler, onSubmit?: SubmissionHandler) => Promise<unknown>,
  resetField: (field: string, state?: Partial<FieldState>) => void) => {
  isRequiredPublished.value = false;
  // ใช้ isFormDirty จาก deep-watch ของ body (reset หลังโหลด/เซฟ) แทน meta.dirty ของ vee-validate
  // ซึ่ง dirty ตั้งแต่โหลดเพราะ field จับค่า initial ตอน mount แล้วโดน normalize
  resetField('annoucementTitle', { value: detailStore.body.announcementTitle, errors: undefined, touched: false, });

  await nextTick();

  if (!hasPlanSelected()) return;
  handleSubmit(() => onSubmit());

}

const saveDocumentFirst = async (): Promise<void> => {
  if (currentTab.value === '1' && approveProcurmentDocumentRef.value && 'saveDocumentFirst' in approveProcurmentDocumentRef.value) {
    await approveProcurmentDocumentRef.value.saveDocumentFirst();
  } else if (currentTab.value === '2' && publishDocumentRef.value && 'saveDocumentFirst' in publishDocumentRef.value) {
    await publishDocumentRef.value.saveDocumentFirst();
  }
};

const saveDocument = async () => {
  await saveDocumentFirst();
  await onSubmit();
};

const handleRestoreApproveVersion = async (): Promise<void> => {
  const planAnnouncementId = detailStore.body.planAnnouncementId;
  if (!planAnnouncementId) {
    ToastHelper.error('เกิดข้อผิดพลาด', 'ไม่สามารถรีเซ็ตเอกสารได้ ข้อมูลไม่ครบถ้วน');
    return;
  }
  const { status } = await PlanAnnouncementService.resetDocumentAsync(planAnnouncementId, 'approve');
  if (status === HttpStatusCode.Ok) {
    ToastHelper.success('สำเร็จ', 'ทำการรีเซ็ตเอกสารสำเร็จ');
    await detailStore.getPlanAnnouncement(planAnnouncementId);
  }
};

const handleRestoreAnnouncementVersion = async (): Promise<void> => {
  const planAnnouncementId = detailStore.body.planAnnouncementId;
  if (!planAnnouncementId) {
    ToastHelper.error('เกิดข้อผิดพลาด', 'ไม่สามารถรีเซ็ตเอกสารได้ ข้อมูลไม่ครบถ้วน');
    return;
  }
  const { status } = await PlanAnnouncementService.resetDocumentAsync(planAnnouncementId, 'announcement');
  if (status === HttpStatusCode.Ok) {
    ToastHelper.success('สำเร็จ', 'ทำการรีเซ็ตเอกสารสำเร็จ');
    await detailStore.getPlanAnnouncement(planAnnouncementId);
  }
};

const onSubmit = async (status?: PlanAnnouncementStatus): Promise<void> => {
  // Save ChEditor content first before calling API
  await saveDocumentFirst();

  // Set flags so backend re-replaces document — only when form data changed
  if (status) {
    detailStore.body.status = status;
  }

  if (id.value) {
    if (isFormDirty.value
        && (detailStore.body.approveDocumentId
            || detailStore.body.announcementDocumentId) && !status) {
      const saveOption = await showSaveOptionDialogAsync();
      if (saveOption === undefined) return;
      detailStore.body.isApproveDocumentIdReplace = saveOption;
      detailStore.body.isAnnouncementDocumentIdReplace = saveOption;
    }

    isInitialized.value = false;
    await detailStore.updatePlanAnnouncement(id.value.toString(), status);
    await detailStore.getPlanAnnouncement(id.value.toString());
    isFormDirty.value = false;
    await nextTick();
    isInitialized.value = true;
    setCurrentTab()
    return;
  }

  isInitialized.value = false;
  const newId = await detailStore.createPlanAnnouncement();
  setCurrentTab();

  if (newId) {
    id.value = newId;

    router.replace(`detail/${newId}`);

    await detailStore.getPlanAnnouncement(newId);
    isFormDirty.value = false;
  }
  await nextTick();
  isInitialized.value = true;
};

const setCurrentTab = async () => {
  if (detailStore.body.approveDocumentId) {
    currentTab.value = '1'
  }
  else if (detailStore.body.announcementDocumentId) {
    currentTab.value = '2'
  }
};

const onCheckPubishDetail = async (handleSubmit: (evt: Event | SubmissionHandler, onSubmit?: SubmissionHandler) => Promise<unknown>) => {
  isRequiredPublished.value = true;
  await nextTick();

  handleSubmit(() => confirmAssignAsync());
}

const hasPlanSelected = (): boolean => {
  if (detailStore.body.planSelected.length > 0) return true;
  ToastHelper.planSelectedAtLeastMessageToast();
  return false;
};

const confirmAssignAsync = async (): Promise<void> => {
  if (!hasPlanSelected()) return;

  if (
    detailStore.body.assignees.filter((f) => f.assigneeType === AssigneeType.Assignee).length <= 0
  ) {
    return ToastHelper.assignAtLeastMessageToast();
  }

  if (!(await showConfirmDialogAsync(ConfirmDialogType.Assigned))) return;

  await onSubmit(PlanAnnouncementStatus.WaitingAssign);

  isRequiredPublished.value = false;

  if (detailStore.body.acceptors.length == 0) {
    await detailStore.getDefaultApproverAsync();
  }
};

const onCheckSendApprove = async (handleSubmit: (evt: Event | SubmissionHandler, onSubmit?: SubmissionHandler) => Promise<unknown>) => {
  isRequiredPublished.value = true;
  await nextTick();

  handleSubmit(() => sendApproveAsync());
}

const sendApproveAsync = async (): Promise<void> => {
  if (!hasPlanSelected()) return;

  if (detailStore.body.acceptors.length <= 0) {
    return ToastHelper.approvalAtLeastMessageToast();
  };

  if (!(await showConfirmDialogAsync(ConfirmDialogType.SendApproveConfirm))) return;

  await onSubmit(PlanAnnouncementStatus.WaitingAcceptor);

  isRequiredPublished.value = false;
};

const sendPublishAsync = async () => {
  if (!(await showConfirmDialogAsync(ConfirmDialogType.AnnouncementPlan))) return;

  await detailStore.onActionPlan(PlanAnnouncementAction.DirectorAnnouncement);
};

onMounted(async () => {
  if (!id.value) {
    await detailStore.getJorPorDirector();
  }

  if (id.value) {
    await detailStore.getPlanAnnouncement(id.value.toString());

  }

  if (detailStore.body.acceptors.length == 0) {
    await detailStore.getDefaultApproverAsync();
  }

  await nextTick();
  isInitialized.value = true;
});

watch(
  () => detailStore.body,
  () => {
    if (isInitialized.value) {
      isFormDirty.value = true;
    }
  },
  { deep: true }
);

onUnmounted(() => {
  detailStore.onClearBody();
});

const setDocumentReviewId = async (id: string): Promise<void> => {
  isInitialized.value = false;

  const documentType = currentTab.value === '1' ? 'approve' : 'announcement';

  if (documentType === 'approve') {
    detailStore.body.approveDocumentId = id;
  } else {
    detailStore.body.announcementDocumentId = id;
  }

  await nextTick();
  isInitialized.value = true;
};

const canEditDocument = computed(() => {
  return detailStore.body.status === PlanAnnouncementStatus.Draft
    || detailStore.body.status === PlanAnnouncementStatus.Rejected
});

watch(() => detailStore.body.status, (val: PlanAnnouncementStatus) => {
  if ([PlanAnnouncementStatus.Draft, PlanAnnouncementStatus.Rejected].includes(val)) {
    currentAccordion.value = ["0"];
  }

  if ([PlanAnnouncementStatus.WaitingAssign].includes(val)) {
    currentAccordion.value = ["0", "1"];
  }

  if ([PlanAnnouncementStatus.WaitingAcceptor].includes(val)) {
    currentAccordion.value = ["1"];
    currentTab.value = '1';
  }

  if ([PlanAnnouncementStatus.WaitingAnnouncement].includes(val)) {
    currentAccordion.value = ["2"];
    currentTab.value = '2';
  }

  if ([PlanAnnouncementStatus.Announcement].includes(val)) {
    currentAccordion.value = ["0", "1", "2"];
    currentTab.value = '2';
  }

}, { immediate: true });

const onAssignSegmentApproverAsync = async (assignSegmentCode: string) => {
  switch (assignSegmentCode) {
    case AssignDepartmentCodeEnum.SegmentJorPorOther:
      await detailStore.getDefaultSegmentOtherManagerApproverAsync();
      break;
    case AssignDepartmentCodeEnum.SegmentJorPorIT:
      await detailStore.getDefaultSegmentITManagerApproverAsync();
      break;
    default:
      break;
  }
};

// auto-save ผู้รับผิดชอบ โดยคร่อม guard เหมือน onSubmit
// กันไม่ให้การ refresh body หลังเซฟไปตั้ง isFormDirty (ซึ่งจะทำให้ dialog รีเซต Ch-Editor เด้งทั้งที่ยังไม่ได้แก้หน้าจอ)
const onUpsertAssigneesAsync = async (): Promise<void> => {
  isInitialized.value = false;
  await detailStore.onUpsertAssignees();
  isFormDirty.value = false;
  await nextTick();
  isInitialized.value = true;
};
</script>

<template>

  <TitleHeader label="ขออนุมัติเผยแพร่แผนจัดซื้อจัดจ้าง" :route-items="routeItems">
    <template #action>
      <div class="flex items-center gap-2">
        <p class="text-sm">สถานะ :</p>
        <BadgeComponent :label="AnnouncementStatusName(detailStore.body.status)"
          :bg-color-class="AnnouncementStatusColor(detailStore.body.status).bgColorClass"
          :text-color-class="AnnouncementStatusColor(detailStore.body.status).textColorClass" />
      </div>
      <Button label="ประวัติการใช้งาน" icon="pi pi-refresh" variant="outlined" severity="primary"
        v-if="detailStore.body.planAnnouncementId" class="bg-white! hover:bg-red-50!"
        @click="() => showActivityDialog(detailStore.body.planAnnouncementId!)" />
    </template>
  </TitleHeader>
  <Form @submit="onSubmit()" @invalidSubmit="ToastHelper.invalidMessageToast()" v-slot="{ handleSubmit, resetField }">
    <div class="grid lg:grid-cols-7 gap-2">
      <div class="lg:col-span-5 order-2 lg:order-1">
        <Tabs :value="currentTab" unstyled @update:value="(tab) => (currentTab = tab.toString())">
          <TabHeader :items="HeaderItem.filter((f, i) => (detailStore.body.planAnnouncementId ? f : i === 0))
            " class="sticky top-[57px] z-99 bg-[#F7F7F7]" />
          <TabPanels>
            <TabPanel value="0">
              <Detail />
            </TabPanel>
            <TabPanel value="1">
              <ApproveProcurment ref="approveProcurmentDocumentRef" :readonly="!canEditDocument" :save="saveDocument"
                :versions="detailStore.body.approveDocumentVersions"
                :canRestoreVersion="canEdit && menuStore.hasManage && canEditDocument"
                @restore-version="handleRestoreApproveVersion" />
            </TabPanel>
            <TabPanel value="2">
              <PublishDocument ref="publishDocumentRef" :readonly="!canEditDocument" :save="saveDocument"
                :versions="detailStore.body.announcementDocumentVersions"
                :canRestoreVersion="canEdit && menuStore.hasManage && canEditDocument"
                @restore-version="handleRestoreAnnouncementVersion" />
            </TabPanel>
          </TabPanels>
          <Card class="mt-4">
            <template #content>
              <UploadFileGroup v-model="detailStore.body.attachments" :disabled="!canEdit || !menuStore.hasManage" />
            </template>
          </Card>
        </Tabs>
      </div>
      <div class="relative lg:col-span-2  order-1 lg:order-2">
        <div class="flex flex-col gap-4 ml-3 sticky top-[100px]">
          <div class="flex items-center justify-end gap-2" v-if="menuStore.hasManage">
            <Button v-if="canEdit && authStore.profile.departmentCode === DepartmentId.JorPor" label="บันทึก" icon="pi pi-save" @click="onHandleSubmit(handleSubmit, resetField)"
              severity="success" />

            <ButtonRecall @click="detailStore.onActionPlan(PlanAnnouncementAction.Recall)" v-if="canRecall" />

            <ButtonConfirmAssign v-if="jorPorAssign" @click="onCheckPubishDetail(handleSubmit)" />
            <ButtonApproveConfirm v-if="canSendApprove" @click="onCheckSendApprove(handleSubmit)" />

            <ButtonSendEdit v-if="canAcceptAndReject" @click="detailStore.onActionPlan(PlanAnnouncementAction.AcceptorReject)" />
            <ButtonConfirm v-if="canAcceptAndReject && isLastApproval" @click="detailStore.onActionPlan(PlanAnnouncementAction.AcceptorApprove)" />
            <ButtonApprove v-if="canAcceptAndReject && !isLastApproval" @click="detailStore.onActionPlan(PlanAnnouncementAction.AcceptorApprove)" />

            <ButtonPublish v-if="canPublish" @click="sendPublishAsync()" />
          </div>

          <Accordion v-model:value="currentAccordion" unstyled multiple>
            <AccordionPanel value="999" class="mb-4" v-if="currentTab !== '0'">
              <AccordHeader label="Dictionary" />
              <AccordionContent>
                <div></div>
                <Card class="rounded-none overflow-auto h-[800px]">
                  <template #content>
                    <DocumentMapping pathToGet="plan/announcement" @on-click-select="
                      (text, hint) =>
                        currentTab == '1'
                          ? approveProcurmentDocumentRef?.setPlaceholderInDocument(text, hint)
                          : publishDocumentRef?.setPlaceholderInDocument(text, hint)
                    " />
                  </template>
                </Card>
              </AccordionContent>
            </AccordionPanel>
            <AccordionPanel value="0">
              <AccordAssignee title="มอบหมายผู้รับผิดชอบ" v-model="detailStore.body.assignees"
                :group="AssigneeGroup.JorPor" :disabled="(!jorPorAssign && !AssignCanEdit) || !menuStore.hasManage"
                :is-dropdown="(jorPorAssign || !AssignCanEdit)" :dropdown="detailStore.assignDepartmentDDL"
                v-on:change="onAssignSegmentApproverAsync" @on-change="onUpsertAssigneesAsync" />
            </AccordionPanel>

            <AccordionPanel value="1" class="mt-4">
              <AccordAcceptor title="ผู้มีอำนาจเห็นชอบ/อนุมัติ" v-model="detailStore.body.acceptors" is-manage
                :acceptorType="AcceptorType.Approver" @set-default="detailStore.getDefaultApproverAsync"
                :is-set-default="isConfirmAssign || menuStore.hasManage"
                :is-disable="!isConfirmAssign || !menuStore.hasManage" is-approve />
            </AccordionPanel>

            <AccordionPanel v-if="isAnnouncementVisible" value="2" class="mt-4">
              <AccordHeader label="ผอ. จพ. อนุมัติเผยแพร่แผน" />
              <AccordionContent>
                <Card class="rounded-none!">
                  <template #content>
                    <p>{{ detailStore.body.assigneeAnnouncement?.fullName }}</p>
                    <small class="text-gray-300">{{
                      detailStore.body.assigneeAnnouncement?.positionName
                      }}</small>
                  </template>
                </Card>
              </AccordionContent>
            </AccordionPanel>
          </Accordion>
        </div>
      </div>
    </div>
  </Form>
  <DialogReviewDocument v-if="!!id" v-model="showReviewDocumentDialog" :docId="reviwDocumentId"
    :docName="`plan-${new Date().toISOString()}-${id.toString()}`" @on-click-use-document="setDocumentReviewId" />
</template>
