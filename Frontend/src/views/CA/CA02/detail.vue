<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { BadgeStatus as BadgeComponent } from '@/components';
import { useCA02DetailStore } from '@/stores/CA/ca02';
import { storeToRefs } from 'pinia';
import type { MenuItem } from 'primevue/menuitem';
import { Form as VeeForm } from 'vee-validate';
import { defineAsyncComponent, nextTick, onBeforeMount, onMounted, ref, watch } from 'vue';
import { CA02Helper } from '@/helpers/CA/ca02';
import { CA02Accordion, CA02Status, CA02TabHeader } from '@/enums/CA/ca02';
import { UploadFileGroup } from '@/components/forms';
import { AccordAcceptor } from '@/components/Accordions';
import { AcceptorType } from '@/enums/participants';
import ToastHelper from '@/helpers/toast';
import { useRoute, useRouter } from 'vue-router';
import { showActivityDialog, showSaveOptionDialogAsync } from '@/helpers/dialog';
import DocumentMapping from '@/components/DocumentMapping.vue';
import ModeSelectionDialog from '@/views/CA/CA02/components/ModeSelectionDialog.vue';

const routeItems = ref<Array<MenuItem>>([
  { label: 'รายการใบรับรองผลงาน', url: '/ca/ca02' },
  { label: 'ข้อมูลใบรับรองผลงาน', },
]);

const route = useRoute();
const router = useRouter();
const store = useCA02DetailStore();
const { body } = storeToRefs(store);
const { fn: { onGetByIdAsync, onUpdateDutiesStatusAsync, onSendCommitteeApprovalAsync, onSubmitAsync, onResetBody, onRecallAsync, onSendApproveOrRejectAsync, onUpsertAttachments } } = store;
const { BadgeStatus, TabHeaderItem, AccordionName } = CA02Helper;

const showModeDialog = ref(false);
const modeSelected = ref(false);

const CertificateRequisitionDocument = defineAsyncComponent(() => import('@/views/CA/CA02/components/CertificateRequisitionDocument.vue'));
const certificateRequisitionDocumentRef = ref<InstanceType<typeof CertificateRequisitionDocument> | null>(null);
const currentTab = ref<string>(CA02TabHeader.Detail);
const isFormDirty = ref(false);
const isInitialized = ref(false);

onBeforeMount(() => {
  onResetBody();
});

const onChooseReference = () => {
  showModeDialog.value = false;
  modeSelected.value = true;
};
const onChooseManual = () => {
  showModeDialog.value = false;
  store.body.isManual = true;
  modeSelected.value = true;
};
const onCancelMode = () => router.back();

onMounted(async () => {
  if (route.params.id) {
    await onGetByIdAsync(undefined, route.params.id as string);
    modeSelected.value = true;
  } else {
    showModeDialog.value = true;
  }
  await nextTick();
  isInitialized.value = true;
});

const defaultDocumentStatuses = [
  CA02Status.WaitingForCommitteeApproval,
  CA02Status.Approved,
];

watch(
  () => body.value.status,
  (newStatus: CA02Status) => {
    if (defaultDocumentStatuses.includes(newStatus)) {
      currentTab.value = CA02TabHeader.Document;
    }
  },
  { immediate: true }
);

watch(
  () => body.value,
  () => {
    if (isInitialized.value) {
      isFormDirty.value = true;
    }
  },
  { deep: true }
);

const saveDocument = async () => {
  await submitAsync();
};

const submitAsync = async (): Promise<void> => {
  if (store.body.isManual && store.body.inspectionCommittees.committees.length === 0) {
    ToastHelper.warning('ผู้ตรวจรับ/คณะกรรมการ', 'ต้องเพิ่มรายชื่ออย่างน้อย 1 คน');
    return;
  }

  if (certificateRequisitionDocumentRef.value?.saveDocumentFirst && store.body.documentId) {
    await certificateRequisitionDocumentRef.value.saveDocumentFirst();
  }

  if (isFormDirty.value && store.body.documentId && store.body.id) {
    const saveOption = await showSaveOptionDialogAsync();
    if (saveOption === undefined) return;
    store.body.isResetDocument = saveOption;
  }

  isInitialized.value = false;
  await onSubmitAsync();
  isFormDirty.value = false;
  await nextTick();
  isInitialized.value = true;

  if (store.body.documentId && !store.body.isReplace) {
    currentTab.value = CA02TabHeader.Document;
  }
};

const ContractInfo = defineAsyncComponent(() => import("@/views/CA/CA02/components/ContractInfo.vue"));
const CertificateInfo = defineAsyncComponent(() => import("@/views/CA/CA02/components/CertificateInfo.vue"));
const CommitteeSection = defineAsyncComponent(() => import("@/views/CA/CA02/components/CommitteeSection.vue"));
</script>

<template>
  <TitleHeader label="ใบรองรับผลงาน" :routeItems />
  <ModeSelectionDialog v-model:visible="showModeDialog" @select-reference="onChooseReference"
    @select-manual="onChooseManual" @cancel="onCancelMode" />

  <VeeForm v-if="modeSelected" @submit="submitAsync" @invalidSubmit="ToastHelper.invalidMessageToast()" v-slot="{ handleSubmit: submit }">
    <ContractInfo v-model="body.contractVendorInfo"
      @onSelected="(e) => onGetByIdAsync(e.id)" :disabled="!!body.id"
      :auto-open="!store.body.isManual && !body.id && !route.params.id" />

    <div v-if="body.contractVendorInfo.id || store.body.isManual">
      <TitleHeader label="ข้อมูลใบรับรองผลงาน">
        <template #action>
          <div class="flex items-center gap-2">
            <p class="text-sm">สถานะ :</p>
            <BadgeComponent :label="BadgeStatus(body.status).label" :color="BadgeStatus(body.status).color" />
          </div>
          <Button label="ประวัติการใช้งาน" icon="pi pi-refresh" variant="outlined" severity="primary" v-if="body.id"
            class="bg-white! hover:bg-red-50!" @click="() => showActivityDialog(body.id!)" />
        </template>
      </TitleHeader>
      <div class="grid lg:grid-cols-7 gap-2">
        <div class="lg:col-span-5">
          <Tabs :value="currentTab" unstyled @update:value="(tab) => (currentTab = tab.toString())">
            <TabHeader :items="TabHeaderItem.filter(f => body.id ? f : f.value === CA02TabHeader.Detail)"
              class="sticky top-14 z-3 pt-2 bg-[#F7F7F7]" />
            <TabPanel :value="CA02TabHeader.Detail">
              <CertificateInfo v-model="body" :disabled="!(store.states.isEdit || (store.body.isManual && !store.body.id))" />
              <CommitteeSection v-if="store.body.isManual" v-model="body.inspectionCommittees.committees" v-model:isCommittee="body.inspectionCommittees.isCommittee" class="mt-4" :is-disabled="!(store.body.isManual && (store.states.isEdit || !store.body.id || [CA02Status.Draft, CA02Status.Rejected, CA02Status.Edit].includes(body.status)))" />
            </TabPanel>
            <TabPanel :value="CA02TabHeader.Document">
              <CertificateRequisitionDocument ref="certificateRequisitionDocumentRef" :save="saveDocument" />
            </TabPanel>
          </Tabs>
          <div class="mt-4">
            <UploadFileGroup v-if="!store.body.id" v-model="store.body.attachments"
              :disabled="!store.states.isEdit" />
            <UploadFileGroup v-if="store.body.id" v-model="store.body.attachments"
              @upload="onUpsertAttachments" @remove-file="onUpsertAttachments"
              @remove-group="onUpsertAttachments" @reorder="onUpsertAttachments"
              :disabled="!store.states.isEdit" />
          </div>
        </div>

        <div class="lg:col-span-2 relative">
          <div class="flex flex-col gap-4 ml-3 sticky top-14 pt-2">
            <div id="button-section" class="flex items-center gap-2 justify-end">
              <div id="committee-edit" class="flex items-center gap-2" v-if="store.states.isEdit || !store.body.id">
                <ButtonSave type="submit" />
                <ButtonSendApprove v-if="store.body.id" @click="() => submit(onSendCommitteeApprovalAsync)" />
              </div>

              <div id="committee-approve" class="flex items-center gap-2"
                v-if="store.states.isCommitteeCurrentApproval || store.states.isRecall">
                <ButtonRecall v-if="store.states.isRecall" @click="() => submit(onRecallAsync)" />
                <ButtonNotAgree v-if="store.states.isCommitteeCurrentApproval"
                  @click="() => submit(() => onSendApproveOrRejectAsync('Reject'))" />
                <ButtonApprove v-if="store.states.isCommitteeCurrentApproval && !store.states.isBossCommitteeApproval"
                  @click="() => submit(() => onSendApproveOrRejectAsync('Approve'))" />
                <ButtonConfirm v-if="store.states.isCommitteeCurrentApproval && store.states.isBossCommitteeApproval"
                  @click="() => submit(() => onSendApproveOrRejectAsync('Approve'))" />
              </div>
            </div>

            <div id="accordion-section">
              <Accordion :value="Object.entries(CA02Accordion).map(([_, value]) => value)" unstyled multiple>
                <AccordionPanel value="999" class="mb-4" v-if="currentTab == CA02TabHeader.Document">
                  <AccordHeader label="Dictionary" />
                  <AccordionContent>
                    <div></div>
                    <Card class="rounded-none overflow-auto h-[800px]">
                      <template #content>
                        <DocumentMapping pathToGet="contract-draft-vendor/certificate-requisition" @on-click-select="
                          (text, hint) => certificateRequisitionDocumentRef?.setPlaceholderInDocument(text, hint)
                        " />
                      </template>
                    </Card>
                  </AccordionContent>
                </AccordionPanel>
                <AccordionPanel class="mb-4" :value="CA02Accordion.Committee">
                  <AccordAcceptor :title="AccordionName(CA02Accordion.Committee)" v-model="store.body.acceptors"
                    :acceptor-type="AcceptorType.AcceptanceCommittee" is-approve
                    @set-is-unable-to-perform-duties="(isUnableDuties: boolean, id: string, remark?: string) => submit(() => onUpdateDutiesStatusAsync(isUnableDuties, id, remark))"
                    :is-disable="(!store.states.isEdit && !store.states.isCommitteeApproval)" />
                </AccordionPanel>
              </Accordion>
            </div>
          </div>
        </div>
      </div>
    </div>
  </VeeForm>
</template>