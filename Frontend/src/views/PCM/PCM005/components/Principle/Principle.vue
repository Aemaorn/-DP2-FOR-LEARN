<script setup lang="ts">
import { BadgeStatus } from '@/components';
import { AccordAcceptor, AccordAssignee } from '@/components/Accordions';
import { ButtonApprove, ButtonApproveConfirm, ButtonConfirm, ButtonConfirmAssign, ButtonNotAgree, ButtonRecall, ButtonSave, ButtonSendApprove, ButtonSendEdit } from '@/components/Button';
import { TabHeader, TitleHeader } from '@/components/cosmetic';
import DocumentMapping from '@/components/DocumentMapping.vue';
import principleConstant from '@/constants/PCM005/principle';
import { ConfirmDialogType } from '@/enums/dialog';
import { AcceptorType, AssigneeType } from '@/enums/participants';
import { CommitteeGroupType, PrincipleStatus } from '@/enums/PCM005/principle';
import { showActivityDialog, showConfirmDialogAsync, showSaveOptionDialogAsync } from '@/helpers/dialog';
import ToastHelper from '@/helpers/toast';
import type { Option } from '@/models/shared/option';
import { useMenuStore } from '@/stores/menu';
import { usePcm005DetailStore } from '@/stores/PCM/PCM005/pcm005';
import { usePcm005PrincipleStore } from '@/stores/PCM/PCM005/principle';
import { TabPanel, TabPanels, Tabs } from 'primevue';
import { Form } from 'vee-validate';
import { watch, computed, defineAsyncComponent, nextTick, onMounted, onUnmounted, ref } from 'vue';

const props = defineProps({
  readonly: { type: Boolean, default: false },
});

const Detail = defineAsyncComponent(() => import('./Sub/Detail.vue'));
const Committee = defineAsyncComponent(() => import('./Sub/Committee.vue'));
const MedianPrice = defineAsyncComponent(() => import('./Sub/MedianPrice.vue'));
const Budget = defineAsyncComponent(() => import('./Sub/Budget.vue'));
const Document = defineAsyncComponent(() => import('./Sub/Document.vue'));

const documentRef = ref<InstanceType<typeof Document> | null>(null);
const currentTab = ref('0');
const isFormDirty = ref(false);
const isInitialized = ref(false);

const menuStore = useMenuStore();
const store = usePcm005PrincipleStore();
const pcmStore = usePcm005DetailStore();
const { principleStatusColor, principleStatusName } = principleConstant;

const HeaderItem = ref([
  {
    label: 'รายละเอียด',
    value: '0',
  },
  {
    label: 'เอกสารขออนุมัติหลักการเช่า',
    value: '1',
  },
] as Option[]);

onMounted(() => {
  store.onResetBody();
  initAsync();
});

onUnmounted(() => {
  store.onResetBody();
});

const initAsync = async () => {
  await Promise.all([
    store.onGetDocumentTemplateDDLAsync(),
    store.onGetPsoDDLAsync(),
    store.getDepartmentDropdownAsync(),
    store.getBudgetTypeDropdownAsync(),
    store.getAccountCodeDropdownAsync(),
    store.getDefaultApproverAsync(),
    store.onGetJorPorAsync(),
  ]);

  // ถ้ามี id ให้ fetch data ถ้าไม่มีให้ใช้ค่า default จาก onResetBody
  if (pcmStore.body.principleApproval?.id) {
    await store.getByIdAsync(pcmStore.body.principleApproval.id);
  } else {
    await store.getOperDefaultDepartmentApproveAsync();
  }

  await nextTick();
  isInitialized.value = true;
};

const checkDataBeforeSendApprove = (): { isCancel: boolean; reason: string } => {
  const conditions = [
    {
      check: store.body.committees.some(c => c.groupType === CommitteeGroupType.RentCommittee),
      reason: 'กรุณาเพิ่มคณะกรรมการจัดเช่าอย่างน้อย 1 คน',
    },
    {
      check: store.body.committees.some(c => c.groupType === CommitteeGroupType.AcceptanceCommittee),
      reason: 'กรุณาเพิ่มคณะกรรมการตรวจรับอย่างน้อย 1 คน',
    },
    {
      check: store.body.acceptors.some(a => a.acceptorType === AcceptorType.DepartmentDirectorAgree),
      reason: 'กรุณาเพิ่มสายงานเห็นชอบอย่างน้อย 1 คน',
    },
  ];

  const failed = conditions.find(c => !c.check);

  return failed ? { isCancel: true, reason: failed.reason } : { isCancel: false, reason: '' };
};

const saveDocument = async () => {
  await onSubmitAsync();
};

const onSubmitAsync = async (): Promise<void> => {
  if (documentRef.value?.saveDocumentFirst && store.body.documentTemplateId) {
    await documentRef.value.saveDocumentFirst();
  }

  if (store.body.id) {
    if (isFormDirty.value && store.body.documentTemplateId
    && [PrincipleStatus.Edit, PrincipleStatus.Draft, PrincipleStatus.Rejected].includes(store.body.status)) {
      const saveOption = await showSaveOptionDialogAsync();
      if (saveOption === undefined) return;
      store.body.isDocumentTemplateIdReplaced = saveOption;
    }

    isInitialized.value = false;
    setCurrentTab();
    await store.updateAsync(store.body.status);
    isFormDirty.value = false;
    await nextTick();
    isInitialized.value = true;
    return;
  }

  isInitialized.value = false;
  await store.createAsync(PrincipleStatus.Draft);
  isFormDirty.value = false;
  await nextTick();
  isInitialized.value = true;
  setCurrentTab();
};

const setCurrentTab = async () => {
  if (store.body.documentTemplateId) {
    currentTab.value = '1';
  }
};

const onSaveUpdateAsync = async (): Promise<void> => {
  await store.updateAsync(store.body.status);

  if (!store.body.acceptors.some(a => a.acceptorType === AcceptorType.Approver)) {
    store.getDefaultApproverAsync();
  }
};

const onSendApproveAsync = async (): Promise<void> => {
  const { isCancel, reason } = checkDataBeforeSendApprove();

  if (isCancel) {
    return ToastHelper.errorDescription(reason);
  }

  if (store.body.isRentCommittee && store.body.committees.filter(c => c.groupType === CommitteeGroupType.RentCommittee && c.committeePositionsCode === store.positionOnBoardDDL[0].value).length === 0) {
    return ToastHelper.mustHaveLeaderBoardToast();
  }

  if (store.body.isAcceptanceCommittee && store.body.committees.filter(c => c.groupType === CommitteeGroupType.AcceptanceCommittee && c.committeePositionsCode === store.positionOnBoardDDL[0].value).length === 0) {
    return ToastHelper.mustHaveLeaderBoardToast();
  }

  if (!await showConfirmDialogAsync(ConfirmDialogType.SendApprove)) return;

  if (store.body.id) {
    if (documentRef.value?.saveDocumentFirst && store.body.documentTemplateId) {
      await documentRef.value.saveDocumentFirst();
    }

    isInitialized.value = false;
    await store.updateAsync(PrincipleStatus.WaitingUnitApproval);
    isFormDirty.value = false;
    await nextTick();
    isInitialized.value = true;
  }
};

const onRecallAsync = async (): Promise<void> => {
  if (!await showConfirmDialogAsync(ConfirmDialogType.Edit)) return;

  await store.updateAsync(PrincipleStatus.Edit);
};

const onConfirmAssignAsync = async (): Promise<void> => {
  if (!store.body.assignees.some(x => x.assigneeType === AssigneeType.Assignee)) {
    return ToastHelper.assignAtLeastMessageToast();
  }

  if (!await showConfirmDialogAsync(ConfirmDialogType.Assigned)) return;

  await store.updateAsync(PrincipleStatus.WaitingComment);

  if (!store.body.acceptors.some(a => a.acceptorType === AcceptorType.Approver)) {
    await store.getDefaultApproverAsync();
  }
};

const onConfirmWaitingUnitApprovalAsync = async (): Promise<void> => {
  if (!store.body.assignees.some(x => x.remark)) {
    return ToastHelper.assignneeCommentAtLeastMessageToast();
  }

  if (!store.body.acceptors.some(a => a.acceptorType === AcceptorType.Approver)) {
    return ToastHelper.approvalAtLeastMessageToast();
  }

  if (!await showConfirmDialogAsync(ConfirmDialogType.SendApproveConfirm)) return;

  await store.updateAsync(PrincipleStatus.WaitingAcceptance);
};

watch(
  () => store.body,
  () => {
    if (isInitialized.value) {
      isFormDirty.value = true;
    }
  },
  { deep: true }
);

const defaultDocumentStatuses = [
  PrincipleStatus.WaitingUnitApproval,
  PrincipleStatus.Edit,
];

watch(
  () => store.body.status,
  (newStatus: PrincipleStatus) => {
    if (defaultDocumentStatuses.includes(newStatus)) {
      currentTab.value = '1';
    }
  },
  { immediate: true }
);

const canEditDocument = computed(() => {
  return store.body.status === PrincipleStatus.Edit
    || store.body.status === PrincipleStatus.Draft
    || store.body.status === PrincipleStatus.Rejected
    || store.body.status === PrincipleStatus.WaitingComment
});

</script>

<template>
  <TitleHeader label="ขออนุมัติหลักการ">
    <template #action>
      <BadgeStatus :color="principleStatusColor(store.body.status).color"
        :label="principleStatusName(store.body.status)" v-if="store.body.status" />
      <Button label="ประวัติการใช้งาน" icon="pi pi-refresh" variant="outlined" severity="primary" v-if="store.body.id"
        class="bg-white! hover:bg-red-50!" @click="() => showActivityDialog(store.body.id!)" />
    </template>
  </TitleHeader>
  <Form @submit="onSubmitAsync" @invalidSubmit="ToastHelper.invalidMessageToast()" v-slot="{ handleSubmit }">
    <div class="grid grid-cols-1 lg:grid-cols-7 gap-2">
      <div class="lg:col-span-5 order-2 lg:order-1">
        <Tabs :value="currentTab" unstyled @update:value="(tab) => currentTab = tab.toString()">
          <TabHeader :items="HeaderItem.filter((h, i) => store.body.id ? h : i === 0)"
            class="sticky top-[57px] z-99 bg-[#F7F7F7]" />
          <TabPanels>
            <TabPanel value="0">
              <div class="flex flex-col gap-4">
                <Detail />
                <Committee title="คณะกรรมการจัดเช่า" :type="CommitteeGroupType.RentCommittee" />
                <Committee title="คณะกรรมการตรวจรับ" :type="CommitteeGroupType.AcceptanceCommittee" />
                <!-- <Consideration /> -->
                <!-- <AnalysisBuilding /> -->
                <MedianPrice />
                <Budget />
              </div>
            </TabPanel>
            <TabPanel value="1">
              <div class="grid grid-rows-1 gap-2 mb-5">
                <Document ref="documentRef" :save="saveDocument" :readonly="!canEditDocument" />
              </div>
            </TabPanel>
          </TabPanels>
        </Tabs>
      </div>
      <div class="lg:col-span-2 relative order-1 lg:order-2">
        <div class="flex flex-col gap-4 lg:ml-3 sticky top-[100px]">
          <div class="flex items-center justify-end" v-if="menuStore.hasManage && !props.readonly">
            <div class="draft flex gap-2 items-center">
              <ButtonSave type="submit" v-if="store.status.canEdit" />
              <ButtonSendApprove @click="() => handleSubmit(() => onSendApproveAsync())"
                v-if="store.status.canEdit && store.body.id" />
            </div>
            <div class="waiting-unit flex gap-2 items-center">
              <ButtonRecall @click="onRecallAsync" v-if="store.status.canRecall" />
              <ButtonNotAgree @click="store.unitRejectAsync" v-if="store.status.canUnitApprove" />
              <ButtonApprove @click="store.unitAppoveAsync" v-if="store.status.canUnitApprove" />
            </div>
            <div class="assign flex gap-2 items-center">
              <ButtonSendEdit @click="store.assigneeRejectAsync" v-if="store.status.canDirectorAssign" />
              <ButtonSave @click="onSaveUpdateAsync"
                v-if="store.status.canDirectorAssign" />
              <ButtonSave @click="onSaveUpdateAsync" text="บันทึกผู้รับผิดชอบ"
                v-if="store.status.canAssignAndConfirm || store.status.canComment" />
              <ButtonConfirmAssign @click="onConfirmAssignAsync" v-if="store.status.canAssignAndConfirm" />
              <ButtonApproveConfirm @click="onConfirmWaitingUnitApprovalAsync" v-if="store.status.canComment" />
            </div>
            <div class="approve flex gap-2 items-center">
              <ButtonSendEdit @click="store.rejectAsync" v-if="store.status.canApprove" />
              <ButtonApprove @click="store.approveAsync"
                v-if="store.status.canApprove && !store.status.isLastApprove" />
              <ButtonConfirm @click="store.approveAsync" v-if="store.status.canApprove && store.status.isLastApprove" />
            </div>
          </div>
          <Accordion :value="['0', '1', '2']" unstyled multiple>
            <AccordionPanel value="999" class="mb-4" v-if="currentTab !== '0'">
              <AccordHeader label="Dictionary" />
              <AccordionContent>
                <div></div>
                <Card class="rounded-none overflow-auto h-[800px]">
                  <template #content>
                    <DocumentMapping pathToGet="procurement/principle-approval" @on-click-select="
                      (text, hint) => documentRef?.setPlaceholderInDocument(text, hint)
                    " />
                  </template>
                </Card>
              </AccordionContent>
            </AccordionPanel>
            <AccordionPanel value="0">
              <AccordAcceptor v-model="store.body.acceptors" title="สายงานเห็นชอบ"
                :acceptor-type="AcceptorType.DepartmentDirectorAgree" isManage :is-set-default="true"
                :is-disable="!store.status.canEdit || !menuStore.hasManage || props.readonly" v-if="store.body.acceptors"
                @set-default="() => store.getOperDefaultDepartmentApproveAsync()" />
            </AccordionPanel>
            <AccordionPanel value="1" class="mt-4">
              <AccordAssignee v-model="store.body.assignees" title="เจ้าหน้าที่พัสดุให้ความเห็น"
                :disabled="(!store.status.canAssigneesAssign && !store.status.canComment) || !menuStore.hasManage || props.readonly"
                v-if="store.body.assignees" :is-comment="store.status.canComment"
                @on-comment="(e) => store.commentAsync(e.reason)" />
            </AccordionPanel>
            <AccordionPanel value="2" class="mt-4">
              <AccordAcceptor v-model="store.body.acceptors" title="ผู้มีอำนาจเห็นชอบ/อนุมัติ"
                :is-disable="!store.status.canAssignee || !menuStore.hasManage || props.readonly" :acceptor-type="AcceptorType.Approver"
                :is-set-default="true" isManage v-if="store.body.acceptors"
                @set-default="() => store.getDefaultApproverAsync()" />
            </AccordionPanel>
          </Accordion>
        </div>
      </div>
    </div>
  </Form>
</template>
