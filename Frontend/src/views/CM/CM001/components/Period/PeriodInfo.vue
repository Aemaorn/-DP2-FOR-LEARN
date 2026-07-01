<script setup lang="ts">
import { Card } from 'primevue';
import { TitleHeader, InfoItem } from '@/components/cosmetic';
import { formatCurrency } from '@/helpers/currency';
import type { CM001PeriodBody } from '@/models/CM/cm001';
import { souceType } from '@/enums/CM/cm001';
import { ToDateOnly } from '@/helpers/dateTime';
import router from '@/router';
import { useCm001DetailStore } from '@/stores/CM/cm001';

type Props = {
  data: CM001PeriodBody;
  contractType?: string;
};

const { data, contractType } = defineProps<Props>();
const cm001Store = useCm001DetailStore();

const onRouteProcurementToDetail = (id: string, type?: string) => {
  let path = "";

  switch (type) {
    case 'Plan':
      path = `/pl/pl001/detail/${id}`;
      break;

    case 'Procurement':
      path = `/pp/detail/${id}`;
      break;

    default:
      return;
  }

  const route = router.resolve(path);
  window.open(route.href, "_blank");
};

</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader :label="`ข้อมูลรายการจัดซื้อจัดจ้าง/ข้อมูลสัญญา ${contractType ?? ''}`" hidden-icon />
      <div class="px-4 mt-2 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 items-center" v-if="data.cm001Info">
        <!-- Plan type fields -->
        <template v-if="data.cm001Info.sourceType === souceType.Plan">
          <InfoItem class="col-start-1" title="เลขที่รายการจัดซื้อจัดจ้าง">
            <template #content>
              <p v-if="data.cm001Info.planId" class="text-blue-500 underline cursor-pointer"
                @click="onRouteProcurementToDetail(data.cm001Info.planId, 'Plan')">
                {{ data.cm001Info.planCode }}
              </p>
              <template v-else>-</template>
            </template>
          </InfoItem>

          <InfoItem title="เลขที่การจัดซื้อจัดจ้าง">
            <template #content>
              <p v-if="data.cm001Info.procurementId" class="text-blue-500 underline cursor-pointer"
                @click="onRouteProcurementToDetail(data.cm001Info.procurementId, 'Procurement')">
                {{ data.cm001Info.procurementNumber }}
              </p>
              <template v-else>-</template>
            </template>
          </InfoItem>

          <InfoItem title="ฝ่าย/ภาคเขต">
            <template #content>
              {{ data.cm001Info.departmentName ?? "-" }}
            </template>
          </InfoItem>

          <InfoItem title="โครงการ">
            <template #content>
              {{ data.cm001Info.name ?? "-" }}
            </template>
          </InfoItem>

          <InfoItem title="ปีงบประมาณ">
            <template #content>
              {{ data.cm001Info.budgetYear ?? "-" }}
            </template>
          </InfoItem>

          <InfoItem title="งบประมาณ">
            <template #content>
              {{ formatCurrency(data.cm001Info.budget ?? 0) }}
            </template>
          </InfoItem>

          <InfoItem title="วิธีจัดหา">
            <template #content>
              {{ data.cm001Info.supplyMethod ?? "-" }}
            </template>
          </InfoItem>

          <InfoItem title="">
            <template #content>
              {{ data.cm001Info.supplyMethodType ?? "" }}
            </template>
          </InfoItem>

          <InfoItem title="">
            <template #content>
              {{ data.cm001Info.supplyMethodSpecialType ?? "" }}
            </template>
          </InfoItem>
        </template>

        <!-- ContractDraftVendor type fields -->
        <template v-if="data.cm001Info.sourceType === souceType.ContractDraftVendor">
          <InfoItem class="col-start-1" title="เลขที่รายการจัดซื้อจัดจ้าง">
            <template #content>
              <p v-if="data.cm001Info.planId" class="text-blue-500 underline cursor-pointer"
                @click="onRouteProcurementToDetail(data.cm001Info.planId, 'Plan')">
                {{ data.cm001Info.planCode }}
              </p>
              <template v-else>-</template>
            </template>
          </InfoItem>

          <InfoItem title="เลขที่การจัดซื้อจัดจ้าง">
            <template #content>
              <p v-if="data.cm001Info.procurementId" class="text-blue-500 underline cursor-pointer"
                @click="onRouteProcurementToDetail(data.cm001Info.procurementId, 'Procurement')">
                {{ data.cm001Info.procurementNumber }}
              </p>
              <template v-else>-</template>
            </template>
          </InfoItem>

          <InfoItem title="ฝ่าย/ภาคเขต">
            <template #content>
              <p>
                {{ data.cm001Info.departmentName ?? "-" }}
              </p>
            </template>
          </InfoItem>

          <InfoItem title="คู่ค้า">
            <template #content>
              <p>
                {{ data.cm001Info.vendorName ?? "-" }}
              </p>
            </template>
          </InfoItem>

          <InfoItem title="Email">
            <template #content>
              <p>
                {{ data.cm001Info.vendorEmail ?? "-" }}
              </p>
            </template>
          </InfoItem>

          <InfoItem class="col-start-1" title="เลขที่สัญญา">
            <template #content>
              <p>
                {{ data.cm001Info.contractNumber ?? "-" }}
              </p>
            </template>
          </InfoItem>

          <InfoItem title="วงเงินตามสัญญา">
            <template #content>
              <p>
                {{ formatCurrency(data.cm001Info.contractBudget ?? 0) }}
              </p>
            </template>
          </InfoItem>

          <InfoItem title="ชื่อสัญญา">
            <template #content>
              <p>
                {{ data.cm001Info.name ?? "-" }}
              </p>
            </template>
          </InfoItem>

          <InfoItem title="ประเภทสัญญา">
            <template #content>
              <p>
                {{ data.cm001Info.contractTypeName ?? "-" }}
              </p>
            </template>
          </InfoItem>

          <InfoItem title="รูปแบบสัญญา">
            <template #content>
              <p>
                {{ data.cm001Info.templateName ?? "-" }}
              </p>
            </template>
          </InfoItem>

          <InfoItem title="วันที่ลงนามตามสัญญา">
            <template #content>
              <p>
                {{ ToDateOnly(data.cm001Info.contractDate) }}
              </p>
            </template>
          </InfoItem>

          <InfoItem class="col-start-1" title="วิธีจัดหา">
            <template #content>
              <p>
                {{ data.cm001Info.supplyMethod ?? "-" }}
              </p>
            </template>
          </InfoItem>

          <InfoItem title="">
            <template #content>
              <p>
                {{ data.cm001Info.supplyMethodType ?? "" }}
              </p>
            </template>
          </InfoItem>

          <InfoItem title="">
            <template #content>
              <p>
                {{ data.cm001Info.supplyMethodSpecialType ?? "" }}
              </p>
            </template>
          </InfoItem>
        </template>

        <!-- ContractDraftVendorEdit type fields -->
        <template v-if="data.cm001Info.sourceType === souceType.ContractDraftVendorEdit">
          <InfoItem class="col-start-1" title="เลขที่รายการจัดซื้อจัดจ้าง">
            <template #content>
              <p v-if="data.cm001Info.planId" class="text-blue-500 underline cursor-pointer"
                @click="onRouteProcurementToDetail(data.cm001Info.planId, 'Plan')">
                {{ data.cm001Info.planCode }}
              </p>
              <template v-else>-</template>
            </template>
          </InfoItem>

          <InfoItem title="เลขที่การจัดซื้อจัดจ้าง">
            <template #content>
              <p v-if="data.cm001Info.procurementId" class="text-blue-500 underline cursor-pointer"
                @click="onRouteProcurementToDetail(data.cm001Info.procurementId, 'Procurement')">
                {{ data.cm001Info.procurementNumber }}
              </p>
              <template v-else>-</template>
            </template>
          </InfoItem>

          <InfoItem title="ฝ่าย/ภาคเขต">
            <template #content>
              <p>{{ data.cm001Info.departmentName ?? "-" }}</p>
            </template>
          </InfoItem>

          <InfoItem title="คู่ค้า">
            <template #content>
              <p>{{ data.cm001Info.vendorName ?? "-" }}</p>
            </template>
          </InfoItem>

          <InfoItem title="Email">
            <template #content>
              <p>{{ data.cm001Info.vendorEmail ?? "-" }}</p>
            </template>
          </InfoItem>

          <InfoItem class="col-start-1" title="เลขที่สัญญา">
            <template #content>
              <p>{{ data.cm001Info.contractNumber ?? "-" }}</p>
            </template>
          </InfoItem>
          {{ data.contractBudgetAmount }}
          <InfoItem title="วงเงินตามสัญญา">
            <template #content>
              <p>{{ formatCurrency(data.cm001Info.contractBudget ?? 0) }}</p>
            </template>
          </InfoItem>

          <InfoItem title="ชื่อสัญญา">
            <template #content>
              <p>{{ data.cm001Info.name ?? "-" }}</p>
            </template>
          </InfoItem>

          <InfoItem title="ประเภทสัญญา">
            <template #content>
              <p>{{ data.cm001Info.contractTypeName ?? "-" }}</p>
            </template>
          </InfoItem>

          <InfoItem title="รูปแบบสัญญา">
            <template #content>
              <p>{{ data.cm001Info.templateName ?? "-" }}</p>
            </template>
          </InfoItem>

          <InfoItem title="วันที่ลงนามตามสัญญา">
            <template #content>
              <p>{{ ToDateOnly(data.cm001Info.contractDate) }}</p>
            </template>
          </InfoItem>

          <InfoItem class="col-start-1" title="วิธีจัดหา">
            <template #content>
              <p>{{ data.cm001Info.supplyMethod ?? "-" }}</p>
            </template>
          </InfoItem>

          <InfoItem title="">
            <template #content>
              <p>{{ data.cm001Info.supplyMethodType ?? "" }}</p>
            </template>
          </InfoItem>

          <InfoItem title="">
            <template #content>
              <p>{{ data.cm001Info.supplyMethodSpecialType ?? "" }}</p>
            </template>
          </InfoItem>
        </template>

        <!-- Procurement type fields -->
        <template v-if="data.cm001Info.sourceType === souceType.Procurement">
          <InfoItem class="col-start-1" title="เลขที่รายการจัดซื้อจัดจ้าง">
            <template #content>
              <p v-if="data.cm001Info.planId" class="text-blue-500 underline cursor-pointer"
                @click="onRouteProcurementToDetail(data.cm001Info.planId, 'Plan')">
                {{ data.cm001Info.planCode }}
              </p>
              <template v-else>-</template>
            </template>
          </InfoItem>

          <InfoItem title="เลขที่การจัดซื้อจัดจ้าง">
            <template #content>
              <p v-if="data.cm001Info.procurementId" class="text-blue-500 underline cursor-pointer"
                @click="onRouteProcurementToDetail(data.cm001Info.procurementId, 'Procurement')">
                {{ data.cm001Info.procurementNumber }}
              </p>
              <template v-else>-</template>
            </template>
          </InfoItem>

          <InfoItem title="ฝ่าย/ภาคเขต">
            <template #content>
              {{ data.cm001Info.departmentName ?? "-" }}
            </template>
          </InfoItem>

          <InfoItem title="โครงการ">
            <template #content>
              {{ data.cm001Info.name ?? "-" }}
            </template>
          </InfoItem>

          <InfoItem title="ปีงบประมาณ">
            <template #content>
              {{ data.cm001Info.budgetYear ?? "-" }}
            </template>
          </InfoItem>

          <InfoItem title="งบประมาณ">
            <template #content>
              {{ formatCurrency(data.cm001Info.budget ?? 0) }}
            </template>
          </InfoItem>

          <InfoItem title="วิธีจัดหา">
            <template #content>
              {{ data.cm001Info.supplyMethod ?? "-" }}
            </template>
          </InfoItem>

          <InfoItem title="">
            <template #content>
              {{ data.cm001Info.supplyMethodType ?? "" }}
            </template>
          </InfoItem>

          <InfoItem title="">
            <template #content>
              {{ data.cm001Info.supplyMethodSpecialType ?? "" }}
            </template>
          </InfoItem>
        </template>

        <!-- Manual type fields (no reference) -->
        <template v-if="data.cm001Info.sourceType === souceType.Manual">
          <InfoItem class="col-start-1" title="เลขที่เอกสาร">
            <template #content>
              {{ cm001Store.body.number ?? cm001Store.body.refCode ?? "-" }}
            </template>
          </InfoItem>

          <InfoItem title="ฝ่าย/ภาคเขต">
            <template #content>
              {{ data.cm001Info.departmentName ?? "-" }}
            </template>
          </InfoItem>

          <InfoItem class="col-start-1" title="โครงการ">
            <template #content>
              {{ data.cm001Info.name ?? "-" }}
            </template>
          </InfoItem>

          <InfoItem title="งบประมาณ">
            <template #content>
              {{ formatCurrency(data.cm001Info.budget ?? 0) }}
            </template>
          </InfoItem>

          <InfoItem class="col-start-1" title="วิธีจัดหา">
            <template #content>
              {{ data.cm001Info.supplyMethod ?? "-" }}
            </template>
          </InfoItem>

          <InfoItem title="">
            <template #content>
              {{ data.cm001Info.supplyMethodType ?? "" }}
            </template>
          </InfoItem>

          <InfoItem title="">
            <template #content>
              {{ data.cm001Info.supplyMethodSpecialType ?? "" }}
            </template>
          </InfoItem>
        </template>
      </div>
    </template>
  </Card>
</template>