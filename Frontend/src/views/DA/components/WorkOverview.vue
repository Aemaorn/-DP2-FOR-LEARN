<script setup lang="ts">
import type { OptionBadge } from '@/models/shared/option';
import { Card } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import { InputField, Select, StatusGroupButton } from '@/components/forms';
import { ref } from 'vue';
import { Pagination, StatusChip, BadgeStatus } from '@/components';
import { formatCurrency } from '@/helpers/currency';
import InfoRow from '@/components/cosmetic/InfoRow.vue';
import router from '@/router';

const optionBadges = ref<OptionBadge[]>([
  {
    bgColorClass: 'bg-gray-400',
    textColorClass: 'text-black',
    count: 1,
    value: 'all',
    label: 'ทั้งหมด'
  },
  {
    bgColorClass: 'bg-gray-400',
    textColorClass: 'text-white',
    count: 1,
    value: 'ppp',
    label: 'Pre-Procurement'
  },
  {
    bgColorClass: 'bg-gray-400',
    textColorClass: 'text-white',
    count: 1,
    value: 'pp',
    label: 'Procurement'
  },
  {
    bgColorClass: 'bg-gray-400',
    textColorClass: 'text-white',
    count: 1,
    value: 'ca',
    label: 'Contract Agreement'
  },
  {
    bgColorClass: 'bg-gray-400',
    textColorClass: 'text-white',
    count: 1,
    value: 'cm',
    label: 'Contract Management'
  },
]);

const keyword = ref();

const routeToDetail = (id: string) => {
  router.push(`/da001/detail/${id}`);
};
</script>

<template>
  <Card>
    <template #content>
      <TitleHeader label="ภาพรวมผู้ปฎิบัติงาน" />
      <div class="mt-5">
        <div class="grid lg:grid-cols-2 gap-2">
          <InputField label="คำค้นหา" v-model.trim="keyword" />
        </div>
        <div class="grid lg:grid-cols-10 gap-2">
          <Select label="ฝ่าย/สำนัก" :options="[]" v-model="keyword" class="col-span-2" />
          <Select label="ปีงบประมาณ" :options="[]" v-model="keyword" class="col-span-2" />
          <Select label="วิธีการจัดหา" :options="[]" v-model="keyword" class="col-span-2" />
          <Select :options="[]" v-model="keyword" class="col-span-2" />
          <Select :options="[]" v-model="keyword" class="col-span-2" />
        </div>
      </div>
    </template>
  </Card>
  <Card class="mt-5">
    <template #content>
      <StatusGroupButton :optionBadges="optionBadges" />
      <DataView :value="[{}]" data-key="id">
        <template #list="{ items }">
          <div v-for="(data, index) in items" :key="index" class="border-1 border-gray-300 rounded-sm mb-2 p-1">
            <div class="grid lg:grid-cols-12 px-2">
              <div class="lg:col-span-8">
                <InfoRow label="เลขที่รายการจัดซื้อจัดจ้าง">
                  <p class="underline text-blue-400 cursor-pointer">
                    DP670001
                  </p>
                </InfoRow>
                <InfoRow label="ชื่อโครงการ">
                  <p class="font-bold">
                    โครงการจ้างบริการรักษาความสะอาดประจำสาขาในสังกัดภาคตะวันออกเฉียงเหนือตอนบน และศูนย์วิเคราะห์สินเชื่อ
                    อุดรธานี ของธนาคารอาคารสงเคราะห์
                  </p>
                </InfoRow>
                <InfoRow label="วงเงินงบประมาณ">
                  <p>
                    {{ formatCurrency(100000) }}
                  </p>
                </InfoRow>
                <InfoRow label="ประเภทแผน">
                  <StatusChip :label="'แผนรวมปี'" size="Medium" color="Info" />
                </InfoRow>
                <InfoRow label="ฝ่าย/ภาคเขต">
                  <p>
                    ฝ่ายจัดหาและการพัสดุ
                  </p>
                </InfoRow>
                <InfoRow label="วิธีจัดหา">
                  <p>
                    พ.ร.บ.จัดซื้อจัดจ้างฯ 2560 : เฉพาะเจาะจง
                  </p>
                </InfoRow>
              </div>
              <div class="flex items-start justify-end gap-1.5 lg:col-span-4">
                <div class="flex items-center gap-2 mt-2 mr-2">
                  <p class="text-sm">สถานะ :</p>
                  <BadgeStatus label="แบบร่าง" color="gray" size="sm" />

                  <BadgeStatus label="ขอเปลี่ยนแปลง" color="amber" size="sm" v-if="data.isChange" />

                  <BadgeStatus label="ขอยกเลิก" color="rose" size="sm" v-if="data.isCancel" />
                </div>
                <Button icon="pi pi-pen-to-square" class="text-blue-600! hover:bg-blue-300/20! focus:bg-blue-300/20!"
                  size="small" variant="text" @click="() => routeToDetail('1')" />
                <Button icon="pi pi-trash" class="text-red-600!  hover:bg-red-300/20! focus:bg-red-300/20!" size="small"
                  variant="text" />
              </div>
            </div>
          </div>
        </template>
        <template #empty>
          <p class="text-center">ไม่พบข้อมูล</p>
        </template>
      </DataView>
      <Pagination :page-number="1" :page-size="10" :total-record="20" />
    </template>
  </Card>
</template>
