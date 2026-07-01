<script setup lang="ts">
import type { CM001Table } from '@/models/CM/cm001';
import { useRouter } from 'vue-router';
import { Pagination, BadgeStatus as BadgeComponent } from '@/components';
import { TitleHeader } from '@/components/cosmetic';
import { InputField, Select, CriteriaGroupButton, StatusGroupButton } from '@/components/forms';
import { SharedConstants } from '@/constants';
import { onBeforeMount, onMounted, watch, ref } from 'vue';
import { formatCurrency } from '@/helpers/currency';
import { useCm001ListStore } from '@/stores/CM/cm001';
import { CM001Helper } from '@/helpers/CM/cm001';
import { Form } from 'vee-validate';
import InfoRow from '@/components/cosmetic/InfoRow.vue';
import { ProcurementType } from '@/enums/procurement';
import type { Option } from '@/models/shared/option';
import SharedService from '@/services/Shared/dropdown';
import { EGroupCode } from '@/enums/shared';
import { HttpStatusCode } from 'axios';
import { usePcmContractDraftStore } from '@/stores/PCM/PCM005/pcmContractDraft';
import { useMenuStore } from '@/stores/menu';
import { Button } from 'primevue';
import { souceType } from '@/enums/CM/cm001';


const menuStore = useMenuStore();
const router = useRouter();
const store = useCm001ListStore();
const storeContract = usePcmContractDraftStore();
const dropdown = ref<Array<Option>>([]);
const dropdownContract = ref<Array<Option>>([]);

const { BadgeStatus } = CM001Helper;
const { WorkProcessOptions } = SharedConstants;

onBeforeMount(() => {
  store.fn.getDefaultDropdownCriteriaAsync();
});

onMounted(async () => {
  await store.fn.getListAsync();
  await storeContract.api.getCmRentalTypeAsync();
  await onGetContractDropdownAsync();
  onWatchCriteria();
});

const onGetContractDropdownAsync = async () => {
  const [department, rentalType] = await Promise.all([
    SharedService.onGetParameterByGroupCodeAsync(EGroupCode.CType),
    SharedService.onGetParameterByGroupCodeAsync(EGroupCode.CMRentalType, undefined, undefined, EGroupCode.CMType)]);

  if (department.status === HttpStatusCode.Ok) {
    dropdown.value = department.data;
  }

  if (rentalType.status === HttpStatusCode.Ok) {
    dropdownContract.value = rentalType.data;
  }
};

const onWatchCriteria = () => {
  watch(() => [
    store.searchCriteria.pageNumber,
    store.searchCriteria.pageSize,
    store.searchCriteria.workProcess,
    store.searchCriteria.status], async (): Promise<void> => {
      await store.fn.getListAsync();
    });
};

const onRounte = (id: string, type: ProcurementType) => {
  const routeData = router.resolve({
    name: type === ProcurementType.Procurement ? 'ppDetail' : 'pcm005Detail',
    params: { id }
  });

  window.open(routeData.href, '_blank');
}

const routeToRefData = (data: CM001Table) => {
  if (!data.refId) return;

  if (data.sourceType == souceType.Plan) {
    const routeData = router.resolve({ name: 'pl001Detail', params: { id: data.refId } })
    window.open(routeData.href, '_blank');
  }

  if (data.sourceType == souceType.ContractDraftVendor || data.sourceType == souceType.ContractDraftVendorEdit) {
    if (data.processType) {
      onRounte(data.refId, data.processType)
    }
  }

  if (data.sourceType == souceType.Procurement) {
    const routeData = router.resolve({ name: 'pp001Detail', params: { id: data.refId } })
    window.open(routeData.href, '_blank');
  }
}

const routeToRefPlanData = (data: CM001Table) => {
  if (!data.planId) return;
  const routeData = router.resolve({ name: 'pl001Detail', params: { id: data.planId } })
  window.open(routeData.href, '_blank');
}
</script>

<template>
  <TitleHeader label="บันทึกรายงานผลการตรวจรับ (จพ.008)">
    <template #action>
      <Button label="สร้างรายการตรวจรับ" icon="pi pi-plus" severity="primary" variant="outlined"
        class="hover:bg-red-200 hover:text-red-900 bg-red-50" @click="() => router.push({ name: 'cm001Detail' })"
        v-if="menuStore.hasManage" />
    </template>
  </TitleHeader>
  <Card class="mb-4">
    <template #content>
      <Form @submit="store.fn.getListAsync">
        <CriteriaGroupButton :options="WorkProcessOptions" v-model="store.searchCriteria.workProcess" />
        <div class="mt-10 space-y-8 lg:space-y-10">
          <div class="grid grid-cols-1 lg:grid-cols-5 gap-8 lg:gap-2">
            <InputField label="คำค้นหา" class="lg:col-span-3" v-model.trim="store.searchCriteria.keyword"
              hide-details />
          </div>
          <div class="grid grid-cols-1 lg:grid-cols-5 gap-8 lg:gap-2">
            <Select label="ฝ่าย/ภาค เขต" v-model="store.searchCriteria.departmentCode"
              :options="store.departmentCodeDropdown" @enterClose="store.fn.getListAsync" hide-details />
            <Datepicker label="วันที่ลงนามสัญญาตั้งแต่" v-model="store.searchCriteria.contractDateStart" hide-details />
            <Datepicker label="ถึงวันที่" v-model="store.searchCriteria.contractDateEnd" hide-details />
          </div>
          <div class="grid grid-cols-1 lg:grid-cols-5 gap-8 lg:gap-2">
            <Select label="ประเภทอนุมัติใบสั่งซื้อ/จ้าง/เช่า" :options="dropdown"
              v-model="store.searchCriteria.contractType" hide-details />
            <Select label="ประเภทสัญญา" :options="dropdownContract" v-model="store.searchCriteria.contractTypeCode"
              hide-details />
            <div class="lg:col-span-3 flex items-end justify-end gap-2">
              <ButtonSearch class="lg:w-fit w-full" type="submit" />
              <ButtonClear class="lg:w-fit w-full" @click="() => store.fn.resetCriteriaAsync()" />
            </div>
          </div>
        </div>
      </Form>
    </template>
  </Card>
  <Card class="mb-4">
    <template #content>
      <StatusGroupButton :optionBadges="store.statusOptionBadge" v-model="store.searchCriteria.status" />
      <DataView :value="store.table.data" data-key="id">
        <template #list="{ items }">
          <div v-for="(data, index) in (items as CM001Table[])" :key="index"
            class="border-1 border-gray-300 rounded-sm mb-2 p-1">
            <div class="flex flex-col-reverse lg:grid lg:grid-cols-12 px-2">
              <div class="lg:col-span-8" v-if="data.sourceType == souceType.Plan">
                <InfoRow label="เลขที่แผนจัดซื้อจัดจ้าง">
                  <p class="text-blue-500 underline cursor-pointer w-fit" @click="() => routeToRefData(data)">
                    {{ data.planNumber }}
                  </p>
                </InfoRow>
                <InfoRow label="ชื่อโครงการ">
                  <p class="font-bold">{{ data.name }}</p>
                </InfoRow>
                <InfoRow label="วงเงินงบประมาณ">
                  <p>{{ formatCurrency(data.budget) }}</p>
                </InfoRow>
                <InfoRow label="ฝ่าย/ภาคเขต">
                  <p>{{ data.departmentName }}</p>
                </InfoRow>
                <InfoRow label="วิธีจัดหา">
                  <p>
                    {{ data.supplyMethod }} {{ data.supplyMethodType }} {{ data.supplyMethodSpecialType }}
                  </p>
                </InfoRow>
                <InfoRow v-if="data.glAccounts?.length" label="รหัสบัญชี">
                  <p class="break-words text-base text-gray-600">
                    <template v-for="(gl, glIndex) in data.glAccounts" :key="glIndex">
                      <span v-if="Number(glIndex) > 0" class="mx-1.5 text-gray-300">|</span>
                      <span class="font-bold text-gray-900 tabular-nums">{{ gl.split(' : ')[0] }}</span>
                      <span class="text-gray-900">{{ gl.split(' : ').slice(1).length ? ' ' + gl.split(' : ').slice(1).join(' : ') : '' }}</span>
                    </template>
                  </p>
                </InfoRow>
              </div>
              <div class="lg:col-span-8" v-if="data.sourceType == souceType.ContractDraftVendor">
                <InfoRow label="เลขที่แผนจัดซื้อจัดจ้าง">
                  <p class="text-blue-500 underline cursor-pointer w-fit" @click="() => routeToRefPlanData(data)">
                    {{ data.planNumber }}
                  </p>
                </InfoRow>
                <InfoRow label="เลขที่สัญญา">
                  <p class="text-blue-500 underline cursor-pointer w-fit" @click="() => routeToRefData(data)">
                    {{ data.refNumber }}
                  </p>
                </InfoRow>
                <InfoRow label="เลขที่ PO (SAP)">
                  <p>
                    {{ data.poNumber }}
                  </p>
                </InfoRow>
                <InfoRow label="ชื่อสัญญา">
                  <p>{{ data.name }}</p>
                </InfoRow>
                <InfoRow label="คู่ค้า">
                  <p class="font-bold">{{ data.vendorName }}</p>
                </InfoRow>
                <InfoRow label="วงเงินตามสัญญา">
                  <p>{{ formatCurrency(data.budget) }}</p>
                </InfoRow>
                <InfoRow label="ฝ่าย/ภาคเขต">
                  <p>{{ data.departmentName }}</p>
                </InfoRow>
                <InfoRow label="วิธีจัดหา">
                  <p>
                    {{ data.supplyMethod }} {{ data.supplyMethodType }} {{ data.supplyMethodSpecialType }}
                  </p>
                </InfoRow>
                <InfoRow v-if="data.glAccounts?.length" label="รหัสบัญชี">
                  <p class="break-words text-base text-gray-600">
                    <template v-for="(gl, glIndex) in data.glAccounts" :key="glIndex">
                      <span v-if="Number(glIndex) > 0" class="mx-1.5 text-gray-300">|</span>
                      <span class="font-bold text-gray-900 tabular-nums">{{ gl.split(' : ')[0] }}</span>
                      <span class="text-gray-900">{{ gl.split(' : ').slice(1).length ? ' ' + gl.split(' : ').slice(1).join(' : ') : '' }}</span>
                    </template>
                  </p>
                </InfoRow>
              </div>
              <div class="lg:col-span-8" v-if="data.sourceType == souceType.ContractDraftVendorEdit">
                <InfoRow label="เลขที่แผนจัดซื้อจัดจ้าง">
                  <p class="text-blue-500 underline cursor-pointer w-fit" @click="() => routeToRefPlanData(data)">
                    {{ data.planNumber }}
                  </p>
                </InfoRow>
                <InfoRow label="เลขที่สัญญา">
                  <p class="text-blue-500 underline cursor-pointer w-fit" @click="() => routeToRefData(data)">
                    {{ data.refNumber }}
                  </p>
                </InfoRow>
                <InfoRow label="เลขที่ PO (SAP)">
                  <p>{{ data.poNumber }}</p>
                </InfoRow>
                <InfoRow label="ชื่อสัญญา">
                  <p>{{ data.name }}</p>
                </InfoRow>
                <InfoRow label="คู่ค้า">
                  <p class="font-bold">{{ data.vendorName }}</p>
                </InfoRow>
                <InfoRow label="วงเงินตามสัญญา">
                  <p>{{ formatCurrency(data.budget) }}</p>
                </InfoRow>
                <InfoRow label="ฝ่าย/ภาคเขต">
                  <p>{{ data.departmentName }}</p>
                </InfoRow>
                <InfoRow label="วิธีจัดหา">
                  <p>
                    {{ data.supplyMethod }} {{ data.supplyMethodType }} {{ data.supplyMethodSpecialType }}
                  </p>
                </InfoRow>
                <InfoRow v-if="data.glAccounts?.length" label="รหัสบัญชี">
                  <p class="break-words text-base text-gray-600">
                    <template v-for="(gl, glIndex) in data.glAccounts" :key="glIndex">
                      <span v-if="Number(glIndex) > 0" class="mx-1.5 text-gray-300">|</span>
                      <span class="font-bold text-gray-900 tabular-nums">{{ gl.split(' : ')[0] }}</span>
                      <span class="text-gray-900">{{ gl.split(' : ').slice(1).length ? ' ' + gl.split(' : ').slice(1).join(' : ') : '' }}</span>
                    </template>
                  </p>
                </InfoRow>
              </div>
              <div class="lg:col-span-8" v-if="data.sourceType == souceType.Procurement">
                <InfoRow label="เลขที่แผนจัดซื้อจัดจ้าง">
                  <p class="text-blue-500 underline cursor-pointer w-fit" @click="() => routeToRefPlanData(data)">
                    {{ data.planNumber }}
                  </p>
                </InfoRow>
                <InfoRow label="เลขที่โครงการจัดซื้อจัดจ้าง">
                  <p class="text-blue-500 underline cursor-pointer w-fit" @click="() => routeToRefData(data)">
                    {{ data.refNumber }}
                  </p>
                </InfoRow>
                <InfoRow label="ชื่อโครงการ">
                  <p class="font-bold">{{ data.name }}</p>
                </InfoRow>
                <InfoRow label="วงเงินงบประมาณ">
                  <p>{{ formatCurrency(data.budget) }}</p>
                </InfoRow>
                <InfoRow label="ฝ่าย/ภาคเขต">
                  <p>{{ data.departmentName }}</p>
                </InfoRow>
                <InfoRow label="วิธีจัดหา">
                  <p>
                    {{ data.supplyMethod }} {{ data.supplyMethodType }} {{ data.supplyMethodSpecialType }}
                  </p>
                </InfoRow>
                <InfoRow v-if="data.glAccounts?.length" label="รหัสบัญชี">
                  <p class="break-words text-base text-gray-600">
                    <template v-for="(gl, glIndex) in data.glAccounts" :key="glIndex">
                      <span v-if="Number(glIndex) > 0" class="mx-1.5 text-gray-300">|</span>
                      <span class="font-bold text-gray-900 tabular-nums">{{ gl.split(' : ')[0] }}</span>
                      <span class="text-gray-900">{{ gl.split(' : ').slice(1).length ? ' ' + gl.split(' : ').slice(1).join(' : ') : '' }}</span>
                    </template>
                  </p>
                </InfoRow>
              </div>
              <div class="lg:col-span-8" v-if="data.sourceType == souceType.Manual">
                <InfoRow label="เลขที่เอกสาร">
                  <p class="text-blue-500 underline cursor-pointer w-fit"
                    @click="() => router.push({ name: 'cm001Detail', params: { id: data.id } })">
                    {{ data.refNumber }}
                  </p>
                </InfoRow>
                <InfoRow label="ชื่อโครงการ">
                  <p class="font-bold">{{ data.name }}</p>
                </InfoRow>
                <InfoRow label="วงเงินงบประมาณ">
                  <p>{{ formatCurrency(data.budget) }}</p>
                </InfoRow>
                <InfoRow label="ฝ่าย/ภาคเขต">
                  <p>{{ data.departmentName }}</p>
                </InfoRow>
                <InfoRow label="วิธีจัดหา">
                  <p>
                    {{ data.supplyMethod }} {{ data.supplyMethodType }} {{ data.supplyMethodSpecialType }}
                  </p>
                </InfoRow>
                <InfoRow v-if="data.glAccounts?.length" label="รหัสบัญชี">
                  <p class="break-words text-base text-gray-600">
                    <template v-for="(gl, glIndex) in data.glAccounts" :key="glIndex">
                      <span v-if="Number(glIndex) > 0" class="mx-1.5 text-gray-300">|</span>
                      <span class="font-bold text-gray-900 tabular-nums">{{ gl.split(' : ')[0] }}</span>
                      <span class="text-gray-900">{{ gl.split(' : ').slice(1).length ? ' ' + gl.split(' : ').slice(1).join(' : ') : '' }}</span>
                    </template>
                  </p>
                </InfoRow>
              </div>
              <div class="flex items-start justify-end gap-1.5 lg:col-span-4 mb-2 lg:mb-0">
                <div class="flex items-center gap-2 mt-2 mr-2">
                  <p class="text-sm">สถานะ :</p>
                  <BadgeComponent :label="BadgeStatus(data.status).label"
                    :color="BadgeStatus(data.status).color" />
                </div>
                <Button icon="pi pi-pen-to-square" class="text-blue-600! hover:bg-blue-300/20! focus:bg-blue-300/20!"
                  size="small" variant="text" rounded
                  @click="() => router.push({ name: 'cm001Detail', params: { id: data.id } })" />
                <Button v-if="data.isCanDelete" icon="pi pi-trash" size="small" text rounded severity="danger"
                  @click="store.fn.onDeleteById(data.id)" />
              </div>
            </div>
          </div>
        </template>
        <template #empty>
          <p class="text-center">ไม่พบข้อมูล</p>
        </template>
      </DataView>
      <Pagination :page-number="store.searchCriteria.pageNumber" :page-size="store.searchCriteria.pageSize"
        :total-record="store.table.totalRecords" @change="store.fn.onChangePageSize" />
    </template>
  </Card>
</template>