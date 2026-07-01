import type { Budgets, Entrepreneurs, PP008Detail } from '../../models/PP008/pp008model';
import http from '@/configs/axios';

const createAsync = async (procurementId: string, body: PP008Detail) =>
  await http.post(`/api/procurement/${procurementId}/purchase-order-approval`, body);

const getByIdAsync = async (procurementId: string, id?: string,) => {
  const endpoint = `/api/procurement/${procurementId}/purchase-order-approval`;
  const finalEndpoint = id ? `${endpoint}/${id}` : endpoint;

  return await http.get(finalEndpoint);
};

const updateAsync = async (id: string, procurementId: string, body: PP008Detail) =>
  await http.put(`/api/procurement/${procurementId}/purchase-order-approval/${id}`, body);

const approveAsync = async (id: string, procurementId: string, remark?: string) =>
  await http.post(`/api/procurement/${procurementId}/purchase-order-approval/${id}/approve`, { remark });

const rejectAsync = async (id: string, procurementId: string, remark?: string, isAssignee: boolean = false) =>
  await http.post(`/api/procurement/${procurementId}/purchase-order-approval/${id}/reject`, { remark, isAssignee });

const createBudgetAsync = async (id: string, procurementId: string, body: Budgets) =>
  await http.post(`/api/procurement/${procurementId}/create-purchase-order-approval/${id}/budget`, body);

const updateBudgetAsync = async (body: Budgets) =>
  await http.put(`/api/procurement/update-purchase-order-approval/budget/${body.id}`, body);

const onDeleteBudgetAsync = async (id: string) =>
  await http.delete(`/api/procurement/delete-purchase-order-approval/Budget/${id}`);

const createEntrepreneursAsync = async (id: string, procurementId: string, body: Entrepreneurs) =>
   await http.post(`/api/procurement/${procurementId}/purchase-order-approval/${id}/entrepreneurs`, body);

const PP008Service = {
  createAsync,
  getByIdAsync,
  updateAsync,
  approveAsync,
  rejectAsync,
  createBudgetAsync,
  updateBudgetAsync,
  createEntrepreneursAsync,
  onDeleteBudgetAsync,
};

export default PP008Service