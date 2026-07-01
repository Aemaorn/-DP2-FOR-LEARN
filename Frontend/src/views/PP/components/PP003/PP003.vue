<script setup lang="ts">
import { computed, nextTick, onMounted, ref, watch, type PropType } from 'vue';
import type { Option } from '@/models/shared/option';
import ChEditor from '@/components/Document/ChEditor.vue';
import { TitleHeader, TabHeader } from '@/components/cosmetic';
import { ButtonSave, ButtonSendApprove, ButtonApprove, ButtonRecall, ButtonSendEdit, ButtonConfirm, ButtonConfirmAssign, ButtonApproveConfirm, ButtonNotAgree, ButtonSendChange, ButtonSendCancel } from '@/components/Button';
import { AccordAcceptor, AccordAssignee } from '@/components/Accordions';
import { BadgeStatus } from '@/components/index';
import { usePP003DetailStore } from '@/views/PP/stores/PP003/pp003Store';
import { MedianPriceSection } from './components/sub';
import { Form as VeeForm } from 'vee-validate';
import ToastHelper from '@/helpers/toast';
import MedianPriceHelper from '@/helpers/medianPrice';
import { MedianPriceAccordion, PP003Status } from '../../enums/pp003';
import { AcceptorType, AssigneeType } from '@/enums/participants';
import { usePPDetailStore } from '../../../../stores/PP/ppStore';
import { showActivityDialog, showSaveOptionDialogAsync } from '@/helpers/dialog';
import DocumentMapping from '@/components/DocumentMapping.vue';
import DialogReviewDocument from '@/components/Document/DialogReviewDocument.vue';
import { useMenuStore } from '@/stores/menu';
import { ProgramHistoryName } from '@/enums/programHistoryName';
import PP003Service from '../../services/PP003/pp003Service';
import { HttpStatusCode } from 'axios';

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

const _headerItem = ref([
  {
    label: 'รายละเอียด',
    value: 'detail',
  },
  {
    label: 'เอกสารขออนุมัติกำหนดราคากลาง',
    value: 'document',
  },
] as Array<Option>);

const { MapStatusColor, AccordionName } = MedianPriceHelper;
const menuStore = useMenuStore();
const procurementStore = usePPDetailStore();
const detailStore = usePP003DetailStore();

const showReviewDocumentDialog = ref(false);
const reviwDocumentId = ref<string>('');
const docRef = ref<InstanceType<typeof ChEditor> | null>(null);

const currentAccordion = ref<MedianPriceAccordion[]>([]);
const isFormDirty = ref(false);
const isInitialized = ref(false);

const setPlaceholderInDocument = (text: string, hint?: string) => {
  if (docRef.value) {
    docRef.value.setPlaceholderInDocument(text, hint);
  }
};

// Save document to Collabora first before API call
const saveDocumentFirst = async (): Promise<void> => {
  // Only save if document is editable (not read-only)
  if (detailStore.currentTab === 'document' && docRef.value?.saveAndWait && canEditDocument.value) {
    return new Promise((resolve) => {
      docRef.value!.saveAndWait(() => {
        resolve();
      });
    });
  }
};

const saveDocumentInEditor = async (): Promise<void> => {
  if (detailStore.currentTab === 'document') {
    await saveDocumentFirst();
  }
};

onMounted(async (): Promise<void> => {
  detailStore.resetBody();

  await detailStore.onGetTemplateOptionsAsync();

  if (props.id) {
    await detailStore.onGetByIdAsync(props.procurementId, props.id);

  };

  if (!props.id) {
    await detailStore.setDefaultCommitteeAsync(props.procurementId);
  }


  switch (procurementStore.procurementDetail.hasMd) {
    case true:
      if (detailStore.body.assignees.filter(f => f.assigneeType === AssigneeType.Director).length === 0) {
        await detailStore.setDefaultJorPorDirectorAsync(true);
      }

      if (detailStore.body.acceptors.filter(f => f.acceptorType === AcceptorType.DepartmentDirectorAgree).length === 0) {
        await detailStore.setDefaultUnitAsync(true);
      }

      if (detailStore.body.assignees.some(f => f.assigneeType === AssigneeType.Assignee) && detailStore.body.acceptors.filter(f => f.acceptorType === AcceptorType.Approver).length === 0) {
        await detailStore.setDefaultAcceptorAsync();
      }

      break;

    case false:
      if (detailStore.body.acceptors.some(s => s.acceptorType === AcceptorType.MedianPriceCommittee) && detailStore.body.acceptors.filter(f => f.acceptorType === AcceptorType.Approver).length === 0) {
        await detailStore.setDefaultAcceptorAsync();
      }

      break;
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

const setDocumentReviewId = async (id: string): Promise<void> => {
  isInitialized.value = false;
  detailStore.body.medianPriceDocumentId = id;
  await nextTick();
  isInitialized.value = true;
};

const canEditDocument = computed(() => {
  return detailStore.body.status == PP003Status.Draft
    || detailStore.body.status == PP003Status.Edit
    || detailStore.body.status == PP003Status.Rejected
    || detailStore.body.status == PP003Status.WaitingComment
});

const accordionMappingWithMd = new Map<PP003Status, MedianPriceAccordion[]>([
  [PP003Status.Draft, [MedianPriceAccordion.MedianPriceCommittee, MedianPriceAccordion.Units]],
  [PP003Status.Edit, [MedianPriceAccordion.MedianPriceCommittee, MedianPriceAccordion.Units]],
  [PP003Status.Rejected, [MedianPriceAccordion.MedianPriceCommittee, MedianPriceAccordion.Units]],
  [PP003Status.WaitingCommitteeApproval, [MedianPriceAccordion.MedianPriceCommittee]],
  [PP003Status.WaitingUnitApproval, [MedianPriceAccordion.Units]],
  [PP003Status.WaitingAssign, [MedianPriceAccordion.JorPorSuggestion, MedianPriceAccordion.Acceptor]],
  [PP003Status.WaitingComment, [MedianPriceAccordion.JorPorSuggestion, MedianPriceAccordion.Acceptor]],
  [PP003Status.RejectToAssignee, [MedianPriceAccordion.JorPorSuggestion, MedianPriceAccordion.Acceptor]],
  [PP003Status.WaitingApproval, [MedianPriceAccordion.Acceptor]],
]);

const accordionMappingWithoutMd = new Map<PP003Status, MedianPriceAccordion[]>([
  [PP003Status.Draft, [MedianPriceAccordion.MedianPriceCommittee, MedianPriceAccordion.Acceptor]],
  [PP003Status.Edit, [MedianPriceAccordion.MedianPriceCommittee, MedianPriceAccordion.Acceptor]],
  [PP003Status.Rejected, [MedianPriceAccordion.MedianPriceCommittee, MedianPriceAccordion.Acceptor]],
  [PP003Status.WaitingCommitteeApproval, [MedianPriceAccordion.MedianPriceCommittee]],
  [PP003Status.WaitingApproval, [MedianPriceAccordion.Acceptor]],
]);

const updateAccordionBasedOnStatus = (status: PP003Status): void => {
  if (status === PP003Status.Approved) {
    currentAccordion.value = [
      MedianPriceAccordion.MedianPriceCommittee,
      MedianPriceAccordion.Units,
      MedianPriceAccordion.JorPorSuggestion,
      MedianPriceAccordion.Acceptor
    ];
    return;
  }

  const mapping = procurementStore.procurementDetail.hasMd
    ? accordionMappingWithMd
    : accordionMappingWithoutMd;

  currentAccordion.value = mapping.get(status) || [];
};

const defaultDocumentStatuses = [
  PP003Status.WaitingCommitteeApproval,
  PP003Status.WaitingUnitApproval,
  PP003Status.WaitingAssign,
  PP003Status.WaitingComment,
  PP003Status.RejectToAssignee,
  PP003Status.WaitingApproval,
  PP003Status.Approved,
];

watch(
  () => detailStore.body.status,
  (newStatus: PP003Status) => {
    updateAccordionBasedOnStatus(newStatus);

    if (defaultDocumentStatuses.includes(newStatus)) {
      detailStore.currentTab = 'document';
    }
  }, { immediate: true });

const saveDocument = async () => {
  // Save document in editor before updating (only on document tab)
  await saveDocumentInEditor();

  // Set flag so backend re-replaces document — only when form data changed
  if (isFormDirty.value && detailStore.body.id && detailStore.body.medianPriceDocumentId
      && [PP003Status.Draft, PP003Status.Edit, PP003Status.Rejected].includes(detailStore.body.status)) {
    const saveOption = await showSaveOptionDialogAsync();
    if (saveOption === undefined) return;
    detailStore.body.isMedianPriceDocumentIdReplaced = saveOption;
  }

  isInitialized.value = false;
  await detailStore.onSubmitAsync(props.procurementId);
  isFormDirty.value = false;
  await nextTick();
  isInitialized.value = true;

  if (detailStore.body.medianPriceDocumentId) {
    detailStore.currentTab = 'document';
  }
};

const updateAssignee = async (): Promise<void> => {
  // Save document in editor before updating (only on document tab)
  await saveDocumentInEditor();

  await detailStore.onAssignAsync();

  if (!detailStore.body.acceptors.some(a => a.acceptorType === AcceptorType.Approver)) {
    await detailStore.setDefaultAcceptorAsync();
  }
};

const onCommitteeSendApproveAsync = async (): Promise<void> => {
  await saveDocumentInEditor();
  await detailStore.onCommitteeSendApproveAsync(props.procurementId);
};

const onRecallAsync = async (): Promise<void> => {
  await saveDocumentInEditor();
  await detailStore.onRecallAsync(props.procurementId, props.id!);
};

const onRejectByTypeAsync = async (isNotAgree = false): Promise<void> => {
  await saveDocumentInEditor();
  await detailStore.onRejectByTypeAsync(props.procurementId, props.id!, isNotAgree);
};

const onApprovedByTypeAsync = async (acceptorType: AcceptorType, isLast = false): Promise<void> => {
  await saveDocumentInEditor();
  await detailStore.onApprovedByTypeAsync(props.procurementId, props.id!, acceptorType, isLast);
};

const onAssigneeRejectAsync = async (): Promise<void> => {
  await saveDocumentInEditor();
  await detailStore.onAssigneeRejectAsync(props.procurementId, props.id!);
};

const onAssignAsyncWithConfirm = async (): Promise<void> => {
  await saveDocumentInEditor();
  await detailStore.onAssignAsync(true);
};

const onJorPorSendApprovalAsync = async (): Promise<void> => {
  await saveDocumentInEditor();
  await detailStore.onJorPorSendApprovalAsync();
};

const onRequestChangeOrCancelled = async (isCancel = false): Promise<void> => {
  await saveDocumentInEditor();
  await detailStore.onRequestChangeOrCancelled(isCancel);
};

const onSetIsUnableToPerformDutiesByIdAsync = async (acceptorId: string, e: boolean, remark?: string): Promise<void> => {
  await saveDocumentInEditor();
  await detailStore.onSetIsUnableToPerformDutiesByIdAsync(acceptorId, e, remark);
};

const onJorPorCommentAsync = async (reason: string): Promise<void> => {
  await saveDocumentInEditor();
  await detailStore.onJorPorCommentAsync(props.procurementId!, props.id!, reason);
};

const handleJorPorComment = (e: { reason: string; userId: string }): Promise<void> => onJorPorCommentAsync(e.reason);

const handleRestoreVersion = async (): Promise<void> => {
  if (!props.procurementId || !detailStore.body.id) {
    ToastHelper.error('เกิดข้อผิดพลาด', 'ไม่สามารถรีเซ็ตเอกสารได้ ข้อมูลไม่ครบถ้วน');
    return;
  }

  const { status } = await PP003Service.resetDocumentAsync(props.procurementId, detailStore.body.id);

  if (status === HttpStatusCode.Ok) {
    ToastHelper.success('สำเร็จ', 'ทำการรีเซ็ตเอกสารสำเร็จ');
    await detailStore.onGetByIdAsync(props.procurementId, detailStore.body.id);
  }
};

</script>

<template>
  <TitleHeader label="กำหนดราคากลาง (ราคาอ้างอิง)">
    <template #action>
      <div class="flex items-center gap-4">
        <BadgeStatus :label="MapStatusColor(detailStore.body.status).label"
          :color="MapStatusColor(detailStore.body.status).color" />
        <BadgeStatus v-if="detailStore.body.isChange" label="ขอเปลี่ยนแปลง" color="amber" />
        <BadgeStatus v-if="detailStore.body.isCancel" label="ขอยกเลิก" color="red" />
      </div>

      <div class="w-px self-stretch min-h-[30px] bg-gray-300 mx-1" v-if="detailStore.body.id" />

      <Button label="ประวัติการใช้งาน" icon="pi pi-refresh" variant="outlined" severity="primary"
        v-if="detailStore.body.id" class="bg-white! hover:bg-red-50!"
        @click="() => showActivityDialog(detailStore.body.id!, ProgramHistoryName.MedianPrice)" />
    </template>
  </TitleHeader>
  <VeeForm @submit="saveDocument" v-slot="{ handleSubmit }"
    :initial-values="detailStore.body" @invalidSubmit="ToastHelper.invalidMessageToast()">
    <div class="grid grid-cols-1 lg:grid-cols-7 gap-2">
      <div class="lg:col-span-5 order-2 lg:order-1">
        <Tabs :value="detailStore.currentTab" unstyled @update:value="(tab) => detailStore.currentTab = tab.toString()">
          <TabHeader
            :items="_headerItem.filter((value, index) => (detailStore.isChangeTemplate || !props.id) ? index === 0 : value)"
            class="sticky top-14 z-3 bg-[#F7F7F7] pt-2" />
          <TabPanels>
            <TabPanel value="detail">
              <MedianPriceSection v-model="detailStore.body" :states="detailStore.states" :readonly="readonly" />
              <component v-if="detailStore.states.currentTemplate" :is="detailStore.states.currentTemplate"
                v-model="detailStore.body" :key="detailStore.body.medianPriceDocumentTemplateCode"
                :states="detailStore.states" />
            </TabPanel>
            <TabPanel value="document">
              <Card class="mb-4">
                <template #content>
                  <ChEditor :docId="detailStore.body.medianPriceDocumentId" :docName="new Date().toISOString()"
                    :readonly="!menuStore.hasManage || !canEditDocument" ref="docRef"
                    :key="detailStore.body.medianPriceDocumentId" v-if="detailStore.body.medianPriceDocumentId"
                    :save="saveDocument"
                    :versions="detailStore.body.documentVersions"
                    :canRestoreVersion="detailStore.states.isEditor && menuStore.hasManage && canEditDocument"
                    @restore-version="handleRestoreVersion" />
                </template>
              </Card>
            </TabPanel>
          </TabPanels>
        </Tabs>
      </div>

      <div class="lg:col-span-2 order-1 lg:order-2">
        <div class="flex flex-col gap-4 lg:ml-3 sticky top-[100px]">
          <div class="flex items-center gap-2 justify-end" v-if="menuStore.hasManage && !readonly">
            <ButtonSave label="ยกเลิกคำขอ" variant="outlined" v-if="detailStore.states.conRestoreState"
              icon="pi pi-undo" severity="primary" @click="detailStore.onRestoreStateAsync" />

            <div id="medianPrice-section" class="flex items-center gap-2" v-if="detailStore.states.isEditor">
              <ButtonSave type="submit" />
              <ButtonSendApprove type="button" v-if="!detailStore.isChangeTemplate && props.id"
                @click="() => handleSubmit(() => onCommitteeSendApproveAsync())" />
            </div>

            <ButtonRecall v-if="detailStore.states.canCommitteeRecall"
              @click="() => onRecallAsync()" />

            <div id="medianPrice-approve-section" class="flex items-center gap-2"
              v-if="detailStore.states.isCommitteeApproval">
              <ButtonNotAgree v-if="detailStore.states.isCommitteeCurrentApproval"
                @click="() => onRejectByTypeAsync(true)" />

              <ButtonApprove v-if="detailStore.states.isCommitteeCurrentApproval"
                @click="() => onApprovedByTypeAsync(AcceptorType.MedianPriceCommittee)" />
            </div>

            <div id="unit-approve-section" class="flex items-center gap-2" v-if="detailStore.states.isUnitApproval">
              <ButtonSendEdit v-if="detailStore.states.isCurrentUnitApproval" @click="() => onRejectByTypeAsync()" />

              <ButtonApprove v-if="detailStore.states.isCurrentUnitApproval"
                @click="() => onApprovedByTypeAsync(AcceptorType.DepartmentDirectorAgree)" />
            </div>

            <div id="jorpor-section" class="flex items-center gap-2" v-if="detailStore.states.isJorPorSection">
              <ButtonSendEdit v-if="detailStore.states.isJorPorAssign || detailStore.states.isJorPorAssignByAssignee" @click="() => onAssigneeRejectAsync()" />

              <ButtonSave text="บันทึกผู้รับผิดชอบ" @click="updateAssignee()"
                v-if="detailStore.states.isJorPorAssign || detailStore.states.isJorPorAssignByAssignee" />

              <ButtonSave @click="updateAssignee()"
                v-if="detailStore.states.isJorPorComment" />

              <ButtonConfirmAssign v-if="detailStore.states.isJorPorAssignByAssignee"
                @click="() => handleSubmit(() => onAssignAsyncWithConfirm())" />

              <ButtonApproveConfirm v-if="detailStore.states.isJorPorComment"
                @click="() => handleSubmit(() => onJorPorSendApprovalAsync())" />
            </div>

            <ButtonRecall v-if="detailStore.states.isCanRecall" @click="() => onRecallAsync()" />

            <div id="acceptor-approve-section" class="flex items-center gap-2"
              v-if="detailStore.states.isAcceptorApproval">
              <ButtonSendEdit v-if="detailStore.states.isCurrentAcceptorApproval"
                @click="() => onRejectByTypeAsync()" />

              <ButtonConfirm
                v-if="detailStore.states.isCurrentAcceptorApproval && detailStore.states.isLastAcceptorApproval"
                @click="() => onApprovedByTypeAsync(AcceptorType.Approver, detailStore.states.isLastAcceptorApproval)" />

              <ButtonApprove
                v-if="detailStore.states.isCurrentAcceptorApproval && !detailStore.states.isLastAcceptorApproval"
                @click="() => onApprovedByTypeAsync(AcceptorType.Approver)" />
            </div>

            <div id="request-cancel-change-section" class="flex items-center gap-2"
              v-if="detailStore.states.isCancelOrChange">
              <ButtonSendChange @click="() => onRequestChangeOrCancelled()" v-if="detailStore.states.isCommittee" />
              <ButtonSendCancel @click="() => onRequestChangeOrCancelled(true)" v-if="detailStore.states.isCommittee" />
            </div>
          </div>

          <div v-if="detailStore.body.acceptors">
            <Accordion v-model:value="currentAccordion" unstyled multiple>

              <AccordionPanel value="Document" class="mb-4" v-if="detailStore.currentTab == 'document'">
                <AccordHeader label="Dictionary" />
                <AccordionContent>
                  <div></div>
                  <Card class="rounded-none overflow-auto h-[800px]">
                    <template #content>
                      <DocumentMapping pathToGet="procurement/median-price" @on-click-select="
                        (text, hint) => setPlaceholderInDocument(text, hint)
                      " />
                    </template>
                  </Card>
                </AccordionContent>
              </AccordionPanel>

              <AccordionPanel :value="MedianPriceAccordion.MedianPriceCommittee">
                <AccordAcceptor :title="AccordionName(MedianPriceAccordion.MedianPriceCommittee)"
                  v-model="detailStore.body.acceptors" :acceptor-type="AcceptorType.MedianPriceCommittee"
                  :is-disable="(!detailStore.states.isCommitteeApproval && !detailStore.states.isEditor) || !menuStore.hasManage || !detailStore.body.id || readonly"
                  @setIsUnableToPerformDuties="(e: boolean, acceptorId: string, remark?: string) => handleSubmit(() => onSetIsUnableToPerformDutiesByIdAsync(acceptorId, e, remark))"
                  is-approve />
              </AccordionPanel>

              <AccordionPanel class="mt-5" :value="MedianPriceAccordion.Units"
                v-if="procurementStore.procurementDetail.hasMd">
                <AccordAcceptor :title="AccordionName(MedianPriceAccordion.Units)" v-model="detailStore.body.acceptors"
                  :acceptor-type="AcceptorType.DepartmentDirectorAgree" isManage
                  :isSetDefault="detailStore.states.isCanSetDefaultUnit && menuStore.hasManage"
                  :is-disable="(!detailStore.states.isEditor) || !menuStore.hasManage || readonly"
                  @set-default="() => detailStore.setDefaultUnitAsync()" isApprove />
              </AccordionPanel>

              <AccordionPanel class="mt-5" :value="MedianPriceAccordion.JorPorSuggestion"
                v-if="procurementStore.procurementDetail.hasMd">
                <AccordAssignee v-if="detailStore.body.assignees"
                  :title="AccordionName(MedianPriceAccordion.JorPorSuggestion)" v-model="detailStore.body.assignees"
                  :isComment="detailStore.states.isJorPorComment" @on-comment="handleJorPorComment"
                  :disabled="(!detailStore.states.isJorPorAssign && !detailStore.states.isJorPorComment && !detailStore.states.isJorPorAssignByAssignee) || !menuStore.hasManage || readonly" />
              </AccordionPanel>

              <AccordionPanel class="mt-5" :value="MedianPriceAccordion.Acceptor">
                <AccordAcceptor :title="AccordionName(MedianPriceAccordion.Acceptor)"
                  v-model="detailStore.body.acceptors" :acceptor-type="AcceptorType.Approver" isManage
                  :isDisable="!detailStore.states.isMangeMd || !menuStore.hasManage || readonly"
                  :isSetDefault="detailStore.states.isCanSetDefaultApprover && menuStore.hasManage"
                  @set-default="() => detailStore.setDefaultAcceptorAsync()" isApprove />
              </AccordionPanel>
            </Accordion>
          </div>

        </div>
      </div>
    </div>
  </VeeForm>
  <DialogReviewDocument v-model="showReviewDocumentDialog" :docId="reviwDocumentId"
    :docName="`pp003-${new Date().toISOString()}-${reviwDocumentId}`" @on-click-use-document="setDocumentReviewId" />
</template>