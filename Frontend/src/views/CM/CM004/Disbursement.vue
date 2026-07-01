<script setup lang="ts">
import type { MenuItem } from 'primevue/menuitem';
import type { Option } from '@/models/shared/option';
import { TabHeader, TitleHeader } from '@/components/cosmetic';
import { useRoute } from 'vue-router';
import { BadgeStatus } from '@/components';
import {
  ButtonSave,
  ButtonConfirmAssign,
  ButtonSendEdit,
  ButtonApproveConfirm,
  ButtonConfirm,
  ButtonApprove,
} from '@/components/Button';
import { AccordAcceptor, AccordAssignee } from '@/components/Accordions';
import { AcceptorType, AssigneeType } from '@/enums/participants';
import { computed, defineAsyncComponent, onMounted, onUnmounted, ref, watch } from 'vue';
import { Form } from 'vee-validate';
import { useCm004DetailStore } from '@/stores/CM/cm004';
import { CmDisbursementApprovalStatus } from '@/enums/CM/cm004';
import { showActivityDialog, showConfirmDialogAsync } from '@/helpers/dialog';
import { ConfirmDialogType } from '@/enums/dialog';
import { AssignDepartmentCodeEnum } from '@/enums/shared';
import { useMenuStore } from '@/stores/menu';
import ChEditor from '@/components/Document/ChEditor.vue';
import cm004Constant from '@/constants/CM/cm004';
import ToastHelper from '@/helpers/toast';
import DocumentMapping from '@/components/DocumentMapping.vue';
import DialogReviewDocument from '@/components/Document/DialogReviewDocument.vue';
import { UploadFileGroup } from '@/components/forms';

interface Validate {
  check: boolean;
  reason: string;
}

const DisbursementDetail = defineAsyncComponent(() => import('./components/DisbursementDetail.vue'));

const menuStore = useMenuStore();
const store = useCm004DetailStore();
const route = useRoute();
const { cm004StatusColor, cm004StatusName } = cm004Constant;
const id = route.params?.id as string;
const disbursementId = computed(() => route.params?.disbursementId as string);
const routeItems: MenuItem[] = [
  { label: 'รายการขออนุมัติเบิกจ่าย', url: '/cm/cm004' },
  { label: 'รายละเอียดการขออนุมัติเบิกจ่าย', url: `/cm/cm004/detail/${id}` },
  { label: 'รายละเอียดการขออนุมัติเบิกจ่าย' },
];
const HeaderItem = ([
  {
    label: 'รายละเอียด',
    value: '0',
  },
  {
    label: 'เอกสารขออนุมัติเบิกจ่าย',
    value: '1',
  },
] as Option[]);

const currentTab = ref('0');
const showReviewDocumentDialog = ref(false);
const reviwDocumentId = ref<string>('');

const docRef = ref<InstanceType<typeof ChEditor> | null>(null);

onMounted(async () => {
  initAsync();
  await store.getAssignDepartmentDDLAsync();
});

onUnmounted(() => {
  store.onClearDisbursementData();
});

const initAsync = async (): Promise<void> => {
  if (store.body.assignees && store.body.assignees.length === 0) {
    await store.getDefaultJorporAsync();
  }

  if (disbursementId.value && id) {
    await store.getDetailDisbursementAsync(id, disbursementId.value);
  }
};

const validateData = (conditionList?: Validate[]): { isCancel: boolean; reason: string } => {
  let conditions = [
    {
      check: store.body.acceptors.some(a => a.acceptorType === AcceptorType.Approver),
      reason: 'กรุณาเพิ่มผู้มีอำนาจเห็นชอบ/อนุมัติ',
    },
  ];

  if (conditionList && conditionList.length > 0) {
    conditions = [...conditions, ...conditionList];
  }

  const failed = conditions.find(c => !c.check);

  return failed ? { isCancel: true, reason: failed.reason } : { isCancel: false, reason: '' };
};

// Save document to CHEditor first, then call API
const saveDocumentFirst = (): Promise<void> => {
  return new Promise((resolve) => {
    // Only save if document is editable (not read-only)
    if (docRef.value?.saveAndWait && canEditDocument.value) {
      docRef.value.saveAndWait(() => {
        resolve();
      });
    } else {
      resolve();
    }
  });
};

const saveDocument = async () => {
  await onSubmitAsync();
};

const onSubmitAsync = async (): Promise<void> => {
  if (docRef.value && store.body.documentId) {
    await saveDocumentFirst();
  }

  if (store.body.documentId && !store.body.isDocumentReplaced) {
    store.body.isDocumentReplaced = true;
  }

  if (disbursementId.value) {
    await store.updateDisbursementAsync(id);
    setCurrentTab();
    return
  }

  await store.createDisbursementAsync(id);
  setCurrentTab();
};

const setCurrentTab = async () => {
  if (store.body.documentId && !store.body.isDocumentReplaced) {
    currentTab.value = '1';
  }
};

const saveDocAndSetFlag = async () => {
  await saveDocumentFirst();

  if (store.body.documentId && !store.body.isDocumentReplaced) {
    store.body.isDocumentReplaced = true;
  }
};

const onConfirmAssignAsync = async (): Promise<void> => {
  if (!store.body.assignees.some(a => a.assigneeType == AssigneeType.Assignee)) {
    return ToastHelper.assignAtLeastMessageToast();
  }

  if (!await showConfirmDialogAsync(ConfirmDialogType.Assigned)) return;

  await saveDocAndSetFlag();

  if (disbursementId.value) {
    await store.updateDisbursementAsync(id, CmDisbursementApprovalStatus.Assigned);

    return
  }

  await store.createDisbursementAsync(id, CmDisbursementApprovalStatus.Assigned);
};

const onSendApproveAsync = async (): Promise<void> => {
  if (store.body.acceptors.length === 0) {
    return ToastHelper.approvalAtLeastMessageToast();
  }

  const conditionList = [
    {
      check: store.body.installments && store.body.installments.length > 0,
      reason: 'กรุณาเพิ่มรายการขออนุมัติเบิกจ่าย',
    }
  ] as Validate[];

  const validate = validateData(conditionList);

  if (validate.isCancel) {
    return ToastHelper.warning('ข้อมูลไม่ถูกต้อง', validate.reason);
  }

  if (!await showConfirmDialogAsync(ConfirmDialogType.SendApproveConfirm)) return;

  await saveDocAndSetFlag();

  await store.updateDisbursementAsync(id, CmDisbursementApprovalStatus.WaitingApproval);
};

const onTabChange = (tab: string): void => {
  currentTab.value = tab;
};


const setDocumentReviewId = (id: string): void => {

  store.body.documentId = id;
  // Note: replacement flag may need to be added to PL002 model
  store.body.isDocumentReplaced = true;
};

const canEditDocument = computed(() => {
  return store.body.status == CmDisbursementApprovalStatus.Draft
    || store.body.status == CmDisbursementApprovalStatus.Rejected
});

const onAssignSegmentApproverAsync = async (assignSegmentCode: string) => {
  switch (assignSegmentCode) {
    case AssignDepartmentCodeEnum.SegmentJorPorOther:
      await store.getDefaultSegmentOtherManagerApproverAsync();

      await store.getDefaultApproverAsync();
      break;
    case AssignDepartmentCodeEnum.SegmentJorPorIT:
      await store.getDefaultSegmentITManagerApproverAsync();

      await store.getDefaultApproverAsync();
      break;
    default:
      break;
  }
};

watch(() => store.body.assignees, async () => {
  if (store.body.acceptors.some(a => a.id)) {
    return
  }

  await store.getDefaultApproverAsync();
}, { deep: true });
</script>

<template>
  <TitleHeader label="ขออนุมัติเบิกจ่าย" :route-items="routeItems">
    <template #action>
      <BadgeStatus :label="cm004StatusName(store.body.status)" :color="cm004StatusColor(store.body.status)?.color"
        v-if="store.body.status" />
      <Button label="ประวัติการใช้งาน" icon="pi pi-refresh" variant="outlined" severity="primary" v-if="store.body.id"
        class="bg-white! hover:bg-red-50!" @click="() => showActivityDialog(store.body.id!)" />
    </template>
  </TitleHeader>
  <Form @submit="onSubmitAsync" @invalidSubmit="ToastHelper.invalidMessageToast()" v-slot="{ handleSubmit }">
    <div class="grid grid-cols-1 lg:grid-cols-7 gap-2">
      <div class="lg:col-span-5 ">
        <Tabs :value="currentTab" unstyled @update:value="(tab) => onTabChange(tab.toString())">
          <TabHeader :items="HeaderItem.filter((h, i) => store.body.id ? h : i === 0)"
            class="sticky top-[57px] z-99 bg-[#F7F7F7]" />
          <TabPanels>
            <TabPanel value="0">
              <DisbursementDetail />
            </TabPanel>
            <TabPanel value="1">
              <div>
                <ChEditor :docId="store.body.documentId" :docName="new Date().toISOString()"
                  :readonly="!menuStore.hasManage || !canEditDocument" ref="docRef" v-if="store.body.documentId"
                  :key="store.body.documentId" :save="saveDocument"
                  :versions="store.body.documentVersions ?? []"
                  :canRestoreVersion="store.status.canRestoreVersion"
                  @restore-version="() => store.resetDocumentAsync(id, disbursementId)" />
              </div>
            </TabPanel>
            <div class="mt-4">
              <UploadFileGroup v-if="!store.body.id" v-model="store.body.attachments"
                :disabled="!menuStore.hasManage" />
              <UploadFileGroup v-if="store.body.id" v-model="store.body.attachments"
                @upload="() => store.onUpsertAttachments(id, disbursementId)"
                @remove-file="() => store.onUpsertAttachments(id, disbursementId)"
                @remove-group="() => store.onUpsertAttachments(id, disbursementId)"
                @reorder="() => store.onUpsertAttachments(id, disbursementId)" :disabled="!menuStore.hasManage" />
            </div>
          </TabPanels>
        </Tabs>
      </div>
      <div class="lg:col-span-2">
        <div class="flex flex-col gap-4 ml-3 sticky top-[100px]">
          <div class="flex items-center justify-end">
            <div class="draft flex gap-2 items-center">
              <ButtonSave type="submit" v-if="store.status.canEdit" />
              <ButtonConfirmAssign v-if="store.status.canAssign" @click="() => handleSubmit(onConfirmAssignAsync)" />
              <ButtonApproveConfirm v-if="store.status.canSendApprove"
                @click="() => handleSubmit(onSendApproveAsync)" />
            </div>
            <div class="approve flex gap-2 items-center">
              <ButtonSendEdit @click="() => store.onRejectAsync(id, disbursementId)" v-if="store.status.canApprove" />
              <ButtonApprove @click="() => store.onApproveAsync(id, disbursementId)"
                v-if="store.status.canApprove && !store.status.isLastApprovalApprover" />
              <ButtonConfirm @click="() => store.onApproveAsync(id, disbursementId)"
                v-if="store.status.canApprove && store.status.isLastApprovalApprover" />
            </div>
          </div>
          <Accordion :value="['0', '1']" unstyled multiple>
            <AccordionPanel value="999" class="mb-4" v-if="currentTab !== '0'">
              <AccordHeader label="Dictionary" />
              <AccordionContent>
                <Card class="rounded-none overflow-auto h-[800px]">
                  <template #content>
                    <DocumentMapping pathToGet="contract/disbursement-approval" @on-click-select="
                      (text, hint) => docRef?.setPlaceholderInDocument(text, hint)
                    " />
                  </template>
                </Card>
              </AccordionContent>
            </AccordionPanel>
            <AccordionPanel value="0" class="mt-4">
              <AccordAssignee v-model="store.body.assignees" title="มอบหมายผู้รับผิดชอบ"
                :disabled="!store.status.canAssign" :isDropdown="store.status.canAssign"
                :dropdown="store.assignDepartmentDDL" v-if="store.body.assignees"
                v-on:change="onAssignSegmentApproverAsync" />
            </AccordionPanel>
            <AccordionPanel value="1" class="mt-4">
              <AccordAcceptor v-model="store.body.acceptors" title="ผู้มีอำนาจเห็นชอบ/อนุมัติ"
                @set-default="store.getDefaultApproverAsync" :acceptor-type="AcceptorType.Approver" is-manage
                :is-disable="!store.status.canEdit"
                v-if="store.body.acceptors && store.body.assignees.filter(x => x.assigneeType === AssigneeType.Assignee).length > 0"
                is-approve :is-set-default="(store.status.canApprove || store.status.canEdit) || menuStore.hasManage" />
            </AccordionPanel>
          </Accordion>
        </div>
      </div>
    </div>
  </Form>
  <DialogReviewDocument v-if="!!id" v-model="showReviewDocumentDialog" :docId="reviwDocumentId"
    :docName="`plan-${new Date().toISOString()}-${id.toString()}`" @on-click-use-document="setDocumentReviewId" />
</template>
