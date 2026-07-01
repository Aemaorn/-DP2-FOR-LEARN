import { Cam01PoStep, Cam01Status, Cam01Type } from "@/enums/CAM/CAM01/cam01";
import { ConfirmDialogType } from "@/enums/dialog";
import { showConfirmDialogAsync } from "@/helpers/dialog";
import ToastHelper from "@/helpers/toast";
import type { Cam01Body } from "@/models/CAM/CAM01/cam01";
import router from "@/router";
import Cam01Service from "@/services/CAM/CAM01/cam01";
import { HttpStatusCode } from "axios";
import { defineStore } from "pinia";
import type { MenuItem } from "primevue/menuitem";
import { ref } from "vue";

export const useCam01DetailStore = defineStore('cam01-detail-store', () => {
  const initbody = {
    type: Cam01Type.AppendNewPurchaseOrder,
    steps: [] as Array<Cam01PoStep>,
    status: Cam01Status.Draft,
  } as Cam01Body;

  const routeItems = ref(
    [
      { label: 'รายการบันทึกต่อท้ายสัญญา', url: '/cam/cam01' },
      { label: 'จัดการบันทึกต่อท้ายสัญญา' },
    ] as MenuItem[]);

  const body = ref<Cam01Body>(structuredClone(initbody));

  const onResetBody = () => {
    body.value = structuredClone(initbody);
  };

  const onGetByIdAsync = async (id: string) => {
    const { data, status } = await Cam01Service.onGetByIdAsync(id);

    if (status === HttpStatusCode.Ok) {
      body.value = data;
    }
  };

  const onSubmitAsync = async () => {
    if (!body.value.contractDraftVendorId) {
      return ToastHelper.errorDescription("กรุณาเลือกสัญญา");
    }

    if (!await showConfirmDialogAsync(ConfirmDialogType.ConfirmData)) return;

    const { data, status } = await Cam01Service.onCreateAsync(body.value);

    if (status === HttpStatusCode.Created) {
      ToastHelper.createdMessageToast();
      router.replace({ name: 'cam01-detail', params: { id: data } });
      await onGetByIdAsync(data);
    }
  };

  const onUpsertAttachments = async () => {
    if (!body.value.id) return;

    const { status } = await Cam01Service.onUpsertAttachmentsAsync(body.value.id, body.value.attachments);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();

      await onGetByIdAsync(body.value.id);
    };
  };

  return {
    // Variables
    routeItems,
    body,

    // Actions
    onResetBody,
    onGetByIdAsync,
    onSubmitAsync,
    onUpsertAttachments,
  };
});