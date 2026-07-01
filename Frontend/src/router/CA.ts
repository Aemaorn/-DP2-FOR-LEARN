import type { RouteRecordRaw } from 'vue-router';

export const CARoute: RouteRecordRaw = {
  path: '/ca',
  children: [
    {
      path: 'ca02',
      name: 'ca02',
      component: () => import('@/views/CA/CA02/index.vue'),
    },
    {
      path: "ca02/detail/:id?",
      name: "ca02Detail",
      component: () => import("@/views/CA/CA02/detail.vue"),
    },
  ],
};
