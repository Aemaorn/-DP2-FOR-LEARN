import { checkType } from "@/enums/RP/rp004";
import { showPartnerDialogAsync } from "@/helpers/dialog";
import ToastHelper from "@/helpers/toast";
import type { TCheckHistoryItemResponse, TRp004Body } from "@/models/RP/rp004";
import rp004Service from "@/services/RP/rp004";
import suVendorShareholdersService from "@/services/SU/suVendorShareholders";
import { HttpStatusCode } from "axios";
import { defineStore } from "pinia";
import { ref } from "vue";

export type TSearchBy = 'idNumber' | 'name' | 'nacc';

export type TVendorItem = {
  searchBy?: TSearchBy;
  vendorName?: string;
  taxpayerIdentificationNo?: string;
  firstName?: string;
  lastName?: string;
  isDirector?: boolean;
  isShareholder?: boolean;
  isJuristic?: boolean;
};

export const useRp004Store = defineStore("rp004", () => {
  const body = ref<TRp004Body>({
    checkType: checkType.COI,
    result: false,
  } as TRp004Body);

  const vendorItems = ref<TVendorItem[]>([{ searchBy: 'idNumber', isJuristic: undefined }]);
  const checkResults = ref<TCheckHistoryItemResponse[]>([]);
  const isUnKnow = ref(false);

  const addVendorItem = (): void => {
    vendorItems.value.push({ searchBy: 'idNumber', isJuristic: undefined });
  };

  const removeVendorItem = async (index: number): Promise<void> => {
    if (body.value.vendorId) {
      await suVendorShareholdersService.deleteByVendorIdAsync(body.value.vendorId);
    }
    vendorItems.value.splice(index, 1);
    if (vendorItems.value.length === 0) {
      vendorItems.value.push({ searchBy: 'idNumber', isJuristic: undefined });
    }
  };

  const selectVendor = async () => {
    const res = await showPartnerDialogAsync();

    if (res) {
      body.value.vendorId = res.id;
      body.value.nationality = res.nationality;
      body.value.type = res.type;
      body.value.entrepreneurType = res.entrepreneurType;
      body.value.email = res.email;
      body.value.tel = res.tel;
      body.value.taxpayerIdentificationNo = res.taxpayerIdentificationNo;
      body.value.placeName = res.placeName;
      body.value.vendorName = res.establishmentName;

      const { data } = await suVendorShareholdersService.getByVendorIdAsync(res.id);
      vendorItems.value = data.length > 0
        ? data.map(s => ({
            searchBy: 'name' as TSearchBy,
            firstName: [s.firstName, s.lastName].filter(Boolean).join(' '),
            isDirector: s.isDirector ?? false,
            isShareholder: s.isShareholder ?? false,
            isJuristic: s.isJuristic ?? false,
          }))
        : [{ searchBy: 'idNumber' as TSearchBy }];
    }
  }

  const onCreateVendorCheckHistory = async (saveShareholders: boolean = true) => {
    const mappedItems = vendorItems.value.map((item): TVendorItem => {
      if (body.value.checkType === checkType.WatchList && item.isJuristic) {
        return { ...item, firstName: (item.firstName ?? '').trim(), lastName: undefined };
      }
      const fullName = (item.firstName ?? '').trim().replace(/\s+/g, ' ');
      const parts = fullName.split(' ');
      return { ...item, firstName: parts[0] ?? '', lastName: parts.slice(1).join(' ') || undefined };
    });

    const { data, status } = await rp004Service.checkHistoryLookup({
      // ส่ง vendorId เฉพาะตอนต้องการบันทึก backend จะบันทึก shareholder ก็ต่อเมื่อมี vendorId
      vendorId: saveShareholders ? body.value.vendorId : undefined,
      checkType: body.value.checkType,
      items: mappedItems,
    });

    if (status === HttpStatusCode.Ok) {
      const raw = data as unknown;
      if (!Array.isArray(raw) && (raw as { result?: string })?.result === 'UnKnow') {
        isUnKnow.value = true;
        // ข้อความจากระบบ Watchlist/COI (เช่น validation error) — ว่างถ้าเป็น error เชิงเทคนิค
        body.value.remark = (raw as { remark?: string })?.remark;
        body.value.resultDate = new Date();
        return;
      }

      isUnKnow.value = false;
      checkResults.value = data;
      body.value.resultDate = new Date();
      body.value.result = data.every((item): boolean => item.result === true);
      body.value.remark = data.map((item): string => `${item.name}: ${item.remark}`).join('\n');

      ToastHelper.success("ตรวจสอบ", `ตรวจสอบข้อมูล ${body.value.checkType} สำเร็จ`);
    }
  }

  const onClearBody = (): void => {
    const currentCheckType = body.value.checkType;
    body.value = {
      checkType: currentCheckType,
      result: false,
    } as TRp004Body;
    vendorItems.value = [{ searchBy: 'idNumber', isJuristic: undefined }];
    checkResults.value = [];
    isUnKnow.value = false;
  }

  return {
    body,
    vendorItems,
    checkResults,
    isUnKnow,
    selectVendor,
    addVendorItem,
    removeVendorItem,
    onCreateVendorCheckHistory,
    onClearBody
  }
})