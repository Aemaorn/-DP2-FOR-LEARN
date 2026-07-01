import { useCam01DetailStore } from '@/stores/CAM/CAM01/cam01.detail';
import { useCam02DetailStore } from '@/stores/CAM/CAM02/cam02Store';
import type { RouteRecordRaw } from 'vue-router';

export const CAMRoute: RouteRecordRaw = {
  path: '/cam',
  children: [
    {
      path: 'cam01',
      name: 'cam01',
      component: () => import('@/views/CAM/CAM01/index.vue'),
    },
    {
      path: 'cam01/detail/:id?',
      name: 'cam01-detail',
      component: () => import('@/views/CAM/CAM01/detail.vue'),
      beforeEnter: async (to, _, next) => {
        const store = useCam01DetailStore();
        store.onResetBody();

        if (to.params.id) {
          await store.onGetByIdAsync(to.params.id as string);
        }

        next();
      }
    },
    {
      path: 'cam02',
      name: 'cam02',
      component: () => import('@/views/CAM/CAM02/index.vue'),
    },
    {
      path: 'cam02/detail/:id?',
      name: 'cam02-detail',
      component: () => import('@/views/CAM/CAM02/detail.vue'),
      beforeEnter: async (to, _, next) => {
        const store = useCam02DetailStore();
        store.resetDetail();

        if (to.params.id) {
          await store.onGetCommitteeChangeById(to.params.id as string);
        }

        next();
      }
    },
  ],
};
