import type { RouteRecordRaw } from 'vue-router';

export const RPRoute: RouteRecordRaw = {
  path: '/rp',
  children: [
    {
      path: 'rp001',
      name: 'rp001',
      component: () => import('@/views/RP/RP001/index.vue'),
    },
    {
      path: 'rp001/detail/:id?',
      name: 'rp001Detail',
      component: () => import('@/views/RP/RP001/detail.vue'),
      meta: { skipPermission: true },
    },
    {
      path: "rp002",
      name: 'rp002',
      component: () => import('@/views/RP/RP002/index.vue'),
    },
    {
      path: "rp002/detail/:id?",
      name: 'rp002Detail',
      component: () => import("@/views/RP/RP002/detail.vue"),
      meta: { skipPermission: true },
    },
    {
      path: 'rp003',
      name: 'rp003',
      component: () => import('@/views/RP/RP003/index.vue'),
    },
    {
      path: 'rp004',
      name: 'rp004',
      component: () => import('@/views/RP/RP004/index.vue'),
    },
  ],
};