import http from '@/configs/axios';
import { Cam02Status } from '@/enums/CAM/CAM02/cam02';
import type { TCam02Body, TCam02Criteria, SourceTypeList, TCam02List, TChangeCommitteeSendAction } from '@/models/CAM/CAM02/cam02';
import type { Attachments } from '@/models/shared/uploadFile';

const onGetListAsync = async (criteria: TCam02Criteria) => {

  const params = {
    ...criteria,
    status: criteria.status === Cam02Status.All ? undefined : criteria.status,
  } as TCam02Criteria;

  return await http.get<TCam02List>(`/api/change-committee`, { params });
};

const onApproveAsync = async (id: string, body: TChangeCommitteeSendAction) =>
  http.post(`api/change-committee/${id}/approve`, body);

const onRejectAsync = async (id: string, body: TChangeCommitteeSendAction) =>
  http.post(`api/change-committee/${id}/reject`, body);

// TODO: Fix this
const onGetByIdAsync = async (id: string) => {
  return await http.get<TCam02Body>(`/api/change-committee/${id}`);
};

const onDropDownCommitteeAsync = async () => {
  return await http.get('/api/dropdown/committee-group-type');
};

// TODO: Fix this
const onCreateAsync = async (body: TCam02Body) => {
  return await http.post(`/api/change-committee`, body);
};

const onUpdateAsync = async (id: string, body: TCam02Body) => {
  return await http.put(`/api/change-committee/${id}`, body);
}

// TODO: Fix this
const onUpsertAttachmentsAsync = async (id: string, attachments: Array<Attachments>) => {
  const body = {
    attachments: attachments,
  };

  return await http.put(`/api/change-committee/${id}/attachments`, body);
};

const onGetCommitteeGroupTypeAsync = async (procurementId: string) => {
  return await http.get<SourceTypeList[]>(`/api/procurement/${procurementId}/committee-group-type`);
};

const onGetCommitteeBySourceTypeAsync = async (procurementId: string, sourceId: string, sourceType: string, committeeGroupType: string) => {
  return await http.get(`/api/procurement/${procurementId}/source/${sourceId}/source-type/${sourceType}/committee-group-type/${committeeGroupType}`);
};

const onAssigneeCommentAsync = async (id: string, reason: string) =>
  http.post(`/api/change-committee/${id}/assignee-comment`, { reason });

const onSetIsUnableToPerformDutiesAsync = async (id: string, acceptorId: string, isUnableToPerformDuties: boolean, remark?: string) => {
  const body = {
    isUnableToPerformDuties,
    remark,
  }

  return http.put(`/api/change-committee/${id}/acceptor/${acceptorId}/duties-status`, body);
};

const Cam02Service = {
  onGetListAsync,
  onCreateAsync,
  onUpdateAsync,
  onGetByIdAsync,
  onUpsertAttachmentsAsync,
  onGetCommitteeGroupTypeAsync,
  onGetCommitteeBySourceTypeAsync,
  onDropDownCommitteeAsync,
  onApproveAsync,
  onRejectAsync,
  onAssigneeCommentAsync,
  onSetIsUnableToPerformDutiesAsync,
};

export default Cam02Service;