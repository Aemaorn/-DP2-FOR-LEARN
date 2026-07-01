import { createRouter, createWebHistory } from 'vue-router'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      name: 'overview',
      component: () => import('@/views/OverviewDashboard.vue'),
    },
    {
      path: '/tracking',
      name: 'tracking',
      component: () => import('@/views/DashboardView.vue'),
    },
  ],
})

export default router
