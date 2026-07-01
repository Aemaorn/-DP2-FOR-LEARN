import type { NotiCriteria, NotiListRes } from '@/models/notification/notification';
import { HttpStatusCode } from 'axios';
import { defineStore } from 'pinia';
import { ref } from 'vue';
import notificationService from '@/services/notification/notification';

export const useNotificationStore = defineStore('notification-store', () => {
  const criteria = ref<NotiCriteria>({
    pageNumber: 1,
    pageSize: 10,
  } as NotiCriteria);
  const notiRes = ref<NotiListRes>({} as NotiListRes);

  const getListAsync = async () => {
    const { data, status } = await notificationService.getListAsync(criteria.value);

    if (status === HttpStatusCode.Ok) {
      notiRes.value = data;
    }
  };

  const onClickNotiAsync = async (id: string, url: string, isRead: boolean) => {
    if (!isRead) {
      await notificationService.maskToReadNotiAsync(id);
    }

    await getListAsync();

    const path = url.startsWith('http') ? new URL(url).pathname : url;
    window.location.href = path;
  };

  const onClearData = () => {
    criteria.value = {
      pageNumber: 1,
      pageSize: 10,
    }

    notiRes.value = {
      ...notiRes.value,
      notifications: {
        data: [],
        totalRecords: 10,
      }
    }
  };

  const onReadAllNotiAsync = async () => {
    await notificationService.markAllToReadNotiAsync();
    await getListAsync();
  }

  return {
    criteria,
    notiRes,
    getListAsync,
    onClearData,
    onClickNotiAsync,
    onReadAllNotiAsync,
  };
});
