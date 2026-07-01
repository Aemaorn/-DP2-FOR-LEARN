import type { RouteRecordRaw } from 'vue-router'

export const ANNRoute: RouteRecordRaw = {
  path: '/ann',
  children: [
    {
      path: 'ann001',
      name: 'ann001',
      component: () => import('@/views/ANN/ANN001/index.vue'),
    },
    {
      path: 'ann001/detail/:id?',
      name: 'ann001Detail',
      component: () => import('@/views/ANN/ANN001/detail.vue'),
    },
    {
      path: 'ann002',
      name: 'ann002',
      component: () => import('@/views/ANN/ANN002/index.vue'),
    },
    {
      path: 'ann002/detail/:id?',
      name: 'ann002Detail',
      component: () => import('@/views/ANN/ANN002/detail.vue'),
    },
    {
      path: 'ann003',
      name: 'ann003',
      component: () => import('@/views/ANN/ANN003/index.vue'),
    },
    {
      path: 'ann003/detail/:id?',
      name: 'ann003Detail',
      component: () => import('@/views/ANN/ANN003/detail.vue'),
    },
  ],
};
