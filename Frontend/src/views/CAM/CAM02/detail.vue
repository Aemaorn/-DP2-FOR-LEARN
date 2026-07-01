<script setup lang="ts">
import { computed, defineAsyncComponent, nextTick, onMounted, onUnmounted, ref, watch } from 'vue';
import { Form } from 'vee-validate';
import type { MenuItem } from 'primevue/menuitem';
import { TitleHeader } from '@/components/cosmetic';
import { UploadFileGroup } from '@/components/forms';
import { usePPDetailStore } from '@/stores/PP/ppStore';
import { useMenuStore } from '@/stores/menu';
import type { Option } from '@/models/shared/option';
import { CommitteeType, SourceType, type TListCam02Committee } from '@/models/CAM/CAM02/cam02';
import type { ParticipantsCommitteeAcceptor } from '@/models/shared/participants';
import { AcceptorStatus, AcceptorType, AssigneeGroup } from '@/enums/participants';
import PlanSelect from './components/PlanSelect.vue';
import { committeeGroupTypeMappingName } from './components/CommitteeTypeMappingName';
import SourceSelect from './components/SourceSelect.vue';
import ListCommittee from './components/ListCommittee.vue';
import { AccordAcceptor, AccordAssignee } from '@/components/Accordions';
import { useCam02DetailStore } from '@/stores/CAM/CAM02/cam02Store';
import { Cam02Status } from '@/enums/CAM/CAM02/cam02';
import { ButtonSave, ButtonSendApprove, ButtonRecall, ButtonSendEdit, ButtonApprove } from '@/components/Button';
import { useRoute, useRouter } from 'vue-router';
import { showActivityDialog, showConfirmDialogAsync, showSaveOptionDialogAsync } from '@/helpers/dialog';
import { ConfirmDialogType } from '@/enums/dialog';
import ToastHelper from '@/helpers/toast';
import Cam02Constants from '@/constants/CAM/CAM02/cam02';
import { BadgeStatus as BadgeComponent } from '@/components';

const CommitteeChangeDocument = defineAsyncComponent((): Promise<typeof import('./components/CommitteeChangeDocument.vue')> => import('./components/CommitteeChangeDocument.vue'));

const route = useRoute();
const router = useRouter();

const preProcurementDetailStore = usePPDetailStore();
const store = useCam02DetailStore();
const menuStore = useMenuStore();

const id = ref(route.params?.id);
const documentRef = ref<InstanceType<typeof CommitteeChangeDocument> | null>(null);
const isFormDirty = ref(false);
const isInitialized = ref(false);

const routeItems = ref([
  { label: 'รายการแก้ไขคณะกรรมการ', url: '/cam/cam02' },
  { label: 'ขอแก้ไขคณะกรรมการ' },
] as MenuItem[]);

const getDefaultAcceptor = async () => {
  if(store.procurementDetail.sourceType === SourceType.Appoint)
  {
    await store.getDefaultAcceptorAppointAsync();
  }
  else if(store.procurementDetail.sourceType === SourceType.PurchaseRequisition)
  {
    await store.getDefaultAcceptorJp004Async();
  }
  else if(store.procurementDetail.sourceType === SourceType.Jp005)
  {
    await store.getDefaultAcceptorJp005Async();
  }
  else if (store.procurementDetail.sourceType === SourceType.PurchaseOrderApproval)
  {
    await store.getDefaultAcceptorPurchaseOrderApprovalAsync();
  }
  else if (store.procurementDetail.sourceType === SourceType.PrincipleApproval)
  {
    await store.getDefaultAcceptorPrincipleApprovalAsync();
  }
};

onMounted(async (): Promise<void> => {
  if (!store.procurementDetail.acceptors.some(a => a.acceptorType === AcceptorType.Approver)) {
    await getDefaultAcceptor();
  }

  if (store.procurementDetail.procurementId) {
    store.onGetCommitteeGroupTypeAsync(store.procurementDetail.procurementId);
  }

  await nextTick();
  isInitialized.value = true;
});

const saveDocumentFirst = (): Promise<void> => {
  return new Promise<void>((resolve): void => {
    if (currentTab.value === 'document' && documentRef.value?.saveDocumentFirst) {
      documentRef.value.saveDocumentFirst().then(resolve);
    } else {
      resolve();
    }
  });
};

const sendApproveAsync = async (status: Cam02Status): Promise<void> => {
  if (!store.procurementDetail.acceptors.some(x => x.acceptorType === AcceptorType.Approver)) {
    return ToastHelper.factionAtLeastMessageToast();
  }

  if (!(await showConfirmDialogAsync(ConfirmDialogType.SendApprove))) return;

  if (id.value) {
    await store.onUpdateCommitteeChange(id.value.toString(), status);

    await store.onGetCommitteeChangeById(id.value.toString());
  }
};

const handleAssigneeComment = (e: { reason: string; userId: string }): Promise<void> => store.onAssigneeCommentAsync(e.reason);

const onSubmit = async (): Promise<void> => {
  await saveDocumentFirst();

  if (id.value) {
    if (isFormDirty.value
      && store.procurementDetail.documentId && store.procurementDetail.status && [Cam02Status.Draft, Cam02Status.Edit, Cam02Status.Rejected].includes(store.procurementDetail.status)) {
      const saveOption = await showSaveOptionDialogAsync();
      if (saveOption === undefined) return;
      store.procurementDetail.isResetTemplace = saveOption;
    }

    isInitialized.value = false;
    await store.onUpdateCommitteeChange(id.value.toString(), store.procurementDetail.status!);
    isFormDirty.value = false;
    await nextTick();
    isInitialized.value = true;

    await store.onGetCommitteeChangeById(id.value.toString());
    return;
  }

  const newId = await store.onCreateCommitteeChange(Cam02Status.Draft);

  if (newId) {
    id.value = newId;

    router.replace(`detail/${newId}`);

    await store.onGetCommitteeChangeById(newId);
  }
};

const updateAssignee = async (isConfirm: boolean ): Promise<void> => {
  await store.onAssignAsync(isConfirm);

  if (!store.procurementDetail.acceptors.some(a => a.acceptorType === AcceptorType.Approver)) {
    await store.getDefaultAcceptor();
  }
};

onUnmounted(() => {
  preProcurementDetailStore.resetPreProcurementDetail();
});

const showDialogProcurement = ref(false);

watch(
  () => store.procurementDetail,
  () => {
    if (isInitialized.value) {
      isFormDirty.value = true;
    }
  },
  { deep: true }
);

watch(
  () => preProcurementDetailStore.procurementDetail.id,
  (val) => {
    if (val) {
      store.procurementDetail.procurementId = val;
      store.onGetCommitteeGroupTypeAsync(val);
    }
  }
);

const HeaderItem = ref([
  {
    label: 'รายละเอียด',
    value: 'detail',
  },
  {
    label: 'เอกสารขอแก้ไขคณะกรรมการ',
    value: 'document',
  },
] as Option[]);

const currentTab = ref('detail');

watch(
  () => store.procurementDetail.newCommittees,
  (newCommittees: TListCam02Committee[]): void => {
    if (!newCommittees) return;
    if (store.isLoadingData) return;

    store.procurementDetail.acceptors = [
      ...store.procurementDetail.acceptors.filter((a): boolean => a.acceptorType === AcceptorType.Approver),
      ...newCommittees.map((c, i): ParticipantsCommitteeAcceptor => ({
        userId: c.suUserId,
        fullName: c.fullName,
        positionName: c.fullPositionName,
        committeePositionsCode: c.committeePositionsCode,
        committeePositionName: c.committeePositionsName,
        sequence: i + 1,
        acceptorType: committeeAcceptorType.value,
        status: AcceptorStatus.Draft,
        isUnableToPerformDuties: false,
        departmentName: '',
      })),
    ];
  }
);

watch(
  () => store.procurementDetail.isJorPorComment,
  async (val: boolean | undefined): Promise<void> => {
    if (store.isLoadingData) return;
    if (val) {
      await store.getDefaultJorporAsync();
    } else {
      store.procurementDetail.assignees = [];
    }
  }
);

const committeeAcceptorType = computed((): AcceptorType => {
  const map: Record<string, AcceptorType> = {
    TOR: AcceptorType.TorDraftCommittee,
    MedianPrice: AcceptorType.MedianPriceCommittee,
    ProcurementCommittee: AcceptorType.ProcurementCommittee,
    InspectionCommittee: AcceptorType.InspectionCommittee,
    MaintenanceInspectionCommittee: AcceptorType.InspectionCommittee,
    RentCommittee: AcceptorType.RentCommittee,
    AcceptanceCommittee: AcceptorType.AcceptanceCommittee,
  };
  return map[store.procurementDetail.committeeType] ?? AcceptorType.TorDraftCommittee;
});

const canEditDocument = computed(() => {
  return [Cam02Status.Draft, Cam02Status.Edit, Cam02Status.Rejected, Cam02Status.WaitingComment].includes(store.procurementDetail.status!)
});

</script>

<template>
  <TitleHeader :route-items="routeItems" label="ขอแก้ไขคณะกรรมการ">
    <template #action>
      <div class="flex items-center gap-2">
        <p class="text-sm">สถานะ :</p>
        <BadgeComponent :color="Cam02Constants.Cam02BadgeStatus(store.procurementDetail.status).color"
          :label="Cam02Constants.Cam02StatusName(store.procurementDetail.status)" />
      </div>
      <Button label="ประวัติการใช้งาน" icon="pi pi-refresh" variant="outlined" severity="primary"
        v-if="store.procurementDetail.id" class="bg-white! hover:bg-red-50!"
        @click="() => showActivityDialog(store.procurementDetail.id!)" />
    </template>
  </TitleHeader>

  <Form @submit="onSubmit" v-slot="{ handleSubmit }">
    <PlanSelect :procurement="store.procurementDetail.procurement" />
    <div class="grid grid-cols-1 lg:grid-cols-7 gap-2">
      <div class="lg:col-span-5 lg:order-1 order-2">
        <Tabs :value="currentTab" unstyled @update:value="(tab) => (currentTab = tab.toString())">
          <TabHeader class="sticky top-[55px] z-3 bg-[#F7F7F7] pt-2"
            :items="HeaderItem.filter((_h, i) => store.procurementDetail.id ? true : i === 0)" />
          <TabPanels>
            <TabPanel value="detail">
              <SourceSelect v-if="store.procurementDetail.procurementId"
                @committee-type-changed="() => { if (!store.procurementDetail.acceptors.some((a) => a.acceptorType === AcceptorType.Approver)) getDefaultAcceptor(); }" />
              <ListCommittee v-model="store.procurementDetail.oldCommittees"
                v-if="store.procurementDetail.oldCommittees" label="(จากเดิม) บุคคล/คณะกรรมการ"
                :committeeType="store.procurementDetail.committeeType as CommitteeType"
                :readonly="true" />
              <ListCommittee v-model="store.procurementDetail.newCommittees"
                v-if="store.procurementDetail.newCommittees" label="(แก้ไขเป็น) บุคคล/คณะกรรมการ"
                :committeeType="store.procurementDetail.committeeType as CommitteeType"
                :readonly="!store.isCanEdit || !menuStore.hasManage" />
            </TabPanel>
            <TabPanel value="document">
              <Card>
                <template #content>
                  <CommitteeChangeDocument v-model="store.procurementDetail.documentId" ref="documentRef"
                    :readonly="!canEditDocument"
                    :versions="store.procurementDetail.documentVersions" />
                </template>
              </Card>
            </TabPanel>
          </TabPanels>
        </Tabs>
      </div>
      <div class="relative lg:col-span-2 lg:order-2 order-1">
        <div class="flex flex-col gap-4 lg:ml-3 sticky top-[55px] pt-2 z-3 bg-[#F7F7F7]">
          <div class="flex items-center gap-2 justify-end" v-if="menuStore.hasManage">
            <div id="approve-section" class="flex items-center gap-2">
              <ButtonRecall v-if="store.canRecallCommittee" @click="store.onRecallAsync(Cam02Status.Edit)" />
              <ButtonSave type="submit" v-if="store.isCanEdit" />
              <ButtonSendApprove @click="handleSubmit(() => sendApproveAsync(Cam02Status.WaitingCommitteeApproval))"
                v-if="store.isCanEdit && store.procurementDetail.id" />
              <ButtonNotAgree @click="() => store.onRejectedAsync()" v-if="store.canCommitteeAcceptAndReject" />
              <ButtonApprove @click="() => store.onApprovedAsync()" v-if="store.canCommitteeAcceptAndReject" />

              <div id="jorpor-section" class="flex items-center gap-2" v-if="store.isJorPorSection">
                <ButtonSendEdit v-if="store.isJorPorAssign" @click="() => store.onAssigneeRejectedAsync()" />
                <ButtonSave text="บันทึกผู้รับผิดชอบ" @click="updateAssignee(false)"
                  v-if="store.isJorPorAssign || store.isJorPorAssignByAssignee" />

                <ButtonSave  @click="updateAssignee(false)"
                  v-if="store.isJorPorComment" />
                <ButtonConfirmAssign v-if="store.isJorPorAssignByAssignee"
                  @click="() => handleSubmit(() => updateAssignee(true))" />
                <ButtonApproveConfirm v-if="store.isJorPorComment"
                  @click="() => handleSubmit(() => store.onJorPorSendApprovalAsync())" />
              </div>

              <ButtonRecall v-if="store.isCanReCall" @click="store.onRecallAsync(Cam02Status.WaitingComment)" />
              <ButtonSendEdit v-if="store.isCurrentApprover" @click="handleSubmit(store.onRejectedAsync)" />
              <ButtonApprove v-if="store.isCurrentApprover && !store.isLastApprover" @click="() => store.onApprovedAsync()"/>
              <ButtonConfirm v-if="store.isCurrentApprover && store.isLastApprover" @click="() => store.onApprovedAsync()" />
            </div>
          </div>
          <Accordion :value="['0', '1', '2']" unstyled multiple>
            <AccordionPanel value="0" class="mt-5">
              <AccordAcceptor :title="committeeGroupTypeMappingName(store.procurementDetail.committeeType)"
                v-model="store.procurementDetail.acceptors" :acceptor-type="committeeAcceptorType"
                :is-disable="!store.isCommitteeApproval || !menuStore.hasManage"
                @setIsUnableToPerformDuties="(e: boolean, acceptorId: string, remark?: string) => handleSubmit(() => store.onSetIsUnableToPerformDutiesByIdAsync(acceptorId, e, remark))"
                isApprove />
            </AccordionPanel>
            <AccordionPanel value="1" class="mt-5" v-if="store.procurementDetail.isJorPorComment">
              <AccordAssignee title="เจ้าหน้าที่พัสดุให้ความเห็น"
                v-model="store.procurementDetail.assignees" :group="AssigneeGroup.JorPor"
                :disabled="(!store.isJorPorAssign && !store.isJorPorComment && !store.isJorPorAssignByAssignee) || !menuStore.hasManage"
                :is-comment="store.isJorPorComment"
                @on-comment="handleAssigneeComment" />
            </AccordionPanel>
            <AccordionPanel value="2" class="mt-5">
              <AccordAcceptor title="ผู้มีอำนาจเห็นชอบ/อนุมัติ" v-model="store.procurementDetail.acceptors"
                @set-default="() => getDefaultAcceptor()" :acceptor-type="AcceptorType.Approver" isManage
                :is-disable="(!store.isCanEdit && !store.isJorPorComment) || !menuStore.hasManage" isApprove :is-set-default="true" />
            </AccordionPanel>
          </Accordion>
        </div>
      </div>
    </div>
  </Form>

  <div>
    <div class="grid grid-cols-1 lg:grid-cols-7 gap-2 z-50">
      <div class="lg:col-span-5 mb-5 my-1">
        <UploadFileGroup v-model="store.procurementDetail.attachments" @upload="store.onUpsertAttachments"
          @remove-file="store.onUpsertAttachments" @remove-group="store.onUpsertAttachments"
          @reorder="store.onUpsertAttachments" :disabled="store.procurementDetail.status === Cam02Status.Approved || !menuStore.hasManage
            " />
      </div>
    </div>
  </div>

  <ProcurementDialog v-model="showDialogProcurement"> </ProcurementDialog>
</template>

<style scoped lang="scss">
.labeled-text {
  display: flex;
  flex-direction: column;
  top: 0;
  gap: 0;
}

.label {
  font-size: 16px;
  font-weight: 500;
  color: #bbbbbb;
}

.text {
  font-size: 20px;
  font-weight: 400;
  color: #374151;
}

.text-gray-500 {
  color: #6b7280;
}
</style>
