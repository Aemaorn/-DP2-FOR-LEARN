import type { RouteRecordRaw } from 'vue-router';

export const PPRoute: RouteRecordRaw = {
  path: '/',
  children: [
    {
      path: 'pp',
      name: 'pp',
      component: () => import('@/views/PP/index.vue'),
    },
    {
      path: 'pp/detail/:id?',
      name: 'ppDetail',
      component: () => import('@/views/PP/detail.vue'),
    },
  ],
};
