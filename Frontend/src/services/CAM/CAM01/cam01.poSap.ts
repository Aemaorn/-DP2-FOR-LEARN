import http from '@/configs/axios';
import type { AcceptorType } from '@/enums/participants';
import type { Cam01PoSapBody, Cam01PoSapPayload } from '@/models/CAM/CAM01/cam01.poSap';

const onGetByIdAsync = async (amendmentId: string, id?: string) => {
  return await http.get<Cam01PoSapBody>(`/api/contract-amendment/${amendmentId}/po-sap/${id ?? ''}`);
};

const onCreateAsync = async (amendmentId: string, payload: Cam01PoSapBody) => {
  const body = mapToPayload(payload);

  return await http.post(`/api/contract-amendment/${amendmentId}/po-sap`, body);
};

const onUpdateAsync = async (amendmentId: string, id: string, payload: Cam01PoSapBody) => {
  const body = mapToPayload(payload);

  return await http.put(`/api/contract-amendment/${amendmentId}/po-sap/${id}`, body);
};

const onRejectedAsync = async (amendmentId: string, id: string, body: { remark?: string, group: AcceptorType }) => {
  return await http.post(`/api/contract-amendment/${amendmentId}/po-sap/${id}/reject`, body);
};

const onApprovedAsync = async (amendmentId: string, id: string, body: { remark?: string, group: AcceptorType }) => {
  return await http.post(`/api/contract-amendment/${amendmentId}/po-sap/${id}/approve`, body);
};

const mapToPayload = (data: Cam01PoSapBody): Cam01PoSapPayload => {
  return {
    id: data.id,
    contractAmendmentId: data.camContractAmendmentId,
    acceptors: data.acceptors,
    poSapNumber: data.newContract.poNumber,
    status: data.status,
  } as Cam01PoSapPayload;
}

const Cam01PoSapService = {
  onGetByIdAsync,
  onCreateAsync,
  onUpdateAsync,
  onRejectedAsync,
  onApprovedAsync,
};

export default Cam01PoSapService;