<script setup lang="ts">
import type { Option } from '@/models/shared/option';
import { Tabs, TabPanels, TabPanel, Button } from 'primevue';
import { TitleHeader, TabHeader, CardSelect } from '@/components/cosmetic';
import { computed, defineAsyncComponent, nextTick, onMounted, ref, watch } from 'vue';
import DateSignDialog from './components/Sub/DateSignDialog.vue';
import { useContractDraftStore } from '../../stores/PP0010/ContractDraft';
import { usePPDetailStore } from '../../../../stores/PP/ppStore';
import { AcceptorType } from '@/enums/participants';
import { AccordAcceptor } from '@/components/Accordions';
import { Form } from 'vee-validate';
import { ButtonSave, ButtonSendEdit } from '@/components/Button';
import { TContractDraftStatus, TPaymentBaseType } from '../../enums/pp010';
import { showActivityDialog, showConfirmDialogAsync, showSaveOptionDialogAsync } from '@/helpers/dialog';
import { ConfirmDialogType } from '@/enums/dialog';
import ContractDraftHelper from '@/helpers/contractDraft';
import { BadgeStatus as BadgeComponent } from '@/components';
import ToastHelper from '@/helpers/toast';
import DocumentMapping from '@/components/DocumentMapping.vue';
import DialogReviewDocument from '@/components/Document/DialogReviewDocument.vue';
import { useMenuStore } from '@/stores/menu';
import ButtonRecall from '@/components/Button/ButtonRecall.vue';
import contractDraftService from '../../services/PP010/ContractDraftService';
import { HttpStatusCode } from 'axios';
import { useRoute } from 'vue-router';
import SectionNavigator from './components/SectionNavigator.vue';
import { useSectionSpy } from './composables/useSectionSpy';
import { useLoadingStore } from '@/stores/loading';

const props = defineProps({ readonly: { type: Boolean, default: false } });

const route = useRoute();
const menuStore = useMenuStore();
const procurementStore = usePPDetailStore();
const PP010ContractInfo = defineAsyncComponent(
  (): Promise<typeof import('./components/PP010ContractInfo.vue')> => import('./components/PP010ContractInfo.vue')
);
const PP010Detail = defineAsyncComponent(
  (): Promise<typeof import('./components/PP010Detail.vue')> => import('./components/PP010Detail.vue')
);
const PP010ContractDraftDocument = defineAsyncComponent(
  (): Promise<typeof import('./components/PP010ContractDraftDocument.vue')> => import('./components/PP010ContractDraftDocument.vue')
);
const PP010ContractApproveDocument = defineAsyncComponent(
  (): Promise<typeof import('./components/PP010ContractApproveDocument.vue')> => import('./components/PP010ContractApproveDocument.vue')
);
const PP010ContractConfidentialDocument = defineAsyncComponent(
  (): Promise<typeof import('./components/PP010ContractConfidentialDocument.vue')> => import('./components/PP010ContractConfidentialDocument.vue')
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

const store = useContractDraftStore();
const { BadgeStatus } = ContractDraftHelper;

const showDateSignModal = ref(false);

const onSaveDateSign = async (date: Date): Promise<void> => {
  store.body.contractSignedDate = date;
  await store.api.onUpdateContractDraft();
};

const checkShowDateSignModal = (): void => {
  if (store.states.canSaveDateSign && !store.body.contractSignedDate) {
    showDateSignModal.value = true;
  }
};

const currentTab = ref('0');
const detailContainerRef = ref<HTMLElement | null>(null);
const { sections, activeSectionId, scrollToSection, reinit } = useSectionSpy(detailContainerRef);

const savedScrollPositions: Record<string, number> = {};

watch(currentTab, async (newTab, oldTab) => {
  if (oldTab !== undefined) {
    savedScrollPositions[oldTab] = window.scrollY;
  }

  if (newTab === '1') {
    reinit();
  }

  await nextTick();

  if (savedScrollPositions[newTab] !== undefined) {
    window.scrollTo(0, savedScrollPositions[newTab]);
  }
});

const HeaderItem = ref([
  { label: 'ข้อมูลสัญญา', value: '0' },
  { label: 'รายละเอียด', value: '1' },
  { label: 'เอกสารร่างสัญญาหรือข้อตกลง', value: '2' },
  { label: 'เอกสารสัญญารักษาความลับ', value: '3' },
  { label: 'เอกสารลงนามสัญญา', value: '4' },
] as Option[]);
const showReviewDocumentDialog = ref(false);
const reviwDocumentId = ref<string>('');
const selectKey = ref(0);
let stopWatcher: (() => void) | null = null
const isDirty = ref(false);

const getContractDraftByVendorId = async (vendorId: string) => {
  const isDeepEqual = (a: any, b: any) => JSON.stringify(a) === JSON.stringify(b);

  const isChanged = !isDeepEqual(store.body, store.cloneBody);

  if (isChanged) {
    const confirmed = await showConfirmDialogAsync(ConfirmDialogType.ConfirmData);

    if (store.vendorId && !confirmed) {
      selectKey.value++;
      return;
    }
  }

  store.dropdown.subTemplateTypeOptions = [];
  store.vendorId = vendorId;
  currentTab.value = '0';
  selectKey.value = 0;
  store.onClearBody();

  await store.api.getContractDraftByVendorIdAsync();

  if (store.body.template) {
    await store.api.getTemplateTypeAsync(store.body.contractType);
    const findItem = store.dropdown.templateTypeOptions.find(f => f.value === store.body.template);
    if (findItem?.id && typeof findItem.id === 'string') {
      await store.api.getSubTemplateTypeAsync(findItem.id);
      const templateIdToUse = store.body.subTemplate
        ? store.dropdown.subTemplateTypeOptions.find(f => f.value === store.body.subTemplate)?.id
        : findItem.id;
      if (templateIdToUse && typeof templateIdToUse === 'string') {
        await store.api.getAttacementTypeAsync(templateIdToUse);
      }
    }
  }
};

const saveDocumentFirst = async (): Promise<void> => {
  if (currentTab.value === '2' && pp010ContractDraftDocumentRef.value && 'saveDocumentFirst' in pp010ContractDraftDocumentRef.value) {
    await pp010ContractDraftDocumentRef.value.saveDocumentFirst();
  } else if (currentTab.value === '3' && pp010ContractConfidentialDocumentRef.value && 'saveDocumentFirst' in pp010ContractConfidentialDocumentRef.value) {
    await pp010ContractConfidentialDocumentRef.value.saveDocumentFirst();
  } else if (currentTab.value === '4' && pp010ContractApproveDocumentRef.value && 'saveDocumentFirst' in pp010ContractApproveDocumentRef.value) {
    await pp010ContractApproveDocumentRef.value.saveDocumentFirst();
  }
};

const saveDocument = async () => {
  await saveDocumentFirst();
  await onSubmitAsync();
};

const handleRestoreContractDraftVersion = async (): Promise<void> => {
  const contractDraftId = procurementStore.procurementDetail.contractDraft?.id;
  const procurementId = procurementStore.procurementDetail.id;
  if (!contractDraftId || !procurementId || !store.vendorId) {
    ToastHelper.error('เกิดข้อผิดพลาด', 'ไม่สามารถรีเซ็ตเอกสารได้ ข้อมูลไม่ครบถ้วน');
    return;
  }
  const { status } = await contractDraftService.resetDocumentAsync(procurementId, contractDraftId, store.vendorId, 'ContractDraft');
  if (status === HttpStatusCode.Ok) {
    ToastHelper.success('สำเร็จ', 'ทำการรีเซ็ตเอกสารสำเร็จ');
    await store.api.getContractDraftByVendorIdAsync();
  }
};

const handleRestoreApprovalVersion = async (): Promise<void> => {
  const contractDraftId = procurementStore.procurementDetail.contractDraft?.id;
  const procurementId = procurementStore.procurementDetail.id;
  if (!contractDraftId || !procurementId || !store.vendorId) {
    ToastHelper.error('เกิดข้อผิดพลาด', 'ไม่สามารถรีเซ็ตเอกสารได้ ข้อมูลไม่ครบถ้วน');
    return;
  }
  const { status } = await contractDraftService.resetDocumentAsync(procurementId, contractDraftId, store.vendorId, 'ApprovalContractDraft');
  if (status === HttpStatusCode.Ok) {
    ToastHelper.success('สำเร็จ', 'ทำการรีเซ็ตเอกสารสำเร็จ');
    await store.api.getContractDraftByVendorIdAsync();
  }
};

const handleRestoreConfidentialVersion = async (): Promise<void> => {
  const contractDraftId = procurementStore.procurementDetail.contractDraft?.id;
  const procurementId = procurementStore.procurementDetail.id;
  if (!contractDraftId || !procurementId || !store.vendorId) {
    ToastHelper.error('เกิดข้อผิดพลาด', 'ไม่สามารถรีเซ็ตเอกสารได้ ข้อมูลไม่ครบถ้วน');
    return;
  }
  const { status } = await contractDraftService.resetDocumentAsync(procurementId, contractDraftId, store.vendorId, 'ConfidentialContractDraft');
  if (status === HttpStatusCode.Ok) {
    ToastHelper.success('สำเร็จ', 'ทำการรีเซ็ตเอกสารสำเร็จ');
    await store.api.getContractDraftByVendorIdAsync();
  }
};

/**
 * ใช้ในกรณีที่ Update ข้อมูลครั้งแรกเนื่องจากไม่มีเอกสารและ
 */
const onCreateDocumentation = () => {
  if (store.body.contractDraftDocumentId) {
    return;
  }

  !store.body.contractDraftDocumentId && (store.body.isContractDraftDocumentIdReplace = true);
  !store.body.confidentialContractDraftDocumentId && (store.body.isConfidentialContractDraftDocumentIdReplace = true);
  !store.body.approvalContractDraftDocumentId && (store.body.isApprovalContractDraftDocumentIdReplace = true);
}

/**
 * บันทึกเอกสารก่อนทำการบันทึกข้อมูล
 */
const onSaveDocumentation = async () => {
  const loading = useLoadingStore();
  loading.setIsLoading(true);
  try {
    pp010ContractDraftDocumentRef.value && await pp010ContractDraftDocumentRef.value.saveDocumentFirst();
    pp010ContractConfidentialDocumentRef.value && await pp010ContractConfidentialDocumentRef.value.saveDocumentFirst();
    pp010ContractApproveDocumentRef.value && await pp010ContractApproveDocumentRef.value.saveDocumentFirst();
  } finally {
    loading.setIsLoading(false);
  }
};

/**
 * ใช้กรณีบันทึกเอกสารและขึ้น Dialog เพื่อให้ผูใช้งานเลือกว่าจะปรับ Template หรือแค่บันทึก Form
 */
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
}

const onSubmitAsync = async () => {
  if (!store.vendorId) return;

  if (store.body.status === TContractDraftStatus.Approved && store.states.canSaveDateSign && !store.body.contractSignedDate) {
    ToastHelper.error('ข้อมูลไม่ถูกต้อง', 'กรุณาระบุวันที่ลงนามในสัญญา');
    return;
  }

  stopTrackingBody();

  onCreateDocumentation();
  await onSaveDocumentation();

  if (
    store.body.detail.payment &&
    store.body.detail.payment.details &&
    [TPaymentBaseType.Term].includes(
      store.body.detail.payment.type
    ) &&
    store.body.detail.payment.details.length < 1
  ) {
    ToastHelper.error('ข้อมูลไม่ถูกต้อง', 'กรุณาระบุข้อมูลรายการชำระเงินอย่างน้อย 1 รายการ');

    return;
  }

  if (store.body.status !== TContractDraftStatus.Approved) {
    if (!await onTriggerSaveOptionDialog()) return
  }

  await store.api.onUpdateContractDraft(false);

  if (store.body.contractDraftDocumentId) {
    currentTab.value = '2';
  }

  restoreState();
};

const onSaveDraftAsync = async () => {
  if (!store.vendorId) return;
  stopTrackingBody();

  await onSaveDocumentation();

  if (store.body.status !== TContractDraftStatus.Approved) {
    if (!await onTriggerSaveOptionDialog()) return
  }

  await store.api.onUpdateContractDraft(true);

  restoreState();
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

const updateStatusPending = async () => {
  stopTrackingBody();
  if (!store.vendorId) return;

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

    return false;
  }

  if (
    store.body.detail.payment &&
    store.body.detail.payment.details &&
    [TPaymentBaseType.Term].includes(
      store.body.detail.payment.type
    ) &&
    store.body.detail.payment.details.length < 1
  ) {
    ToastHelper.error('ข้อมูลไม่ถูกต้อง', 'กรุณาระบุข้อมูลรายการชำระเงินอย่างน้อย 1 รายการ');

    return;
  }

  if (!(await showConfirmDialogAsync(ConfirmDialogType.SendApproveConfirm))) return;
  restoreState();

  await store.api.onUpdateContractDraft(false, TContractDraftStatus.Pending);
};

const updateStatusRecall = async () => {
  if (!store.vendorId) return;

  if (
    store.body.detail.payment &&
    store.body.detail.payment.details &&
    [TPaymentBaseType.Term].includes(
      store.body.detail.payment.type
    ) &&
    store.body.detail.payment.details.length < 1
  ) {
    ToastHelper.error('ข้อมูลไม่ถูกต้อง', 'กรุณาระบุข้อมูลรายการชำระเงินอย่างน้อย 1 รายการ');

    return;
  }

  if (!(await showConfirmDialogAsync(ConfirmDialogType.Edit))) return;

  await store.api.onUpdateContractDraft(false, TContractDraftStatus.Edit);
}

const visibleTabs = computed(() => {
  return HeaderItem.value.filter((item: Option) => {
    switch (item.value) {
      case '0':
      case '1':
        return true;
      case '2':
        return !!store.body.contractDraftDocumentId;
      case '3':
        return !!store.body.confidentialContractDraftDocumentId;
      case '4':
        return !!store.body.approvalContractDraftDocumentId && store.body.status === TContractDraftStatus.Approved;
      default:
        return false;
    }
  });
});

onMounted(async () => {
  const vendorIdParam = route.query.vendorId as string | undefined;

  await Promise.all([
    store.api.getVendorListAsync(vendorIdParam),
    store.api.getPeriodTypeAsync(),
    store.api.getPeriodConditionTypeAsync(),
    store.api.getRCCRTypeAsync(),
    store.api.getFineTypeAsync(),
    store.api.getPayTypeAsync(),
    store.api.getUnitTypeAsync(),
    store.api.getVatTypeAsync(),
    store.api.getUnitMeaTypeAsync(),
    store.api.getConditionTypeOptions(),
    store.api.getPeriodAsync(),
  ]);

  restoreState();
  trackingBody();
  checkShowDateSignModal();
});

watch(() => store.body.status, () => checkShowDateSignModal());


const setDocumentReviewId = (id: string): void => {
  switch (currentTab.value) {
    case '2':
      store.body.contractDraftDocumentId = id;
      store.body.isContractDraftDocumentIdReplace = true;
      break;
    case '3':
      store.body.confidentialContractDraftDocumentId = id;
      store.body.isConfidentialContractDraftDocumentIdReplace = true;
      break;
    case '4':
      store.body.approvalContractDraftDocumentId = id;
      store.body.isApprovalContractDraftDocumentIdReplace = true;
      break;
  }
};

const canEditDocument = computed(() => {
  return store.body.status == TContractDraftStatus.Approved;
});

watch(() =>
  store.body.status,
  (newStatus: TContractDraftStatus) => {
    if (newStatus === TContractDraftStatus.Pending) {
      currentTab.value = '2';
    }
    else if (newStatus === TContractDraftStatus.Approved) {
      currentTab.value = '4';
    }
  }, { immediate: true });

const trackingBody = () => {
  if (stopWatcher) return;


  stopWatcher = watch(() =>
    store.body, () => {
      isDirty.value = true;
    }, { deep: true });
};

const stopTrackingBody = () => {
  stopWatcher?.()
  stopWatcher = null
}
</script>

<template>
  <div class="mt-4">
    <div class="my-2">
      <TitleHeader label="ข้อมูลสัญญา">
        <template #action v-if="store.body.contractStatus">
          <BadgeComponent :label="BadgeStatus(store.body.contractStatus).label"
            :color="BadgeStatus(store.body.contractStatus).color" />
        </template>
      </TitleHeader>
    </div>
    <Form @submit="onSubmitAsync()" @invalidSubmit="onInvalidSubmit" v-slot="{ handleSubmit }">
      <div class="flex items-start justify-between bg-[#F7F7F7] ">
        <div class="max-w-[65dvw]">
          <CardSelect v-if="store.vendorList.length > 1" :key="selectKey" :items="store.vendorList"
            :value="store.vendorId" @select="(e) => getContractDraftByVendorId(e.toString())">
            <template #badge="{ item }">
              <BadgeComponent v-if="store.body.status" :label="BadgeStatus(item.status as any).label"
                :color="BadgeStatus(item.status as any).color" size="sm" />
            </template>
          </CardSelect>
        </div>

        <div v-if="store.vendorId" class="flex items-center justify-end gap-2 pt-3">
          <Button label="ประวัติการใช้งาน" icon="pi pi-refresh" variant="outlined" severity="primary"
            v-if="store.body.id" class="bg-white! hover:bg-red-50!"
            @click="() => showActivityDialog(store.vendorId!)" />
        </div>
      </div>

      <div v-if="store.vendorId" class="grid grid-cols-1 lg:grid-cols-7 gap-2">
        <div class="lg:col-span-5">
          <Tabs :value="currentTab" unstyled @update:value="(tab) => currentTab = tab.toString()">
            <TabHeader :items="visibleTabs" class="sticky pt-2 bg-white z-40 top-14" />
            <TabPanels>
              <TabPanel value="0">
                <PP010ContractInfo :readonly="props.readonly" />
              </TabPanel>
              <TabPanel value="1">
                <div ref="detailContainerRef" class="grid grid-rows-1 gap-4">
                  <PP010Detail :readonly="props.readonly" />
                </div>
              </TabPanel>
              <TabPanel v-if="store.body.contractDraftDocumentId" value="2">
                <PP010ContractDraftDocument :readonly="props.readonly" ref="pp010ContractDraftDocumentRef" :save="saveDocument"
                  :versions="store.body.contractDraftDocumentVersions"
                  :canRestoreVersion="!store.states.canEditDoc && menuStore.hasManage && !canEditDocument"
                  @restore-version="handleRestoreContractDraftVersion" />
              </TabPanel>
              <TabPanel v-if="store.body.confidentialContractDraftDocumentId" value="3">
                <PP010ContractConfidentialDocument
                  :readonly="props.readonly || store.states.canEditDoc || !menuStore.hasManage || canEditDocument"
                  ref="pp010ContractConfidentialDocumentRef" :save="saveDocument"
                  :versions="store.body.confidentialContractDraftDocumentVersions"
                  :canRestoreVersion="!store.states.canEditDoc && menuStore.hasManage && !canEditDocument"
                  @restore-version="handleRestoreConfidentialVersion" />
              </TabPanel>
              <TabPanel v-if="store.body.approvalContractDraftDocumentId" value="4">
                <PP010ContractApproveDocument ref="pp010ContractApproveDocumentRef" :save="saveDocument"
                  :versions="store.body.approvalContractDraftDocumentVersions" :readonly="props.readonly"
                  @restore-version="handleRestoreApprovalVersion" />
              </TabPanel>
            </TabPanels>
          </Tabs>
        </div>

        <div class="relative lg:col-span-2">
          <div class="flex flex-col gap-4 ml-3 sticky pt-2 z-40 top-14">
            <div class="flex flex-wrap justify-end gap-2" v-if="menuStore.hasManage && !props.readonly">
              <ButtonSave class="w-[calc(33.333%-0.34rem)]" type="button" v-if="store.states.canEdit"
                label="บันทึกชั่วคราว" @click="onSaveDraftAsync()" />
              <ButtonSave class="w-[calc(33.333%-0.34rem)]" type="submit" v-if="store.states.canEdit"
                label="ยืนยันบันทึก" />
              <Button class="w-[calc(33.333%-0.34rem)]" label="ส่งตรวจสอบ" icon="pi pi-check" severity="warn"
                @click="handleSubmit(() => updateStatusPending())"
                v-if="store.states.canEdit && (store.body.contractDraftDocumentId || store.body.approvalContractDraftDocumentId || store.body.confidentialContractDraftDocumentId)" />
              <ButtonRecall class="w-[calc(33.333%-0.34rem)]" v-if="store.states.canRecall"
                @click="updateStatusRecall()" />
              <ButtonSendEdit class="w-[calc(33.333%-0.34rem)]" v-if="store.states.canAcceptAndReject"
                @click="store.api.rejectAsync()" />
              <Button class="w-[calc(33.333%-0.34rem)]" label="ยืนยันตรวจสอบ" icon="pi pi-user-plus" severity="success"
                v-if="store.states.canAcceptAndReject" @click="store.api.approveAsync()" />
              <ButtonSave class="w-[calc(33.333%-0.34rem)]" v-if="store.states.canSaveDateSign"
                label="บันทึกวันที่ลงนาม" @click="onSubmitAsync()" />
            </div>
            <Accordion :value="['0']" unstyled multiple>
              <AccordionPanel value="999" class="mb-4" v-if="currentTab !== '0' && currentTab !== '1'">
                <AccordHeader label="Dictionary" />
                <AccordionContent>
                  <div></div>
                  <Card class="rounded-none overflow-auto h-[800px]">
                    <template #content>
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
                  :acceptorType="AcceptorType.Approver" v-if="store.body.acceptors" isShowCheckBoxAll isApprove isManage
                  :is-disable="![TContractDraftStatus.Draft, TContractDraftStatus.Rejected, TContractDraftStatus.Edit].includes(
                    store.body.status
                  ) || !menuStore.hasManage || props.readonly"
                  :is-set-default="store.states.canEdit"
                  @set-default="() => store.api.onGetDefaultContractDirector()" />
              </AccordionPanel>
            </Accordion>
          </div>
        </div>
      </div>
    </Form>
    <SectionNavigator v-if="currentTab === '1' && sections.length > 0" :sections="sections"
      :active-section-id="activeSectionId" @navigate="scrollToSection" />
    <DialogReviewDocument v-model="showReviewDocumentDialog" :docId="reviwDocumentId"
      :docName="`pp010-${new Date().toISOString()}-${reviwDocumentId}`" @on-click-use-document="setDocumentReviewId" />
    <DateSignDialog v-model:visible="showDateSignModal" :on-save="onSaveDateSign" />
  </div>
</template>
