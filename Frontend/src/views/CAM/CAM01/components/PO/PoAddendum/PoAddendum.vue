<script setup lang="ts">
import { Tabs, TabPanels, TabPanel } from 'primevue';
import { AccordAcceptor, AccordAssignee } from '@/components/Accordions';
import { TitleHeader, TabHeader } from '@/components/cosmetic';
import { BadgeStatus } from '@/components';
import { ButtonSave, ButtonSendApprove, ButtonRecall, ButtonNotAgree, ButtonApprove, ButtonConfirmAssign, ButtonApproveConfirm, ButtonSendEdit, ButtonConfirm } from '@/components/Button';
import { useCam01PoAddendumStore } from '@/stores/CAM/CAM01/PO/cam01.poAddendum';
import { useMenuStore } from '@/stores/menu';
import { defineAsyncComponent, onMounted, ref, computed } from 'vue';
import type { Option } from '@/models/shared/option';
import { storeToRefs } from 'pinia';
import { useCam01DetailStore } from '@/stores/CAM/CAM01/cam01.detail';
import { AcceptorType, AssigneeGroup } from '@/enums/participants';
import Cam01PoAddendumConstants from '@/constants/CAM/CAM01/cam01.poAddendum';
import { Form } from 'vee-validate';
import { showActivityDialog } from '@/helpers/dialog';
import ToastHelper from '@/helpers/toast';
import DocumentMapping from '@/components/DocumentMapping.vue';
import DialogReviewDocument from '@/components/Document/DialogReviewDocument.vue';
import ContractAddendumDocument from '@/components/Document/ChEditor.vue';
import ContractAmendmentRequestDocument from '@/components/Document/ChEditor.vue';
import { Cam01PoAddendumStatus } from '@/enums/CAM/CAM01/cam01.poAddendum';

const contractAddendumDocument = ref<InstanceType<typeof ContractAddendumDocument> | null>(null);
const contractAmendmentRequestDocument = ref<InstanceType<typeof ContractAmendmentRequestDocument> | null>(null);
const showReviewDocumentDialog = ref(false);
const reviwDocumentId = ref<string>('');

const menuStore = useMenuStore();
const amendmentStore = useCam01DetailStore();

const { Cam01AddendumBadgeStatus } = Cam01PoAddendumConstants;
const store = useCam01PoAddendumStore();
const { body } = storeToRefs(store);
const { isCanEdit, isCommitteeApproval, isCanReCall, isCanAssign, isCanComment, isCanApprover, isLastApprover, isCommitteeRecall, canRestoreContractAddendumDocument, canRestoreContractAmendmentRequestDocument } = storeToRefs(store);
const {
  onGetByIdAsync,
  onSubmitAsync,
  onSendCommitteeApprovalAsync,
  onRecallAsync,
  onUpdateDutiesStatusAsync,
  onCommitteeRejectedAsync,
  onCommitteeApprovedAsync,
  onConfirmAssignAsync,
  onSendApprovalAsync,
  onAssigneeCommentAsync,
  onRejectedAsync,
  onAcceptorApprovedAsync } = store;

const currentTab = ref("0");
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

const PoAddendumForm = defineAsyncComponent(
  (): Promise<typeof import('@/views/CAM/CAM01/components/PO/PoAddendum/Sub/PoAddendumForm.vue')> =>
    import('@/views/CAM/CAM01/components/PO/PoAddendum/Sub/PoAddendumForm.vue')
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
  if (amendmentStore.body.id) {
    await onGetByIdAsync(amendmentStore.body.id, amendmentStore.body.poAddendum?.id);
  }
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

const canEditDocument = computed(() => {
  return (
    store.body.status === Cam01PoAddendumStatus.Draft ||
    store.body.status === Cam01PoAddendumStatus.Rejected ||
    store.body.status === Cam01PoAddendumStatus.Edit
  );
});
</script>

<template>
  <TitleHeader label="ข้อมูลบันทึกต่อท้ายสัญญา">
    <template #action>
      <BadgeStatus :color="Cam01AddendumBadgeStatus(body.status).color"
        :label="Cam01AddendumBadgeStatus(body.status).label" v-if="body.status" />
      <Button v-if="body.id" label="ประวัติการใช้งาน" icon="pi pi-refresh" severity="warn"
        class="hover:bg-yellow-50 bg-white" variant="outlined" @click="() => showActivityDialog(body.id!)" />
    </template>
  </TitleHeader>

  <Form @submit="submitAsync" v-slot="{ handleSubmit }" @invalid-submit="ToastHelper.invalidMessageToast()">
    <div class="grid lg:grid-cols-7 gap-2">
      <div class="lg:col-span-5 order-2 lg:order-1">
        <Tabs :value="currentTab" @update:value="(tab) => currentTab = tab.toString()" unstyled>
          <TabHeader :items="filteredHeaderItems"
            class="sticky top-[59px] z-99 bg-[#F7F7F7]" />
          <TabPanels>
            <TabPanel value="0">
              <PoAddendumForm v-model="body" />
            </TabPanel>

            <TabPanel value="1">
              <ContractAddendumDocument :docId="store.body.contractAddendumDocumentId"
                :docName="`po-${store.body.contractAddendumDocumentId}-${new Date().toISOString()}`"
                :readonly="!menuStore.hasManage || !canEditDocument" ref="contractAddendumDocument" :save="saveDocument"
                v-if="store.body.contractAddendumDocumentId" :key="store.body.contractAddendumDocumentId"
                :versions="store.body.contractAddendumDocumentVersions ?? []"
                :canRestoreVersion="canRestoreContractAddendumDocument"
                @restore-version="() => store.resetDocumentAsync('ContractAddendum')" />
            </TabPanel>

            <TabPanel value="2">
              <ContractAmendmentRequestDocument :docId="store.body.contractAmendmentRequestDocumentId"
                :docName="`po-${store.body.contractAmendmentRequestDocumentId}-${new Date().toISOString()}`"
                :readonly="!menuStore.hasManage || !canEditDocument" ref="contractAmendmentRequestDocument" :save="saveDocument"
                v-if="store.body.contractAmendmentRequestDocumentId"
                :key="store.body.contractAmendmentRequestDocumentId"
                :versions="store.body.contractAmendmentRequestDocumentVersions ?? []"
                :canRestoreVersion="canRestoreContractAmendmentRequestDocument"
                @restore-version="() => store.resetDocumentAsync('ContractAmendmentRequest')" />
            </TabPanel>
          </TabPanels>
        </Tabs>
      </div>

      <div class="relative lg:col-span-2 order-1 lg:order-2">
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
              <ButtonSendEdit @click="onRejectedAsync()"  v-if="isCanApprover"/>
              <ButtonApprove v-if="!isLastApprover && isCanApprover" @click="onAcceptorApprovedAsync()" />
              <ButtonConfirm v-if="isLastApprover && isCanApprover" @click="onAcceptorApprovedAsync()" />
            </div>
          </div>

          <Accordion :value="['0', '1', '2']" unstyled multiple>
            <AccordionPanel value="999" class="mb-4" v-if="currentTab !== 'detail'">
              <AccordHeader label="Dictionary" />
              <AccordionContent>
                <div></div>
                <Card class="rounded-none overflow-auto h-[800px]">
                  <template #content>
                    <!-- Dictionary mapping for PO addendum document. -->
                    <DocumentMapping pathToGet="contract-amendment/contract-po-addendum" @on-click-select="onClickSelectMapping" />
                  </template>
                </Card>
              </AccordionContent>
            </AccordionPanel>
            <AccordionPanel value="0">
              <AccordAcceptor title="คณะกรรมการตรวจรับ" v-model="body.acceptors"
                :acceptor-type="AcceptorType.AcceptanceCommittee" :is-disable="!isCanEdit && !isCommitteeApproval" is-approve
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
    :docName="`po-${new Date().toISOString()}-${store.body.id?.toString()}`"
    @on-click-use-document="setDocumentReviewId" />
</template>