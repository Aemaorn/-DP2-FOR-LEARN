<script setup lang="ts">
import { TitleHeader, TabHeader } from '@/components/cosmetic';
import { TabPanel, TabPanels, Tabs } from 'primevue';
import type { Option } from '@/models/shared/option';
import { computed, defineAsyncComponent, nextTick, onMounted, onUnmounted, ref, watch } from 'vue';
import { UploadFileGroup } from '@/components/forms';
import DocumentMapping from '@/components/DocumentMapping.vue';
import type { MenuItem } from 'primevue/menuitem';
import { Form } from 'vee-validate';
import { useRp001DetailStore } from '@/stores/RP/rp001';
import ToastHelper from '@/helpers/toast';
import { useRoute, useRouter } from 'vue-router';
import { AcceptorType } from '@/enums/participants';
import { ButtonApprove, ButtonApproveConfirm, ButtonConfirm, ButtonRecall, ButtonSave } from '@/components/Button';
import { rp001Status } from '@/enums/RP/rp001';
import { showActivityDialog, showConfirmDialogAsync, showSaveOptionDialogAsync } from "@/helpers/dialog";
import { ConfirmDialogType } from "@/enums/dialog";
import { BadgeStatus as BadgeComponent } from '@/components';
import rp001Constant from '@/constants/RP/rp001';
import AuditReportDocument from '@/components/Document/ChEditor.vue';
import AuditGeneralReportDocument from '@/components/Document/ChEditor.vue';
import RevenueReportDocument from '@/components/Document/ChEditor.vue';
import ButtonSendEdit from '@/components/Button/ButtonSendEdit.vue';
import { useLoadingStore } from '@/stores/loading';
import { AccordAcceptor } from '@/components/Accordions';

const { BadgeStatus } = rp001Constant;

const route = useRoute();
const router = useRouter();

const DocumentDetail = defineAsyncComponent(() => import('./components/DocumentDetail.vue'));
const ContractList = defineAsyncComponent(() => import('./components/ContractList.vue'));

const id = computed(() => route.params.id?.toString());

const store = useRp001DetailStore();

const HeaderItem = ref([
  {
    label: 'รายละเอียด',
    value: '0',
  },
  {
    label: 'รายงาน สตง. และหนังสือถึงสรรพากร',
    value: '1',
  },
  {
    label: 'รายงาน สตง. ถึงผู้ว่าการตรวจเงินแผ่นดิน',
    value: '2',
  },
  {
    label: 'รายงานหนังสือถึงสรรพากรถึงอธิบดีกรมสรรพากร',
    value: '3',
  },
] as Option[]);

const currentTab = ref('0');

const auditReportDocumentRef = ref<InstanceType<typeof AuditReportDocument> | null>(null);
const auditGeneralReportDocumentRef = ref<InstanceType<typeof AuditGeneralReportDocument> | null>(null);
const revenueReportDocumentRef = ref<InstanceType<typeof RevenueReportDocument> | null>(null);

const isFormDirty = ref(false);
const isInitialized = ref(false);

const routeItems = ref([
  { label: 'รายงานสำนักงานการตรวจเงินแผ่นดินและกรมสรรพากร', url: '/rp/rp001' },
  { label: 'ข้อมูลรายงานสำนักงานการตรวจเงินแผ่นดินและกรมสรรพากร' },
] as MenuItem[]);

const canEditDocument = computed(() => {
  return [rp001Status.Draft, rp001Status.Edit, rp001Status.Rejected].includes(store.body.status) || !store.body.status;
});

const saveDocumentFirst = async (): Promise<void> => {
  if (!canEditDocument.value) return;

  const activeEditor = currentTab.value === '1' ? auditReportDocumentRef.value
    : currentTab.value === '2' ? auditGeneralReportDocumentRef.value
      : currentTab.value === '3' ? revenueReportDocumentRef.value : null;

  if (activeEditor?.saveAndWait) {
    return new Promise((resolve) => {
      activeEditor.saveAndWait(() => {
        resolve();
      });
    });
  }
};

const onSaveDocument = async (): Promise<void> => {
  const loading = useLoadingStore();
  try {
    const promises: Promise<void>[] = [];

    if (auditReportDocumentRef.value?.saveAndWait) {
      promises.push(new Promise((resolve) => {
        auditReportDocumentRef.value!.saveAndWait(() => { resolve(); });
      }));
    }
    if (auditGeneralReportDocumentRef.value?.saveAndWait) {
      promises.push(new Promise((resolve) => {
        auditGeneralReportDocumentRef.value!.saveAndWait(() => { resolve(); });
      }));
    }
    if (revenueReportDocumentRef.value?.saveAndWait) {
      promises.push(new Promise((resolve) => {
        revenueReportDocumentRef.value!.saveAndWait(() => { resolve(); });
      }));
    }


    loading.setIsLoading(true);
    await Promise.all(promises);
  } finally {
    loading.setIsLoading(false);
  }
}

const onSubmitAsync = async () => {
  if (currentTab.value === '1' || currentTab.value === '2' || currentTab.value === '3') {
    await saveDocumentFirst();
  }

  if (id.value) {
    if (store.body.auditReportDocumentId) {
      store.body.isAuditReportDocumentIdReplaced = isFormDirty.value;
    }
    if (store.body.auditGeneralReportDocumentId) {
      store.body.isAuditGeneralReportDocumentIdReplaced = isFormDirty.value;
    }
    if (store.body.revenueReportDocumentId) {
      store.body.isRevenueReportDocumentIdReplaced = isFormDirty.value;
    }

    if (isFormDirty.value
      && (store.body.auditReportDocumentId
        || store.body.auditGeneralReportDocumentId
        || store.body.revenueReportDocumentId)
      && [rp001Status.Draft, rp001Status.Edit, rp001Status.Rejected].includes(store.body.status)) {
      const saveOption = await showSaveOptionDialogAsync();
      if (saveOption === undefined) return;
      store.body.isAuditReportDocumentIdReplaced = saveOption;
      store.body.isAuditGeneralReportDocumentIdReplaced = saveOption;
      store.body.isRevenueReportDocumentIdReplaced = saveOption;
    }

    isInitialized.value = false;
    await store.api.updateRp001();
    isFormDirty.value = false;
    await nextTick();
    isInitialized.value = true;
    return;
  }

  isInitialized.value = false;
  await store.api.createRp001();
  router.replace({ name: "rp001Detail", params: { id: store.body.id } });
  isFormDirty.value = false;
  await nextTick();
  isInitialized.value = true;
}

const onSendApproveAsync = async () => {
  if (store.body.details.length === 0) {
    return ToastHelper.errorDescription("จะต้องมีรายการสัญญาอย่างน้อย 1 สัญญา");
  }

  if (store.body.approvalAcceptors.length === 0) {
    return ToastHelper.approvalAtLeastMessageToast();
  }

  await onSaveDocument();

  if (!(await showConfirmDialogAsync(ConfirmDialogType.SendApproveConfirm))) return;

  store.body.status = rp001Status.WaitingApproval;
  store.body.isAuditReportDocumentIdReplaced = false;
  store.body.isAuditGeneralReportDocumentIdReplaced = false;
  store.body.isRevenueReportDocumentIdReplaced = false;

  if (id.value) {
    await store.api.updateRp001SendApprove();
    return;
  };

  await store.api.createRp001SendApprove();

  router.replace({ name: "rp001Detail", params: { id: store.body.id } });
};

const onEditAsync = async () => {
  if (!id.value) return;

  if (!await showConfirmDialogAsync(ConfirmDialogType.Edit)) return;

  await store.api.updateRp001Recall();
}

watch(() => store.body, () => {
  if (isInitialized.value) {
    isFormDirty.value = true;
  }
}, { deep: true });

onUnmounted(() => {
  store.clearBody();
})

onMounted(async () => {
  if (id.value) {
    await store.api.getByIdRp001(id.value);
  } else {
    await store.api.getDefaultAcceptor();
  }
  await nextTick();
  isInitialized.value = true;
})

const handleVersionRestored = async (documentType: string) => {
  if (!store.body.id) return;
  await store.api.resetDocumentAsync(documentType);
};

const saveDocument = async () => {
  await onSubmitAsync();
};
</script>

<template>
  <TitleHeader label="ข้อมูลรายงานสำนักงานการตรวจเงินแผ่นดินและกรมสรรพากร" :routeItems="routeItems">
    <template #action>
      <div class="flex items-center gap-2">
        <p class="text-sm">สถานะ :</p>
        <BadgeComponent :label="BadgeStatus(store.body.status).label" :color="BadgeStatus(store.body.status).color" />
      </div>
      <Button label="ประวัติการใช้งาน" icon="pi pi-refresh" variant="outlined" severity="primary" v-if="store.body.id"
        class="bg-white! hover:bg-red-50!" @click="() => showActivityDialog(store.body.id!)" />
    </template>
  </TitleHeader>

  <Form @submit="onSubmitAsync" @invalidSubmit="ToastHelper.invalidMessageToast()" v-slot="{ handleSubmit }">
    <div class="grid grid-cols-1 lg:grid-cols-7 gap-2">
      <div class="md:col-span-5 ">
        <Tabs :value="currentTab" unstyled @update:value="(tab) => currentTab = tab.toString()">
          <TabHeader :items="HeaderItem.filter((h, i) => id ? h : i === 0)" class="sticky top-14 z-3 pt-2 bg-[#F7F7F7]" />
          <TabPanels>
            <TabPanel value="0">
              <div class="grid grid-rows-1 gap-4">
                <DocumentDetail />
                <ContractList />
              </div>
            </TabPanel>
            <TabPanel value="1">
              <div class="grid grid-rows-1 gap-4">
                <AuditReportDocument :docId="store.body.auditReportDocumentId" :docName="new Date().toISOString()"
                  :readonly="!canEditDocument" ref="auditReportDocumentRef" v-if="store.body.auditReportDocumentId"
                  :key="`${store.body.auditReportDocumentId}-${store.body.status}`"
                  :versions="store.body.auditReportDocumentVersions" :canRestoreVersion="canEditDocument"
                  @restore-version="handleVersionRestored('AuditReport')" :save="saveDocument" />
              </div>
            </TabPanel>
            <TabPanel value="2">
              <div class="grid grid-rows-1 gap-4">
                <AuditGeneralReportDocument :docId="store.body.auditGeneralReportDocumentId"
                  :docName="new Date().toISOString()" :readonly="!canEditDocument" ref="auditGeneralReportDocumentRef"
                  v-if="store.body.auditGeneralReportDocumentId"
                  :key="`${store.body.auditGeneralReportDocumentId}-${store.body.status}`"
                  :versions="store.body.auditGeneralReportDocumentVersions" :canRestoreVersion="canEditDocument"
                  @restore-version="handleVersionRestored('AuditGeneralReport')" :save="saveDocument" />
              </div>
            </TabPanel>
            <TabPanel value="3">
              <div class="grid grid-rows-1 gap-4">
                <RevenueReportDocument :docId="store.body.revenueReportDocumentId" :docName="new Date().toISOString()"
                  :readonly="!canEditDocument" ref="revenueReportDocumentRef" v-if="store.body.revenueReportDocumentId"
                  :key="`${store.body.revenueReportDocumentId}-${store.body.status}`"
                  :versions="store.body.revenueReportDocumentVersions" :canRestoreVersion="canEditDocument"
                  @restore-version="handleVersionRestored('RevenueReport')" :save="saveDocument" />
              </div>
            </TabPanel>
          </TabPanels>
          <UploadFileGroup v-if="store.body.id" v-model="store.body.attachments" @upload="store.api.onUpsertAttachments"
            @remove-file="store.api.onUpsertAttachments" @remove-group="store.api.onUpsertAttachments"
            @reorder="store.api.onUpsertAttachments" :disabled="!store.state.canEdit" />
        </Tabs>
      </div>

      <div class="lg:col-span-2 relative">
        <div class="flex flex-col gap-4 ml-3 sticky top-[100px]">
          <div class="flex items-center justify-end gap-2">
            <ButtonSave v-if="store.state.canEdit" type="submit" />
            <ButtonApproveConfirm v-if="store.state.canEdit" @click="handleSubmit(() => onSendApproveAsync())" />

            <ButtonRecall v-if="store.state.isRecall" @click="onEditAsync()" />
            <ButtonSendEdit v-if="store.state.canApproveReject" @click="store.api.onRejectAsync" />
            <ButtonApprove v-if="store.state.canApproveReject && !store.state.isLastApproval"
              @click="store.api.onApproveAsync" />
            <ButtonConfirm v-if="store.state.canApproveReject && store.state.isLastApproval"
              @click="store.api.onApproveAsync" />
          </div>

          <Accordion :value="['0']" unstyled multiple>
            <AccordionPanel value="999" class="mb-4" v-if="currentTab !== '0'">
              <AccordHeader label="Dictionary" />
              <AccordionContent>
                <div></div>
                <Card class="rounded-none overflow-auto h-[800px]">
                  <template #content>
                    <DocumentMapping pathToGet="contract-amendment/audit-revenue"
                      @on-click-select="
                        (text, hint) =>
                          currentTab === '1' ? auditReportDocumentRef?.setPlaceholderInDocument(text, hint) :
                            currentTab === '2' ? auditGeneralReportDocumentRef?.setPlaceholderInDocument(text, hint) :
                              currentTab === '3' ? revenueReportDocumentRef?.setPlaceholderInDocument(text, hint) : null" />
                  </template>
                </Card>
              </AccordionContent>
            </AccordionPanel>
            <AccordionPanel value="0">
              <AccordAcceptor title="ผู้มีอำนาจเห็นชอบ/อนุมัติ" v-model="store.body.approvalAcceptors"
                :acceptor-type="AcceptorType.Approver" :isManage="store.state.canEdit" isApprove
                :is-disable="!store.state.canEdit" @set-default="() => store.api.getDefaultAcceptor()"
                :is-set-default="true"/>
            </AccordionPanel>
          </Accordion>
        </div>
      </div>
    </div>
  </Form>
</template>