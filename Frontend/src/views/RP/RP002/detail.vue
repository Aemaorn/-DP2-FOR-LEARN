<script setup lang="ts">
import { BadgeStatus } from '@/components';
import { AccordAcceptor } from '@/components/Accordions';
import { ButtonApprove,  ButtonConfirm, ButtonRecall, ButtonSendEdit } from '@/components/Button';
import { TabHeader, TitleHeader } from '@/components/cosmetic';
import DocumentMapping from '@/components/DocumentMapping.vue';
import RP002Constants from '@/constants/RP/rp002';
import { ConfirmDialogType } from '@/enums/dialog';
import { AcceptorType } from '@/enums/participants';
import { RP002Status } from '@/enums/RP/rp002';
import { showActivityDialog, showConfirmDialogAsync, showSaveOptionDialogAsync } from '@/helpers/dialog';
import ToastHelper from '@/helpers/toast';
import type { Option } from '@/models/shared/option';
import { useLoadingStore } from '@/stores/loading';
import { useRP002DetailStore } from '@/stores/RP/RP002/detail';
import { Button, TabPanel, TabPanels, Tabs, } from 'primevue';
import type { MenuItem } from 'primevue/menuitem';
import { Form } from 'vee-validate';
import { computed, defineAsyncComponent, nextTick, onBeforeMount, onMounted, ref, watch } from 'vue';
import { useRoute } from 'vue-router';

const DetailComponent = defineAsyncComponent(
  (): Promise<typeof import('@/views/RP/RP002/components/Detail.vue')> =>
    import('@/views/RP/RP002/components/Detail.vue')
);

const { StatusColorLabel } = RP002Constants;
const route = useRoute();
const store = useRP002DetailStore();

const routeItems = ref([
  { label: 'รายงานสัญญาแล้วเสร็จตามไตรมาส', url: '/rp/rp002' },
  { label: 'ข้อมูลรายงานสัญญาแล้วเสร็จตามไตรมาส' },
] as MenuItem[]);

const HeaderItem = computed((): Option[] => {
  if (store.body.documentId) {
    return [
      { label: 'รายละเอียด', value: 0 },
      { label: 'เอกสารสัญญาแล้วเสร็จตามไตรมาส', value: 1 },
    ];
  }

  return [
    { label: 'รายละเอียด', value: 0 },
  ];
});

const currentTab = ref(0);
const isFormDirty = ref(false);
const isInitialLoad = ref(true);

const Document = defineAsyncComponent(
  (): Promise<typeof import('./components/Document.vue')> => import('./components/Document.vue')
);

const documentRef = ref<InstanceType<typeof Document> | null>(null);
const id = computed((): string | undefined => route.params?.id as string);

const initAsync = async (): Promise<void> => {
  if (id.value) {
    await store.getByIdAsync(id.value);
  } else {
    await store.getDefaultAcceptor();
  }
};

watch(
  () => store.body,
  () => {
    if (!!id.value && !isInitialLoad.value) {
      isFormDirty.value = true;
    }
  },
  { deep: true }
);

const resetFormState = async (): Promise<void> => {
  isFormDirty.value = false;
  await nextTick();
  isInitialLoad.value = false;
};

const canShowSaveOptionDialog = (): boolean => {
  return isFormDirty.value
    && !!store.body.documentId
    && [RP002Status.Draft, RP002Status.Edit, RP002Status.Rejected].includes(store.body.status);
};

const submitOrCreateAsync = async (status?: RP002Status): Promise<void> => {
  isInitialLoad.value = true;

  if (id.value) {
    await store.updateAsync(id.value, status);
  } else {
    await store.createAsync(status);
  }

  await resetFormState();
};

const onSaveDocumentFirst = async () => {
  const { setIsLoading } = useLoadingStore();
  try {
    if (documentRef.value && store.body.documentId) {
      setIsLoading(true);
      await documentRef.value.saveDocumentFirst();
    }
  }
  finally {
    setIsLoading(false);
  }
};

const onSubmitAsync = async (): Promise<void> => {
  await onSaveDocumentFirst();

  if (id.value && canShowSaveOptionDialog()) {
    const saveOption = await showSaveOptionDialogAsync();
    if (saveOption === undefined) return;
    store.body.isDocumentReplace = saveOption;
  }

  await submitOrCreateAsync();
};

const onSendApproveAsync = async (): Promise<void> => {
  await onSaveDocumentFirst();

  if (!await showConfirmDialogAsync(ConfirmDialogType.SendApprove)) return;

  if (!store.body.detail || store.body.detail.length === 0) return ToastHelper.error('ข้อมูลไม่ถูกต้อง', 'กรุณาเลือกรายการสัญญา');

  if (!store.body.acceptors || store.body.acceptors.length === 0) return ToastHelper.approvalAtLeastMessageToast();

  store.body.isDocumentReplace = false;
  await submitOrCreateAsync(RP002Status.WaitingApproval);
};

const onRecallAsync = async (): Promise<void> => {
  if (!await showConfirmDialogAsync(ConfirmDialogType.Edit)) return;

  if (id.value) {
    await store.updateAsync(id.value, RP002Status.Edit);
  }
};

onBeforeMount((): void => {
  store.onResetData();
});

onMounted(async (): Promise<void> => {
  await initAsync();
  setTimeout(() => {
    isInitialLoad.value = false;
  }, 500);
});
</script>

<template>
  <Form @submit="onSubmitAsync" @invalidSubmit="ToastHelper.invalidMessageToast()" v-slot="{ handleSubmit }">
    <TitleHeader label="ข้อมูลสัญญาแล้วเสร็จตามไตรมาส" :routeItems>
      <template #action>
        <div class="flex items-center gap-2" v-if="store.body.status">
          <p class="text-sm">สถานะ :</p>
          <BadgeStatus :color="StatusColorLabel(store.body.status).color"
            :label="StatusColorLabel(store.body.status).label" />
        </div>
        <Button label="ประวัติการใช้งาน" icon="pi pi-refresh" variant="outlined" severity="primary"
          class="bg-white! hover:bg-red-50!" @click="() => showActivityDialog(store.body.id!)" />
      </template>
    </TitleHeader>
    <div class="mt-4">
      <div class="grid grid-cols-1 lg:grid-cols-7 gap-2">
        <div class="lg:col-span-5">
          <Tabs :value="currentTab" unstyled @update:value="(tab) => currentTab = Number(tab)">
            <TabHeader :items="HeaderItem.filter((h, i) => id ? h : i === 0)" class="sticky top-14 z-3 pt-2 bg-[#F7F7F7]" />
            <TabPanels>
              <TabPanel :value="0">
                <DetailComponent />
              </TabPanel>
              <TabPanel :value="1">
                <Card>
                  <template #content>
                    <Document v-model="store.body.documentId" :readonly="!store.status.canEdit" ref="documentRef" />
                  </template>
                </Card>
              </TabPanel>
              <div class="mt-4">
                <UploadFileGroup v-if="store.body.id" v-model="store.body.attachments"
                  @upload="store.onUpsertAttachments" @remove-file="store.onUpsertAttachments"
                  @remove-group="store.onUpsertAttachments" @reorder="store.onUpsertAttachments"
                  :disabled="!store.status.canEdit" />
              </div>
            </TabPanels>
          </Tabs>
        </div>

        <div class="relative lg:col-span-2">
          <div class="flex flex-col gap-4 ml-3 sticky top-[100px]">
            <div class="flex items-center gap-2 justify-end" v-if="store.status.canEdit">
              <Button label="บันทึก" icon="pi pi-save" severity="success" type="submit" />
              <Button label="ส่งเห็นชอบ/อนุมัติ" icon="pi pi-check" severity="warn"
                @click="handleSubmit(onSendApproveAsync)" />
            </div>
            <div class="flex items-center gap-2 justify-end">
              <ButtonRecall @click="onRecallAsync" v-if="store.status.canRecall" />
              <ButtonSendEdit @click="store.rejectAsync" v-if="store.status.canApproveReject" />
              <ButtonApprove @click="store.approveAsync" v-if="store.status.canApproveReject && !store.status.isLastApproval" />
              <ButtonConfirm v-if="store.status.canApproveReject && store.status.isLastApproval" @click="store.approveAsync" />
            </div>
            <Accordion :value="['0']" unstyled multiple>
              <AccordionPanel value="999" class="mb-4" v-if="currentTab !== 0">
                <AccordHeader label="Dictionary" />
                <AccordionContent>
                  <div></div>
                  <Card class="rounded-none overflow-auto h-[800px]">
                    <template #content>
                      <DocumentMapping pathToGet="report/contract-completion-by-quarter" @on-click-select="
                        (text, hint) => documentRef?.setPlaceholderInDocument(text, hint)" />
                    </template>
                  </Card>
                </AccordionContent>
              </AccordionPanel>
              <AccordionPanel value="0">
                <AccordAcceptor v-if="store.body.acceptors" title="ผู้มีอำนาจเห็นชอบ/อนุมัติ"
                  v-model="store.body.acceptors" :acceptor-type="AcceptorType.Approver"
                  :isManage="store.status.canEdit" isApprove :is-disable="!store.status.canEdit"
                  @set-default="() => store.getDefaultAcceptor()" :is-set-default="true" />
              </AccordionPanel>
            </Accordion>
          </div>
        </div>
      </div>
    </div>
  </Form>
</template>
