<script setup lang="ts">
import { computed, defineAsyncComponent, nextTick, onMounted, ref } from 'vue';
import { TitleHeader, CardSelect } from '@/components/cosmetic';

const props = defineProps({
  readonly: { type: Boolean, default: false },
});

import { BadgeStatus as BadgeComponent } from '@/components';
import { usePcm005ContractInvitationStore as ContractStore } from '@/stores/PCM/PCM005/contractInvitation';
import { ContractInvitationHeader, PP009Accordion, PP009Status } from '@/views/PP/enums/pp009';
import { Form as VeeForm, type SubmissionHandler } from 'vee-validate';
import { AcceptorType } from '@/enums/participants';
import { showActivityDialog } from '@/helpers/dialog';
import { useMenuStore } from '@/stores/menu';
import ContractInvitationHelper from '@/helpers/contractInvitation';
import ToastHelper from '@/helpers/toast';
import ChEditor from '@/components/Document/ChEditor.vue';
import DocumentMapping from '@/components/DocumentMapping.vue';
import DialogReviewDocument from '@/components/Document/DialogReviewDocument.vue';
import {
  ButtonSave,
  ButtonApproveConfirm,
  ButtonRecall,
  ButtonApprove,
  ButtonConfirm,
  ButtonSendEdit,
} from '@/components/Button';
import { AccordAcceptor } from '@/components/Accordions';
import { Button } from 'primevue';


const DetailComponent = defineAsyncComponent(() => import('@/views/PP/components/PP009/components/PP009Detail.vue'));

const { tabHeaderOptions, AccordionName, BadgeStatus } = ContractInvitationHelper;

const vendorCardOptions = computed(() =>
  store.body.vendors.map(v => ({
    title: v.contractNumber ?? '',
    description: v.budgetDetail,
    value: v.purchaseOrderApprovalContractId,
    isCompleted: !!(v.email && v.contractOfficerPhone && v.coiDate && v.watchlistDate && v.egpDate),
  } as const))
);
const menuStore = useMenuStore();
const store = ContractStore();

const currentTab = ref(ContractInvitationHeader.Detail);
const docRef = ref<InstanceType<typeof ChEditor> | null>(null);
const showReviewDocumentDialog = ref(false);
const reviwDocumentId = ref<string>('');
const currentVendor = ref<string>('');

const invalidSubmit = () => {
  ToastHelper.invalidMessageToast();
  store.currentStatus = store.body.status;
};

const onSendApproval = async (handleSubmit: (evt: Event | SubmissionHandler, onSubmit?: SubmissionHandler) => Promise<unknown>) => {
  store.currentStatus = PP009Status.WaitingApproval;
  await nextTick();
  await handleSubmit(() => store.fn.onSendApprovalAsync());
};

onMounted(async () => {
  store.fn.onResetBody();

  await store.fn.onGetByIdAsync();

  if (store.currentVendor) {
    const vendor = store.body.vendors.find(v => v.purchaseOrderApprovalContractId === store.currentVendor);
    if (vendor) {
      currentVendor.value = vendor.id!;
    }
  }

  if (!store.body.acceptors || store.body.acceptors.length === 0) {
    await store.fn.onSetDefaultAcceptors();
  }
});

const onChangeVendorTab = (vendorId: string) => {
  const vendor = store.body.vendors.find(v => v.purchaseOrderApprovalContractId === vendorId);

  if (vendor) {
    currentVendor.value = vendor.id!;
    store.currentVendor = vendorId;
  }
};

const onChangeCurrentTab = (tab: string, vendorId?: string) => {

  if (vendorId) {
    currentVendor.value = vendorId;
  }

  switch (tab) {
    case ContractInvitationHeader.Detail:
      currentTab.value = ContractInvitationHeader.Detail;
      break;
    case ContractInvitationHeader.InvitationDocument:
      currentTab.value = ContractInvitationHeader.InvitationDocument;
      const vendor = store.body.vendors.find(v => v.id === vendorId);
      if (vendor != null && !vendor.isDocumentIdReplace) {
        getReviewDocumentAsync();
      }
      break;
    default:
      currentTab.value = ContractInvitationHeader.Detail;
  }
};

const getReviewDocumentAsync = async (): Promise<void> => {
  if (!store.body.id) return;

  const idDocument = await store.fn.getReviewDocumentAsync(store.body.id, store.body.procurementId, currentVendor.value);

  reviwDocumentId.value = idDocument;
  setDocumentReviewId(idDocument);
};

const setDocumentReviewId = (id: string): void => {
  const vendor = store.body.vendors.find(v => v.id === currentVendor.value);
  if (vendor) {
    vendor.documentId = id;
    vendor.isDocumentIdReplace = true;
  }
};

// Save document to CHEditor first, then call API
const saveDocumentFirst = (): Promise<void> => {
  return new Promise((resolve) => {
    // Only save if document is editable
    if (docRef.value?.saveAndWait && store.states.isEdit && menuStore.hasManage) {
      docRef.value.saveAndWait(() => {
        resolve();
      });
    } else {
      resolve();
    }
  });
};

const onSubmitAsync = async () => {
  // Save current document to Collabora first
  const vendor = store.body.vendors.find(v => v.id === currentVendor.value);
  if (docRef.value && vendor?.documentId) {
    await saveDocumentFirst();
  }

  await store.fn.onSubmitAsync();
};
</script>

<template>
  <div class="mt-4">
    <div class="my-2">
      <TitleHeader label="หนังสือเชิญชวนทำสัญญา">
        <template #action>
          <BadgeComponent :label="BadgeStatus(store.body.status).label" :color="BadgeStatus(store.body.status).color" />
          <Button label="ประวัติการใช้งาน" icon="pi pi-refresh" variant="outlined" severity="primary" v-if="store.body.id"
            class="bg-white! hover:bg-red-50!" @click="() => showActivityDialog(store.body.id!)" />
        </template>
      </TitleHeader>
    </div>

    <VeeForm @submit="onSubmitAsync" @invalid-submit="invalidSubmit()" v-slot="{ handleSubmit }">
      <div class="grid grid-cols-1 lg:grid-cols-7 gap-2">
        <div class="lg:col-span-5">
          <CardSelect v-if="store.body.vendors.length > 1 && store.currentVendor"
            :items="vendorCardOptions" :value="store.currentVendor"
            @select="(e) => onChangeVendorTab(e.toString())">
          </CardSelect>

          <div v-for="(item, index) in store.body.vendors" :key="index">
            <section v-if="item.purchaseOrderApprovalContractId == store.currentVendor">
              <Tabs :value="currentTab" unstyled @update:value="(tab) => onChangeCurrentTab(tab.toString(), item.id!)">
                <TabHeader :items="tabHeaderOptions.filter((t, i) => item.id ? t : i === 0)"
                  class="sticky top-12 z-3 pt-4 bg-[#F7F7F7]" />
                <TabPanels>
                  <TabPanel :value="ContractInvitationHeader.Detail">
                    <DetailComponent v-model="store.body.vendors[index]"
                      :is-validate="store.states.isRequired(item.purchaseOrderApprovalContractId).value"
                      :is-disabled="!store.states.isEdit || !menuStore.hasManage" />
                  </TabPanel>
                  <TabPanel :value="ContractInvitationHeader.InvitationDocument">
                    <ChEditor
                      :docId="item.documentId"
                      :docName="`invite-${new Date().toISOString()}`"
                      :readonly="!menuStore.hasManage"
                      ref="docRef"
                      v-if="item.documentId"
                      :key="item.documentId"
                      :save="onSubmitAsync"
                      :versions="item.documentVersions ?? []"
                      :canRestoreVersion="store.states.canRestoreVersion(item.id!).value"
                      @restore-version="() => store.fn.resetDocumentAsync(item.id!)" />
                  </TabPanel>
                </TabPanels>
              </Tabs>
            </section>
          </div>
        </div>
        <div class="col-span-2 relative">
          <div class="flex flex-col gap-4 ml-3 sticky top-22">

            <div v-if="menuStore.hasManage && !props.readonly">
              <div class="flex items-center gap-2 justify-end" v-if="store.states.isEdit">
                <ButtonSave type="submit" />
                <ButtonApproveConfirm type="button" @click="() => onSendApproval(handleSubmit)" v-if="store.body.id" />
              </div>

              <div class="flex items-center gap-2 justify-end"
                v-if="store.states.isRecall || store.states.isCurrentApproval">
                <ButtonRecall v-if="store.states.isRecall" @click="() => store.fn.onRecallAsync()" />
                <ButtonSendEdit v-if="store.states.isCurrentApproval"
                  @click="() => store.fn.onApprovedRejectedAsync('Reject')" />
                <ButtonApprove v-if="store.states.isCurrentApproval && !store.states.isLastApproval"
                  @click="() => store.fn.onApprovedRejectedAsync('Approve')" />
                <ButtonConfirm v-if="store.states.isCurrentApproval && store.states.isLastApproval"
                  @click="() => store.fn.onApprovedRejectedAsync('Approve')" />
              </div>
            </div>

            <Accordion :value="Object.entries(PP009Accordion).map(([_, value]) => value)" unstyled multiple>
              <AccordionPanel value="Document" class="mb-4"
                v-if="currentTab == ContractInvitationHeader.InvitationDocument">
                <AccordHeader label="Dictionary" />
                <AccordionContent>
                  <div></div>
                  <Card class="rounded-none overflow-auto h-[800px]">
                    <template #content>
                      <DocumentMapping pathToGet="procurement/contractInvitation" @on-click-select="
                        (text, hint) => docRef?.setPlaceholderInDocument(text, hint)
                      " />
                    </template>
                  </Card>
                </AccordionContent>
              </AccordionPanel>
              <AccordionPanel :value="PP009Accordion.Acceptor">
                <AccordAcceptor :title="AccordionName(PP009Accordion.Acceptor)" :acceptorType="AcceptorType.Approver"
                  v-model="store.body.acceptors" isApprove isManage :is-disable="!store.states.isEdit || props.readonly" isSetDefault
                  @set-default="store.fn.onSetDefaultAcceptors" />
              </AccordionPanel>
            </Accordion>
          </div>
        </div>
      </div>
    </VeeForm>

    <DialogReviewDocument v-model="showReviewDocumentDialog" :docId="reviwDocumentId"
      :docName="`pcm005-ci-${new Date().toISOString()}-${reviwDocumentId}`"
      @on-click-use-document="setDocumentReviewId" />
  </div>
</template>