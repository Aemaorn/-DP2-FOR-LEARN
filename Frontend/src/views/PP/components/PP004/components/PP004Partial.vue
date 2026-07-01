<script setup lang="ts">
import { computed } from 'vue';
import {
  OriginReasonSection,
  DeliveryConditionSection,
  UserListSection,
  PaymentConditionListSection,
} from './sub';
import { BudgetProcurement, ConsiderInformation, CriteriaConditionSection, ScopeOfWorkList } from '../../PP';
import { pp004CommitteeType } from '@/views/PP/enums/pp004';
import { usePP004Store } from '@/views/PP/stores/PP004/pp004Store';
import TORObjectReason from './sub/TORObjectReason.vue';

const pp004Store = usePP004Store();

const props = defineProps({
  preProcurementId: {
    type: String,
  },
  budget: {
    type: Number,
    default: 0,
  },
  torId: {
    type: String,
  }
});

const procCommittee = computed({
  get: () => pp004Store.body.committees.filter(item => item.groupType === pp004CommitteeType.ProcurementCommittee),
  set: (newValue) => {
    pp004Store.body.committees = [
      ...pp004Store.body.committees.filter(item => item.groupType !== pp004CommitteeType.ProcurementCommittee),
      ...newValue.map(item => ({ ...item, groupType: pp004CommitteeType.ProcurementCommittee }))
    ];
  }
});

const inspCommittee = computed({
  get: () => pp004Store.body.committees.filter(item => item.groupType === pp004CommitteeType.InspectionCommittee),
  set: (newValue) => {
    pp004Store.body.committees = [
      ...pp004Store.body.committees.filter(item => item.groupType !== pp004CommitteeType.InspectionCommittee),
      ...newValue.map(item => ({ ...item, groupType: pp004CommitteeType.InspectionCommittee }))
    ];
  }
});

const maCommittee = computed({
  get: () => pp004Store.body.committees.filter(item => item.groupType === pp004CommitteeType.MaintenanceInspectionCommittee),
  set: (newValue) => {
    pp004Store.body.committees = [
      ...pp004Store.body.committees.filter(item => item.groupType !== pp004CommitteeType.MaintenanceInspectionCommittee),
      ...newValue.map(item => ({ ...item, groupType: pp004CommitteeType.MaintenanceInspectionCommittee }))
    ];
  }
});

const supCommittee = computed({
  get: () => pp004Store.body.committees.filter(item => item.groupType === pp004CommitteeType.ConstructionSupervisor),
  set: (newValue) => {
    pp004Store.body.committees = [
      ...pp004Store.body.committees.filter(item => item.groupType !== pp004CommitteeType.ConstructionSupervisor),
      ...newValue.map(item => ({ ...item, groupType: pp004CommitteeType.ConstructionSupervisor }))
    ];
  }
});

const procUserIds = computed(() => procCommittee.value.map(item => item.suUserId));
const inspUserIds = computed(() => inspCommittee.value.map(item => item.suUserId));
</script>

<template>
  <TORObjectReason v-model="pp004Store.body.torObjectResponses" v-if="pp004Store.body.torObjectResponses.length >= 1" />
  <OriginReasonSection />
  <ScopeOfWorkList v-if="pp004Store.body.scopeOfWorks.length >= 1" v-model="pp004Store.body.scopeOfWorks" is-disabled />
  <ConsiderInformation v-model="pp004Store.body.requisition" :is-over-price="props.budget > 100000" :number="pp004Store.body.requisition.purchaseRequisitionNumber"
    title="การแจ้งข้อมูลเบื้องต้น (จพ.004)" :is-disabled="!pp004Store.IsEdit" v-model:document-date="pp004Store.body.requisition.documentDate"/>
  <BudgetProcurement v-model="pp004Store.body.budgets" :disabled="!pp004Store.IsEdit" :template-code="pp004Store.body.torTemplateCode"/>
  <PaymentConditionListSection />
  <DeliveryConditionSection />
  <CriteriaConditionSection v-model="pp004Store.body.requisition.evaluationCriteriaCode"
    :is-disabled="!pp004Store.IsEdit" />
  <UserListSection :disable="pp004Store.IsEdit" :showOption="false" person="ผู้จัดซื้อ/ผู้จัดจ้าง"
    label="ผู้จัดซื้อ/คณะกรรมการจัดซื้อจัดจ้าง" v-model:committee="procCommittee"
    v-model:spacialOption="pp004Store.positionProcOptions" v-model:is-committee="pp004Store.body.isProcurementCommittee"
    :groupType="pp004CommitteeType.ProcurementCommittee" :excludeUserIds="inspUserIds" />
  <UserListSection :disable="pp004Store.IsEdit" :showOption="false" label="ผู้ตรวจรับพัสดุ/คณะกรรมการตรวจรับพัสดุ"
    person="ผู้ตรวจรับพัสดุ" v-model:committee="inspCommittee" v-model:spacialOption="pp004Store.positionInspOptions"
    v-model:is-committee="pp004Store.body.isInspectCommittee" :groupType="pp004CommitteeType.InspectionCommittee"
    :excludeUserIds="procUserIds" />
  <UserListSection :disable="pp004Store.IsEdit" :showOption="true" person="ผู้ตรวจรับพัสดุงานจ้างบริการบำรุงรักษา"
    label="คณะกรรมการตรวจรับพัสดุ (งานจ้างบริการบำรุงรักษา)" v-model:committee="maCommittee"
    v-model:spacialOption="pp004Store.positionMaOptions" v-model:is-committee="pp004Store.body.isMaCommittee"
    v-model:isHas="pp004Store.body.requisition.hasInspectionCommittee"
    :groupType="pp004CommitteeType.MaintenanceInspectionCommittee" />
  <UserListSection :disable="pp004Store.IsEdit" :showOption="true" person="ผู้ควบคุมงาน"
    label="ผู้ควบคุมงาน (เฉพาะงานก่อสร้าง)" v-model:committee="supCommittee"
    v-model:spacialOption="pp004Store.positionSupOptions" v-model:is-committee="pp004Store.body.isSupCommittee"
    v-model:isHas="pp004Store.body.requisition.hasConstructionSupervisor"
    :groupType="pp004CommitteeType.ConstructionSupervisor" />
</template>