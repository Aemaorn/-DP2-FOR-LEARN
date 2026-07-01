import http from '@/configs/axios';
import type { NotiCriteria } from '@/models/notification/notification';

const getListAsync = async (params: NotiCriteria) =>
  await http.get('api/su/notifications', { params, headers: { isDisabledLoad: true } });

const maskToReadNotiAsync = async (id: string) =>
  await http.put(`api/su/notifications/${id}/mark-read`, { headers: { isDisabledLoad: true } });

const markAllToReadNotiAsync = async () =>
  await http.put('api/su/notifications/mark-read-all', { headers: { isDisabledLoad: true } });

const notificationService = {
  getListAsync,
  maskToReadNotiAsync,
  markAllToReadNotiAsync,
};

export default notificationService
