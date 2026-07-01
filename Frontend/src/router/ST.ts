import type { RouteRecordRaw } from 'vue-router';

export const STRoute: RouteRecordRaw = {
  path: '/st',
  children: [
    {
      path: 'st001',
      name: 'st001',
      component: () => import('@/views/ST/ST001/index.vue'),
    },
    {
      path: 'st001/detail/:id?',
      name: 'st001Detail',
      component: () => import('@/views/ST/ST001/detail.vue'),
    },
    {
      path: 'st003',
      name: 'st003',
      component: () => import('@/views/ST/ST003/index.vue'),
    },
    {
      path: 'st003/detail/:id?',
      name: 'st003Detail',
      component: () => import('@/views/ST/ST003/detail.vue'),
    },
    {
      path: 'st004',
      name: 'st004',
      component: () => import('@/views/ST/ST004/index.vue'),
    },
    {
      path: 'st004/detail/:code?',
      name: 'st004Detail',
      component: () => import('@/views/ST/ST004/detail.vue'),
    },
    {
      path: 'st005',
      name: 'st005',
      component: () => import('@/views/ST/ST005/index.vue'),
    },
    {
      path: 'st005/detail/:id?',
      name: 'st005Detail',
      component: () => import('@/views/ST/ST005/detail.vue'),
    },
    {
      path: 'st006',
      name: 'st006',
      component: () => import('@/views/ST/ST006/index.vue'),
    },
    {
      path: 'st006/detail/:id?',
      name: 'st006Detail',
      component: () => import('@/views/ST/ST006/detail.vue'),
    },
    {
      path: 'st007',
      name: 'st007',
      component: () => import('@/views/ST/ST007/index.vue'),
    },
    {
      path: 'st007/detail/:id?',
      name: 'st007Detail',
      component: () => import('@/views/ST/ST007/detail.vue'),
    },
    {
      path: 'st008',
      name: 'st008',
      component: () => import('@/views/ST/ST008/index.vue'),
    },
    {
      path: 'st009',
      name: 'st009',
      component: () => import('@/views/ST/ST009/index.vue'),
      meta: { skipPermission: true },
    },
    {
      path: 'st009/detail/:id?',
      name: 'st009Detail',
      component: () => import('@/views/ST/ST009/detail.vue'),
      meta: { skipPermission: true },
    },
    {
      path: 'st010',
      name: 'st010',
      component: () => import('@/views/ST/ST010/index.vue'),
    },
    {
      path: 'st010/detail/:id?',
      name: 'st010Detail',
      component: () => import('@/views/ST/ST010/detail.vue'),
    },
    {
      path: 'st099',
      name: 'st099',
      component: () => import('@/views/ST/ST099/index.vue'),
    },
    {
      path: 'st099/detail/:id?',
      name: 'st099Detail',
      component: () => import('@/views/ST/ST099/detail.vue'),
    },
  ],
};
