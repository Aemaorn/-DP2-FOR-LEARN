import type { RouteRecordRaw } from 'vue-router';

export const WorklistRoute: RouteRecordRaw = {
  path: '/wl',
  name: 'worklist',
  component: () => import('@/views/Worklist/index.vue'),
};