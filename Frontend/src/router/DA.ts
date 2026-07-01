import type { RouteRecordRaw } from 'vue-router';

export const DashboardRoute: RouteRecordRaw = {
  path: '/da',
  children: [
    {
      path: '/da001',
      name: 'da',
      component: () => import('@/views/DA/components/sub/WorkOverviewDetail.vue'),
    },
    {
      path: '/da002',
      name: 'dashboardMd',
      component: () => import('@/views/DAMD/DashboardMd.vue'),
    },
    {
      path: '/da003',
      name: 'da003',
      component: () => import('@/views/DA003/Da003.vue'),
    },
    {
      path: '/da004',
      name: 'da004',
      component: () => import('@/views/DA004/Da004.vue'),
    }
  ]
};
