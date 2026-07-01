<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { CriteriaGroupButton, Datepicker, InputField, Select, StatusGroupButton } from '@/components/forms';
import { BadgeStatus as BadgeComponent, Pagination } from '@/components';
import InfoRow from '@/components/cosmetic/InfoRow.vue';
import { YearOptions } from '@/constants/date';
import { PlanAnnouncementConstants, SharedConstants } from '@/constants';
import { formatCurrency } from '@/helpers/currency';
import { ToDateOnly } from '@/helpers/dateTime';
import { usePl002ListStore } from '@/stores/PL/pl002';
import { onBeforeMount, onMounted, watch } from 'vue';
import { useRouter } from 'vue-router';
import { PlanAnnouncementStatus } from '@/enums/planAnnouncement';
import { Form } from 'vee-validate';
import { useMenuStore } from '@/stores/menu';
import { useAuthenticationStore } from '@/stores/authentication';
import { DepartmentId } from '@/enums/businessUnit';

const pL002ListStore = usePl002ListStore();
const menuStore = useMenuStore();
const authStore = useAuthenticationStore();
const router = useRouter();
const { AnnouncementStatusColor, AnnouncementStatusName } = PlanAnnouncementConstants;
const { WorkProcessOptions } = SharedConstants;

onBeforeMount(async (): Promise<void> => {
  await pL002ListStore.getSupplyMethodDDLAsync();
});

onMounted(async (): Promise<void> => {
  await pL002ListStore.clearCriteriaAsync();
  onWatchCriteria();
});

const onWatchCriteria = () => {
  watch(() => [
    pL002ListStore.searchCriteria.pageNumber,
    pL002ListStore.searchCriteria.pageSize,
    pL002ListStore.searchCriteria.workProcess,
    pL002ListStore.searchCriteria.status], async (): Promise<void> => {
      await pL002ListStore.getListAsync();
    });
};
</script>
<template>
  <div class="flex flex-col gap-4">
    <TitleHeader label="ขออนุมัติเผยแพร่แผนจัดซื้อจัดจ้าง">
      <template #action>
        <Button label="สร้างรายการขอเผยแพร่แผน" icon="pi pi-plus" severity="primary" variant="outlined"
          class="hover:bg-red-200 hover:text-red-900 bg-red-50" @click="() => router.push({ name: 'pl002Detail' })"
          v-if="menuStore.hasManage && authStore.profile.departmentCode === DepartmentId.JorPor" />
      </template>
    </TitleHeader>
    <Card>
      <template #content>
        <Form @submit="pL002ListStore.getListAsync">
          <CriteriaGroupButton :options="WorkProcessOptions" v-model="pL002ListStore.searchCriteria.workProcess" />
          <div class="grid grid-cols-1 lg:grid-cols-5 gap-2 gap-y-6 mt-6">
            <InputField label="คำค้นหา" class="lg:col-span-3 col-span-1"
              v-model.trim="pL002ListStore.searchCriteria.searchText" />
            <Select label="วิธีการจัดหา" class="lg:col-start-1 col-start-auto" :options="pL002ListStore.supplyMethodDDL"
              v-model="pL002ListStore.searchCriteria.supplyMethodCode" @enterClose="pL002ListStore.getListAsync" />
            <Select label="ปีงบประมาณ ตั้งแต่" :options="YearOptions"
              v-model="pL002ListStore.searchCriteria.fromBudgetYear" @enterClose="pL002ListStore.getListAsync" />
            <Select label="ถึง" :options="YearOptions" v-model="pL002ListStore.searchCriteria.toBudgetYear"
              @enterClose="pL002ListStore.getListAsync" />
            <Datepicker label="วันที่เผยแพร่ ตั้งแต่" class="col-start-auto lg:col-start-1"
              v-model="pL002ListStore.searchCriteria.fromAnnouncementDate" />
            <Datepicker label="ถึง" v-model="pL002ListStore.searchCriteria.toAnnouncementDate" />
            <div class="lg:col-span-3 flex items-end justify-end gap-2">
              <ButtonSearch class="lg:w-fit w-full" type="submit" />
              <ButtonClear class="lg:w-fit w-full" @click="pL002ListStore.clearCriteriaAsync" />
            </div>
          </div>
        </Form>
      </template>
    </Card>

    <Card>
      <template #content>
        <StatusGroupButton v-model="pL002ListStore.searchCriteria.status"
          :optionBadges="pL002ListStore.statusOptionBadge" />
        <DataView :value="pL002ListStore.table.data" data-key="id">
          <template #list="{ items }">
            <div v-for="(data) in items" :key="data.id" class="border-1 border-gray-300 rounded-sm mb-2 p-1">
              <div class="flex flex-col-reverse lg:grid lg:grid-cols-12 px-2">
                <div class="lg:col-span-9">
                  <InfoRow label="เลขที่คำขอเผยแพร่แผนจัดซื้อจัดจ้าง">
                    <p class="text-blue-400 underline cursor-pointer"
                      @click="() => router.push({ name: 'pl002Detail', params: { id: data.id } })">
                      {{ data.announcementNumber || '-' }}
                    </p>
                  </InfoRow>

                  <InfoRow label="ชื่อประกาศ">
                    <p :class="`${data.announcementName ? 'underline font-bold' : ''}`">
                      {{ data.announcementName || '-' }}
                    </p>
                  </InfoRow>

                  <InfoRow label="ปีงบประมาณ">
                    <p>{{ data.year || '-' }}</p>
                  </InfoRow>

                  <InfoRow label="จำนวนรายการจัดซื้อจัดจ้าง">
                    <p>{{ data.planCount || '-' }}</p>
                  </InfoRow>

                  <InfoRow label="งบประมาณรวม">
                    <p>{{ formatCurrency(data.summaryBudget) }}</p>
                  </InfoRow>

                  <InfoRow label="วิธีจัดหา">
                    <p>{{ data.supplyMethodName || '-' }}</p>
                  </InfoRow>

                  <InfoRow label="วันที่เผยแพร่">
                    <p>{{ ToDateOnly(data.announcementDate) || '-' }}</p>
                  </InfoRow>
                </div>

                <div class="flex items-start justify-end gap-1.5 lg:col-span-3 mb-2 lg:mb-0">
                  <div class="flex items-center gap-2 mt-2 mr-2">
                    <p class="text-sm">สถานะ :</p>
                    <BadgeComponent :label="AnnouncementStatusName(data.status)"
                      :bg-color-class="AnnouncementStatusColor(data.status).bgColorClass"
                      :text-color-class="AnnouncementStatusColor(data.status).textColorClass" />
                  </div>
                  <Button icon="pi pi-pen-to-square" class="text-blue-600! hover:bg-blue-300/20! focus:bg-blue-300/20!"
                    size="small" variant="text"
                    @click="() => router.push({ name: 'pl002Detail', params: { id: data.id } })" />
                  <Button icon="pi pi-trash" class="text-red-600! hover:bg-red-300/20! focus:bg-red-300/20!"
                    size="small" variant="text"
                    v-if="[PlanAnnouncementStatus.Draft, PlanAnnouncementStatus.Rejected].includes(data.status) && menuStore.hasManage && authStore.profile.departmentCode === DepartmentId.JorPor"
                    @click="() => pL002ListStore.deleteByIdAsync(data.id)" />
                </div>
              </div>
            </div>
          </template>

          <template #empty>
            <p class="text-center">ไม่พบข้อมูล</p>
          </template>
        </DataView>
        <Pagination :page-number="pL002ListStore.searchCriteria.pageNumber"
          :page-size="pL002ListStore.searchCriteria.pageSize" :total-record="pL002ListStore.table.totalRecords"
          @change="pL002ListStore.onChangePageSize" />
      </template>
    </Card>
  </div>
</template>