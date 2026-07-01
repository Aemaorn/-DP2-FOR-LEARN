import type { RouteRecordRaw } from 'vue-router';

export const PRoute: RouteRecordRaw = {
  path: '/pl',
  children: [
    {
      path: 'pl001',
      name: 'pl001',
      component: () => import('@/views/PL/PL001/index.vue'),
    },
    {
      path: 'pl001/detail/:id?',
      name: 'pl001Detail',
      component: () => import('@/views/PL/PL001/detail.vue'),
    },
    {
      path: 'pl002',
      name: 'pl002',
      component: () => import('@/views/PL/PL002/index.vue'),
    },
    {
      path: 'pl002/detail/:id?',
      name: 'pl002Detail',
      component: () => import('@/views/PL/PL002/detail.vue'),
      meta: { skipPermission: true },
    },
  ],
};
