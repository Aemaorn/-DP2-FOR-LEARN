import http from '@/configs/axios';
import type { TDataTableResult } from '@/models/shared/paginated';
import type { TSt004Criteria, TSt004Detail, TSt004List } from '@/models/ST/st004';

const onGetListAsync = async (params: TSt004Criteria) => await http.get<TDataTableResult<TSt004List>>(`/api/st/st004`, { params });

const onGetProgramPermissionAsync = async () => await http.get(`/api/st/st004/program`);

const onGetByCodeAsync = async (code: string) => await http.get(`/api/st/st004/${code}`);

const onCreateAsync = async (body: TSt004Detail) => await http.post(`/api/st/st004`, body);

const onUpdateAsync = async (code: string, body: TSt004Detail) =>
  await http.put(`/api/st/st004/${code}`, body);

const onDeleteAsync = async (code: string) => await http.delete(`/api/st/st004/${code}`);

const ST004Service = {
  onGetListAsync,
  onGetProgramPermissionAsync,
  onGetByCodeAsync,
  onCreateAsync,
  onUpdateAsync,
  onDeleteAsync,
};

export default ST004Service;
