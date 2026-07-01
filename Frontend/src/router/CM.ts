import type { RouteRecordRaw } from 'vue-router';

export const CMRoute: RouteRecordRaw = {
  path: '/cm',
  children: [
    {
      path: 'cm001',
      name: 'cm001',
      component: () => import('@/views/CM/CM001/index.vue'),
    },
    {
      path: 'cm001/detail/:id?',
      name: 'cm001Detail',
      component: () => import('@/views/CM/CM001/detail.vue'),
    },
    {
      path: 'cm001/detail/:id/period/:periodId?',
      name: 'cm001PeriodDetail',
      component: () => import('@/views/CM/CM001/periodDetail.vue'),
    },
    {
      path: 'cm004',
      name: 'cm004',
      component: () => import('@/views/CM/CM004/index.vue'),
    },
    {
      path: 'cm004/detail/:id',
      name: 'cm004Detail',
      component: () => import('@/views/CM/CM004/detail.vue'),
    },
    {
      path: 'cm004/detail/:id/disbursement/:disbursementId?',
      name: 'cm004DisbursementDetail',
      component: () => import('@/views/CM/CM004/Disbursement.vue'),
    },
    {
      path: 'cm005',
      name: 'cm005',
      component: () => import('@/views/CM/CM005/index.vue'),
    },
    {
      path: 'cm005/contract/:contractId/detail/:id',
      name: 'cm005Detail',
      component: () => import('@/views/CM/CM005/detail.vue'),
    },
    {
      path: 'cm005/contract-selected',
      name: 'cm005ContractSelected',
      component: () => import('@/views/CM/CM005/ContractSelect.vue'),
    },
    {
      path: 'cm006',
      name: 'cm006',
      component: () => import('@/views/CM/CM006/index.vue'),
    },

    {
      path: 'cm006/detail/:contractVendorId?/:id?',
      name: 'cm006Detail',
      component: () => import('@/views/CM/CM006/detail.vue'),
    },
    {
      path: 'cm007',
      name: 'cm007',
      component: () => import('@/views/CM/CM007/index.vue'),
    },
    {
      path: 'cm007/detail/:id?',
      name: 'cm007Detail',
      component: () => import('@/views/CM/CM007/detail.vue'),
    },
  ],
};
