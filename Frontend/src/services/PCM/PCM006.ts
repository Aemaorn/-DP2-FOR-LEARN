import http from '@/configs/axios';
import { EPcm006Status } from '@/enums/pcm006';
import type { TPcm006Criteria, TPcm006Detail } from '@/models/PCM/pcm006';
import type { Attachments } from '@/models/shared/uploadFile';

const createsync = async (body: TPcm006Detail) =>
  await http.post('api/petty-cash-reimbursement', body);

const getListAsync = async (criteria: TPcm006Criteria) => {
  const params = {
    ...criteria
  }

  if (params.status == EPcm006Status.All) {
    params.status = undefined
  }

  return http.get('api/petty-cash-reimbursement', { params });
}

const getGlAccountsAsync = async (departmentCode?: string) => {
  const params = {
    departmentCode,
  }

  return http.get('api/petty-cash-reimbursement/gl-accounts', { params });
}

const getByIdAsync = async (id: string) =>
  await http.get(`api/petty-cash-reimbursement/${id}`);

const updateAsync = async (id: string, body: TPcm006Detail) =>
  await http.put(`api/petty-cash-reimbursement/${id}`, body);

const approveAsync = async (id: string, remark?: string) => {
  const body = {
    remark,
  }

  return await http.post(`api/petty-cash-reimbursement/${id}/approve`, body)
}

const rejectAsync = async (id: string, remark: string) => {
  const body = {
    remark,
  }

  return await http.post(`api/petty-cash-reimbursement/${id}/reject`, body);
}

const attachmentsAsync = async (pcm006Id: string, attachments: Array<Attachments>) => {
  const body = {
    attachments: attachments,
  };

  return await http.put(`/api/petty-cash-reimbursement/${pcm006Id}/attachment`, body);
};

const onDeleteAsync = async (id: string) => {
  return await http.delete(`api/petty-cash-reimbursement/${id}`);
}

const pcm006Service = {
  createsync,
  getListAsync,
  getGlAccountsAsync,
  getByIdAsync,
  updateAsync,
  approveAsync,
  rejectAsync,
  attachmentsAsync,
  onDeleteAsync
}

export default pcm006Service;