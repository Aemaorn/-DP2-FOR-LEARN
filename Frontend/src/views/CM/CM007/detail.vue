<script setup lang="ts">
import { Accordion, AccordionPanel, AccordionContent, InputGroupAddon } from 'primevue';
import { TitleHeader, TabHeader, AccordHeader } from '@/components/cosmetic';
import InfoItem from '@/components/cosmetic/InfoItem.vue';
import { AccordAcceptor, AccordAssignee } from '@/components/Accordions';
import { ButtonSave, ButtonSendApprove, ButtonSendEdit, ButtonApprove, ButtonConfirmAssign, ButtonConfirm, ButtonRecall, ButtonApproveConfirm, ButtonSendReview, ButtonConfirmReview } from '@/components/Button';
import { BadgeStatus } from '@/components';
import Cm007Constants from '@/constants/CM/cm007';
import { useMenuStore } from '@/stores/menu';
import { storeToRefs } from 'pinia';
import { Form as VeeForm } from 'vee-validate';
import { Cm007AccordionTab, Cm007Status } from '@/enums/CM/cm007';
import { AcceptorType, AssigneeGroup, AssigneeType } from '@/enums/participants';
import { computed, nextTick, onMounted, ref, watch, type Component } from 'vue';
import type { Option } from '@/models/shared/option';
import type { MenuItem } from 'primevue/menuitem';
import ToastHelper from '@/helpers/toast';
import { useCm007DetailStore } from '@/stores/CM/CM007/cm007.detail';
import { useCm007DialogStore } from '@/stores/CM/CM007/cm007Dialog';
import { useRoute } from 'vue-router';
import { showActivityDialog, showSaveOptionDialogAsync, showConfirmDialogAsync } from '@/helpers/dialog';
import { ConfirmDialogType } from '@/enums/dialog';
import { formatCurrency } from '@/helpers/currency';
import { ToDateOnly } from '@/helpers/dateTime';
import { InputArea, InputField, UploadFileGroup } from '@/components/forms';

import ContractInfo from './components/ContractInfo.vue';
import SectionSelectModal from './components/SectionSelectModal.vue';
import SectionCard from './components/SectionCard.vue';
import SelectDiaglog from './components/Sub/SelectDiaglog.vue';

import ChEditor from '@/components/Document/ChEditor.vue';
import DocumentMapping from '@/components/DocumentMapping.vue';
import { useContractDraftStore } from '@/views/PP/stores/PP0010/ContractDraft';
import ContractAcceptorSign from '@/views/PP/components/PP010/components/Sub/ContractAcceptorSign.vue';

interface ChEditorExposed {
  clickSave: () => void;
  saveAndWait: (callback: () => void) => void;
  setPlaceholderInDocument: (text: string, hint?: string) => void;
  resetToCurrentVersion: () => void;
}

const { Cm007BadgeStatus, Cm007AccordionTabName, SECTION_COMPONENT_MAP } = Cm007Constants;

const route = useRoute();
const menuStore = useMenuStore();
const { hasManage } = storeToRefs(menuStore);
const store = useCm007DetailStore();
const { body } = storeToRefs(store);
const dialogStore = useCm007DialogStore();
const { selectedItem } = storeToRefs(dialogStore);
const pp010Store = useContractDraftStore();
const {
  isCanEdit, isCommittee, isCurrentCommitteeApproval,
  isCanAssign, isWaitingComment, isCurrentApprover,
  isLastApprover, isCommitteeRecall, isRecall, isDirectorWaitingComment,
  isCanAssignContractDrafter, isCanAssignAddendumDrafter, isWaitingDraftAddendumDrafter, isCurrentReviewer, canRestoreVersion,
} = storeToRefs(store);
const { onUpsertAttachments } = store;

const id = computed<string | undefined>(() => route.params.id as string | undefined);
const showSelectDialog = ref(false);
const showSectionModal = ref(false);
const currentTab = ref<string>('detail');
const isFormDirty = ref(false);
const isInitialLoad = ref(true);
const isHydratingDetail = ref(false);

const extractSectionNumber = (label: string): number => {
  const match = label.match(/ข้อ\s+(\d+)/);
  return match ? parseInt(match[1], 10) : -1;
};

const sortedComponents = computed(() =>
  [...body.value.components].sort(
    (a, b) => extractSectionNumber(a.componentName) - extractSectionNumber(b.componentName)
  )
);

const getExtraProps = (componentCode: string): Record<string, boolean> => {
  if (componentCode === 'Payment') return { showContractDates: true };
  if (componentCode === 'Mulct') return { showIsPenalty: true };
  return {};
};

const amendmentDocRef = ref<ChEditorExposed>();
const approvalRequestDocRef = ref<ChEditorExposed>();

const resolvedComponents = ref<Record<string, Component>>({});
const sectionsReady = ref(false);

const routeItems = ref<Array<MenuItem>>([
  { label: 'รายการบันทึกต่อท้ายสัญญา', url: '/cm/cm007' },
  { label: 'รายละเอียดบันทึกต่อท้ายสัญญา' },
]);

const tabHeader = ref<Array<Option>>([
  { label: 'รายละเอียดบันทึกต่อท้าย', value: 'detail' },
  { label: 'เอกสารบันทึกต่อท้ายสัญญา', value: 'document' },
  { label: 'เอกสารขออนุมัติบันทึกต่อท้ายสัญญา', value: 'approval' },
]);

const filteredTabHeader = computed<Array<Option>>(() => {
  if (!body.value.id) {
    return tabHeader.value.filter((_, i) => i === 0);
  }

  const [detail, document, approval] = tabHeader.value;
  const documentTabStatuses = [Cm007Status.WaitingDraftAddendum, Cm007Status.WaitingReview, Cm007Status.Approved];

  if (documentTabStatuses.includes(body.value.status)) {
    return [detail, approval, document];
  }

  return [detail, approval];
});

const accordionValue = computed(() => {
  const status = body.value.status;

  if ([Cm007Status.Draft, Cm007Status.Editing, Cm007Status.WaitingCommitteeApproval, Cm007Status.Rejected].includes(status)) {
    return [Cm007AccordionTab.Committee];
  }
  if ([Cm007Status.WaitingAssignment, Cm007Status.RejectedToAssignee].includes(status)) {
    return [Cm007AccordionTab.Assignee];
  }
  if ([Cm007Status.WaitingComment].includes(status)) {
    return [Cm007AccordionTab.Assignee, Cm007AccordionTab.Acceptor];
  }
  if ([Cm007Status.WaitingApproval].includes(status)) {
    return [Cm007AccordionTab.Acceptor];
  }
  if ([Cm007Status.WaitingAssignment, Cm007Status.WaitingAddendumAssignment, Cm007Status.WaitingDraftAddendum].includes(status)) {
    return [Cm007AccordionTab.AddendumDrafter, Cm007AccordionTab.Reviewer];
  }
  if ([Cm007Status.WaitingReview].includes(status)) {
    return [Cm007AccordionTab.Reviewer];
  }

  return Object.values(Cm007AccordionTab);
});

const canEditDocument = computed(() => !!body.value.id);

const assigneeDefaultKey = computed(() =>
  body.value.assignees
    .filter(x => x.assigneeType === AssigneeType.Assignee)
    .map(x => `${x.userId}:${x.delegateeUserId ?? ''}:${x.sequence}`)
    .sort()
    .join('|')
);

// Load resolved components for each active section (batch to avoid multiple re-renders)
const loadSectionComponents = async () => {
  sectionsReady.value = false;

  const toLoad = body.value.components.filter(
    comp => !resolvedComponents.value[comp.componentCode] && SECTION_COMPONENT_MAP[comp.componentCode]
  );

  if (toLoad.length > 0) {
    const loaded = await Promise.all(
      toLoad.map(async comp => {
        const mod = await SECTION_COMPONENT_MAP[comp.componentCode]();
        return { code: comp.componentCode, component: (mod as any).default ?? mod };
      })
    );

    const updated = { ...resolvedComponents.value };
    for (const item of loaded) {
      updated[item.code] = item.component;
    }
    resolvedComponents.value = updated;
  }

  await nextTick();
  sectionsReady.value = true;
};

watch(() => body.value.components, loadSectionComponents, { deep: true, immediate: true });

const loadPP010Dropdowns = async () => {
  // 1. Load all general dropdowns in parallel
  await Promise.all([
    pp010Store.api.getVatTypeAsync(),
    pp010Store.api.getUnitTypeAsync(),
    pp010Store.api.getFineTypeAsync(),
    pp010Store.api.getUnitMeaTypeAsync(),
    pp010Store.api.getPeriodTypeAsync(),
    pp010Store.api.getPeriodConTypeAsync(),
    pp010Store.api.getPayTypeAsync(),
    pp010Store.api.getRCCRTypeAsync(),
    pp010Store.api.getWarrantyTypeAsync(),
    pp010Store.api.getBankAsync(),
    pp010Store.api.getConditionTypeOptions(),
    pp010Store.api.getPeriodConditionTypeAsync(),
    pp010Store.api.getPeriodAsync(),
    pp010Store.api.getPTimeTypeAsync(),
    // Load contract type first, needed for template chain
    pp010Store.api.getContractTypeAsync(),
  ]);

  // 2. Load template options → sub-template → attachment type (chain)
  if (body.value.contractTypeCode) {
    await pp010Store.api.getTemplateTypeAsync(body.value.contractTypeCode);

    const templateOption = pp010Store.dropdown.templateTypeOptions.find(
      (f: any) => f.value === body.value.templateCode
    );

    if (templateOption?.id && typeof templateOption.id === 'string') {
      // 3. Load sub-template options
      await pp010Store.api.getSubTemplateTypeAsync(templateOption.id as string);

      // 4. Determine which ID to use for attachment types
      let attachmentParentId = templateOption.id as string;

      if (body.value.subTemplateCode) {
        const subTemplateOption = pp010Store.dropdown.subTemplateTypeOptions.find(
          (f: any) => f.value === body.value.subTemplateCode
        );
        if (subTemplateOption?.id && typeof subTemplateOption.id === 'string') {
          attachmentParentId = subTemplateOption.id as string;
        }
      }

      // 5. Load attachment type options
      await pp010Store.api.getAttacementTypeAsync(attachmentParentId);
    }
  }
};

onMounted(async () => {
  store.onResetBody();
  isHydratingDetail.value = true;

  if (id.value) {
    await store.onGetById(id.value);
    await loadPP010Dropdowns();
  }

  await store.loginUserDefaultAcceptor();

  await nextTick();
  isHydratingDetail.value = false;
  isInitialLoad.value = false;
});

watch(id, async (newId, oldId) => {
  if (newId && newId !== oldId) {
    isHydratingDetail.value = true;
    store.onResetBody();
    await store.onGetById(newId);
    await loadPP010Dropdowns();
    await store.loginUserDefaultAcceptor();
    await nextTick();
    isHydratingDetail.value = false;
  }
});

watch(assigneeDefaultKey, async (newKey, oldKey) => {
  if (!id.value || isInitialLoad.value || isHydratingDetail.value) {
    return;
  }

  if (!newKey || newKey === oldKey) {
    return;
  }

  await store.assigneeDefaultAcceptor();
});

watch(
  () => store.body,
  () => {
    if (!!id.value && !isInitialLoad.value) {
      isFormDirty.value = true;
    }
  },
  { deep: true }
);

watch(() => body.value.status, (newStatus) => {
  if (!body.value.id) {
    currentTab.value = 'detail';
  } else if ([Cm007Status.WaitingAddendumAssignment, Cm007Status.WaitingDraftAddendum, Cm007Status.WaitingReview].includes(newStatus)) {
    currentTab.value = 'document';
  } else if ([Cm007Status.WaitingCommitteeApproval, Cm007Status.WaitingComment, Cm007Status.WaitingApproval, Cm007Status.Approved].includes(newStatus)) {
    currentTab.value = 'approval';
  } else {
    currentTab.value = 'detail';
  }
}, { immediate: true });

const onTabChange = (tab: string): void => {
  currentTab.value = tab;

  nextTick(() => {
    if (tab === 'document') {
      amendmentDocRef.value?.resetToCurrentVersion();
      approvalRequestDocRef.value?.resetToCurrentVersion();
    }
  });
};

const saveDocumentFirst = (): Promise<void> => {
  return new Promise((resolve) => {
    if (!canEditDocument.value) {
      resolve();
      return;
    }

    if (currentTab.value === 'document' && amendmentDocRef.value?.saveAndWait) {
      amendmentDocRef.value.saveAndWait(() => {
        if (approvalRequestDocRef.value?.saveAndWait) {
          approvalRequestDocRef.value.saveAndWait(() => resolve());
        } else {
          resolve();
        }
      });
    } else {
      resolve();
    }
  });
};

const submitAsync = async () => {
  if (currentTab.value === 'document') {
    await saveDocumentFirst();
  }

  let shouldReplaceAmendment = false;
  let shouldReplaceApproval = false;

  if (isFormDirty.value && id.value && (body.value.amendmentDocumentId || body.value.amendmentApprovalRequestDocumentId)
    && ![Cm007Status.Rejected].includes(body.value.status)) {
    const saveOption = await showSaveOptionDialogAsync();
    if (saveOption === undefined) return;
    body.value.isAmendmentDocumentIdReplaced = saveOption;
    shouldReplaceAmendment = saveOption;
    body.value.isAmendmentApprovalRequestDocumentIdReplaced = saveOption;
    shouldReplaceApproval = saveOption;
  }

  await store.onSaveAsync();

  if (shouldReplaceAmendment && body.value.amendmentDocumentId) {
    const docId = await store.getReviewDocumentAsync('Amendment');
    if (docId) body.value.amendmentDocumentId = docId;
  }

  if (shouldReplaceApproval && body.value.amendmentApprovalRequestDocumentId) {
    const docId = await store.getReviewDocumentAsync('AmendmentApprovalRequest');
    if (docId) body.value.amendmentApprovalRequestDocumentId = docId;
  }

  isFormDirty.value = false;
  body.value.isAmendmentDocumentIdReplaced = false;
  body.value.isAmendmentApprovalRequestDocumentIdReplaced = false;
};

const onSendApprovalWithDocSave = async () => {
  if (!await showConfirmDialogAsync(ConfirmDialogType.SendApproveConfirm)) return;

  await saveDocumentFirst();
  await store.onSubmitCommitteeApprovalAsync();
};

const onSectionConfirm = (sections: { componentCode: string; componentName: string }[]) => {
  const selectedCodes = new Set(sections.map(s => s.componentCode));
  const existingCodes = new Set(body.value.components.map(c => c.componentCode));

  // Update existing components: isEdited = true if selected, false if not
  const updatedComponents = body.value.components.map(c => ({
    ...c,
    isEdited: selectedCodes.has(c.componentCode),
  }));

  // Add new components for selections not yet in body.value.components
  const newComponents = sections
    .filter(s => !existingCodes.has(s.componentCode))
    .map(s => ({
      componentCode: s.componentCode,
      componentName: s.componentName,
      isEdited: true,
    }));

  store.onUpdateComponents([...updatedComponents, ...newComponents]);
  showSectionModal.value = false;
};

const saveDocument = async () => {
  await saveDocumentFirst();
  await store.onSaveAsync();
};

const onClickSelectMapping = (text: string, hint?: string): void => {
  amendmentDocRef.value?.setPlaceholderInDocument(text, hint);
};

const onCommentWithDocSave = async (e: { reason: string; userId: string }) => {
  await saveDocumentFirst();
  await store.onSubmitCommentAsync(e.reason);
};

const onConfirmCreate = async () => {
  if (!selectedItem.value) return;

  if (!await showConfirmDialogAsync(ConfirmDialogType.ConfirmData)) return;

  await dialogStore.fn.onConfirmCreateAsync();
};
</script>

<template>
  <!-- New document: show search dialog -->
  <Card v-if="!id">
    <template #content>
      <TitleHeader label="บันทึกต่อท้ายสัญญา" hidden-icon :routeItems="routeItems">
        <template #action>
          <ButtonSave v-if="selectedItem" @click="onConfirmCreate" />
        </template>
      </TitleHeader>
      <div class="px-4 mt-2">
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 items-center">
          <InfoItem class="col-start-1" title="เลขที่อ้างอิงในระบบ">
            <template #content>
              <InputField v-if="menuStore.hasManage" :modelValue="selectedItem?.contractNumber ?? ''" rules="required"
                class="w-4/5" disabled>
                <template #appendAction>
                  <InputGroupAddon v-if="menuStore.hasManage">
                    <Button label="ค้นหา"
                      class="rounded-l-none rounded-r-none text-white! bg-gray-500! border-none! h-full"
                      @click="showSelectDialog = true" />
                  </InputGroupAddon>
                </template>
              </InputField>
            </template>
          </InfoItem>
        </div>

        <!-- Show selected contract info -->
        <div v-if="selectedItem" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 items-center mt-4">
          <InfoItem title="เลขที่ร่างสัญญา" :content="selectedItem.contractDraftNumber" />
          <InfoItem title="เลขที่ PO (SAP)" :content="selectedItem.poNumber" />
          <InfoItem title="ชื่อสัญญา" :content="selectedItem.contractName" />
          <InfoItem title="คู่ค้า" :content="selectedItem.entrepreneurName" />
          <InfoItem title="เลขประจำตัวผู้เสียภาษี" :content="selectedItem.taxId" />
          <InfoItem title="วงเงินตามสัญญา" :content="formatCurrency(selectedItem.budget ?? 0)" />
          <InfoItem title="วันที่ลงนามสัญญา" :content="ToDateOnly(selectedItem.contractSignedDate)" />
          <InfoItem title="รูปแบบสัญญา" :content="selectedItem.templateCode ?? '-'" />
          <InfoItem title="หน่วยงาน" :content="selectedItem.departmentName ?? '-'" />
        </div>
      </div>
    </template>
  </Card>

  <!-- Detail view -->
  <template v-if="id && body.id">
    <TitleHeader label="รายละเอียดบันทึกต่อท้ายสัญญา" :routeItems="routeItems">
      <template #action>
        <div class="flex items-center gap-2">
          <p class="text-sm">สถานะ :</p>
          <BadgeStatus :label="Cm007BadgeStatus(body.status).label" :color="Cm007BadgeStatus(body.status).color" />
        </div>
        <Button v-if="id" label="ประวัติการใช้งาน" severity="warn" icon="pi pi-refresh" variant="outlined"
          class="bg-white! hover:bg-red-50!" @click="showActivityDialog(id)" />
      </template>
    </TitleHeader>
    <ContractInfo :data="body" />
    <VeeForm @submit="submitAsync" @invalidSubmit="ToastHelper.invalidMessageToast()" :validateOnMount="false"
      v-slot="{ handleSubmit }">
      <div class="grid lg:grid-cols-7 gap-2">
        <!-- Left column: content -->
        <div class="lg:col-span-5 order-2 lg:order-1">
          <Tabs :value="currentTab" unstyled @update:value="(tab) => onTabChange(tab.toString())">
            <TabHeader class="sticky top-[55px] z-3 bg-[#F7F7F7] pt-2" :items="filteredTabHeader" />
            <TabPanels>
              <!-- Tab 1: Detail -->
              <TabPanel value="detail">
                <ContractAcceptorSign v-model="store.body.acceptors"
                  :is-disabled="!store.isCanEdit || !menuStore.hasManage" label="ลงนามโดย" />
                <Card class="mt-3">
                  <template #content>
                    <div class="grid grid-cols-3 gap-4 gap-y-8 mt-10">
                      <Datepicker label="วันที่เอกสาร" v-model="body.documentDate"
                        :disabled="!store.isCanEdit" />
                    </div>
                    <div class="flex flex-col gap-8 mt-10">
                      <InputField v-model="body.title" label="เรื่อง" rules="required"
                        :disabled="!isCanEdit || !menuStore.hasManage" />
                      <InputArea v-model="body.description" label="แก้ไขคำบรรยายลักษณะงาน" helperText="เช่น ขอบเขตงาน, ใบเสนอราคา ฯลฯ" rules="required"
                        :disabled="!isCanEdit || !menuStore.hasManage" />
                    </div>
                  </template>
                </Card>
                <Card class="mt-3">
                  <template #content>
                    <TitleHeader label="แก้ไขรายละเอียดข้อสัญญา">
                      <template #action>
                        <Button v-if="id" label="เลือกข้อแก้ไข" severity="danger" variant="outlined"
                          class="bg-white! hover:bg-red-50!" @click="showSectionModal = true" />
                      </template>
                    </TitleHeader>
                    <div class="flex flex-col gap-8 mt-4">
                      <template v-if="sectionsReady">
                        <template v-for="comp in sortedComponents" :key="comp.componentCode">
                          <SectionCard v-if="comp.isEdited && resolvedComponents[comp.componentCode]"
                            :componentCode="comp.componentCode" :componentName="comp.componentName"
                            :isEdited="comp.isEdited" :disabled="!isCanEdit">
                            <template #old>
                              <component v-if="body.oldData" :is="resolvedComponents[comp.componentCode]"
                                v-model:body="(body.oldData)" :dropdown="pp010Store.dropdown.attacementTypeOptions"
                                :label="comp.componentName" :disable="true"
                                v-bind="getExtraProps(comp.componentCode)" />
                            </template>
                            <template #new>
                              <component :is="resolvedComponents[comp.componentCode]" v-model:body="body"
                                :dropdown="pp010Store.dropdown.attacementTypeOptions" :label="comp.componentName"
                                :disable="!isCanEdit && !isWaitingComment" v-bind="getExtraProps(comp.componentCode)" />
                            </template>
                          </SectionCard>
                        </template>
                      </template>
                    </div>
                  </template>
                </Card>
                <UploadFileGroup class="mt-3" v-if="body.id" v-model="store.body.fileAttachments"
                  @upload="onUpsertAttachments" @remove-file="onUpsertAttachments"
                  @remove-group="onUpsertAttachments" @reorder="onUpsertAttachments"
                  :disabled="!menuStore.hasManage" />
              </TabPanel>

              <!-- Tab 2: Documents -->
              <TabPanel value="document">
                <Card class="my-3" v-if="body.amendmentDocumentId">
                  <template #content>
                    <ChEditor :docId="body.amendmentDocumentId" :docName="new Date().toISOString()"
                      :readonly="!hasManage || !canEditDocument" :save="saveDocument" ref="amendmentDocRef"
                      :key="body.amendmentDocumentId" :versions="body.amendmentDocumentVersions ?? []"
                      :canRestoreVersion="canRestoreVersion"
                      @restore-version="() => store.resetDocumentAsync('Amendment')" />
                  </template>
                </Card>
              </TabPanel>

              <TabPanel value="approval">
                <Card class="my-3" v-if="body.amendmentApprovalRequestDocumentId">
                  <template #content>
                    <ChEditor :docId="body.amendmentApprovalRequestDocumentId" :docName="new Date().toISOString()"
                      :readonly="!hasManage || !canEditDocument" :save="saveDocument" ref="approvalRequestDocRef"
                      :key="body.amendmentApprovalRequestDocumentId"
                      :versions="body.approvalRequestDocumentVersions ?? []" :canRestoreVersion="canRestoreVersion"
                      @restore-version="() => store.resetDocumentAsync('AmendmentApprovalRequest')" />
                  </template>
                </Card>
              </TabPanel>
            </TabPanels>
          </Tabs>
        </div>

        <!-- Right column: actions + accordions -->
        <div class="relative lg:col-span-2 order-1 lg:order-2">
          <div class="flex flex-col gap-4 lg:ml-3 sticky top-[55px] pt-2 z-3 bg-[#F7F7F7]">
            <div v-if="hasManage" class="flex items-center justify-end gap-2 flex-wrap">

              <!-- Draft / Editing / Rejected: Save + Send Approval -->
              <div class="flex gap-2" v-if="isCanEdit">
                <ButtonSave type="submit" />
                <ButtonSendApprove v-if="body.id" @click="handleSubmit(() => onSendApprovalWithDocSave())" />
              </div>

              <!-- Committee Approval -->
              <div class="flex gap-2"
                v-if="[Cm007Status.WaitingCommitteeApproval].includes(body.status) && isCommittee">
                <ButtonRecall v-if="isCommitteeRecall" @click="store.onRecallAsync" />
                <ButtonSendEdit label="ไม่เห็นชอบ"  @click="store.onRejectedAsync" />
                <ButtonApprove v-if="isCurrentCommitteeApproval" @click="store.onCommitteeApprovedAsync" />
              </div>

              <!-- Assignment -->
              <div class="flex gap-2" v-if="isCanAssign">
                <ButtonSendEdit @click="store.onRejectedAsync()" />
                <ButtonSave type="submit" />
                <ButtonConfirmAssign @click="store.onAssignAsync()" />
              </div>

              <!-- Waiting Comment (assignee can comment + send to approval) -->
              <div class="flex gap-2" v-if="isWaitingComment">
                <ButtonSave type="submit" />
                <ButtonApproveConfirm @click="handleSubmit(() => store.onSubmitToApprovalAsync())" />
              </div>

              <!-- Waiting Comment (director can send back) -->
              <div class="flex gap-2" v-if="isDirectorWaitingComment">
                <ButtonSendEdit @click="store.onRejectedAsync()" />
              </div>

              <!-- WaitingAssignment (Contract) / WaitingAddendumAssignment (AddendumDrafter): Save + Confirm Assign -->
              <div class="flex gap-2" v-if="isCanAssignContractDrafter || isCanAssignAddendumDrafter">
                <ButtonSave type="submit" />
                <ButtonConfirmAssign @click="handleSubmit(() => store.onConfirmAddendumDrafterAsync())" />
              </div>

              <!-- WaitingDraftAddendum: Save + ส่งตรวจสอบ -->
              <div class="flex gap-2" v-if="isWaitingDraftAddendumDrafter">
                <ButtonSave type="submit" />
                <ButtonSendReview @click="handleSubmit(() => store.onConfirmDraftAddendumAsync())" />
              </div>

              <!-- WaitingReview: ส่งกลับแก้ไข + ยืนยันตรวจสอบ -->
              <div class="flex gap-2" v-if="isCurrentReviewer">
                <ButtonSendEdit @click="store.onReviewerRejectAsync()" />
                <ButtonConfirmReview @click="store.onReviewerApproveAsync()" />
              </div>

              <!-- Approver -->
              <div class="flex gap-2">
                <ButtonRecall v-if="isRecall" @click="store.onRecallAsync" />
                <ButtonSendEdit @click="store.onRejectedAsync()" v-if="isCurrentApprover" />
                <ButtonApprove v-if="!isLastApprover && isCurrentApprover" @click="store.onApproverApproveAsync()" />
                <ButtonConfirm v-if="isLastApprover && isCurrentApprover" @click="store.onApproverApproveAsync()" />
              </div>

            </div>

            <!-- Dictionary panel for document tab -->
            <Accordion v-if="currentTab === 'document' || currentTab === 'approval'" :value="[]" unstyled multiple>
              <AccordionPanel value="dict" class="mb-4">
                <AccordHeader label="Dictionary" />
                <AccordionContent>
                  <div></div>
                  <Card class="rounded-none overflow-auto h-[800px]">
                    <template #content>
                      <DocumentMapping pathToGet="contract/contract-draft-vendor-edit"
                        @on-click-select="onClickSelectMapping" />
                    </template>
                  </Card>
                </AccordionContent>
              </AccordionPanel>
            </Accordion>

            <Accordion :value="accordionValue" unstyled multiple>
              <AccordionPanel :value="Cm007AccordionTab.Committee">
                <AccordAcceptor :acceptorType="AcceptorType.AcceptanceCommittee"
                  :title="Cm007AccordionTabName(Cm007AccordionTab.Committee)" v-model="body.acceptors"
                  :is-disable="!body.id || ![Cm007Status.Draft, Cm007Status.Editing, Cm007Status.Rejected, Cm007Status.WaitingCommitteeApproval].includes(body.status)"
                  isSetDefault
                  @set-is-unable-to-perform-duties="(status: boolean, acceptorId: string, remark?: string) => store.onUpdateDutiesStatusAsync(status, acceptorId, remark)" />
              </AccordionPanel>

              <AccordionPanel class="mt-4" :value="Cm007AccordionTab.Assignee">
                <AccordAssignee :group="AssigneeGroup.Contract"
                  :title="Cm007AccordionTabName(Cm007AccordionTab.Assignee)" v-model="body.assignees"
                  :disabled="!isCanAssign && !isWaitingComment && !isCanAssignContractDrafter" :is-comment="isWaitingComment"
                  @on-comment="(e) => onCommentWithDocSave(e)" />
              </AccordionPanel>

              <AccordionPanel class="my-4" :value="Cm007AccordionTab.Acceptor">
                <AccordAcceptor :acceptorType="AcceptorType.Approver"
                  @set-default="() => store.assigneeDefaultAcceptor()"
                  :title="Cm007AccordionTabName(Cm007AccordionTab.Acceptor)" v-model="body.acceptors" isSetDefault
                  :isManage="isCanAssign || isWaitingComment" :is-disable="!isCanAssign && !isWaitingComment"
                  @set-is-unable-to-perform-duties="(status: boolean, acceptorId: string, remark?: string) => store.onUpdateDutiesStatusAsync(status, acceptorId, remark)" />
              </AccordionPanel>

              <AccordionPanel class="mt-4" :value="Cm007AccordionTab.AddendumDrafter">
                <AccordAssignee :group="AssigneeGroup.AddendumDrafter"
                  :title="Cm007AccordionTabName(Cm007AccordionTab.AddendumDrafter)" v-model="body.assignees"
                  :disabled="![Cm007Status.Approved, Cm007Status.WaitingAddendumAssignment, Cm007Status.WaitingDraftAddendum].includes(body.status)" />
              </AccordionPanel>

              <AccordionPanel class="mt-4" :value="Cm007AccordionTab.Reviewer">
                <AccordAcceptor :acceptorType="AcceptorType.Reviewer"
                  :title="Cm007AccordionTabName(Cm007AccordionTab.Reviewer)" v-model="body.acceptors"
                  :is-disable="![Cm007Status.WaitingDraftAddendum].includes(body.status)" />
              </AccordionPanel>
            </Accordion>
          </div>
        </div>
      </div>
    </VeeForm>
  </template>

  <SelectDiaglog v-model="showSelectDialog" :defaultDepartment="false" />

  <SectionSelectModal v-model="showSectionModal" :templateCode="body.templateCode ?? ''" :components="body.components"
    @confirm="onSectionConfirm" :disable-form="!isCanEdit || !menuStore.hasManage" />
</template>
