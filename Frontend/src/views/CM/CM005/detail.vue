<script setup lang="ts">
import type { MenuItem } from 'primevue/menuitem';
import type { Option } from '@/models/shared/option';
import { InfoItem, TabHeader, TitleHeader } from '@/components/cosmetic';
import { Form } from 'vee-validate';
import { computed, h, nextTick, onBeforeMount, ref, watch } from 'vue';
import Card from 'primevue/card';
import {
  ButtonConfirm,
  ButtonApproveConfirm,
  ButtonSave,
  ButtonSendApprove,
  ButtonApprove,
  ButtonSendEdit,
  ButtonConfirmAssign,
} from '@/components/Button';
import { AccordAcceptor, AccordAssignee } from '@/components/Accordions';
import { AcceptorType, AssigneeType } from '@/enums/participants';
import { Datepicker, InputArea } from '@/components/forms';
import { useCm005DetailStore } from '@/stores/CM/cm005';
import { formatCurrency } from '@/helpers/currency';
import { useRoute } from 'vue-router';
import { showActivityDialog, showConfirmDialogAsync, showSaveOptionDialogAsync } from '@/helpers/dialog';
import { ConfirmDialogType } from '@/enums/dialog';
import { CmContractTerminationStatus } from '@/enums/CM/cm005';
import { useMenuStore } from '@/stores/menu';
import { useLoadingStore } from '@/stores/loading';
import ToastHelper from '@/helpers/toast';
import BadgeStatus from '@/components/BadgeStatus.vue';
import ChEditor from '@/components/Document/ChEditor.vue';
import cm005Constant from '@/constants/CM/cm005';
import DocumentMapping from '@/components/DocumentMapping.vue';
import DialogReviewDocument from '@/components/Document/DialogReviewDocument.vue';
import ButtonRecall from '@/components/Button/ButtonRecall.vue';

interface Validate {
  check: boolean;
  reason: string;
}

const { cm005StatusColor, cm005StatusName } = cm005Constant;
const menuStore = useMenuStore();
const loadingStore = useLoadingStore();
const store = useCm005DetailStore();
const route = useRoute();
const contractId = route.params?.contractId as string;
const id = route.params?.id as string;
const isLoaded = ref(!(contractId && id));
const currentTab = ref('0');
const currentAccordion = ref<Array<string>>([]);
const HeaderItem = computed<Option[]>(() => {
  const items: Option[] = [
    { label: 'รายละเอียด', value: '0' },
  ];
  if (store.body.contractTermination.isProposedApprover) {
    items.push({ label: 'เอกสารขออนุมัติบอกเลิกสัญญา', value: '1' });
  } else if (currentTab.value === '1') {
    // eslint-disable-next-line vue/no-side-effects-in-computed-properties
    currentTab.value = '0';
  }
  return items;
});
const routeItems: MenuItem[] = [
  { label: 'รายการขออนุมัติยกเลิกสัญญา', url: '/cm/cm005' },
  { label: 'รายละเอียดขออนุมัติยกเลิกสัญญา' },
];

const requestApproveDocRef = ref<InstanceType<typeof ChEditor> | null>(null);
const showReviewDocumentDialog = ref(false);
const reviwDocumentId = ref<string>('');
const isFormDirty = ref(false);
const isInitialLoad = ref(true);

const ContractInfoCard = () => h(Card, null, {
  content: () => [
    h(TitleHeader, { label: 'ข้อมูลสัญญา' }),
    h('div', { class: 'grid grid-cols-12 gap-y-4 px-8' }, [
      h(InfoItem, { title: 'คู่ค้า', content: store.body.entrepreneurName, class: 'col-span-4' }),
      h(InfoItem, { title: 'Email', content: store.body.entrepreneurEmail, class: 'col-span-8' }),
      h(InfoItem, { title: 'เลขที่สัญญา', content: store.body.contractNumber, class: 'col-span-4' }),
      h(InfoItem, { title: 'เลขที่ PO (SAP)', content: store.body.poNumber, class: 'col-span-4' }),
      h(InfoItem, { title: 'วงเงินตามสัญญา', content: formatCurrency(store.body.budget), class: 'col-span-4' }),
      h(InfoItem, { title: 'ชื่อสัญญา', content: store.body.contractName, class: 'col-span-4' }),
      h(InfoItem, { title: 'ประเภทสัญญา', content: store.body.contractType, class: 'col-span-4' }),
      h(InfoItem, { title: 'รูปแบบสัญญา', content: store.body.contractTemplate, class: 'col-span-4' }),
    ]),
  ],
});

onBeforeMount(() => {
  initAsync();
})

const skipTabSwitch = [CmContractTerminationStatus.Draft, CmContractTerminationStatus.Rejected];

const initAsync = async (): Promise<void> => {
  await store.getCTRDrowndownAsync();

  if (contractId && id) {
    await store.getDetailAsync(contractId, id);

    if (!skipTabSwitch.includes(store.body.contractTermination.status)) {
      currentTab.value = '1';
    }
  }

  isLoaded.value = true;
  isInitialLoad.value = false;
};

const validateData = (conditionList?: Validate[]): { isCancel: boolean; reason: string } => {
  let conditions = [
    {
      check: store.body.contractTermination.acceptors.some(a => a.acceptorType === AcceptorType.AcceptanceCommittee),
      reason: 'กรุณาเพิ่มคณะกรรมการตรวจรับ',
    },
  ];

  if (conditionList && conditionList.length > 0) {
    conditions = [...conditions, ...conditionList];
  }

  const failed = conditions.find(c => !c.check);

  return failed ? { isCancel: true, reason: failed.reason } : { isCancel: false, reason: '' };
};

const saveDocumentFirst = (): Promise<void> => {
  return new Promise((resolve) => {
    const canSaveDoc = (store.status.canEdit || store.status.canEditAsAssignee) && menuStore.hasManage;
    if (requestApproveDocRef.value?.saveAndWait && canSaveDoc) {
      requestApproveDocRef.value.saveAndWait(() => {
        resolve();
      });
    } else {
      resolve();
    }
  });
};

const onSubmitAsync = async (): Promise<void> => {
  if (isFormDirty.value
    && store.body.contractTermination.contractTerminationDocumentId
    && id) {
    const saveOption = await showSaveOptionDialogAsync();
    if (saveOption === undefined) return;
    store.body.contractTermination.isContractTerminationDocumentIdReplace = saveOption;
  }

  loadingStore.setIsLoading(true);
  await saveDocumentFirst();
  await store.updateAsync(contractId, id);

  await nextTick();
  isFormDirty.value = false;
  loadingStore.setIsLoading(false);

  if (!skipTabSwitch.includes(store.body.contractTermination.status)) {
    currentTab.value = '1';
  }
};

const onSaveAssigneeAsync = async (): Promise<void> => {
  loadingStore.setIsLoading(true);
  await saveDocumentFirst();
  await store.updateAsync(contractId, id);
  await nextTick();
  isFormDirty.value = false;
  loadingStore.setIsLoading(false);

  if (!skipTabSwitch.includes(store.body.contractTermination.status)) {
    currentTab.value = '1';
  }
};

const onSendCommitteeApproveAsync = async (): Promise<void> => {
  if (!await showConfirmDialogAsync(ConfirmDialogType.SendApprove)) return;

  const validate = validateData();

  if (validate.isCancel) {
    return ToastHelper.warning('ข้อมูลไม่ถูกต้อง', validate.reason);
  }

  loadingStore.setIsLoading(true);
  await saveDocumentFirst();
  await store.updateAsync(contractId, id, CmContractTerminationStatus.WaitingCommitteeApproval);
  loadingStore.setIsLoading(false);
};

const onSendApproveAsync = async (): Promise<void> => {
  if (!store.body.contractTermination.assignees.some(a => a.remark)) {
    return ToastHelper.assignneeCommentAtLeastMessageToast();
  }

  if (!store.body.contractTermination.acceptors.some(s => s.acceptorType === AcceptorType.Approver)) {
    return ToastHelper.approvalAtLeastMessageToast();
  }

  if (!await showConfirmDialogAsync(ConfirmDialogType.SendApproveConfirm)) return;

  await saveDocumentFirst();
  store.updateAsync(contractId, id, CmContractTerminationStatus.WaitingApproval);
};

const onConfirmAssignAsync = async (): Promise<void> => {
  if (!store.body.contractTermination.assignees.some(s => s.assigneeType === AssigneeType.Assignee)) {
    return ToastHelper.assignAtLeastMessageToast();
  }

  if (!await showConfirmDialogAsync(ConfirmDialogType.Assigned)) return;

  store.updateAsync(contractId, id, CmContractTerminationStatus.WaitingComment);
};

const onTabChange = (tab: string): void => {
  currentTab.value = tab;
};

const setDocumentReviewId = (id: string): void => {
  showReviewDocumentDialog.value = false;

  store.body.contractTermination.contractTerminationDocumentId = id;
  // Note: replacement flag may need to be added to PL002 model
  store.body.contractTermination.isContractTerminationDocumentIdReplace = true;
};

const onRecallAsync = async (): Promise<void> => {
  if (!await showConfirmDialogAsync(ConfirmDialogType.Edit)) return;

  await store.updateAsync(contractId, id,
    store.body.contractTermination.status === CmContractTerminationStatus.WaitingCommitteeApproval
      ? CmContractTerminationStatus.Draft
      : CmContractTerminationStatus.WaitingComment);
};

const accordionMapper: Record<CmContractTerminationStatus, string[]> = {
  [CmContractTerminationStatus.All]: [],
  [CmContractTerminationStatus.Draft]: ['0'],
  [CmContractTerminationStatus.WaitingCommitteeApproval]: ['0'],
  [CmContractTerminationStatus.WaitingAssign]: ['1'],
  [CmContractTerminationStatus.WaitingComment]: ['1', '2'],
  [CmContractTerminationStatus.WaitingApproval]: ['2'],
  [CmContractTerminationStatus.Approved]: ['0', '1', '2'],
  [CmContractTerminationStatus.RejectToAssignee]: ['1'],
  [CmContractTerminationStatus.Rejected]: ['0']
}

watch(() => store.body.contractTermination.status, (val: CmContractTerminationStatus) => {
  currentAccordion.value = accordionMapper[val];
}, { immediate: true });

watch(() => store.body.contractTermination.terminateType, (val) => {
  if (val != 'CTR004') {
    store.body.contractTermination.terminateReasonOther = undefined;
  }
});

watch(
  () => store.body.contractTermination,
  () => {
    if (id && !isInitialLoad.value) {
      isFormDirty.value = true;
    }
  },
  { deep: true }
);

watch(
  () => store.body.contractTermination.assignees.filter(a => a.assigneeType === AssigneeType.Assignee),
  async (newValue, oldValue) => {
    if (isInitialLoad.value) return;

    if (newValue.length !== oldValue.length && newValue.length > 0) {
      await store.assigneeDefaultAcceptorAsync();
    }
  },
  { deep: true, flush: 'post' }
);
</script>

<template>
  <ContractInfoCard />
  <TitleHeader label="ขออนุมัติยกเลิกสัญญา" :route-items="routeItems">
    <template #action>
      <div class="flex items-center gap-2" v-if="store.body.contractTermination.status">
        <p class="text-sm">สถานะ :</p>
        <BadgeStatus :label="cm005StatusName(store.body.contractTermination.status)"
          :color="cm005StatusColor(store.body.contractTermination.status)?.color" />
      </div>
      <Button label="ประวัติการใช้งาน" icon="pi pi-refresh" variant="outlined" severity="primary" v-if="id"
        class="bg-white! hover:bg-red-50!" @click="() => showActivityDialog(id)" />
    </template>
  </TitleHeader>
  <Form @submit="onSubmitAsync" @invalidSubmit="ToastHelper.invalidMessageToast()" v-slot="{ handleSubmit }">
    <div class="grid grid-cols-1 lg:grid-cols-7 gap-2">
      <div class="lg:col-span-5 order-2 lg:order-1">
        <Tabs :value="currentTab" unstyled @update:value="(tab) => onTabChange(tab.toString())">
          <TabHeader :items="HeaderItem" class="sticky top-[57px] z-99 bg-[#F7F7F7]" />
          <TabPanels>
            <TabPanel value="0">
              <Card>
                <template #content>
                  <div class="grid grid-cols-1 lg:grid-cols-3 gap-4 mt-6" v-if="isLoaded">
                    <Select label="สาเหตุการยกเลิกสัญญา" :options="store.ctrDropdown"
                      v-model="store.body.contractTermination.terminateType" rules="required"
                      :disabled="(!store.status.canEdit && !store.status.canEditAsAssignee) || !menuStore.hasManage" />
                    <InputField label="สาเหตุการยกเลิกอื่นๆ" rules="required"
                      v-if="store.body.contractTermination.terminateType == 'CTR004'"
                      v-model="store.body.contractTermination.terminateReasonOther"
                      :disabled="(!store.status.canEdit && !store.status.canEditAsAssignee) || !menuStore.hasManage" />

                    <InputArea class="lg:col-span-3" label="หมายเหตุยกเลิกสัญญา" rules="required"
                      v-model="store.body.contractTermination.terminateReason"
                      :disabled="(!store.status.canEdit && !store.status.canEditAsAssignee) || !menuStore.hasManage" />
                    <InputArea class="lg:col-span-3" label="รายละเอียดการบอกเลิกสัญญา" rules="required"
                      v-model="store.body.contractTermination.terminateReasonDetail"
                      :disabled="(!store.status.canEdit && !store.status.canEditAsAssignee) || !menuStore.hasManage" />
                    <Datepicker class="lg:col-start-1 lg:mt-2" label="การยกเลิกสัญญามีผลตั้งแต่วันที่"
                      v-model="store.body.contractTermination.terminationDate"
                      :disabled="(!store.status.canEdit && !store.status.canEditAsAssignee) || !menuStore.hasManage"
                      rules="required" />
                  </div>
                </template>
              </Card>
            </TabPanel>
            <TabPanel value="1">
              <div>
                <ChEditor :docId="store.body.contractTermination.contractTerminationDocumentId"
                  :docName="new Date().toISOString()"
                  :readonly="!menuStore.hasManage || (!store.status.canEdit && !store.status.canEditAsAssignee)"
                  ref="requestApproveDocRef" v-if="store.body.contractTermination.contractTerminationDocumentId"
                  :key="`${store.body.contractTermination.contractTerminationDocumentId}-${store.body.contractTermination.status}`"
                  :versions="store.body.contractTermination.documentVersions ?? []"
                  :canRestoreVersion="store.status.canRestoreVersion"
                  @restore-version="() => store.resetDocumentAsync(contractId, id)" />
              </div>
            </TabPanel>
          </TabPanels>
        </Tabs>

        <section class="my-2">
          <UploadFileGroup v-model="store.body.contractTermination.attachments"
            @upload="() => store.onUpsertAttachments(contractId, id)"
            @remove-file="() => store.onUpsertAttachments(contractId, id)"
            @remove-group="() => store.onUpsertAttachments(contractId, id)"
            @reorder="() => store.onUpsertAttachments(contractId, id)" />
        </section>
      </div>

      <div class="lg:col-span-2 order-1 lg:order-2">
        <div class="flex flex-col gap-4 lg:ml-3 sticky top-[100px]">
          <div class="flex items-center justify-end">
            <div class="draft flex gap-2 items-center">
              <ButtonSave type="submit" v-if="store.status.canEdit" />
              <ButtonSendApprove @click="handleSubmit(onSendCommitteeApproveAsync)" v-if="store.status.canEdit" />
            </div>
            <div class="waiting-committee flex gap-2 items-center">
              <ButtonRecall v-if="store.status.canCommitteeRecall" @click="onRecallAsync" />
              <ButtonSendEdit @click="store.rejectAsync(contractId, id)" v-if="store.status.canApproveCommittee" />
              <ButtonApprove @click="store.approveAsync(contractId, id)" v-if="store.status.canApproveCommittee" />
            </div>
            <div class="assign flex gap-2 items-center" v-if="store.status.canAssign">
              <ButtonSendEdit @click="store.rejectAsync(contractId, id)" />
              <ButtonSave @click="onSaveAssigneeAsync" />
              <ButtonConfirmAssign @click="onConfirmAssignAsync" />
            </div>
            <div class="comment flex gap-2 items-center"
              v-if="(store.status.canComment && store.body.contractTermination.isProposedApprover)">
              <ButtonSave @click="onSaveAssigneeAsync" v-if="store.status.canSaveAcceptors" />
              <ButtonApproveConfirm @click="onSendApproveAsync"
                v-if="store.status.canComment && store.body.contractTermination.isProposedApprover" />
            </div>
            <div class="wating-approve flex gap-2 items-center">
              <ButtonRecall v-if="store.status.canRecall" @click="onRecallAsync" />
              <ButtonSendEdit @click="store.rejectAsync(contractId, id)" v-if="store.status.canApprove" />
              <ButtonConfirm @click="store.approveAsync(contractId, id)"
                v-if="store.status.isLast && store.status.canApprove" />
              <ButtonApprove @click="store.approveAsync(contractId, id)"
                v-if="!store.status.isLast && store.status.canApprove" />
            </div>
          </div>
          <Accordion v-model:value="currentAccordion" unstyled multiple>
            <AccordionPanel value="999" class="mb-4" v-if="currentTab !== '0'">
              <AccordHeader label="Dictionary" />
              <AccordionContent>
                <div></div>
                <Card class="rounded-none overflow-auto h-[800px]">
                  <template #content>
                    <!-- Dictionary mapping for contract termination -->
                    <DocumentMapping pathToGet="contract/contract-termination" @on-click-select="
                      (text, hint) => requestApproveDocRef?.setPlaceholderInDocument(text, hint)
                    " />
                  </template>
                </Card>
              </AccordionContent>
            </AccordionPanel>
            <AccordionPanel value="0" class="mt-4">
              <AccordAcceptor v-model="store.body.contractTermination.acceptors" title="คณะกรรมการตรวจรับ"
                :acceptor-type="AcceptorType.AcceptanceCommittee" is-approve
                @set-is-unable-to-perform-duties="(status: boolean, acceptorId: string, remark?: string) => store.onUpdateDutiesStatusAsync(contractId, id, status, remark, acceptorId)"
                :is-disable="(!store.status.canEdit && !store.status.isCommitteeApproval) || !menuStore.hasManage" />
            </AccordionPanel>
            <AccordionPanel value="1" class="mt-4">
              <AccordAssignee v-model="store.body.contractTermination.assignees" title="เจ้าหน้าที่พัสดุให้ความเห็น"
                :disabled="!store.status.canAssign || !menuStore.hasManage" :is-comment="store.status.canComment"
                @on-comment="(e) => store.commentAsync(contractId, id, e.reason)">
                <template #additional>
                  <Radio :options="[{ label: 'เสนอ', value: true }, { label: 'ไม่เสนอ', value: false }]"
                    v-model="store.body.contractTermination.isProposedApprover"
                    :disabled="!store.status.canAssign && !store.status.canComment"
                    @update:model-value="() => store.onSetIsProposedApproverAsync(contractId, id, store.body.contractTermination.isProposedApprover)" />
                </template>
              </AccordAssignee>
            </AccordionPanel>
            <AccordionPanel value="2" class="mt-4 mb-4" v-if="store.body.contractTermination.isProposedApprover && store.body.contractTermination.assignees.some(a => a.assigneeType === AssigneeType.Assignee)">
              <AccordAcceptor v-model="store.body.contractTermination.acceptors" title="ผู้มีอำนาจเห็นชอบ/อนุมัติ"
                :acceptor-type="AcceptorType.Approver" isManage isApprove
                :is-disable="(!store.status.canAssign && !store.status.canComment) || !menuStore.hasManage"
                :is-set-default="(store.status.canAssign || store.status.canComment) && menuStore.hasManage"
                @set-default="() => store.assigneeDefaultAcceptorAsync()" />
            </AccordionPanel>
          </Accordion>
        </div>
      </div>
    </div>
  </Form>

  <DialogReviewDocument v-if="!!id" v-model="showReviewDocumentDialog" :docId="reviwDocumentId"
    :docName="`plan-${new Date().toISOString()}-${id.toString()}`" @on-click-use-document="setDocumentReviewId" />
</template>
