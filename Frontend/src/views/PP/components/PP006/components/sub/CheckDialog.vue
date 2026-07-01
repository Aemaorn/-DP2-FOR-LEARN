<script setup lang="ts">
import type { Option } from '@/models/shared/option';
import type { InvitedEntrepreneurs, QualificationResultDto, Shareholder } from '@/views/PP/models/PP006/pp006Model';
import { TitleHeader, InfoItem } from '@/components/cosmetic';
import { Card } from 'primevue';
import { Radio, InputArea } from '@/components/forms';
import { ref, watch } from 'vue';
import { usePP006DetailStore } from '@/views/PP/stores/PP006/PP006Store';
import { PP006EntrepreneurType, QualificationResult } from '@/views/PP/enums/pp006';
import { VendorConstants } from '@/constants';
import { ButtonSave } from '@/components/Button';
import { Form } from 'vee-validate';
import { ToDateOnly, ToDateTime } from '@/helpers/dateTime';
import { useMenuStore } from '@/stores/menu';
import { HttpStatusCode } from 'axios';
import entrepreneurCheckService from '@/services/entrepreneurCheck';
import type { checkCoiBody, checkWatchlistBody } from '@/models/enterpreneurCheck';
import ToastHelper from '@/helpers/toast';

const show = defineModel({
  type: Boolean,
  default: false,
  required: true,
});

const props = defineProps({
  title: { type: String, required: true },
  selected: { type: String, required: true },
});

const { typeNameByCode, nationalityNameBycode } = VendorConstants;
const menuStore = useMenuStore();
const store = usePP006DetailStore();
const checkOptions = [
  { label: 'ผ่าน', value: true },
  { label: 'ไม่ผ่าน', value: false },
] as Option[];

const data = ref<InvitedEntrepreneurs>({} as InvitedEntrepreneurs);

const setDefaultData = (): void => {
  const filterData = store.detail.invitedEntrepreneurs.find(i => i.id === props.selected);

  if (filterData) {
    data.value = JSON.parse(JSON.stringify(filterData));
  }
};

const onSubmitAsync = async (): Promise<void> => {
  if (props.title === PP006EntrepreneurType.COI) {
    data.value.coiResultAt = new Date();
    data.value.shareholders?.forEach(s => {
      s.coiResultAt = new Date();
    });

    if (!data.value.coiCheckerResult) {
      ToastHelper.errorDescription('ไม่สามารถบันทึกข้อมูลได้ เนื่องจากยังไม่ได้ตรวจสอบข้อมูล COI');
      return;
    }

    if (data.value.shareholders?.some(s => !s.coiCheckerResult)) {
      ToastHelper.errorDescription('ไม่สามารถบันทึกข้อมูลได้ เนื่องจากยังไม่ได้ตรวจสอบข้อมูล COI ของผู้ถือหุ้น');
      return;
    }
  }

  if (props.title === PP006EntrepreneurType.EGP) {
    data.value.egpResultAt = new Date();
    data.value.shareholders?.forEach(s => {
      s.egpResultAt = new Date();
    });
  }

  if (props.title === PP006EntrepreneurType.Watchlist) {
    data.value.watchlistResultAt = new Date();
    data.value.shareholders?.forEach(s => {
      s.watchlistResultAt = new Date();
    });

    if (!data.value.watchlistCheckerResult) {
      ToastHelper.errorDescription('ไม่สามารถบันทึกข้อมูลได้ เนื่องจากยังไม่ได้ตรวจสอบข้อมูล Watchlist');
      return;
    }

    if (data.value.shareholders?.some(s => !s.watchlistCheckerResult)) {
      ToastHelper.errorDescription('ไม่สามารถบันทึกข้อมูลได้ เนื่องจากยังไม่ได้ตรวจสอบข้อมูล Watchlist ของผู้ถือหุ้น');
      return;
    }
  }

  const res = await store.updateEntrepreneurAsync(data.value, 'ตรวจสอบ', 'ตรวจสอบสำเร็จ');

  if (res) {
    show.value = false;
  }
};

const checkCoiQualification = async () => {
  if (!data.value.vendorId) return;

  const params: checkCoiBody = {
    name: data.value.entrepreneurName || undefined,
    ssn: data.value.entrepreneurTaxId || undefined,
  };

  const { data: qualificationData, status } = await entrepreneurCheckService.qualificationCoiAsync(params);

  if (status === HttpStatusCode.Ok) {
    if (data.value.coiCheckerResult == undefined) {
      data.value.coiCheckerResult = {} as QualificationResultDto;
    }

    data.value.coiCheckerResult.resultAt = new Date();
    data.value.coiCheckerResult.result = qualificationData.result;
    data.value.coiCheckerResult.remark = qualificationData.remark;
  }
}

const checkWatchlistQualification = async () => {
  if (!data.value.vendorId) return;

  const params = { firstName: data.value.entrepreneurName, isJuristic: data.value.type === 'JuristicPerson' } as checkWatchlistBody;

  const { data: qualificationData, status } = await entrepreneurCheckService.qualificationWatchlistAsync(params);

  if (status === HttpStatusCode.Ok) {
    if (data.value.watchlistCheckerResult == undefined) {
      data.value.watchlistCheckerResult = {} as QualificationResultDto;
    }

    data.value.watchlistCheckerResult.resultAt = new Date();
    data.value.watchlistCheckerResult.result = qualificationData.result;
    data.value.watchlistCheckerResult.remark = qualificationData.remark;
  }
}

const checkCoiShareholder = async (index: number) => {
  if (!data.value.shareholders || !data.value.shareholders[index].taxId) return;

  const params: checkCoiBody = {
    name: `${data.value.shareholders[index].firstName} ${data.value.shareholders[index].lastName}`.trim() || undefined,
    ssn: data.value.shareholders[index].taxId || undefined,
  };

  const { data: qualificationData, status } = await entrepreneurCheckService.qualificationCoiAsync(params);

  if (status === HttpStatusCode.Ok) {
    if (data.value.shareholders[index].coiCheckerResult == undefined) {
      data.value.shareholders[index].coiCheckerResult = {} as QualificationResultDto;
    }

    data.value.shareholders[index].coiCheckerResult.resultAt = new Date();
    data.value.shareholders[index].coiCheckerResult.result = qualificationData.result;
    data.value.shareholders[index].coiCheckerResult.remark = qualificationData.remark;
  }
}

const checkWatchlistShareholder = async (index: number) => {
  if (!data.value.shareholders || !data.value.shareholders[index].taxId) return;

  const params = { firstName: data.value.shareholders[index].firstName, isJuristic: false, lastName: data.value.shareholders[index].lastName } as checkWatchlistBody;

  const { data: qualificationData, status } = await entrepreneurCheckService.qualificationWatchlistAsync(params);

  if (status === HttpStatusCode.Ok) {
    if (data.value.shareholders[index].watchlistCheckerResult == undefined) {
      data.value.shareholders[index].watchlistCheckerResult = {} as QualificationResultDto;
    }

    data.value.shareholders[index].watchlistCheckerResult.resultAt = new Date();
    data.value.shareholders[index].watchlistCheckerResult.result = qualificationData.result;
    data.value.shareholders[index].watchlistCheckerResult.remark = qualificationData.remark;
  }
}

const SetCoiResultWhenUndefined = () => {
  if (props.title === PP006EntrepreneurType.COI && data.value.coiResultAt == undefined) {
    data.value.coiResult = undefined;

    data.value.shareholders?.forEach(s => {
      if (s.coiResultAt == undefined) {
        s.coiResult = undefined;
      }
    });
  }
}

const SetEGPResultWhenUndefined = () => {
  if (props.title === PP006EntrepreneurType.EGP && data.value.egpResultAt == undefined) {
    data.value.egpResult = undefined;

    data.value.shareholders?.forEach(s => {
      if (s.egpResultAt == undefined) {
        s.egpResult = undefined;
      }
    });
  }
}

const SetWatchlistResultWhenUndefined = () => {
  if (props.title === PP006EntrepreneurType.Watchlist && data.value.watchlistResultAt == undefined) {
    data.value.watchlistResult = undefined;

    data.value.shareholders?.forEach(s => {
      if (s.watchlistResultAt == undefined) {
        s.watchlistResult = undefined;
      }
    });
  }
}

const CheckCoiQualificationWhenNeeded = (tasks: Promise<any>[]) => {
  if (props.title === PP006EntrepreneurType.COI && !data.value.coiCheckerResult) {
    tasks.push(checkCoiQualification());
  }
}

const CheckWatchlistQualificationWhenNeeded = (tasks: Promise<any>[]) => {
  if (props.title === PP006EntrepreneurType.Watchlist && !data.value.watchlistCheckerResult) {
    tasks.push(checkWatchlistQualification());
  }
}

const CheckCoiShareholdersWhenNeeded = (tasks: Promise<any>[]) => {
  if (props.title === PP006EntrepreneurType.COI) {
    const shareholderTasks = (data.value.shareholders ?? [])
      .map((s, i) => !s.coiCheckerResult ? checkCoiShareholder(i) : null)
      .filter(Boolean) as Promise<any>[];
    tasks.push(...shareholderTasks);
  }
}

const CheckWatchlistShareholdersWhenNeeded = (tasks: Promise<any>[]) => {
  if (props.title === PP006EntrepreneurType.Watchlist) {
    const shareholderTasks = (data.value.shareholders ?? [])
      .map((s, i) => !s.watchlistCheckerResult ? checkWatchlistShareholder(i) : null)
      .filter(Boolean) as Promise<any>[];
    tasks.push(...shareholderTasks);
  }
}

watch(() => show.value, async (newValue) => {
  if (newValue) {
    setDefaultData();

    SetCoiResultWhenUndefined();

    SetEGPResultWhenUndefined();

    SetWatchlistResultWhenUndefined();

    const tasks: Promise<any>[] = [];

    CheckCoiQualificationWhenNeeded(tasks);

    CheckWatchlistQualificationWhenNeeded(tasks);

    CheckCoiShareholdersWhenNeeded(tasks);

    CheckWatchlistShareholdersWhenNeeded(tasks);

    if (tasks.length) {
      await Promise.all(tasks);
    }

    return;
  }

  if (!newValue) {
    data.value = {
      shareholders: [] as Shareholder[],
    } as InvitedEntrepreneurs;
  }
});
</script>

<template>
  <Dialog v-model:visible="show" modal :style="{ width: '80vw' }" :draggable="false" :breakpoints="{ '575px': '90vw' }">
    <template #container>
      <Form @submit="onSubmitAsync" class="p-5 overflow-auto">
        <TitleHeader :label="props.title">
          <template #action>
            <i class="pi pi-times cursor-pointer" @click="() => show = false"></i>
          </template>
        </TitleHeader>
        <Card>
          <template #content>
            <TitleHeader label="ข้อมูลคู่ค้า" />
            <div class="grid lg:grid-cols-3">
              <InfoItem title="สัญชาติของผู้ประกอบการ" :content="nationalityNameBycode(data.nationality)" />
              <InfoItem title="ประเภท" :content="typeNameByCode(data.type)" />
            </div>
            <div class="grid lg:grid-cols-3 mt-3">
              <InfoItem title="ประเภทผู้ประกอบการ" :content="data.entrepreneurType" />
              <InfoItem title="เลขประจำตัวผู้เสียภาษี" :content="data.entrepreneurTaxId" />
              <InfoItem title="ชื่อสถานประกอบการ" :content="data.entrepreneurName" />
            </div>
            <div class="grid lg:grid-cols-3 mt-3">
              <InfoItem title="หมายเลขโทรศัพท์สำหรับติดต่อ" :content="data.tel" />
              <InfoItem title="อีเมล" :content="data.entrepreneurEmail" />
            </div>
          </template>
        </Card>
        <Card class="mt-5">
          <template #content>
            <TitleHeader :label="`ข้อมูล ${props.title}`">
              <template #action>
                <Button label="ตรวจสอบ" icon="pi pi-check" severity="primary" variant="outlined"
                  v-if="store.status.canEdit && menuStore.hasManage && props.title == PP006EntrepreneurType.COI"
                  @click="checkCoiQualification" />
                <Button label="ตรวจสอบ" icon="pi pi-check" severity="primary" variant="outlined"
                  v-if="store.status.canEdit && menuStore.hasManage && props.title == PP006EntrepreneurType.Watchlist"
                  @click="checkWatchlistQualification" />
              </template>
            </TitleHeader>
            <div class="flex flex-col gap-4 bg-gray-100 p-5 rounded-md ">
              <TitleHeader :label="`ข้อมูล ${props.title}`" hidden-icon>
                <template #action>
                  <p
                    v-if="(props.title === PP006EntrepreneurType.COI && data.coiCheckerResult) || (props.title === PP006EntrepreneurType.Watchlist && data.watchlistCheckerResult)">
                    ตรวจสอบ ณ วันที่ : {{ props.title === PP006EntrepreneurType.COI ?
                      ToDateOnly(data.coiCheckerResult?.resultAt) :
                      ToDateOnly(data.watchlistCheckerResult?.resultAt) }}
                  </p>
                </template>
              </TitleHeader>
              <div v-if="props.title === PP006EntrepreneurType.COI && data.coiCheckerResult">
                <div class="flex gap-2 items-center text-gray-400 text-xl"
                  v-if="data.coiCheckerResult.result === QualificationResult.Pass">
                  <p class="material-symbols-outlined text-green-400">
                    check_circle
                  </p>
                  <p>
                    ผ่าน
                  </p>
                </div>
                <div class="flex gap-2 items-center text-gray-400 text-xl"
                  v-if="data.coiCheckerResult.result === QualificationResult.Fail">
                  <p class="material-symbols-outlined text-[#F9A825]">
                    error
                  </p>
                  <p>
                    {{ data.coiCheckerResult.remark }}
                  </p>
                </div>
                <div class="flex gap-2 items-center text-gray-400 text-xl"
                  v-if="data.coiCheckerResult.result === QualificationResult.UnKnow">
                  <span class="material-symbols-outlined text-gray-400">
                    Help
                  </span>
                  <span>
                    ไม่สามารถเชื่อมต่อระบบ COI ได้ กรุณาลองใหม่อีกครั้ง
                  </span>
                </div>
              </div>
              <div v-if="props.title === PP006EntrepreneurType.Watchlist && data.watchlistCheckerResult">
                <div class="flex gap-2 items-center text-gray-400 text-xl"
                  v-if="data.watchlistCheckerResult.result === QualificationResult.Pass">
                  <p class="material-symbols-outlined text-green-400">
                    check_circle
                  </p>
                  <p>
                    ผ่าน
                  </p>
                </div>
                <div class="flex gap-2 items-center text-gray-400 text-xl"
                  v-if="data.watchlistCheckerResult.result === QualificationResult.Fail">
                  <p class="material-symbols-outlined text-[#F9A825]">
                    error
                  </p>
                  <p>
                    {{ data.watchlistCheckerResult.remark }}
                  </p>
                </div>
                <div class="flex gap-2 items-center text-gray-400 text-xl"
                  v-if="data.watchlistCheckerResult.result === QualificationResult.UnKnow">
                  <span class="material-symbols-outlined text-gray-400">
                    Help
                  </span>
                  <span>
                    ไม่สามารถเชื่อมต่อระบบ Watchlist ได้ กรุณาลองใหม่อีกครั้ง
                  </span>
                </div>
              </div>
            </div>
            <Radio class="mt-5" :options="checkOptions" v-model="data.coiResult"
              v-if="props.title === PP006EntrepreneurType.COI"
              :disabled="!store.status.canEdit || !menuStore.hasManage" />
            <Radio class="mt-5" :options="checkOptions" v-model="data.watchlistResult"
              v-if="props.title === PP006EntrepreneurType.Watchlist"
              :disabled="!store.status.canEdit || !menuStore.hasManage" />
            <Radio class="mt-5" :options="checkOptions" v-model="data.egpResult"
              v-if="props.title === PP006EntrepreneurType.EGP"
              :disabled="!store.status.canEdit || !menuStore.hasManage" />
            <InputArea label="หมายเหตุ" v-model="data.coiResultRemark" v-if="props.title === PP006EntrepreneurType.COI"
              :disabled="!store.status.canEdit || !menuStore.hasManage" />
            <InputArea label="หมายเหตุ" v-model="data.watchlistResultRemark"
              v-if="props.title === PP006EntrepreneurType.Watchlist"
              :disabled="!store.status.canEdit || !menuStore.hasManage" />
            <InputArea label="หมายเหตุ" v-model="data.egpResultRemark" v-if="props.title === PP006EntrepreneurType.EGP"
              :disabled="!store.status.canEdit || !menuStore.hasManage" />
            <div class="flex justify-end">
              <div class="text-end">
                <p class="mt-5"
                  v-if="props.title === PP006EntrepreneurType.COI ? data.coiResultAt : (props.title === PP006EntrepreneurType.EGP ? data.egpResultAt : data.watchlistResultAt)">
                  ตรวจสอบ ณ วันที่ : {{ ToDateTime(props.title === PP006EntrepreneurType.COI ? data.coiResultAt :
                    (props.title ===
                      PP006EntrepreneurType.EGP ? data.egpResultAt : data.watchlistResultAt)) }}</p>
              </div>
            </div>
          </template>
        </Card>
        <Card class="mt-5" v-if="data.shareholders && data.shareholders.length > 0">
          <template #content>
            <TitleHeader :label="`ข้อมูล ${props.title} สำหรับผู้ถือหุ้น`" />
            <div class="bg-gray-100 p-5 rounded-md mt-5" v-for="(value, index) in data.shareholders" :key="value.id">
              <div class="flex justify-between gap-5">
                <div class="flex gap-5">
                  <p class="mt-3">1.</p>
                  <div>
                    <p class="mt-3">เลขประจำตัวผู้เสียภาษี : {{ value.taxId }}</p>
                    <div class="flex gap-5 mt-3">
                      <p>ชื่อ : {{ value.firstName }}</p>
                      <p>นามสกุล : {{ value.lastName }}</p>
                    </div>
                    <p class="mt-3">
                      {{ [value.isDirector ? 'กรรมการ' : null, value.isShareholder ? 'ผู้ถือหุ้น' : null].filter(Boolean).join(' / ') || '-' }}
                    </p>
                  </div>
                </div>
                <div>
                  <Button label="ตรวจสอบ" icon="pi pi-check" severity="primary" variant="outlined"
                    v-if="store.status.canEdit && menuStore.hasManage && props.title == PP006EntrepreneurType.COI"
                    @click="() => checkCoiShareholder(index)" />
                  <Button label="ตรวจสอบ" icon="pi pi-check" severity="primary" variant="outlined"
                    v-if="store.status.canEdit && menuStore.hasManage && props.title == PP006EntrepreneurType.Watchlist"
                    @click="() => checkWatchlistShareholder(index)" />
                </div>
              </div>
              <div v-if="props.title === PP006EntrepreneurType.COI && value.coiCheckerResult" class="ml-7 mt-4">
                <div class="flex gap-2 items-center text-gray-400 text-xl"
                  v-if="value.coiCheckerResult.result === QualificationResult.Pass">
                  <p class="material-symbols-outlined text-green-400">
                    check_circle
                  </p>
                  <p>
                    ผ่าน
                  </p>
                </div>
                <div class="flex gap-2 items-center text-gray-400 text-xl"
                  v-if="value.coiCheckerResult.result === QualificationResult.Fail">
                  <p class="material-symbols-outlined text-[#F9A825]">
                    error
                  </p>
                  <p>
                    {{ value.coiCheckerResult.remark }}
                  </p>
                </div>
                <div class="flex gap-2 items-center text-gray-400 text-xl"
                  v-if="value.coiCheckerResult.result === QualificationResult.UnKnow">
                  <span class="material-symbols-outlined text-gray-400">
                    Help
                  </span>
                  <span>
                    ไม่สามารถเชื่อมต่อระบบ COI ได้ กรุณาลองใหม่อีกครั้ง
                  </span>
                </div>
              </div>
              <div v-if="props.title === PP006EntrepreneurType.Watchlist && value.watchlistCheckerResult"
                class="ml-7 mt-4">
                <div class="flex gap-2 items-center text-gray-400 text-xl"
                  v-if="value.watchlistCheckerResult.result === QualificationResult.Pass">
                  <p class="material-symbols-outlined text-green-400">
                    check_circle
                  </p>
                  <p>
                    ผ่าน
                  </p>
                </div>
                <div class="flex gap-2 items-center text-gray-400 text-xl"
                  v-if="value.watchlistCheckerResult.result === QualificationResult.Fail">
                  <p class="material-symbols-outlined text-[#F9A825]">
                    error
                  </p>
                  <p>
                    {{ value.watchlistCheckerResult.remark }}
                  </p>
                </div>
                <div class="flex gap-2 items-center text-gray-400 text-xl"
                  v-if="value.watchlistCheckerResult.result === QualificationResult.UnKnow">
                  <span class="material-symbols-outlined text-gray-400">
                    Help
                  </span>
                  <span>
                    ไม่สามารถเชื่อมต่อระบบ Watchlist ได้ กรุณาลองใหม่อีกครั้ง
                  </span>
                </div>
              </div>
              <Radio class="mt-5" :options="checkOptions" v-model="value.coiResult"
                v-if="props.title === PP006EntrepreneurType.COI"
                :disabled="!store.status.canEdit || !menuStore.hasManage" />
              <Radio class="mt-5" :options="checkOptions" v-model="value.watchlistResult"
                v-if="props.title === PP006EntrepreneurType.Watchlist"
                :disabled="!store.status.canEdit || !menuStore.hasManage" />
              <Radio class="mt-5" :options="checkOptions" v-model="value.egpResult"
                v-if="props.title === PP006EntrepreneurType.EGP"
                :disabled="!store.status.canEdit || !menuStore.hasManage" />
              <InputArea label="หมายเหตุ" v-model="value.coiResultRemark"
                v-if="props.title === PP006EntrepreneurType.COI"
                :disabled="!store.status.canEdit || !menuStore.hasManage" />
              <InputArea label="หมายเหตุ" v-model="value.watchlistResultRemark"
                v-if="props.title === PP006EntrepreneurType.Watchlist"
                :disabled="!store.status.canEdit || !menuStore.hasManage" />
              <InputArea label="หมายเหตุ" v-model="value.egpRemark" v-if="props.title === PP006EntrepreneurType.EGP"
                :disabled="!store.status.canEdit || !menuStore.hasManage" />
              <div class="flex justify-end">
                <div class="text-end">
                  <p class="mt-5"
                    v-if="props.title === PP006EntrepreneurType.COI ? value.coiResultAt : (props.title === PP006EntrepreneurType.EGP ? value.egpResultAt : value.watchlistResultAt)">
                    ตรวจสอบ ณ วันที่ : {{ ToDateTime(props.title === PP006EntrepreneurType.COI ? value.coiResultAt :
                      (props.title ===
                        PP006EntrepreneurType.EGP ? value.egpResultAt : value.watchlistResultAt)) }}</p>
                </div>
              </div>
            </div>
          </template>
        </Card>
        <div class="flex items-center gap-3 justify-end mt-5" v-if="store.status.canEdit && menuStore.hasManage">
          <Button label="ยกเลิก" severity="secondary" variant="outlined" @click="() => show = false" />
          <ButtonSave type="submit" />
        </div>
      </Form>
    </template>
  </Dialog>
</template>
