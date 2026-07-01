<script setup lang="ts">
import type { Option } from '@/models/shared/option';
import { computed, nextTick, onMounted, onUnmounted, ref, watch } from 'vue';
import { Form } from 'vee-validate';
import { AccordAcceptor } from '@/components/Accordions';
import { ButtonApprove, ButtonConfirm, ButtonApproveConfirm, ButtonSave, ButtonSendEdit, ButtonRecall, ButtonSendChange, ButtonSendCancel } from '@/components/Button';
import { TabHeader, TitleHeader } from '@/components/cosmetic';
import { BadgeStatus as BadgeComponent } from '@/components';
import { AcceptorType } from '@/enums/participants';
import { ConfirmDialogType } from '@/enums/dialog';
import { showActivityDialog, showConfirmDialogAsync, showSaveOptionDialogAsync } from '@/helpers/dialog';
import { useAuthenticationStore } from '@/stores/authentication';
import { AppointmentSection } from './components';
import { CommitteeListSection } from '../PP';
import { AppointStatus } from '../../enums/pp001';
import { usePP001DetailStore } from '../../stores/PP001/pp001Store';
import { usePPDetailStore } from '../../../../stores/PP/ppStore';
import ChEditor from '@/components/Document/ChEditor.vue';
import AppointmenttHelper from '@/helpers/appointment';
import ToastHelper from '@/helpers/toast';
import DocumentMapping from '@/components/DocumentMapping.vue';
import DialogReviewDocument from '@/components/Document/DialogReviewDocument.vue';
import { useMenuStore } from '@/stores/menu';
import type { TCommitteeSection, TDutySection } from '@/models/PP/ppModel';
import type { ParticipantsAcceptor } from '@/models/shared/participants';
import { ProgramHistoryName } from '@/enums/programHistoryName';
import appointmentService from '../../services/PP001/PP001Service';
import { HttpStatusCode } from 'axios';

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
  budgetYear: {
    type: Number,
    default: 0,
  },
  readonly: {
    type: Boolean,
    default: false,
  },
});


const { BadgeStatus } = AppointmenttHelper;
const procurementStore = usePPDetailStore();
const userStore = useAuthenticationStore();
const menuStore = useMenuStore();
const preProcurement001DetailStore = usePP001DetailStore();
const showReviewDocumentDialog = ref(false);
const reviwDocumentId = ref<string>('');

const budgetMedianPriceCondition = 100000;

const docRef = ref<InstanceType<typeof ChEditor> | null>(null);
const currentTab = ref('detail');
const currentAccordion = ref<string[]>(['0']);
const isFormDirty = ref(false);
const isInitialized = ref(false);

const HeaderItem = ref([
  {
    label: 'รายละเอียด',
    value: 'detail',
  },
  {
    label: 'เอกสารขออนุมัติแต่งตั้งบุคคล/คกก.จัดทำขอบเขตของงาน/ราคากลาง',
    value: 'review',
  },
] as Option[]);

const setPlaceholderInDocument = (text: string, hint?: string) => {
  if (docRef.value) {
    docRef.value.setPlaceholderInDocument(text, hint);
  }
};

const saveDocumentFirst = async (): Promise<void> => {
  if (currentTab.value === 'review' && docRef.value?.saveAndWait && canEditDocument.value) {
    return new Promise((resolve) => {
      docRef.value!.saveAndWait(() => {
        resolve();
      });
    });
  }
};

const saveDocument = async () => {
  // Wait for Collabora to save first if on document tab
  if (currentTab.value === 'review') {
    await saveDocumentFirst();
  }
  await onSubmit();
};

const onSubmit = async (status?: AppointStatus): Promise<void> => {
  await saveDocumentFirst();

  const { appoint } = preProcurement001DetailStore.pp001Detail;

  if (preProcurement001DetailStore.pp001Detail.torDraftCommittees.length === 0) {
    return ToastHelper.errorDescription("กรูณาระบุ บุคคล/คณะกรรมการจัดทำร่างขอบเขตของงาน");
  }

  if (props.budget > budgetMedianPriceCondition && preProcurement001DetailStore.pp001Detail.medianPriceCommittees.length === 0) {
    return ToastHelper.errorDescription("กรูณาระบุ บุคคล/คณะกรรมการกำหนดราคากลาง");
  }

  if (status === AppointStatus.WaitingApproval && !validateSendApproval()) {
    return;
  }

  if (status === AppointStatus.WaitingApproval && !await showConfirmDialogAsync(ConfirmDialogType.SendApproveConfirm)) return;

  if (appoint.id) {
    // If document is already replaced and form was edited, confirm before overwriting
    if (isFormDirty.value
      && preProcurement001DetailStore.pp001Detail.appointDocumentId
      && !status) {
      const saveOption = await showSaveOptionDialogAsync();
      if (saveOption === undefined) return;
      preProcurement001DetailStore.pp001Detail.isAppointDocumentIdReplaced = saveOption;
    }

    isInitialized.value = false;
    await preProcurement001DetailStore.updateAppointById(appoint.id, status);
    isFormDirty.value = false;
    await nextTick();
    isInitialized.value = true;
    setCurrentTab();
    return;
  }

  isInitialized.value = false;
  await preProcurement001DetailStore.createAppointment();
  isFormDirty.value = false;
  await nextTick();
  isInitialized.value = true;
  setCurrentTab();
};

const setCurrentTab = async () => {
  if (preProcurement001DetailStore.pp001Detail.appointDocumentId) {
    currentTab.value = 'review';
  }
};

// Validation functions (merged during conflict resolution)
const validateTorCommittees = (
  committees: Array<TCommitteeSection>,
  duties: Array<TDutySection>,
  isCommittee: boolean
): boolean => {
  const { pobDropdown } = preProcurement001DetailStore;

  if (committees.length === 0) {
    ToastHelper.errorDescription('จะต้องมีบุคคล/คณะกรรมการจัดทำร่างขอบเขตของงานอย่างน้อย 1 คน');
    return false;
  }

  if (duties.length === 0) {
    ToastHelper.errorDescription('จะต้องมีอำนาจหน้าที่ของบุคคล/คณะกรรมการจัดทำร่างขอบเขตของงานอย่างน้อย 1 รายการ');
    return false;
  }

  if (committees.filter(f => f.departmentCode === procurementStore.procurementDetail.departmentCode).length === 0) {
    ToastHelper.errorDescription('จะต้องมีบุคคล/คณะกรรมการจัดทำร่างขอบเขตของงานที่เป็นฝ่ายเดียวกันกับข้อมูลจัดซื้อจัดจ้างอย่างน้อย 1 คน');
    return false;
  }

  if (isCommittee && committees.filter(f => f.committeePositionsCode === pobDropdown[0].value).length === 0) {
    ToastHelper.mustHaveLeaderBoardToast();
    return false;
  }

  return true;
};

const validateMdpCommittee = (
  committees: Array<TCommitteeSection>,
  duties: Array<TDutySection>,
  isCommittee: boolean
): boolean => {
  const { pobDropdown } = preProcurement001DetailStore;

  if (committees.length === 0) {
    ToastHelper.errorDescription('จะต้องมีบุคคล/คณะกรรมการกำหนดราคากลางอย่างน้อย 1 คน');
    return false;
  }

  if (duties.length === 0) {
    ToastHelper.errorDescription('จะต้องมีอำนาจหน้าที่ของบุคคล/คณะกรรมการกำหนดราคากลางอย่างน้อย 1 รายการ');
    return false;
  }

  if (committees.filter(f => f.departmentCode === procurementStore.procurementDetail.departmentCode).length === 0) {
    ToastHelper.errorDescription('จะต้องมีอำนาจหน้าที่ของบุคคล/คณะกรรมการกำหนดราคากลางที่อยู่ฝ่ายเดียวกับงานอย่างน้อย 1 คน');
    return false;
  }

  if (isCommittee && committees.filter(f => f.committeePositionsCode === pobDropdown[0].value).length === 0) {
    ToastHelper.mustHaveLeaderBoardToast();
    return false;
  }

  return true;
};

const validateAcceptors = (acceptors: Array<ParticipantsAcceptor>): boolean => {
  if (acceptors.length <= 0) {
    ToastHelper.approvalAtLeastMessageToast();
    return false;
  }
  return true;
};

const validateSendApproval = (): boolean => {
  const {
    torDraftCommittees,
    torDraftCommitteeDuties,
    isTorCommittee,
    medianPriceCommittees,
    medianPriceCommitteeDuties,
    isMedianPriceCommittee,
    acceptors,
  } = preProcurement001DetailStore.pp001Detail;

  if (!validateTorCommittees(torDraftCommittees, torDraftCommitteeDuties, isTorCommittee)) return false;

  if (props.budget > budgetMedianPriceCondition && !validateMdpCommittee(medianPriceCommittees, medianPriceCommitteeDuties, isMedianPriceCommittee)) return false;

  if (!validateAcceptors(acceptors)) return false;

  return true;
};

const setDocumentReviewId = async (id: string): Promise<void> => {
  isInitialized.value = false;
  preProcurement001DetailStore.pp001Detail.appointDocumentId = id;
  await nextTick();
  isInitialized.value = true;
};

onMounted(async () => {
  if (props.preProcurementId) {
    preProcurement001DetailStore.getPobDDLAsync();
    preProcurement001DetailStore.getPob1DDLAsync();
    preProcurement001DetailStore.pp001Detail.appoint.procurementId = props.preProcurementId;
    preProcurement001DetailStore.pp001Detail.appoint.procurementBudgetYear = props.budgetYear;
  }

  if (props.id) {
    await preProcurement001DetailStore.onGetAppointmentById(props.id);
  }

  await preProcurement001DetailStore.onSetDefaultDuties();

  if (!procurementStore.procurementDetail.appoint) {
    await preProcurement001DetailStore.getDefaultAcceptor(procurementStore.procurementDetail.budget, userStore.profile.id, procurementStore.procurementDetail.supplyMethodCode, procurementStore.procurementDetail.supplyMethodSpecialTypeCode, procurementStore.procurementDetail.isStock, procurementStore.procurementDetail.isCommercialMaterial);
  }

  await nextTick();
  isInitialized.value = true;
});

watch(
  () => preProcurement001DetailStore.pp001Detail,
  () => {
    if (isInitialized.value) {
      isFormDirty.value = true;
    }
  },
  { deep: true }
);

const defaultDocumentStatuses = new Set([
  AppointStatus.WaitingApproval,
  AppointStatus.Edit,
  AppointStatus.Approved,
]);


watch(
  () => preProcurement001DetailStore.pp001Detail.appoint.status,
  (newStatus: AppointStatus) => {
    if (defaultDocumentStatuses.has(newStatus)) {
      currentTab.value = 'review';
    }
  },
  { immediate: true }
)

onUnmounted(() => {
  preProcurement001DetailStore.resetPP001Detail();
});

const canEditDocument = computed(() => {
  return preProcurement001DetailStore.pp001Detail.appoint.status == AppointStatus.Draft
    || preProcurement001DetailStore.pp001Detail.appoint.status == AppointStatus.Rejected
    || preProcurement001DetailStore.pp001Detail.appoint.status == AppointStatus.Edit
});

const handleRestoreVersion = async (): Promise<void> => {
  const appointId = preProcurement001DetailStore.pp001Detail.appoint.id;
  if (!appointId) {
    ToastHelper.error('เกิดข้อผิดพลาด', 'ไม่สามารถรีเซ็ตเอกสารได้ ข้อมูลไม่ครบถ้วน');
    return;
  }

  const { status } = await appointmentService.resetDocumentAsync(appointId);

  if (status === HttpStatusCode.Ok) {
    ToastHelper.success('สำเร็จ', 'ทำการรีเซ็ตเอกสารสำเร็จ');
    await preProcurement001DetailStore.onGetAppointmentById(appointId);
  }
};

</script>

<template>
  <TitleHeader label="ขอแต่งตั้งบุคคล/คกก. จัดทำขอบเขตของงาน/ราคากลาง">
    <template #action>
      <div class="flex items-center gap-4">
        <BadgeComponent prefix="สถานะ: "
          :label="BadgeStatus(preProcurement001DetailStore.pp001Detail.appoint.status).label"
          :color="BadgeStatus(preProcurement001DetailStore.pp001Detail.appoint.status).color" />
        <BadgeComponent v-if="preProcurement001DetailStore.pp001Detail.appoint.isChange" label="ขอเปลี่ยนแปลง"
          color="amber" />
        <BadgeComponent v-if="preProcurement001DetailStore.pp001Detail.appoint.isCancel" label="ขอยกเลิก" color="red" />
      </div>

      <div class="w-px self-stretch min-h-[30px] bg-gray-300 mx-1"
        v-if="preProcurement001DetailStore.pp001Detail.appoint.id" />

      <Button label="ประวัติการใช้งาน" icon="pi pi-refresh" variant="outlined" severity="primary"
        v-if="preProcurement001DetailStore.pp001Detail.appoint.id" class="bg-white! hover:bg-red-50!"
        @click="() => showActivityDialog(preProcurement001DetailStore.pp001Detail.appoint.id!, ProgramHistoryName.Appoint)" />
    </template>
  </TitleHeader>
  <Form @submit="onSubmit()" @invalidSubmit="ToastHelper.invalidMessageToast()" v-slot="{ handleSubmit }">
    <div class="grid grid-cols-1 lg:grid-cols-7 gap-2">
      <div class="lg:col-span-5 lg:order-1 order-2">
        <Tabs :value="currentTab" unstyled @update:value="(tab) => currentTab = tab.toString()">
          <TabHeader class="sticky top-[55px] z-3 bg-[#F7F7F7] pt-2"
            :items="HeaderItem.filter((h, i) => preProcurement001DetailStore.pp001Detail.appoint.id ? h : i === 0)" />
          <TabPanels>
            <TabPanel value="detail">
              <AppointmentSection :readonly="props.readonly" />
              <CommitteeListSection
                v-model:committeeSection="preProcurement001DetailStore.pp001Detail.torDraftCommittees"
                v-model:dutySection="preProcurement001DetailStore.pp001Detail.torDraftCommitteeDuties"
                v-model:is-committee="preProcurement001DetailStore.pp001Detail.isTorCommittee"
                :is-edit="preProcurement001DetailStore.isEdit && menuStore.hasManage"
                :readonly="props.readonly"
                label="บุคคล/คณะกรรมการจัดทำร่างขอบเขตของงาน" />
              <CommitteeListSection
                v-model:committeeSection="preProcurement001DetailStore.pp001Detail.medianPriceCommittees"
                v-model:dutySection="preProcurement001DetailStore.pp001Detail.medianPriceCommitteeDuties"
                v-model:is-committee="preProcurement001DetailStore.pp001Detail.isMedianPriceCommittee"
                :is-edit="preProcurement001DetailStore.isEdit && menuStore.hasManage"
                :copy-type="preProcurement001DetailStore.pp001Detail.isTorCommittee"
                :copy-list="preProcurement001DetailStore.pp001Detail.torDraftCommittees"
                :readonly="props.readonly"
                v-if="props.budget > budgetMedianPriceCondition" label="บุคคล/คณะกรรมการกำหนดราคากลาง" />
            </TabPanel>
            <TabPanel value="review">
              <Card class="mb-4">
                <template #content>
                  <ChEditor :docId="preProcurement001DetailStore.pp001Detail.appointDocumentId"
                    :docName="new Date().toISOString()" :readonly="!menuStore.hasManage || !canEditDocument"
                    ref="docRef" v-if="preProcurement001DetailStore.pp001Detail.appointDocumentId"
                    :key="`${preProcurement001DetailStore.pp001Detail.appointDocumentId}-${preProcurement001DetailStore.pp001Detail.appoint.status}`"
                    :save="saveDocument" :versions="preProcurement001DetailStore.pp001Detail.appointDocumentVersions"
                    :canRestoreVersion="preProcurement001DetailStore.isEdit && menuStore.hasManage && canEditDocument"
                    @restore-version="handleRestoreVersion" />
                </template>
              </Card>
            </TabPanel>
          </TabPanels>
        </Tabs>
      </div>
      <div class="relative lg:col-span-2 lg:order-2 order-1">
        <div class="flex flex-col gap-4 lg:ml-3 sticky top-[55px] pt-2 z-3 bg-[#F7F7F7]">
          <div class="flex items-center gap-2 justify-end" v-if="menuStore.hasManage && !readonly">
            <ButtonSave label="ยกเลิกคำขอ" variant="outlined" v-if="preProcurement001DetailStore.states.isReturn"
              icon="pi pi-undo" severity="primary" @click="preProcurement001DetailStore.onRestoreStateAsync()" />

            <ButtonSave v-if="preProcurement001DetailStore.isEdit" icon="pi pi-save" severity="success" type="submit" />
            <ButtonApproveConfirm
              v-if="preProcurement001DetailStore.pp001Detail.appoint.id && preProcurement001DetailStore.isEdit"
              @click="handleSubmit(() => onSubmit(AppointStatus.WaitingApproval))" />

            <ButtonRecall v-if="preProcurement001DetailStore.states.isRecall"
              @click="preProcurement001DetailStore.onRecallAsync()" />

            <ButtonSendEdit
              v-if="preProcurement001DetailStore.pp001Detail.appoint.status == AppointStatus.WaitingApproval && preProcurement001DetailStore.states.isCurrentApproval"
              @click="preProcurement001DetailStore.RejectAction()" />

            <ButtonApprove
              v-if="preProcurement001DetailStore.pp001Detail.appoint.status == AppointStatus.WaitingApproval && (preProcurement001DetailStore.states.isCurrentApproval && !preProcurement001DetailStore.states.isLastAcceptorApproval)"
              @click="preProcurement001DetailStore.approveAction()" />

            <ButtonConfirm
              v-if="preProcurement001DetailStore.pp001Detail.appoint.status == AppointStatus.WaitingApproval && (preProcurement001DetailStore.states.isCurrentApproval && preProcurement001DetailStore.states.isLastAcceptorApproval)"
              @click="preProcurement001DetailStore.approveAction()" />

            <ButtonSendChange
              v-if="preProcurement001DetailStore.pp001Detail.appoint.status == AppointStatus.Approved && preProcurement001DetailStore.states.isAuthDepartment"
              @click="preProcurement001DetailStore.requestAction(true)" />

            <ButtonSendCancel
              v-if="preProcurement001DetailStore.pp001Detail.appoint.status == AppointStatus.Approved && preProcurement001DetailStore.states.isAuthDepartment"
              @click="preProcurement001DetailStore.requestAction(false)" />
          </div>

          <Accordion v-model:value="currentAccordion" unstyled multiple>
            <AccordionPanel value="999" class="mb-4" v-if="currentTab !== 'detail'">
              <AccordHeader label="Dictionary" />
              <AccordionContent>
                <Card class="rounded-none overflow-auto h-[800px]">
                  <template #content>
                    <DocumentMapping pathToGet="appointments" @on-click-select="
                      (text, hint) => setPlaceholderInDocument(text, hint)
                    " />
                  </template>
                </Card>
              </AccordionContent>
            </AccordionPanel>
            <AccordionPanel value="0">
              <AccordAcceptor title="ผู้มีอำนาจเห็นชอบ/อนุมัติ"
                v-model="preProcurement001DetailStore.pp001Detail.acceptors"
                @set-default="() => preProcurement001DetailStore.getDefaultAcceptor(props.budget, userStore.profile.id, procurementStore.procurementDetail.supplyMethodCode, procurementStore.procurementDetail.supplyMethodSpecialTypeCode, procurementStore.procurementDetail.isStock, procurementStore.procurementDetail.isCommercialMaterial)"
                :acceptor-type="AcceptorType.Approver" isManage
                :is-disable="!preProcurement001DetailStore.isEdit || !menuStore.hasManage || readonly" isApprove
                :is-set-default="preProcurement001DetailStore.states.isCanSetDefaultApprover && menuStore.hasManage" />
            </AccordionPanel>
          </Accordion>
        </div>
      </div>
    </div>
  </Form>
  <DialogReviewDocument v-model="showReviewDocumentDialog" :docId="reviwDocumentId"
    :docName="`pp001-${new Date().toISOString()}-${reviwDocumentId}`" @on-click-use-document="setDocumentReviewId" />
</template>
