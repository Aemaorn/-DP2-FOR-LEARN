import http from '@/configs/axios';
import { Cam01Status } from '@/enums/CAM/CAM01/cam01';
import { ConvertStartToEndDate } from '@/helpers/dateTime';
import type { Cam001ContractCriteria, Cam01Body, Cam01Criteria } from '@/models/CAM/CAM01/cam01';
import type { Attachments } from '@/models/shared/uploadFile';

const onGetListAsync = async (criteria: Cam01Criteria) => {
  const [, endDate] = ConvertStartToEndDate(undefined, criteria.signedDate);

  const params = {
    ...criteria,
    signedDate: endDate,
    status: criteria.status === Cam01Status.All ? undefined : criteria.status,
  } as Cam01Criteria;

  return await http.get(`/api/contract-amendment`, { params });
};

const onGetByIdAsync = async (id: string) => {
  return await http.get<Cam01Body>(`/api/contract-amendment/${id}`);
};

const onGetContractDialogAsync = async (params: Cam001ContractCriteria) => {
  return await http.get(`/api/contract-amendments/contract-list`, { params });
};

const onCreateAsync = async (body: Cam01Body) => {
  return await http.post(`/api/contract-amendment`, body);
};

const onUpsertAttachmentsAsync = async (id: string, attachments: Array<Attachments>) => {
  const body = {
    attachments: attachments,
  };

  return await http.put(`/api/contract-amendments/${id}/attachments`, body);
};

const Cam01Service = {
  onGetListAsync,
  onGetContractDialogAsync,
  onCreateAsync,
  onGetByIdAsync,
  onUpsertAttachmentsAsync,
};

export default Cam01Service;