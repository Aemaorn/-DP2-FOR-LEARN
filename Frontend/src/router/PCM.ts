import type { RouteRecordRaw } from 'vue-router';

export const PCMRoute: RouteRecordRaw = {
  path: '/pcm',
  children: [
    {
      path: 'pcm002',
      name: 'pcm002',
      component: () => import('@/views/PCM/PCM002/index.vue'),
    },
    {
      path: 'pcm002/detail/:id?',
      name: 'pcm002Detail',
      component: () => import('@/views/PCM/PCM002/detail.vue'),
    },
    {
      path: "pcm003",
      name: "pcm003",
      component: () => import("@/views/PCM/PCM003/index.vue"),
    },
    {
      path: "pcm003/detail/:id?",
      name: "pcm003Detail",
      component: () => import("@/views/PCM/PCM003/detail.vue"),
    },
    {
      path: 'pcm004',
      name: 'pcm004',
      component: () => import('@/views/PCM/PCM004/index.vue'),
    },
    {
      path: 'pcm004/detail/:id?',
      name: 'pcm004Detail',
      component: () => import('@/views/PCM/PCM004/detail.vue'),
    },
    {
      path: 'pcm005',
      name: 'pcm005',
      component: () => import('@/views/PCM/PCM005/index.vue'),
    },
    {
      path: "pcm002/detail/:id?",
      name: "pcm002Detail",
      component: () => import("@/views/PCM/PCM002/detail.vue"),
    },
    {
      path: 'pcm005/detail/:id?',
      name: 'pcm005Detail',
      component: () => import('@/views/PCM/PCM005/detail.vue'),
    },
    {
      path: 'pcm006',
      name: 'pcm006',
      component: () => import("@/views/PCM/PCM006/index.vue"),
    },
    {
      path: 'pcm006/detail/:id?',
      name: 'pcm006Detail',
      component: () => import("@/views/PCM/PCM006/detail.vue")
    },
    {
      path: 'pcm007',
      name: 'pcm007',
      component: () => import('@/views/PCM/PCM007/index.vue'),
    },
    {
      path: 'pcm007/detail/:id?',
      name: 'pcm007Detail',
      component: () => import('@/views/PCM/PCM007/detail.vue'),
    },
  ],
};
