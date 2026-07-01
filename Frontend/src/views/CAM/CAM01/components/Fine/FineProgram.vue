<script setup lang="ts">
import { TitleHeader, TabHeader } from '@/components/cosmetic';
import { AccordAcceptor, AccordAssignee } from '@/components/Accordions';
import { ButtonSave, ButtonSendApprove, ButtonRecall, ButtonNotAgree, ButtonApprove, ButtonConfirmAssign, ButtonApproveConfirm, ButtonSendEdit, ButtonConfirm } from '@/components/Button';
import { AcceptorType, AssigneeGroup } from '@/enums/participants';
import Cam01FineConstants from '@/constants/CAM/CAM01/cam01.fine';
import type { Option } from '@/models/shared/option';
import { useCam01DetailStore } from '@/stores/CAM/CAM01/cam01.detail';
import { useMenuStore } from '@/stores/menu';
import { Form } from 'vee-validate';
import { defineAsyncComponent, onMounted, ref, computed } from 'vue';
import { useCam01FineStore } from '@/stores/CAM/CAM01/Fine/cam01.fine';
import { storeToRefs } from 'pinia';
import { showActivityDialog } from '@/helpers/dialog';
import ToastHelper from '@/helpers/toast';
import DocumentMapping from '@/components/DocumentMapping.vue';
import DialogReviewDocument from '@/components/Document/DialogReviewDocument.vue';
import ContractAddendumDocument from '@/components/Document/ChEditor.vue';
import ContractAmendmentRequestDocument from '@/components/Document/ChEditor.vue';
import { Cam01FineStatus } from '@/enums/CAM/CAM01/cam01.fine';

const contractAddendumDocument = ref<InstanceType<typeof ContractAddendumDocument> | null>(null);
const contractAmendmentRequestDocument = ref<InstanceType<typeof ContractAmendmentRequestDocument> | null>(null);
const showReviewDocumentDialog = ref(false);
const reviwDocumentId = ref<string>('');


const menuStore = useMenuStore();
const amendmentStore = useCam01DetailStore();

const { Cam01FineBadgeStatus } = Cam01FineConstants;
const store = useCam01FineStore();
const { body } = storeToRefs(store);
const { isCanEdit, isCommitteeApproval, isCanReCall, isCanAssign, isCanComment, isCanApprover, isLastApprover, isCommitteeRecall, canRestoreContractAddendumDocument, canRestoreContractAmendmentRequestDocument } = storeToRefs(store);
const {
  onGetDropdownAsync,
  onGetByIdAsync,
  onSendCommitteeApprovalAsync,
  onUpdateDutiesStatusAsync,
  onSubmitAsync,
  onRecallAsync,
  onCommitteeRejectedAsync,
  onCommitteeApprovedAsync,
  onConfirmAssignAsync,
  onRejectedAsync,
  onSendApprovalAsync,
  onAcceptorApprovedAsync,
  onAssigneeCommentAsync,
} = store;

const currentTab = ref('0');
const headerItems = ref<Array<Option>>([
  {
    label: 'รายละเอียดแก้ไขใบสั่ง/สัญญา',
    value: "0",
  },
  {
    label: 'เอกสารบันทึกต่อท้ายสัญญา',
    value: "1",
  },
  {
    label: 'เอกสารขออนุมัติแก้ไขใบสั่ง/สัญญา',
    value: "2",
  },
]);

const filteredHeaderItems = computed<Array<Option>>((): Array<Option> => {
  return body.value.id
    ? headerItems.value
    : headerItems.value.filter((_: Option, idx: number): boolean => idx === 0);
});

// Save document to CHEditor first, then call API
const saveDocumentFirst = (): Promise<void> => {
  return new Promise((resolve) => {
    // Only save if document is editable (not read-only)
    if (!canEditDocument.value || !menuStore.hasManage) {
      resolve();
      return;
    }

    if (currentTab.value === '1' && contractAddendumDocument.value?.saveAndWait) {
      contractAddendumDocument.value.saveAndWait(() => {
        resolve();
      });
    } else if (currentTab.value === '2' && contractAmendmentRequestDocument.value?.saveAndWait) {
      contractAmendmentRequestDocument.value.saveAndWait(() => {
        resolve();
      });
    } else {
      resolve();
    }
  });
};

const saveDocAndSetFlag = async () => {
  if (currentTab.value === '1'
    && store.body.contractAddendumDocumentId
    && !store.body.isContractAddendumDocumentIdReplaced) {
    store.body.isContractAddendumDocumentIdReplaced = true;
  } else if (currentTab.value === '2'
    && store.body.contractAmendmentRequestDocumentId
    && !store.body.isContractAmendmentRequestDocumentIdReplaced) {
    store.body.isContractAmendmentRequestDocumentIdReplaced = true;
  }
};

const onSendCommitteeApprovalWithDocSave = async () => {
  await saveDocumentFirst();
  await saveDocAndSetFlag();
  await onSendCommitteeApprovalAsync();
};

const saveDocument = async () => {
  await submitAsync();
};

const submitAsync = async (): Promise<void> => {
  // Save current document to Collabora first
  if ((currentTab.value === '1' && store.body.contractAddendumDocumentId)
    || (currentTab.value === '2' && store.body.contractAmendmentRequestDocumentId)) {
    await saveDocumentFirst();
  }

  if (currentTab.value === '1'
    && store.body.contractAddendumDocumentId
    && !store.body.isContractAddendumDocumentIdReplaced) {
    store.body.isContractAddendumDocumentIdReplaced = true;
  } else if (currentTab.value === '2'
    && store.body.contractAmendmentRequestDocumentId
    && !store.body.isContractAmendmentRequestDocumentIdReplaced) {
    store.body.isContractAmendmentRequestDocumentIdReplaced = true;
  }

  await onSubmitAsync();

  if(store.body.contractAddendumDocumentId && !store.body.isContractAddendumDocumentIdReplaced)
  {
    currentTab.value = '1';
  }else if(store.body.contractAmendmentRequestDocumentId && !store.body.isContractAmendmentRequestDocumentIdReplaced)
  {
     currentTab.value = '2';
  }
};


const setDocumentReviewId = (id: string): void => {

  if (currentTab.value === '1') {
    store.body.contractAddendumDocumentId = id;
    store.body.isContractAddendumDocumentIdReplaced = true;
  }

  if (currentTab.value === '2') {
    store.body.contractAmendmentRequestDocumentId = id;
    store.body.isContractAmendmentRequestDocumentIdReplaced = true;
  }
};

const FineForm = defineAsyncComponent(
  (): Promise<typeof import('@/views/CAM/CAM01/components/Fine/Sub/FineForm.vue')> =>
    import('@/views/CAM/CAM01/components/Fine/Sub/FineForm.vue')
);

const onClickSelectMapping = (text: string, hint?: string): void => {
  if (currentTab.value === '1') {
    contractAddendumDocument.value?.setPlaceholderInDocument(text, hint);
    return;
  }
  if (currentTab.value === '2') {
    contractAmendmentRequestDocument.value?.setPlaceholderInDocument(text, hint);
  }
};

onMounted(async (): Promise<void> => {
  await onGetDropdownAsync();

  if (amendmentStore.body.id) {
    await onGetByIdAsync(amendmentStore.body.id, amendmentStore.body.waiveOrReducePenalty?.id);
  }
});

const canEditDocument = computed(() => {
  return (
    store.body.status === Cam01FineStatus.Draft ||
    store.body.status === Cam01FineStatus.Rejected ||
    store.body.status === Cam01FineStatus.Edit
  );
});
</script>

<template>
  <TitleHeader label="ข้อมูลบันทึกต่อท้ายสัญญา">
    <template #action>
      <BadgeStatus :color="Cam01FineBadgeStatus(body.status).color" :label="Cam01FineBadgeStatus(body.status).label"
        v-if="body.status" />
      <Button v-if="body.id" label="ประวัติการใช้งาน" icon="pi pi-refresh" severity="warn"
        class="hover:bg-yellow-50 bg-white" variant="outlined" @click="() => showActivityDialog(body.id!)" />
    </template>
  </TitleHeader>
  <Form @submit="submitAsync" @invalid-submit="ToastHelper.invalidMessageToast()" v-slot="{ handleSubmit }">
    <div class="grid lg:grid-cols-7 gap-2">
      <div class="lg:col-span-5 lg:order-1 order-2">
        <Tabs v-model:value="currentTab" unstyled>
          <TabHeader :items="filteredHeaderItems"
            class="sticky top-[55px] z-3 bg-[#F7F7F7] pt-2" />
          <TabPanels>
            <TabPanel value="0">
              <FineForm v-model="body" />
            </TabPanel>

            <TabPanel value="1">
              <ContractAddendumDocument :docId="store.body.contractAddendumDocumentId"
                :docName="`po-${store.body.contractAddendumDocumentId}-${new Date().toISOString()}`"
                :readonly="!menuStore.hasManage || !canEditDocument" ref="contractAddendumDocument" :save="saveDocument"
                v-if="store.body.contractAddendumDocumentId" :key="store.body.contractAddendumDocumentId"
                :versions="store.body.contractAddendumDocumentVersions ?? []"
                :canRestoreVersion="canRestoreContractAddendumDocument"
                @restore-version="() => store.resetDocumentAsync('WaiveOrReducePenalty')" />
            </TabPanel>

            <TabPanel value="2">
              <ContractAmendmentRequestDocument :docId="store.body.contractAmendmentRequestDocumentId"
                :docName="`po-${store.body.contractAmendmentRequestDocumentId}-${new Date().toISOString()}`"
                :readonly="!menuStore.hasManage || !canEditDocument" ref="contractAmendmentRequestDocument" :save="saveDocument"
                v-if="store.body.contractAmendmentRequestDocumentId"
                :key="store.body.contractAmendmentRequestDocumentId"
                :versions="store.body.contractAmendmentRequestDocumentVersions ?? []"
                :canRestoreVersion="canRestoreContractAmendmentRequestDocument"
                @restore-version="() => store.resetDocumentAsync('Approved')" />
            </TabPanel>
          </TabPanels>
        </Tabs>
      </div>

      <div class="relative lg:col-span-2 lg:order-2 order-1">
        <div class="flex flex-col gap-4 ml-3 sticky top-[100px]">
          <div id="button-section" class="flex items-center justify-end gap-2" v-if="menuStore.hasManage">

            <div id="edit-section" class="flex items-center gap-2" v-if="isCanEdit">
              <ButtonSave type="submit" />
              <ButtonSendApprove @click="handleSubmit(onSendCommitteeApprovalWithDocSave)" v-if="body.id" />
            </div>

            <div id="commitee-approval-section" class="flex items-center gap-2">
              <ButtonRecall v-if="isCommitteeRecall" @click="onRecallAsync" />
              <ButtonNotAgree v-if="isCommitteeApproval" @click="onCommitteeRejectedAsync" />
              <ButtonApprove v-if="isCommitteeApproval" @click="onCommitteeApprovedAsync" />
            </div>

            <div id="assignee-section" class="flex items-center gap-2" v-if="isCanAssign">
              <ButtonSave type="submit" />
              <ButtonConfirmAssign @click="handleSubmit(onConfirmAssignAsync)" />
            </div>

            <div id="assignee-comment-section" class="flex items-center gap-2" v-if="isCanComment">
              <ButtonSendEdit @click="onRejectedAsync()" />
              <ButtonSave type="submit" />
              <ButtonApproveConfirm @click="handleSubmit(onSendApprovalAsync)" />
            </div>

            <div id="approver-section" class="flex items-center gap-2">
              <ButtonRecall v-if="isCanReCall" @click="onRecallAsync" />
              <ButtonSendEdit @click="onRejectedAsync()" v-if="isCanApprover" />
              <ButtonApprove v-if="!isLastApprover && isCanApprover" @click="onAcceptorApprovedAsync()" />
              <ButtonConfirm v-if="isLastApprover && isCanApprover" @click="onAcceptorApprovedAsync()" />
            </div>
          </div>
          <Accordion :value="['0', '1', '2']" unstyled multiple>
            <AccordionPanel value="999" class="mb-4" v-if="currentTab !== '0'">
              <AccordHeader label="Dictionary" />
              <AccordionContent>
                <div></div>
                <Card class="rounded-none overflow-auto h-[800px]">
                  <template #content>
                    <!-- Dictionary mapping for waive-or-reduce-penalty document. -->
                    <DocumentMapping pathToGet="contract-amendment/waive-or-reduce-penalty" @on-click-select="onClickSelectMapping" />
                  </template>
                </Card>
              </AccordionContent>
            </AccordionPanel>

            <AccordionPanel value="0">
              <AccordAcceptor title="คณะกรรมการตรวจรับ" v-model="body.acceptors"
                :acceptor-type="AcceptorType.AcceptanceCommittee" :is-disable="!isCanEdit && !isCommitteeApproval"
                @set-is-unable-to-perform-duties="(isUnableToPerformDuties: boolean, id: string, remark?: string) => handleSubmit(() => onUpdateDutiesStatusAsync(isUnableToPerformDuties, id, remark))" />
            </AccordionPanel>

            <AccordionPanel value="1" class="mt-5">
              <AccordAssignee title="เจ้าหน้าที่พัสดุให้ความเห็น" v-model="body.assignees"
                @on-comment="(v) => onAssigneeCommentAsync(v.reason)" :group="AssigneeGroup.Contract"
                :disabled="!isCanAssign && !isCanComment" :is-comment="isCanComment" />
            </AccordionPanel>

            <AccordionPanel value="2" class="my-5">
              <AccordAcceptor title="ผู้มีอำนาจเห็นชอบ/อนุมัติ" v-model="body.acceptors" is-approve
                :acceptor-type="AcceptorType.Approver" :is-manage="isCanAssign || isCanComment" />
            </AccordionPanel>
          </Accordion>

        </div>
      </div>
    </div>
  </Form>

  <DialogReviewDocument v-if="!!store.body.id" v-model="showReviewDocumentDialog" :docId="reviwDocumentId"
    :docName="`waive-or-reduce-${new Date().toISOString()}-${store.body.id?.toString()}`"
    @on-click-use-document="setDocumentReviewId" />
</template>