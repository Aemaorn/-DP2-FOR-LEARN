<script setup lang="ts">
import type { Option } from '@/models/shared/option';
import { TitleHeader, InfoItem } from '@/components/cosmetic';
import { Card } from 'primevue';
import { Radio, InputArea, UploadFileGroup, InputField } from '@/components/forms';
import { computed, ref, watch } from 'vue';
import { PP006EntrepreneurType, QualificationResult } from '@/views/PP/enums/pp006';
import { VendorConstants } from '@/constants';
import { ButtonSave } from '@/components/Button';
import { ToDateOnly, ToDateTime } from '@/helpers/dateTime';
import { usePurchaseOrder } from '@/views/PP/stores/PP007/PP007Store';
import type { PP007Entrepreneurs } from '@/views/PP/models/PP007/pp007Model';
import type { Shareholder } from '@/views/PP/models/PP006/pp006Model';
import type { QualificationResultDto } from '@/views/PP/models/PP006/pp006Model';
import { Form } from 'vee-validate';
import { useMenuStore } from '@/stores/menu';
import ToastHelper from '@/helpers/toast';

import entrepreneurCheckService, { type CheckHistorySuVendorItem } from '@/services/entrepreneurCheck';
import { HttpStatusCode } from 'axios';
import { EntrepreneurType } from '@/enums/shared';
import type { EntrepreneurAttachments } from '@/models/shared/uploadFile';

const show = defineModel({
  type: Boolean,
  default: false,
  required: true,
});

const props = defineProps({
  title: { type: String, required: true },
});

const { typeNameByCode, nationalityNameBycode } = VendorConstants;
const menuStore = useMenuStore();
const store = usePurchaseOrder();
const checkOptions = [
  { label: 'ผ่าน', value: true },
  { label: 'ไม่ผ่าน', value: false },
] as Option[];

const personTypeOptions = [
  { value: false, label: 'บุคคลธรรมดา' },
  { value: true, label: 'นิติบุคคล' },
] as Option[];

const data = defineModel<PP007Entrepreneurs>("vendor", { required: true });

const oldData = ref<PP007Entrepreneurs>();

const type = computed(() => {
  switch (props.title) {
    case PP006EntrepreneurType.EGP:
      return EntrepreneurType.Egp;
    case PP006EntrepreneurType.COI:
      return EntrepreneurType.Coi;
    default:
      return EntrepreneurType.Watchlist;
  }
});

const checkTypeForTitle = computed((): string => {
  if (props.title === PP006EntrepreneurType.COI) return 'COI';
  if (props.title === PP006EntrepreneurType.Watchlist) return 'Watchlist';
  return 'EGP';
});

const filteredShareholders = computed((): Shareholder[] =>
  data.value.shareholder?.filter((s): boolean => !s.checkType || s.checkType === checkTypeForTitle.value) ?? []
);

const hasEmptyShareholderRow = computed((): boolean =>
  filteredShareholders.value.some((s): boolean => !s.taxId && !s.firstName)
);

const filteredAttachments = ref<EntrepreneurAttachments[]>([]);

const handleUpsert = () => {
  if (data.value.entrepreneurId) {
    store.onUpsertAttachments(data.value.entrepreneurId!, type.value, filteredAttachments.value);
  }
};

const handleAdjustFileGroup = () => {
  const otherTypeAttachments =
    data.value.attachments?.map(a => ({
      ...a,
      fileAttachments: a.fileAttachments.filter(f => f.type !== type.value)
    })).filter(a => a.fileAttachments.length > 0) ?? [];

  const newAttachments =
    filteredAttachments.value
      ?.map(att => ({
        ...att,
        fileAttachments: att.fileAttachments?.map(f => ({ ...f, type: type.value })) ?? []
      }))
      .filter(att => att.fileAttachments.length > 0) ?? [];

  data.value.attachments = [...otherTypeAttachments, ...newAttachments];
};

const onSubmitAsync = async (): Promise<void> => {

  if (props.title === PP006EntrepreneurType.COI) {
    if (!data.value.coiCheckerResult) {
      ToastHelper.errorDescription('ไม่สามารถบันทึกข้อมูลได้ เนื่องจากยังไม่ได้ตรวจสอบข้อมูล COI');
      return;
    }

    if (filteredShareholders.value.some((s): boolean => !s.coiCheckerResult)) {
      ToastHelper.errorDescription('ไม่สามารถบันทึกข้อมูลได้ เนื่องจากยังไม่ได้ตรวจสอบข้อมูล COI ของผู้ถือหุ้น');
      return;
    }

    data.value.coi.date = new Date();
    filteredShareholders.value.forEach((s): void => {
      s.coiResultAt = new Date();
    });
  }

  if (props.title === PP006EntrepreneurType.EGP) {
    data.value.egp.date = new Date();
    data.value.shareholder?.forEach(s => {
      s.egpResultAt = new Date();
    });
  }

  if (props.title === PP006EntrepreneurType.Watchlist) {
    if (!data.value.watchlistCheckerResult) {
      ToastHelper.errorDescription('ไม่สามารถบันทึกข้อมูลได้ เนื่องจากยังไม่ได้ตรวจสอบข้อมูล Watchlist');
      return;
    }

    if (filteredShareholders.value.some((s): boolean => !s.watchlistCheckerResult)) {
      ToastHelper.errorDescription('ไม่สามารถบันทึกข้อมูลได้ เนื่องจากยังไม่ได้ตรวจสอบข้อมูล Watchlist ของผู้ถือหุ้น');
      return;
    }

    data.value.watchlist.date = new Date();
    filteredShareholders.value.forEach((s): void => {
      s.watchlistResultAt = new Date();
    });
  }

  handleAdjustFileGroup();

  if (data.value.shareholder) {
    data.value.shareholder = data.value.shareholder.map((s): Shareholder => {
      if (s.isJuristic) {
        return { ...s, firstName: (s.firstName ?? '').trim(), lastName: '' };
      }
      const fullName = (s.firstName ?? '').trim().replace(/\s+/g, ' ');
      const parts = fullName.split(' ');
      return { ...s, firstName: parts[0] ?? '', lastName: parts.slice(1).join(' ') || '' };
    });
  }

  if (store.body.jp006Id) {
    if (data.value.entrepreneurId) {
      const success = await store.onUpdateEntrepreneurCheck(data.value);
      if (success) show.value = false;
    } else {
      await store.onUpdateJp006Async();
      show.value = false;
    }
    return;
  }

  await store.onCreateJp006Async();
  show.value = false;
};



const checkEntrepreneur = async (): Promise<void> => {
  const checkType = props.title === PP006EntrepreneurType.COI ? 'COI' : 'Watchlist';
  const isJuristic = data.value.type === 'JuristicPerson' || data.value.type === 'Consortium' || data.value.type === 'JointVenture';

  const item = ((): CheckHistorySuVendorItem | null => {
    const taxId = data.value.entrepreneurTaxId || null;
    if (taxId) {
      return { taxpayerIdentificationNo: taxId, firstName: null, lastName: null, isJuristic };
    }
    const name = (data.value.entrepreneurName ?? '').trim().replace(/\s+/g, ' ');
    if (!name) return null;
    if (!data.value.type || data.value.type === 'Individual') {
      const parts = name.split(' ');
      return { taxpayerIdentificationNo: null, firstName: parts[0] || null, lastName: parts.slice(1).join(' ') || null, isJuristic };
    }
    return { taxpayerIdentificationNo: null, firstName: name, lastName: null, isJuristic };
  })();

  if (!item) return;

  const { data: results, status } = await entrepreneurCheckService.checkHistoryLookupAsync({
    vendorId: data.value.vendorId ?? null,
    checkType,
    items: [item],
  });

  if (status !== HttpStatusCode.Ok) return;

  if (!Array.isArray(results) || !results[0]) {
    const errorResult: QualificationResultDto = {
      result: QualificationResult.UnKnow,
      resultAt: new Date(),
      remark: (!Array.isArray(results) ? (results as { remark: string }).remark : null) ?? 'ตรวจสอบไม่สำเร็จ',
    };
    if (checkType === 'COI') data.value.coiCheckerResult = errorResult;
    else data.value.watchlistCheckerResult = errorResult;
    return;
  }

  const r = results[0];
  const result: QualificationResultDto = {
    result: r.result ? QualificationResult.Pass : QualificationResult.Fail,
    resultAt: new Date(),
    remark: r.remark,
  };

  if (checkType === 'COI') data.value.coiCheckerResult = result;
  else data.value.watchlistCheckerResult = result;
}

const checkShareholders = async (): Promise<void> => {
  if (!data.value.shareholder?.length) return;

  const checkType = props.title === PP006EntrepreneurType.COI ? 'COI' : 'Watchlist';

  const { data: results, status } = await entrepreneurCheckService.checkHistoryLookupAsync({
    vendorId: data.value.vendorId ?? null,
    checkType,
    items: filteredShareholders.value.map((s): { taxpayerIdentificationNo: string | null; firstName: string | null; lastName: string | null; isJuristic: boolean } => {
      const shareholderIsJuristic = s.isJuristic ?? false;
      if (checkType === 'Watchlist' && shareholderIsJuristic) {
        return {
          taxpayerIdentificationNo: s.taxId || null,
          firstName: (s.firstName ?? '').trim() || null,
          lastName: null,
          isJuristic: shareholderIsJuristic,
        };
      }
      const fullName = (s.firstName ?? '').trim().replace(/\s+/g, ' ');
      const parts = fullName.split(' ');
      return {
        taxpayerIdentificationNo: s.taxId || null,
        firstName: parts[0] || null,
        lastName: parts.slice(1).join(' ') || null,
        isJuristic: shareholderIsJuristic,
      };
    }),
  });

  if (status !== HttpStatusCode.Ok) return;

  if (!Array.isArray(results)) {
    const errorResult: QualificationResultDto = {
      result: QualificationResult.UnKnow,
      resultAt: new Date(),
      remark: (results as { remark: string }).remark ?? 'ตรวจสอบไม่สำเร็จ',
    };
    filteredShareholders.value.forEach((s): void => {
      if (props.title === PP006EntrepreneurType.COI) {
        s.coiCheckerResult = errorResult;
      } else {
        s.watchlistCheckerResult = errorResult;
      }
    });
    return;
  }

  filteredShareholders.value.forEach((s): void => {
    const name = `${s.firstName ?? ''} ${s.lastName ?? ''}`.trim();
    const matches = results.filter(r =>
      (s.taxId && r.taxpayerIdentificationNo === s.taxId) ||
      (!s.taxId && r.name === name)
    );

    if (!matches.length) return;

    const allResults: QualificationResultDto[] = matches.map((r): QualificationResultDto => ({
      result: r.result ? QualificationResult.Pass : QualificationResult.Fail,
      resultAt: new Date(),
      remark: r.remark,
    }));

    const hasFail = allResults.some(r => r.result === QualificationResult.Fail);
    const combinedRemark = allResults.map(r => r.remark).filter(Boolean).join('\n');
    const checkerResult: QualificationResultDto = {
      result: hasFail ? QualificationResult.Fail : QualificationResult.Pass,
      resultAt: new Date(),
      remark: combinedRemark,
    };

    if (props.title === PP006EntrepreneurType.COI) {
      s.coiCheckerResults = allResults;
      s.coiCheckerResult = checkerResult;
    } else {
      s.watchlistCheckerResults = allResults;
      s.watchlistCheckerResult = checkerResult;
    }
  });
}

const SetCoiResultWhenUndefined = () => {
  if (props.title === PP006EntrepreneurType.COI && data.value.coi.date == undefined) {
    data.value.coi.result = undefined;

    filteredShareholders.value.forEach((s): void => {
      if (s.coiResultAt == undefined) {
        s.coiResult = undefined;
      }
    });
  }
}

const SetEGPResultWhenUndefined = () => {
  if (props.title === PP006EntrepreneurType.EGP && data.value.egp.date == undefined) {
    data.value.egp.result = undefined;

    data.value.shareholder?.forEach(s => {
      if (s.egpResult == undefined) {
        s.egpResult = undefined;
      }
    });
  }
}

const SetWatchlistResultWhenUndefined = () => {
  if (props.title === PP006EntrepreneurType.Watchlist && data.value.watchlist.date == undefined) {
    data.value.watchlist.result = undefined;

    filteredShareholders.value.forEach((s): void => {
      if (s.watchlistResultAt == undefined) {
        s.watchlistResult = undefined;
      }
    });
  }
}

const CheckCoiQualificationWhenNeeded = (tasks: Promise<any>[]) => {
  if (props.title === PP006EntrepreneurType.COI && !data.value.coiCheckerResult) {
    tasks.push(checkEntrepreneur());
  }
}

const CheckWatchlistQualificationWhenNeeded = (tasks: Promise<any>[]) => {
  if (props.title === PP006EntrepreneurType.Watchlist && !data.value.watchlistCheckerResult) {
    tasks.push(checkEntrepreneur());
  }
}

const removeShareholder = (id: string): void => {
  if (!data.value.shareholder) return;
  data.value.shareholder = data.value.shareholder
    .filter((s): boolean => s.id !== id)
    .map((s, i): Shareholder => ({ ...s, sequence: i + 1 }));
};

const addShareholder = (): void => {
  if (!data.value.shareholder) data.value.shareholder = [];
  const nextSeq = (data.value.shareholder[data.value.shareholder.length - 1]?.sequence ?? 0) + 1;
  data.value.shareholder.push({
    id: crypto.randomUUID(),
    sequence: nextSeq,
    taxId: '',
    firstName: '',
    lastName: '',
    checkType: checkTypeForTitle.value,
    isJuristic: undefined,
    isDirector: false,
    isShareholder: false,
    watchlistResultRemark: '',
    watchlistResultAt: undefined!,
    coiResultRemark: '',
    coiResultAt: undefined!,
    egpRemark: '',
    egpResultAt: undefined!,
  });
};

watch(() => show.value, async (newValue) => {
  if (newValue) {
    oldData.value = { ...data.value };

    data.value.shareholder = data.value.shareholder?.map((s): Shareholder => ({
      ...s,
      firstName: [s.firstName, s.lastName].filter(Boolean).join(' '),
      lastName: '',
      isJuristic: s.isJuristic,
    }));

    filteredAttachments.value =
      data.value.attachments?.map(a => ({
        ...a,
        fileAttachments: a.fileAttachments.filter(f => f.type === type.value)
      })).filter(a => a.fileAttachments.length > 0) ?? [];

    SetCoiResultWhenUndefined();

    SetEGPResultWhenUndefined();

    SetWatchlistResultWhenUndefined();

    const tasks: Promise<any>[] = [];

    CheckCoiQualificationWhenNeeded(tasks);

    CheckWatchlistQualificationWhenNeeded(tasks);

    if (tasks.length && store.canEdit) {
      await Promise.all(tasks);
    }
  }
});

const onClose = (): void => {
  show.value = false;
  data.value = { ...oldData.value } as PP007Entrepreneurs;
};
</script>

<template>
  <Dialog v-model:visible="show" modal :style="{ width: '80vw' }" :draggable="false" :breakpoints="{ '575px': '90vw' }">
    <template #container>
      <Form @submit="onSubmitAsync()" class="p-5 overflow-auto" v-slot="{ handleSubmit }">
        <TitleHeader :label="props.title">
          <template #action>
            <i class="pi pi-times cursor-pointer" @click="() => onClose()"></i>
          </template>
        </TitleHeader>
        <Card>
          <template #content>
            <TitleHeader label="ข้อมูลคู่ค้า" />
            <div class="grid lg:grid-cols-3">
              <InfoItem title="สัญชาติของผู้ประกอบการ" :content="nationalityNameBycode(data.entrepreneurNationality)" />
              <InfoItem title="ประเภท" :content="typeNameByCode(data.type)" />
            </div>
            <div class="grid lg:grid-cols-3 mt-3">
              <InfoItem title="ประเภทผู้ประกอบการ" :content="data.entrepreneurType" />
              <InfoItem title="เลขประจำตัวผู้เสียภาษี" :content="data.entrepreneurTaxId" />
              <InfoItem title="ชื่อสถานประกอบการ" :content="data.entrepreneurName" />
            </div>
            <div class="grid lg:grid-cols-3 mt-3">
              <InfoItem title="หมายเลขโทรศัพท์สำหรับติดต่อ" :content="data.entrepreneurPhoneNumber" />
              <InfoItem title="อีเมล" :content="data.entrepreneurEmail" />
            </div>
          </template>
        </Card>
        <Card class="mt-5">
          <template #content>
            <TitleHeader :label="`ข้อมูล ${props.title}`">
              <template #action>
                <div class="flex items-center gap-2">
                  <a v-if="props.title === PP006EntrepreneurType.COI"
                    href="http://172.16.1.228/GHBANK/apps/default.aspx" target="_blank" rel="noopener noreferrer"
                    class="inline-flex items-center gap-1.5 px-3 py-1 text-xs font-medium text-blue-700 bg-blue-50 border border-blue-200 rounded-full hover:bg-blue-100 transition-colors">
                    <i class="pi pi-globe text-[10px]" />
                    COI
                  </a>
                  <a v-if="props.title === PP006EntrepreneurType.Watchlist"
                    href="https://cbssso.ghb.co.th/SSS/" target="_blank" rel="noopener noreferrer"
                    class="inline-flex items-center gap-1.5 px-3 py-1 text-xs font-medium text-blue-700 bg-blue-50 border border-blue-200 rounded-full hover:bg-blue-100 transition-colors">
                    <i class="pi pi-globe text-[10px]" />
                    Watchlist
                  </a>
                  <a v-if="props.title === PP006EntrepreneurType.EGP"
                    href="http://www.gprocurement.go.th/new_index.html" target="_blank" rel="noopener noreferrer"
                    class="inline-flex items-center gap-1.5 px-3 py-1 text-xs font-medium text-blue-700 bg-blue-50 border border-blue-200 rounded-full hover:bg-blue-100 transition-colors">
                    <i class="pi pi-globe text-[10px]" />
                    e-GP
                  </a>
                  <Button label="ตรวจสอบ" icon="pi pi-check" severity="primary" variant="outlined"
                    v-if="store.canEdit && menuStore.hasManage && (props.title == PP006EntrepreneurType.COI || props.title == PP006EntrepreneurType.Watchlist)"
                    @click="checkEntrepreneur" />
                </div>
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
            <Radio class="mt-8" :options="checkOptions" v-model="data.coi.result"
              v-if="props.title === PP006EntrepreneurType.COI" :disabled="!store.canEdit || !menuStore.hasManage" />
            <Radio class="mt-5" :options="checkOptions" v-model="data.watchlist.result"
              v-if="props.title === PP006EntrepreneurType.Watchlist"
              :disabled="!store.canEdit || !menuStore.hasManage" />
            <Radio class="mt-5" :options="checkOptions" v-model="data.egp.result"
              v-if="props.title === PP006EntrepreneurType.EGP" :disabled="!store.canEdit || !menuStore.hasManage" />
            <InputArea class="mt-4" label="หมายเหตุ" v-model="data.coi.remark"
              v-if="props.title === PP006EntrepreneurType.COI" :disabled="!store.canEdit || !menuStore.hasManage" />
            <InputArea class="mt-4" label="หมายเหตุ" v-model="data.watchlist.remark"
              v-if="props.title === PP006EntrepreneurType.Watchlist"
              :disabled="!store.canEdit || !menuStore.hasManage" />
            <InputArea class="mt-4" label="หมายเหตุ" v-model="data.egp.remark"
              v-if="props.title === PP006EntrepreneurType.EGP" :disabled="!store.canEdit || !menuStore.hasManage" />
            <div class="flex justify-end">
              <div class="text-end">
                <p class="mt-5"
                  v-if="props.title === PP006EntrepreneurType.COI ? data.coi.date : (props.title === PP006EntrepreneurType.EGP ? data.egp.date : data.watchlist.date)">
                  ตรวจสอบ ณ วันที่ : {{ ToDateTime(props.title === PP006EntrepreneurType.COI ? data.coi.date :
                    (props.title ===
                      PP006EntrepreneurType.EGP ? data.egp.date : data.watchlist.date)) }}</p>
              </div>
            </div>
          </template>
        </Card>
        <Card class="mt-5" v-if="props.title !== PP006EntrepreneurType.EGP && (store.canEdit || filteredShareholders.length > 0)">
          <template #content>
            <TitleHeader :label="`ข้อมูล ${props.title} สำหรับผู้ถือหุ้น`">
              <template #action>
                <Button
                  v-if="store.canEdit && menuStore.hasManage && props.title !== PP006EntrepreneurType.EGP && filteredShareholders.length > 0"
                  label="ตรวจสอบ" icon="pi pi-check" severity="primary" variant="outlined"
                  :disabled="hasEmptyShareholderRow"
                  @click="handleSubmit(checkShareholders)" />
                <Button
                  v-if="store.canEdit && menuStore.hasManage"
                  label="เพิ่มรายการ" icon="pi pi-plus" severity="primary" variant="outlined"
                  @click="addShareholder" />
              </template>
            </TitleHeader>
            <table class="w-full border-collapse text-sm mt-4" v-if="filteredShareholders.length > 0">
              <thead>
                <tr class="bg-gray-200 text-gray-900 font-bold">
                  <th class="border border-gray-300 px-3 py-2 text-center w-12">ลำดับที่</th>
                  <th class="border border-gray-300 px-3 py-2 text-center">กรรมการ / ผู้ถือหุ้น</th>
                  <template v-if="props.title === PP006EntrepreneurType.Watchlist">
                    <th class="border border-gray-300 px-3 py-2 text-center">ประเภท</th>
                    <th class="border border-gray-300 px-3 py-2 text-center">เลขที่บัตรประชาชน / เลขประจำตัวผู้เสียภาษี</th>
                    <th class="border border-gray-300 px-3 py-2 text-center">ชื่อ-นามสกุล / บริษัท<br><span class="text-red-500 font-normal text-xs"><b>หมายเหตุ</b> ชื่อ-นามสกุล (ไม่ต้องกรอกคำนำหน้าชื่อ) เช่น สมชาย ใจดี</span></th>
                  </template>
                  <template v-else>
                    <th class="border border-gray-300 px-3 py-2 text-center">เลขที่บัตรประชาชน</th>
                    <th class="border border-gray-300 px-3 py-2 text-center">ชื่อ-นามสกุล<br><span class="text-red-500 font-normal text-xs"><b>หมายเหตุ</b> ชื่อ-นามสกุล (ไม่ต้องกรอกคำนำหน้าชื่อ) เช่น สมชาย ใจดี</span></th>
                  </template>
                  <th class="border border-gray-300 px-3 py-2 text-center">ผลการตรวจสอบ</th>
                  <th class="border border-gray-300 px-3 py-2 text-center">ตรวจสอบ ณ วันที่</th>
                  <th v-if="store.canEdit && menuStore.hasManage" class="border border-gray-300 px-3 py-2 text-center w-16"></th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="value in filteredShareholders" :key="value.id" class="odd:bg-white even:bg-gray-50">
                  <td class="border border-gray-300 px-3 py-2 text-center text-gray-500">{{ value.sequence }}</td>
                  <td class="border border-gray-300 px-3 py-2">
                    <div class="flex flex-wrap gap-4">
                      <div class="flex items-center gap-2">
                        <Checkbox v-model="value.isDirector" :binary="true" :inputId="`isDirector-check-${value.id}`" :disabled="!store.canEdit || !menuStore.hasManage" />
                        <label :for="`isDirector-check-${value.id}`" class="cursor-pointer -mt-4">กรรมการ</label>
                      </div>
                      <div class="flex items-center gap-2">
                        <Checkbox v-model="value.isShareholder" :binary="true" :inputId="`isShareholder-check-${value.id}`" :disabled="!store.canEdit || !menuStore.hasManage" />
                        <label :for="`isShareholder-check-${value.id}`" class="cursor-pointer -mt-4">ผู้ถือหุ้น</label>
                      </div>
                    </div>
                  </td>
                  <td v-if="props.title === PP006EntrepreneurType.Watchlist" class="border border-gray-300 px-3 py-2">
                    <Radio :options="personTypeOptions" v-model="value.isJuristic" rules="required" :disabled="!store.canEdit || !menuStore.hasManage" />
                  </td>
                  <td class="border border-gray-300 px-1 py-1">
                    <InputField v-model.trim="value.taxId" rules="digits13" eager :disabled="!store.canEdit || !menuStore.hasManage" />
                  </td>
                  <td class="border border-gray-300 px-1 py-1">
                    <InputField v-model.trim="value.firstName" class="w-full" size="small" :disabled="!store.canEdit || !menuStore.hasManage" />
                  </td>
                  <td class="border border-gray-300 px-3 py-2">
                    <template v-if="props.title === PP006EntrepreneurType.COI && value.coiCheckerResult">
                      <template v-if="value.coiCheckerResults && value.coiCheckerResults.length > 1">
                        <div v-for="(r, i) in value.coiCheckerResults" :key="i">
                          <span v-if="r.result === QualificationResult.Pass">{{ r.remark }}</span>
                          <span v-else-if="r.result === QualificationResult.Fail">{{ r.remark }}</span>
                          <span v-else>ไม่สามารถเชื่อมต่อระบบ COI ได้ กรุณาลองใหม่อีกครั้ง</span>
                        </div>
                      </template>
                      <template v-else>
                        <span v-if="value.coiCheckerResult.result === QualificationResult.Pass">{{ value.coiCheckerResult.remark }}</span>
                        <span v-if="value.coiCheckerResult.result === QualificationResult.Fail" class="whitespace-pre-line">{{ value.coiCheckerResult.remark }}</span>
                        <span v-else-if="value.coiCheckerResult.result === QualificationResult.UnKnow">ไม่สามารถเชื่อมต่อระบบ COI ได้ กรุณาลองใหม่อีกครั้ง</span>
                      </template>
                    </template>
                    <template v-else-if="props.title === PP006EntrepreneurType.Watchlist && value.watchlistCheckerResult">
                      <template v-if="value.watchlistCheckerResults && value.watchlistCheckerResults.length > 1">
                        <div v-for="(r, i) in value.watchlistCheckerResults" :key="i">
                          <span v-if="r.result === QualificationResult.Pass">{{ r.remark }}</span>
                          <span v-else-if="r.result === QualificationResult.Fail">{{ r.remark }}</span>
                          <span v-else>ไม่สามารถเชื่อมต่อระบบ Watchlist ได้ กรุณาลองใหม่อีกครั้ง</span>
                        </div>
                      </template>
                      <template v-else>
                        <span v-if="value.watchlistCheckerResult.result === QualificationResult.Pass">{{ value.watchlistCheckerResult.remark }}</span>
                        <span v-if="value.watchlistCheckerResult.result === QualificationResult.Fail" class="whitespace-pre-line">{{ value.watchlistCheckerResult.remark }}</span>
                        <span v-else-if="value.watchlistCheckerResult.result === QualificationResult.UnKnow">ไม่สามารถเชื่อมต่อระบบ Watchlist ได้ กรุณาลองใหม่อีกครั้ง</span>
                      </template>
                    </template>
                    <span v-else>-</span>
                  </td>
                  <td class="border border-gray-300 px-3 py-2 text-center">
                    {{
                      ToDateTime(
                        props.title === PP006EntrepreneurType.COI ? value.coiCheckerResult?.resultAt :
                        props.title === PP006EntrepreneurType.EGP ? value.egpResultAt :
                        value.watchlistCheckerResult?.resultAt
                      ) || '-'
                    }}
                  </td>
                  <td v-if="store.canEdit && menuStore.hasManage" class="border border-gray-300 px-2 py-1 text-center">
                    <Button icon="pi pi-trash" severity="danger" variant="text" size="small" @click="removeShareholder(value.id)" />
                  </td>
                </tr>
              </tbody>
            </table>
          </template>
        </Card>
        <UploadFileGroup class="mt-4" v-if="!data.entrepreneurId" v-model="filteredAttachments" :disabled="!menuStore.hasManage" />

        <UploadFileGroup class="mt-4" v-if="data.entrepreneurId" v-model="filteredAttachments" @upload="handleUpsert"
          @remove-file="handleUpsert" @remove-group="handleUpsert" @reorder="handleUpsert"
          :disabled="!menuStore.hasManage" />
        <div class="flex items-center gap-3 justify-end mt-5" v-if="menuStore.hasManage">
          <Button label="ยกเลิก" severity="secondary" variant="outlined" @click="() => onClose()" v-if="store.canEdit" />
          <ButtonSave type="submit" v-if="store.canEdit" />
        </div>
      </Form>
    </template>
  </Dialog>
</template>
