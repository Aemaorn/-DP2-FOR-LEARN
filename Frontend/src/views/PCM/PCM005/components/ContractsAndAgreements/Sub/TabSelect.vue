<script setup lang="ts">
import type { Option } from '@/models/shared/option';
import { TabHeader } from '@/components/cosmetic';
import { computed, defineAsyncComponent, nextTick, onMounted, ref, watch } from 'vue';
import DateSignDialog from '@/views/PP/components/PP010/components/Sub/DateSignDialog.vue';
import { AcceptorType } from '@/enums/participants';
import { TContractDraftStatus } from '@/views/PP/enums/pp010';
import { Form } from 'vee-validate';
import { ConfirmDialogType } from '@/enums/dialog';
import { showConfirmDialogAsync, showSaveOptionDialogAsync } from '@/helpers/dialog';
import ToastHelper from '@/helpers/toast';
import { ButtonSendApprove, ButtonSave, ButtonApprove, ButtonNotAgree } from '@/components/Button';
import { AccordAcceptor } from '@/components/Accordions';
import DocumentMapping from '@/components/DocumentMapping.vue';
import DialogReviewDocument from '@/components/Document/DialogReviewDocument.vue';
import { usePcmContractDraftStore } from '@/stores/PCM/PCM005/pcmContractDraft';
import { useLoadingStore } from '@/stores/loading';
import SectionNavigator from '@/views/PP/components/PP010/components/SectionNavigator.vue';
import { useSectionSpy } from '@/views/PP/components/PP010/composables/useSectionSpy';

const ContractInfo = defineAsyncComponent(() => import('./ContractInfo.vue'));
const Detail = defineAsyncComponent(() => import('./Detail.vue'));
const PP010ContractDraftDocument = defineAsyncComponent(
  () => import('./ContractDraftDocument.vue')
);
const PP010ContractApproveDocument = defineAsyncComponent(
  () => import('./ContractApproveDocument.vue')
);
const PP010ContractConfidentialDocument = defineAsyncComponent(
  () => import('./ContractConfidentialDocument.vue')
);

const pp010ContractDraftDocumentRef = ref<InstanceType<typeof PP010ContractDraftDocument> | null>(
  null
);
const pp010ContractApproveDocumentRef = ref<InstanceType<
  typeof PP010ContractApproveDocument
> | null>(null);
const pp010ContractConfidentialDocumentRef = ref<InstanceType<
  typeof PP010ContractConfidentialDocument
> | null>(null);


const currentTab = ref('0');
const showDateSignModal = ref(false);
const showReviewDocumentDialog = ref(false);
const reviwDocumentId = ref<string>('');

const store = usePcmContractDraftStore();

const checkShowDateSignModal = (): void => {
  if (store.states.canSaveDateSign && !store.body.contractSignedDate) {
    showDateSignModal.value = true;
  }
};

const onSaveDateSign = async (date: Date): Promise<void> => {
  store.body.contractSignedDate = date;
  await store.api.onUpdateContractDraft();
};

onMounted(() => checkShowDateSignModal());
watch(() => store.body.status, () => checkShowDateSignModal());

const isSubmitting = ref(false);
let stopWatcher: (() => void) | null = null;
const isDirty = ref(false);

const detailContainerRef = ref<HTMLElement | null>(null);
const { sections, activeSectionId, scrollToSection, reinit } = useSectionSpy(detailContainerRef);

const trackingBody = () => {
  if (stopWatcher) return;
  stopWatcher = watch(() => store.body, () => {
    if (!store.cloneBody.id) {
      isDirty.value = false;

      return;
    }

    isDirty.value = true;
  }, { deep: true });
};

const stopTrackingBody = () => {
  stopWatcher?.();
  stopWatcher = null;
};


watch(() => store.initial, (val) => {
  if (val) {
    stopTrackingBody();
    isDirty.value = false;

    return;
  }

  restoreState();
}, { deep: true });

watch(currentTab, (tab) => {
  if (tab === '1') {
    reinit();
  }
});

const HeaderItem = ref([
  {
    label: 'ข้อมูลสัญญา',
    value: '0',
  },
  {
    label: 'รายละเอียด',
    value: '1',
  },
  {
    label: 'เอกสารร่างสัญญาหรือข้อตกลง',
    value: '2',
  },
  {
    label: 'เอกสารขออนุมัติสัญญา',
    value: '3',
  },
] as Option[]);

const saveDocument = async () => {
  await onSubmitAsync();
};

const visibleTabs = computed(() => {
  return HeaderItem.value.filter((item) => {
    switch (item.value) {
      case '0':
      case '1':
        return true;
      case '2':
        return !!store.body.contractDraftDocumentId;
      case '3':
        return !!store.body.approvalContractDraftDocumentId && store.body.status === TContractDraftStatus.Approved;
      default:
        return false;
    }
  });
});

const onCreateDocumentation = () => {
  if (store.body.contractDraftDocumentId) {
    return;
  }
  !store.body.contractDraftDocumentId && (store.body.isContractDraftDocumentIdReplace = true);
  !store.body.confidentialContractDraftDocumentId && (store.body.isConfidentialContractDraftDocumentIdReplace = true);
  !store.body.approvalContractDraftDocumentId && (store.body.isApprovalContractDraftDocumentIdReplace = true);
};

const onSaveDocumentation = async () => {
  const loading = useLoadingStore();
  loading.setIsLoading(true);
  pp010ContractDraftDocumentRef.value && await pp010ContractDraftDocumentRef.value.saveDocumentFirst();
  pp010ContractApproveDocumentRef.value && await pp010ContractApproveDocumentRef.value.saveDocumentFirst();
  pp010ContractConfidentialDocumentRef.value && await pp010ContractConfidentialDocumentRef.value.saveDocumentFirst();
  loading.setIsLoading(false);
};

const onTriggerSaveOptionDialog = async () => {
  if (!store.body.contractDraftDocumentId) return true;

  if (!isDirty.value) {
    store.body.isContractDraftDocumentIdReplace = false;
    store.body.isConfidentialContractDraftDocumentIdReplace = false;
    store.body.isApprovalContractDraftDocumentIdReplace = false;
    return true;
  }

  const dialogResult = await showSaveOptionDialogAsync();
  if (dialogResult == undefined) return false;

  store.body.isContractDraftDocumentIdReplace = dialogResult;
  store.body.isConfidentialContractDraftDocumentIdReplace = dialogResult;
  store.body.isApprovalContractDraftDocumentIdReplace = dialogResult;
  return true;
};

const restoreState = () => {
  store.body.isContractDraftDocumentIdReplace = false;
  store.body.isConfidentialContractDraftDocumentIdReplace = false;
  store.body.isApprovalContractDraftDocumentIdReplace = false;
  isDirty.value = false;
  trackingBody();
};

const onSaveDraftAsync = async () => {
  if (!store.vendorId || isSubmitting.value) return;
  isSubmitting.value = true;
  stopTrackingBody();

  try {
    await onSaveDocumentation();

    if (!await onTriggerSaveOptionDialog()) return;
    await store.api.onUpdateContractDraft(true);

    restoreState();
  } finally {
    isSubmitting.value = false;
  }
};

const onSubmitAsync = async () => {
  if (!store.vendorId || isSubmitting.value) return;
  isSubmitting.value = true;
  stopTrackingBody();

  try {
    onCreateDocumentation();
    await onSaveDocumentation();

    if (!await onTriggerSaveOptionDialog()) return;
    await store.api.onUpdateContractDraft(false);

    if (store.body.contractDraftDocumentId) {
      currentTab.value = '2';
    }

    restoreState();
  } finally {
    isSubmitting.value = false;
  }
};

const updateStatusPending = async () => {
  if (!store.vendorId || isSubmitting.value) return;
  isSubmitting.value = true;
  stopTrackingBody();

   if (store.body.coiCheckerResult?.result == null || store.body.watchlistCheckerResult?.result == null || store.body.egpResult == null) {
    ToastHelper.warning('ข้อมูลไม่ถูกต้อง', 'กรุณาตรวจข้อมูล COI, Watchlist, ผู้ทิ้งงาน (e-GP) ให้ครบถ้วน');

    if (currentTab.value !== '1') {
      currentTab.value = '1';
      await nextTick();
    }

    const entrepreneurSection = document.getElementById('entrepreneur-section');
    if (entrepreneurSection) {
      entrepreneurSection.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }

    isSubmitting.value = false;
    return false;
  }

  try {
    if (!await showConfirmDialogAsync(ConfirmDialogType.SendApproveConfirm)) return;

    restoreState();
    await store.api.onUpdateContractDraft(false, TContractDraftStatus.Pending);
  } finally {
    isSubmitting.value = false;
  }
};

const canEditDocument = computed(() => {
  return store.body.status === TContractDraftStatus.Edit
    || store.body.status === TContractDraftStatus.Draft
    || store.body.status === TContractDraftStatus.Rejected
});

const setDocumentReviewId = (id: string): void => {
  switch (currentTab.value) {
    case '2':
      store.body.contractDraftDocumentId = id;
      store.body.isContractDraftDocumentIdReplace = true;
      break;
    case '3':
      store.body.approvalContractDraftDocumentId = id;
      store.body.isApprovalContractDraftDocumentIdReplace = true;
      break;
    case '4':
      store.body.confidentialContractDraftDocumentId = id;
      store.body.isConfidentialContractDraftDocumentIdReplace = true;
      break;
  }
};

const updateStatusRecall = async () => {
  if (!store.vendorId || isSubmitting.value) return;
  isSubmitting.value = true;

  try {
    if (!(await showConfirmDialogAsync(ConfirmDialogType.Edit))) return;

    await store.api.onUpdateContractDraft(false, TContractDraftStatus.Edit);
  } finally {
    isSubmitting.value = false;
  }
};

const onInvalidSubmit = async ({ errors }: { errors: Partial<Record<string, string>> }) => {
  ToastHelper.invalidMessageToast();
  await nextTick();

  const firstErrorKey = Object.keys(errors)[0];
  if (!firstErrorKey) return;

  const errorEl = document.querySelector(`[name="${firstErrorKey}"]`);
  if (!errorEl) return;

  const allTabPanels = document.querySelectorAll('[data-pc-name="tabpanel"]');
  for (let i = 0; i < allTabPanels.length; i++) {
    if (allTabPanels[i].contains(errorEl)) {
      const tabValue = visibleTabs.value[i]?.value;
      if (tabValue != null && currentTab.value !== tabValue.toString()) {
        currentTab.value = tabValue.toString();
        await nextTick();
      }
      break;
    }
  }

  const focusTarget = document.querySelector(`[name="${firstErrorKey}"]`) as HTMLElement | null;
  if (focusTarget) {
    focusTarget.scrollIntoView({ behavior: 'smooth', block: 'center' });
    focusTarget.focus();
  }
};
</script>

<template>
  <Form @submit="onSubmitAsync()" @invalidSubmit="onInvalidSubmit" v-slot="{ handleSubmit }">
    <div class="grid grid-cols-1 lg:grid-cols-7 gap-2">
      <div class="md:col-span-5">
        <Tabs :value="currentTab" unstyled @update:value="(tab) => currentTab = tab.toString()">
          <TabHeader :items="visibleTabs" class="sticky top-[58px] z-99 bg-[#F7F7F7] no-custom" />
          <TabPanels>
            <TabPanel value="0">
              <div class="grid grid-rows-1 gap-4 mb-5">
                <ContractInfo />
              </div>
            </TabPanel>
            <TabPanel value="1">
              <div ref="detailContainerRef" class="grid grid-rows-1 gap-4 mb-5">
                <Detail />
              </div>
            </TabPanel>
            <TabPanel v-if="store.body?.contractDraftDocumentId" value="2">
              <PP010ContractDraftDocument :readonly="!canEditDocument" ref="pp010ContractDraftDocumentRef"
                :save="saveDocument" />
            </TabPanel>
            <TabPanel v-if="store.body?.approvalContractDraftDocumentId" value="3">
              <PP010ContractApproveDocument ref="pp010ContractApproveDocumentRef" :save="saveDocument" />
            </TabPanel>
            <TabPanel v-if="store.body?.confidentialContractDraftDocumentId" value="4">
              <PP010ContractConfidentialDocument :readonly="!canEditDocument" ref="pp010ContractConfidentialDocumentRef"
                :save="saveDocument" />
            </TabPanel>
          </TabPanels>
        </Tabs>
      </div>
      <div class="relative lg:col-span-2">
        <div class="flex flex-col gap-4 ml-3 sticky top-[100px]">
          <div class="flex items-center gap-2 justify-end mt-2">
            <Button type="button" v-if="store.states.canEdit" label="บันทึกชั่วคราว" icon="pi pi-save"
              severity="success" @click="onSaveDraftAsync()" />
            <ButtonSave type="submit" label="ยืนยันบันทึก" v-if="store.states.canEdit" />
            <ButtonSendApprove @click="handleSubmit(() => updateStatusPending())" v-if="store.states.canEdit" />

            <ButtonRecall v-if="store.states.canRecall" @click="updateStatusRecall()" />
            <ButtonNotAgree v-if="store.states.canAcceptAndReject" @click="handleSubmit(() => store.api.rejectAsync())" />
            <ButtonApprove v-if="store.states.canAcceptAndReject"
              @click="store.api.approveAsync()" label="ยืนยันตรวจสอบ" />
            <ButtonSave v-if="store.states.canSaveDateSign" label="บันทึกวันที่ลงนามในสัญญา"
              @click="handleSubmit(() => onSubmitAsync())" />
          </div>
          <Accordion :value="['0']" unstyled multiple>
            <AccordionPanel value="999" class="mb-4" v-if="currentTab !== '0' && currentTab !== '1'">
              <AccordHeader label="Dictionary" />
              <AccordionContent>
                <div></div>
                <Card class="rounded-none overflow-auto h-[800px]">
                  <template #content>
                    <!-- Dictionary mapping for contract draft documents -->
                    <DocumentMapping pathToGet="procurement/contract-draft" @on-click-select="
                      (text, hint) =>
                        currentTab == '2'
                          ? pp010ContractDraftDocumentRef?.setPlaceholderInDocument(text, hint)
                          :
                          currentTab == '3'
                            ? pp010ContractApproveDocumentRef?.setPlaceholderInDocument(text, hint)
                            : pp010ContractConfidentialDocumentRef?.setPlaceholderInDocument(text, hint)
                    " />
                  </template>
                </Card>
              </AccordionContent>
            </AccordionPanel>
            <AccordionPanel value="0">
              <AccordAcceptor title="ผู้ตรวจสอบสัญญา" v-model="store.body.acceptors"
                :acceptorType="AcceptorType.Approver" isShowCheckBoxAll isApprove isManage
                :is-disable="![TContractDraftStatus.Draft, TContractDraftStatus.Rejected, TContractDraftStatus.Edit].includes(store.body.status)"
                :is-set-default="store.states.canEdit"
                @set-default="() => store.api.onGetDefaultContractDirector()"/>
            </AccordionPanel>
          </Accordion>
        </div>
      </div>
    </div>
  </Form>

  <SectionNavigator v-if="currentTab === '1' && sections.length > 0" :sections="sections"
    :active-section-id="activeSectionId" @navigate="scrollToSection" />

  <DialogReviewDocument v-model="showReviewDocumentDialog" :docId="reviwDocumentId"
    :docName="`pcm005-${new Date().toISOString()}-${reviwDocumentId}`" @on-click-use-document="setDocumentReviewId" />
  <DateSignDialog v-model:visible="showDateSignModal" :on-save="onSaveDateSign" />
</template>