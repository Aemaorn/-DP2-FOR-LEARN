import type { RouteRecordRaw } from 'vue-router'

export const ACRoute: RouteRecordRaw = {
  path: '/ac',
  children: [
    {
      path: 'ac01',
      name: 'ac01',
      component: () => import('@/views/AC/AC01/index.vue'),
    },
    {
      path: 'ac01/detail/:id',
      name: 'ac01Detail',
      component: () => import('@/views/AC/AC01/detail.vue'),
    },
  ],
};