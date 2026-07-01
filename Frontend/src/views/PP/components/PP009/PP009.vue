<script setup lang="ts">
import { Tabs, TabPanels, TabPanel, Button } from 'primevue';
import { TitleHeader, TabHeader, CardSelect } from '@/components/cosmetic';
import { ButtonSave, ButtonApproveConfirm, ButtonApprove, ButtonConfirm, ButtonSendEdit, ButtonRecall } from '@/components/Button';
import { AccordAcceptor } from '@/components/Accordions';
import { BadgeStatus as BadgeStatusComponent } from '@/components';
import { computed, defineAsyncComponent, nextTick, onMounted, ref, watch, type PropType } from 'vue';
import { usePP009DetailStore } from '../../stores/PP009/PP009Store';
import ContractInvitationHelper from '@/helpers/contractInvitation';
import { ContractInvitationHeader, PP009Accordion, PP009Status } from '../../enums/pp009';
import { Form, type SubmissionHandler } from 'vee-validate';
import ToastHelper from '@/helpers/toast';
import { AcceptorType } from '@/enums/participants';
import { showActivityDialog, showSaveOptionDialogAsync } from '@/helpers/dialog';
import DocumentMapping from '@/components/DocumentMapping.vue';
import DialogReviewDocument from '@/components/Document/DialogReviewDocument.vue';
import { useMenuStore } from '@/stores/menu';

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

const { tabHeaderOptions, AccordionName, BadgeStatus } = ContractInvitationHelper;
const menuStore = useMenuStore();
const detailStore = usePP009DetailStore();
const currentTab = ref(ContractInvitationHeader.Detail);

const DetailComponent = defineAsyncComponent(() => import('@/views/PP/components/PP009/components/PP009Detail.vue'));
const DocumentComponent = defineAsyncComponent(() => import('./components/PP009Document.vue'));
const document09Ref = ref<InstanceType<typeof DocumentComponent>[]>([]);
const showReviewDocumentDialog = ref(false);
const reviwDocumentId = ref<string>('');
const currentVendor = ref<string>('');

const isFormDirty = ref(false);
const isInitialized = ref(false);

const invalidSubmit = () => {
  ToastHelper.invalidMessageToast();
  detailStore.currentStatus = detailStore.body.status;
};

const onSendApproval = async (handleSubmit: (evt: Event | SubmissionHandler, onSubmit?: SubmissionHandler) => Promise<unknown>) => {
  detailStore.currentStatus = PP009Status.WaitingApproval;
  await nextTick();
  await handleSubmit(() => detailStore.fn.onSendApprovalAsync());
};

onMounted(async () => {
  detailStore.fn.onResetBody();

  await detailStore.fn.onGetByIdAsync(props.id);

  if (detailStore.body.acceptors.length === 0) {
    await detailStore.fn.onSetDefaultAcceptors();
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

const onChangeVendorTab = (vendorId: string) => {
  const vendor = detailStore.body.vendors.find(v => v.purchaseOrderApprovalContractId === vendorId);

  if (vendor) {
    currentVendor.value = vendor.id!;
    detailStore.currentVendor = vendorId;
  }
};

const onChangeTabs = (tab: string, vendorId: string) => {
  currentVendor.value = vendorId;

  switch (tab) {
    case ContractInvitationHeader.Detail:
      currentTab.value = ContractInvitationHeader.Detail;
      break;
    case ContractInvitationHeader.InvitationDocument:
      currentTab.value = ContractInvitationHeader.InvitationDocument;
      break;
    default:
      currentTab.value = ContractInvitationHeader.Detail;
  }

};

const setDocumentReviewId = (id: string): void => {

  const vendor = detailStore.body.vendors.find(v => v.id === currentVendor.value);
  if (vendor) {
    vendor.documentId = id;
    vendor.isDocumentIdReplace = true;
  }
};

const setPlaceholderDoc = (text: string, hint?: string): void => {
  const currentVendorIndex = detailStore.body.vendors.findIndex(v => v.id === currentVendor.value);

  if (currentVendorIndex !== -1 && document09Ref.value[currentVendorIndex]) {
    document09Ref.value[currentVendorIndex].setPlaceholderInDocument(text, hint);
  }
};

const canEditDocument = computed(() => {
  return detailStore.body.status == PP009Status.Draft
    || detailStore.body.status == PP009Status.Edit
    || detailStore.body.status == PP009Status.Rejected
});

watch(() =>
  detailStore.body.status,
  (newStatus: PP009Status) => {
    if (newStatus === PP009Status.WaitingApproval) {
      currentTab.value = ContractInvitationHeader.InvitationDocument;
    } else if (newStatus === PP009Status.Approved) {
      currentTab.value = ContractInvitationHeader.InvitationDocument;
    }
  }, { immediate: true });

// Save document to Collabora first for current vendor
const saveDocumentFirst = async (): Promise<void> => {
  const currentVendorIndex = detailStore.body.vendors.findIndex(v => v.id === currentVendor.value);
  const documentComponent = document09Ref.value[currentVendorIndex];

  if (currentVendorIndex !== -1 && documentComponent && 'saveDocumentFirst' in documentComponent) {
    await documentComponent.saveDocumentFirst();
  }
};

const onSubmitAsync = async () => {
  // Wait for Collabora to save first if we're on document tab
  if (currentTab.value === ContractInvitationHeader.InvitationDocument) {
    await saveDocumentFirst();
  }

  if (detailStore.body.id) {
    const vendor = detailStore.body.vendors.find(v => v.purchaseOrderApprovalContractId === detailStore.currentVendor);
    if (isFormDirty.value && vendor?.documentId) {
      const saveOption = await showSaveOptionDialogAsync();
      if (saveOption === undefined) return;
      detailStore.body.isDocumentReplace = saveOption;
    }
  }

  isInitialized.value = false;
  await detailStore.fn.onSubmitAsync();
  isFormDirty.value = false;
  await nextTick();
  isInitialized.value = true;

  if (currentTab.value !== ContractInvitationHeader.InvitationDocument) {
    const vendor = detailStore.body.vendors.find(v => v.purchaseOrderApprovalContractId === detailStore.currentVendor);
    if (vendor) {
      currentVendor.value = vendor.id!;
      currentTab.value = ContractInvitationHeader.InvitationDocument;
    }
  }
};

const handleVersionRestored = async () => {
  await detailStore.fn.onGetByIdAsync(props.id);
};
</script>

<template>
  <div class="mt-4">
    <div class="my-2">
      <TitleHeader label="หนังสือเชิญชวนทำสัญญา">
        <template #action>
          <BadgeStatusComponent :label="BadgeStatus(detailStore.body.status).label"
            :color="BadgeStatus(detailStore.body.status).color" />
          <Button label="ประวัติการใช้งาน" icon="pi pi-refresh" variant="outlined" severity="primary"
            v-if="detailStore.body.id" class="bg-white! hover:bg-red-50!"
            @click="() => showActivityDialog(detailStore.body.id!)" />
        </template>
      </TitleHeader>
    </div>
    <Form @submit="onSubmitAsync" @invalid-submit="invalidSubmit()" v-slot="{ handleSubmit }">
      <div class="grid grid-cols-1 lg:grid-cols-7 gap-2">
        <div class="lg:col-span-5">
          <CardSelect v-if="detailStore.body.vendors.length > 1 && detailStore.currentVendor"
            :items="detailStore.venderOptions" :value="detailStore.currentVendor"
            @select="(e) => onChangeVendorTab(e.toString())"/>

          <div v-for="(item, index) in detailStore.body.vendors" :key="index">
            <section v-if="item.purchaseOrderApprovalContractId == detailStore.currentVendor">
              <Tabs :value="currentTab" unstyled @update:value="(tab) => onChangeTabs(tab.toString(), item.id!)">
                <TabHeader :items="tabHeaderOptions.filter((val, index) => item.id ? val : index === 0)"
                  class="sticky top-12 z-3 pt-4 bg-[#F7F7F7]" />
                <TabPanels>
                  <TabPanel :value="ContractInvitationHeader.Detail">
                    <DetailComponent v-model="detailStore.body.vendors[index]" :vendor-index="index"
                      :is-validate="detailStore.states.isRequired(item.purchaseOrderApprovalContractId).value"
                      :is-disabled="!detailStore.states.isEdit || !menuStore.hasManage"
                      :readonly="props.readonly" />
                  </TabPanel>
                  <TabPanel :value="ContractInvitationHeader.InvitationDocument">
                    <DocumentComponent :doc-id="item.documentId"
                      :readonly="!detailStore.states.isEdit || !menuStore.hasManage || !canEditDocument"
                      :versions="item.documentVersions" :procurement-id="props.procurementId"
                      :contract-invitation-id="detailStore.body.id" :vendor-id="item.id"
                      :can-restore-version="detailStore.states.isEdit && menuStore.hasManage && canEditDocument"
                      ref="document09Ref" :save="onSubmitAsync" @version-restored="handleVersionRestored" />
                  </TabPanel>
                </TabPanels>
              </Tabs>
            </section>
          </div>
        </div>
        <div class="col-span-2 relative">
          <div class="flex flex-col gap-4 ml-3 sticky top-22">

            <div v-if="menuStore.hasManage && !readonly">
              <div class="flex items-center gap-2 justify-end" v-if="detailStore.states.isEdit">
                <ButtonSave type="submit" />
                <ButtonApproveConfirm type="button" @click="() => onSendApproval(handleSubmit)"
                  v-if="detailStore.body.id" />
              </div>

              <div class="flex items-center gap-2 justify-end"
                v-if="detailStore.states.isRecall || detailStore.states.isCurrentApproval">
                <ButtonRecall v-if="detailStore.states.isRecall" @click="() => detailStore.fn.onRecallAsync()" />
                <ButtonSendEdit v-if="detailStore.states.isCurrentApproval"
                  @click="() => detailStore.fn.onApprovedRejectedAsync('Reject')" />
                <ButtonApprove v-if="detailStore.states.isCurrentApproval && !detailStore.states.isLastApproval"
                  @click="() => detailStore.fn.onApprovedRejectedAsync('Approve')" />
                <ButtonConfirm v-if="detailStore.states.isCurrentApproval && detailStore.states.isLastApproval"
                  @click="() => detailStore.fn.onApprovedRejectedAsync('Approve')" />
              </div>
            </div>

            <Accordion :value="Object.entries(PP009Accordion).map(([_, value]) => value)" unstyled multiple>
              <AccordionPanel value="Document" class="mb-4"
                v-if="currentTab === ContractInvitationHeader.InvitationDocument">
                <AccordHeader label="Dictionary" />
                <AccordionContent>
                  <div></div>
                  <Card class="rounded-none overflow-auto h-[800px]">
                    <template #content>
                      <DocumentMapping pathToGet="procurement/contractInvitation"
                        @on-click-select="(text, hint) => setPlaceholderDoc(text, hint)" />
                    </template>
                  </Card>
                </AccordionContent>
              </AccordionPanel>
              <AccordionPanel :value="PP009Accordion.Acceptor">
                <AccordAcceptor :title="AccordionName(PP009Accordion.Acceptor)" :acceptorType="AcceptorType.Approver"
                  v-model="detailStore.body.acceptors" isApprove isManage
                  :is-disable="!detailStore.states.isEdit || !menuStore.hasManage || readonly" isSetDefault
                  @set-default="detailStore.fn.onSetDefaultAcceptors" />
              </AccordionPanel>
            </Accordion>
          </div>
        </div>
      </div>
    </Form>

    <DialogReviewDocument v-model="showReviewDocumentDialog" :docId="reviwDocumentId"
      :docName="`pp009-${new Date().toISOString()}-${reviwDocumentId}`" @on-click-use-document="setDocumentReviewId" />
  </div>
</template>