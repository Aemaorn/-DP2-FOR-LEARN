<script setup lang="ts">
import { watch, onMounted, computed, ref } from 'vue';
import { Button, Card } from 'primevue';
import { useRoute, useRouter } from 'vue-router';
import TitleHeader from '@/components/cosmetic/TitleHeader.vue';
import { Datepicker, InputArea, InputField, Select } from '@/components/forms';
import { BadgeStatus as BadgeComponent } from '@/components';
import { getYearOptionsWithValue } from '@/constants/date';
import { formatCurrency } from '@/helpers/currency';
import { usePl002DetailStore } from '@/stores/PL/pl002';
import { storeToRefs } from 'pinia';
import { PlanAnnouncementStatus } from '@/enums/planAnnouncement';
import { ButtonSendCancel, ButtonSendChange } from '@/components/Button';
import { checkIsSixty } from '@/helpers/supplyMethod';
import InfoRow from '@/components/cosmetic/InfoRow.vue';
import { useMenuStore } from '@/stores/menu';
import { useAuthenticationStore } from '@/stores/authentication';
import { DepartmentId } from '@/enums/businessUnit';
import PlanSelectDialog from './PlanSelectDialog.vue';

const router = useRouter();
const menuStore = useMenuStore();
const authStore = useAuthenticationStore();
const detailStore = usePl002DetailStore();
const { canEdit, AssignCanEdit } = storeToRefs(detailStore);

const route = useRoute();
const id = computed(() => route.params?.id);

onMounted(async () => {
  await detailStore.getSupplyMethodDDLAsync();
  await detailStore.getAssignDepartmentDDLAsync();
});

const onCheckSixty = computed(() => checkIsSixty(detailStore.body.supplyMethodCode));

watch(
  [() => detailStore.body.year, () => detailStore.body.supplyMethodCode],
  async ([year, supplyMethodCode]) => {
    if (year && supplyMethodCode && !id.value && detailStore.body.planSelected.length === 0) {
      await detailStore.getAnnualPlan();
    }
  }
);

const yearOptions = computed(() => {
  return getYearOptionsWithValue(detailStore.body.year);
});

const planSelectDialogRef = ref<InstanceType<typeof PlanSelectDialog> | null>(null);
</script>

<template>
  <Card>
    <template #content>
      <div class="mt-6 mr-6">
        <div class="grid lg:grid-cols-3 gap-2 gap-y-8">
          <InputField label="เลขที่คำขอเผยแพร่แผนจัดซื้อจัดจ้าง" v-model="detailStore.body.planAnnouncementNumber"
            disabled />
          <Datepicker label="วันที่เอกสาร" v-model="detailStore.body.documentDate"
            :disabled="!canEdit || !menuStore.hasManage" />
          <InputField label="เลขกลุ่ม e-GP" v-if="onCheckSixty" v-model="detailStore.body.groupEgpNumber"
            :disabled="!canEdit || !menuStore.hasManage" :rules="onCheckSixty ? 'required' : ''" />
          <Select class="lg:col-start-1" label="ปีงบประมาณ" :options="yearOptions" v-model="detailStore.body.year"
            rules="required" :disabled="!canEdit || !menuStore.hasManage" />
          <Select label="วิธีการจัดหา" :options="detailStore.supplyMethodCodeDDL"
            v-model="detailStore.body.supplyMethodCode" rules="required"
            @update:model-value="() => detailStore.body.groupEgpNumber = undefined"
            :disabled="!canEdit || !menuStore.hasManage" />

          <InputArea label="รายละเอียดโครงการ" class="lg:col-start-1 lg:col-span-2" v-model="detailStore.body.remark"
            :row="5" :disabled="!canEdit || !menuStore.hasManage" />
          <InputField class="lg:col-start-1" label="ชื่อประกาศ" v-model="detailStore.body.announcementTitle"
            id="annoucementTitle" name="annoucementTitle" :rules="AssignCanEdit ? 'required' : ''"
            :disabled="!canEdit || !menuStore.hasManage" />

          <Datepicker label="วันที่ประกาศ" v-model="detailStore.body.announcementDate" id="announcementDate"
            name="announcementDate" disabled />

          <InputField class="lg:col-start-1" label="เบอร์โทร" v-model="detailStore.body.telephone" id="telephone"
            name="telephone" :disabled="!canEdit || !menuStore.hasManage" />
        </div>
      </div>
    </template>
  </Card>
  <Card class="mt-4">
    <template #content>
      <TitleHeader label="รายการจัดซื้อจัดจ้าง (รวมปี)">
        <template #action>
          <Button v-if="canEdit && menuStore.hasManage && authStore.profile.departmentCode === DepartmentId.JorPor" label="ดึงรายการจัดซื้อจัดจ้างเพิ่มเติม"
            icon="pi pi-list-check" severity="primary" variant="outlined"
            @click="planSelectDialogRef?.open()" />
        </template>
      </TitleHeader>
      <div v-for="(data, index) in detailStore.body.planSelected" :key="data.planId"
        class="p-4 border border-gray-300 rounded-xl mt-4">
        <div class="grid lg:grid-cols-12 gap-x-6 gap-y-4">
          <div class="lg:col-span-8">
            <InfoRow label="เลขที่รายการจัดซื้อจัดจ้าง">
              <p class="underline text-blue-500 cursor-pointer"
                @click="() => router.push({ name: 'pl001Detail', params: { id: data.refId ?? data.planId } })">
                {{ data.planNumber }}
              </p>
            </InfoRow>

            <InfoRow label="ชื่อโครงการ">
              <p class="font-bold">{{ data.planTitle }}</p>
            </InfoRow>

            <InfoRow label="วงเงินงบประมาณ">
              <p>{{ formatCurrency(data.budget) }}</p>
            </InfoRow>

            <InfoRow label="ฝ่าย/ภาคเขต">
              <p>{{ data.departmentName }}</p>
            </InfoRow>

            <InfoRow label="วิธีจัดหา">
              <p>{{
                `${data.supplyMethodName} ${data.supplyMethodTypeName ? `: ${data.supplyMethodTypeName}` : ''
                }`
                }}</p>
            </InfoRow>

            <InfoRow label="เลขที่ e-GP">
              <template v-if="canEdit && checkIsSixty(detailStore.body.supplyMethodCode)">
                <InputField v-model="data.egpNumber" :disabled="!canEdit || !menuStore.hasManage" />
              </template>
              <template v-else>
                <p>{{ data.egpNumber ?? '-' }}</p>
              </template>
            </InfoRow>
          </div>

          <div class="lg:col-span-4 flex flex-col items-end justify-between gap-4">
            <div class="flex items-start gap-2" v-if="canEdit && menuStore.hasManage">
              <Button v-if="!!detailStore.body.approveDocumentId" icon="pi pi-refresh" label="ส่งกลับแก้ไข"
                severity="danger" variant="outlined" @click="detailStore.rejectAnnualPlan(data.planId)" />
              <Button v-if="authStore.profile.departmentCode === DepartmentId.JorPor"
                icon="pi pi-trash" severity="danger" variant="text" @click="detailStore.onRemovePlan(index)" />
            </div>

            <div class="flex items-start justify-end gap-2"
              v-if="detailStore.body.status === PlanAnnouncementStatus.Announcement">
              <div>
                <BadgeComponent v-if="data.isCancel" label="มีการขอยกเลิก" color="red" />
                <BadgeComponent v-if="data.isChange" label="มีการขอเปลี่ยนแปลง" color="yellow" />
              </div>

              <ButtonSendChange v-if="!data.isCancel && !data.isChange && menuStore.hasManage"
                @click="() => detailStore.onSendEditOrSendCancelAsync(data.planId, true)" />
              <ButtonSendCancel v-if="!data.isCancel && !data.isChange && menuStore.hasManage"
                @click="() => detailStore.onSendEditOrSendCancelAsync(data.planId)" />
            </div>
          </div>
        </div>
      </div>
    </template>
  </Card>

  <PlanSelectDialog ref="planSelectDialogRef" />
</template>