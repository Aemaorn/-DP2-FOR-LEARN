<script setup lang="ts">
import type { Option } from '@/models/shared/option';
import { Tabs, TabPanels, TabPanel, Button } from 'primevue';
import { TitleHeader, TabHeader } from '@/components/cosmetic';
import {
  ButtonSave,
  ButtonSendApprove,
  ButtonApprove,
  ButtonSendEdit,
  ButtonRecall,
  ButtonConfirm,
  ButtonNotAgree,
} from '@/components/Button';
import { computed, defineAsyncComponent, nextTick, onBeforeMount, ref, watch } from 'vue';
import { Form } from 'vee-validate';
import { usePP006DetailStore } from '../../stores/PP006/PP006Store';
import { PP006Status } from '../../enums/pp006';
import { showActivityDialog, showConfirmDialogAsync, showSaveOptionDialogAsync } from '@/helpers/dialog';
import { BadgeStatus } from '@/components';
import { AccordAcceptor } from '@/components/Accordions';
import { AcceptorType } from '@/enums/participants';
import { ConfirmDialogType } from '@/enums/dialog';
import { useMenuStore } from '@/stores/menu';
import ToastHelper from '@/helpers/toast';
import inviteConstant from '@/constants/invite';
import DocumentMapping from '@/components/DocumentMapping.vue';

const props = defineProps({
  procurementId: {
    type: String,
    required: true,
  },
  inviteId: { type: String, default: null },
  readonly: {
    type: Boolean,
    default: false,
  },
});

const DetailComponent = defineAsyncComponent(() => import('@/views/PP/components/PP006/components/PP006Detail.vue'));
const EtrepreneurDocument = defineAsyncComponent(() => import('./components/sub/EntrepreneurDocument.vue'));

const documentRefs = ref<Map<string, InstanceType<typeof EtrepreneurDocument>>>(new Map());

const setDocumentRef = (entrepreneurId: string, el: InstanceType<typeof EtrepreneurDocument> | null) => {
  if (el) {
    documentRefs.value.set(entrepreneurId, el);
  } else {
    documentRefs.value.delete(entrepreneurId);
  }
};

const entrepreneursWithDocuments = computed(() => {
  return store.detail.invitedEntrepreneurs?.filter(e => e.documentId) || [];
});

const setPlaceholderInAllDocuments = (text: string, hint?: string) => {
  documentRefs.value.forEach(ref => ref?.setPlaceholderInDocument(text, hint));
};

const menuStore = useMenuStore();
const store = usePP006DetailStore();
const { inviteStatusColor } = inviteConstant;

const currentTab = ref("0");
const isFormDirty = ref(false);
const isInitialized = ref(false);

const hasEntrepreneurs = computed(() => {
  return (store.detail.invitedEntrepreneurs?.length ?? 0) > 0;
});

const HeaderItem = computed<Option[]>(() => {
  if (!store.detail.isInvite || !hasEntrepreneurs.value) {
    return [{ label: 'รายละเอียด', value: '0' }];
  }

  return [
    { label: 'รายละเอียด', value: '0' },
    { label: 'เอกสารรายงานหนังสือเชิญชวนผู้ประกอบการ', value: '1' },
  ];
});

onBeforeMount(() => {
  onInitAsync();
});

const onInitAsync = async (): Promise<void> => {
  await store.getByIdAsync(props.inviteId ?? undefined);
  await nextTick();
  isInitialized.value = true;
};

const saveDocument = async () => {
  await onSubmitAsync();
};

const saveAllDocuments = async () => {
  const savePromises: Promise<void>[] = [];
  documentRefs.value.forEach((docRef) => {
    if (docRef?.saveDocumentFirst) {
      savePromises.push(docRef.saveDocumentFirst());
    }
  });
  await Promise.all(savePromises);
};

const onSubmitAsync = async (): Promise<void> => {
  store.detail.procurementId = props.procurementId;

  await saveAllDocuments();

  if (props.inviteId || store.detail.id) {
    const id = props.inviteId ?? store.detail.id;

    if (isFormDirty.value
      && entrepreneursWithDocuments.value.length > 0) {
      const saveOption = await showSaveOptionDialogAsync();
      if (saveOption === undefined) return;
        store.detail.isDocumentReplace = saveOption;
    }

    isInitialized.value = false;
    await store.updateAsync(id, PP006Status.Draft);
    isFormDirty.value = false;
    await nextTick();
    isInitialized.value = true;
    setCurrentTab();
    return;
  }

  isInitialized.value = false;
  await store.createAsync(PP006Status.Draft);
  isFormDirty.value = false;
  await nextTick();
  isInitialized.value = true;
  setCurrentTab();
};

const setCurrentTab = async () => {
  const hasAnyDocument = entrepreneursWithDocuments.value.length > 0;
  const hasAnyNewDocument = entrepreneursWithDocuments.value.some(e => !e.isDocumentReplace);
  if (store.detail.isInvite && hasAnyDocument && hasAnyNewDocument) {
    currentTab.value = '1';
  }
};

const onSendApproveAsync = async (): Promise<void> => {
  if (store.detail.acceptors.length <= 0) {
    return ToastHelper.committeeAtLeastMessageToast();
  }

  if (!await showConfirmDialogAsync(ConfirmDialogType.SendApproveConfirm)) return;

  store.detail.procurementId = props.procurementId;
  await saveAllDocuments();

  isInitialized.value = false;

  if (props.inviteId || store.detail.id) {
    const id = props.inviteId ?? store.detail.id;
    await store.updateAsync(id, PP006Status.Draft, true);
    await store.updateAsync(id, PP006Status.WaitingApproval);
  } else {
    await store.createAsync(PP006Status.Draft);
    if (store.detail.id) {
      await store.updateAsync(store.detail.id, PP006Status.WaitingApproval);
    }
  }

  isFormDirty.value = false;
  await nextTick();
  isInitialized.value = true;
};

const onRecallAsync = async (): Promise<void> => {
  if (!await showConfirmDialogAsync(ConfirmDialogType.Edit)) return;
  await store.updateAsync(props.inviteId, PP006Status.Edit);
};

const canEditDocument = computed(() => {
  return store.detail.status == PP006Status.Draft
    || store.detail.status == PP006Status.Edit
    || store.detail.status == PP006Status.Rejected
});

const defaultDocumentStatuses = [
  PP006Status.WaitingApproval,
  PP006Status.Approved,
];

watch(() =>
  store.detail.status,
  (newStatus: PP006Status) => {
    if (defaultDocumentStatuses.includes(newStatus) && store.detail.isInvite) {
      currentTab.value = '1';
    }
  }, { immediate: true });

watch(
  HeaderItem,
  (items) => {
    const hasDocumentTab = items.some(item => item.value === '1');
    if (!hasDocumentTab) {
      currentTab.value = '0';
    }
  },
  { immediate: true }
);

watch(
  () => store.detail,
  () => {
    if (isInitialized.value) {
      isFormDirty.value = true;
    }
  },
  { deep: true }
);

const handleVersionRestored = async () => {
  const id = props.inviteId ?? store.detail.id;
  if (id) {
    await store.getByIdAsync(id);
  }
};
</script>

<template>
  <Form @submit="() => onSubmitAsync()" @invalidSubmit="ToastHelper.invalidMessageToast()" v-slot="{ handleSubmit }">
    <div class="mt-4">
      <div class="my-2">
        <TitleHeader label="จัดทำหนังสือเชิญชวนผู้ประกอบการ">
          <template #action>
            <BadgeStatus :color="inviteStatusColor(store.detail.status, store.status.isSixtyMorethan100k).color"
              :label="inviteStatusColor(store.detail.status, store.status.isSixtyMorethan100k).label"
              v-if="store.detail.status" />
            <Button label="ประวัติการใช้งาน" icon="pi pi-refresh" variant="outlined" severity="primary"
              v-if="store.detail.id" class="bg-white! hover:bg-red-50!"
              @click="() => showActivityDialog(store.detail.id!)" />
          </template>
        </TitleHeader>
      </div>
      <div class="grid grid-cols-1 lg:grid-cols-7 gap-2">
        <div class="lg:col-span-5">
          <div>
            <Tabs :value="currentTab" unstyled @update:value="(tab) => currentTab = tab.toString()">
              <TabHeader :items="HeaderItem" class="sticky top-14 z-3 pt-2 bg-[#F7F7F7]" />
              <TabPanels>
                <TabPanel value="0">
                  <DetailComponent :readonly="props.readonly" />
                </TabPanel>
                <TabPanel value="1">
                  <div v-if="entrepreneursWithDocuments.length > 0" class="entrepreneur-documents-container">
                    <template v-for="entrepreneur in entrepreneursWithDocuments" :key="entrepreneur.id">
                      <EtrepreneurDocument v-model="entrepreneur.documentId" v-if="entrepreneur.documentId"
                        :readonly="!store.status.canEdit || !menuStore.hasManage || !canEditDocument"
                        :ref="(el: any) => setDocumentRef(entrepreneur.id!, el)"
                        :versions="entrepreneur.documentVersions" :procurement-id="props.procurementId"
                        :invite-id="store.detail.id" :entrepreneur-id="entrepreneur.id"
                        :entrepreneur-name="entrepreneur.entrepreneurName"
                        :can-restore-version="store.status.canEdit && menuStore.hasManage && canEditDocument"
                        :save="saveDocument" @version-restored="handleVersionRestored" />
                    </template>
                  </div>
                  <div v-else class="no-documents-message">
                    <span class="material-symbols-outlined">description</span>
                    <p>ยังไม่มีเอกสาร กรุณาเพิ่มผู้ประกอบการและบันทึกข้อมูลเพื่อสร้างเอกสาร</p>
                  </div>
                </TabPanel>
              </TabPanels>
            </Tabs>
          </div>
        </div>

        <div class="relative lg:col-span-2">
          <div class="flex flex-col gap-4 ml-3 sticky top-[100px]">
            <div class="flex items-center gap-2 justify-end" v-if="menuStore.hasManage && !readonly">
              <div class="flex flex-wrap gap-2" v-if="store.detail.isInvite">
                <ButtonSave type="submit" v-if="store.status.canEdit" />

                <ButtonSendApprove @click="() => handleSubmit(onSendApproveAsync)" v-if="store.status.canEdit" />

                <ButtonRecall v-if="store.status.canRecall" @click="onRecallAsync" />

                <ButtonNotAgree v-if="store.status.canAcceptAndRejectCommittee"
                  @click="() => store.onRejectAsync(true)" />
                <ButtonApprove v-if="store.status.canAcceptAndRejectCommittee" @click="store.onApproveAsync" />

                <ButtonSendEdit v-if="store.status.canAcceptAndRejectApprover" @click="store.onRejectAsync" />
                <ButtonApprove v-if="store.status.canAcceptAndRejectApprover && !store.status.isLastApprovalApprover"
                  @click="store.onApproveAsync" />
                <ButtonConfirm v-if="store.status.canAcceptAndRejectApprover && store.status.isLastApprovalApprover"
                  @click="store.onApproveAsync" />
              </div>

              <div v-if="!store.detail.isInvite">
                <ButtonConfirm v-if="store.status.canEdit" label="ยืนยันไม่เชิญชวน และดำเนินการ จพ.006" severity="warn"
                  @click="() => handleSubmit(store.onNotInviteAsync)" />
              </div>
            </div>
            <Accordion :value="['0', '1']" unstyled multiple>
              <AccordionPanel value="999" class="mb-4" v-if="currentTab !== '0'">
                <AccordHeader label="Dictionary" />
                <AccordionContent>
                  <div></div>
                  <Card class="rounded-none overflow-auto h-[800px]">
                    <template #content>
                      <DocumentMapping pathToGet="procurement/invite" @on-click-select="setPlaceholderInAllDocuments" />
                    </template>
                  </Card>
                </AccordionContent>
              </AccordionPanel>
              <AccordionPanel value="0" v-if="!store.status.isSixtyMorethan100k">
                <AccordAcceptor title="ผู้จัดซื้อจัดจ้าง/คณะกรรมการจัดซื้อจัดจ้าง" v-model="store.detail.acceptors"
                  :acceptor-type="AcceptorType.ProcurementCommittee" v-if="store.detail.acceptors"
                  :is-disable="![PP006Status.Draft, PP006Status.Rejected, PP006Status.Edit, PP006Status.WaitingApproval].includes(store.detail.status) || !menuStore.hasManage || !store.detail.id || readonly"
                  isApprove
                  @set-is-unable-to-perform-duties="(status: boolean, id: string, remark?: string) => handleSubmit(() => store.onUpdateDutieStatusAsync(id, status, remark))" />
              </AccordionPanel>
              <AccordionPanel value="1" class="mt-5" v-if="store.status.isSixtyMorethan100k">
                <AccordAcceptor title="หัวหน้าส่วน" v-model="store.detail.acceptors"
                  @set-default="() => store.setDefaultApproverAsync()" isApprove :acceptor-type="AcceptorType.Approver"
                  v-if="store.detail.acceptors" isShowCheckBoxAll isManage
                  :is-disable="![PP006Status.Draft, PP006Status.Rejected, PP006Status.Edit].includes(store.detail.status) || !menuStore.hasManage || readonly"
                  :is-set-default="store.status.isCanSetDefault" />
              </AccordionPanel>
            </Accordion>
          </div>
        </div>
      </div>
    </div>
  </Form>
</template>

<style scoped>
.entrepreneur-documents-container {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.no-documents-message {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 48px;
  color: #64748b;
  background: #f8fafc;
  border: 1px dashed #cbd5e1;
  border-radius: 8px;
}

.no-documents-message .material-symbols-outlined {
  font-size: 48px;
  margin-bottom: 12px;
  color: #94a3b8;
}

.no-documents-message p {
  margin: 0;
  font-size: 14px;
}
</style>
