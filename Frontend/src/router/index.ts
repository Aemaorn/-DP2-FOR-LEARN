import { createRouter, createWebHistory } from 'vue-router';
import { DashboardRoute } from './DA';
import { STRoute } from './ST';
import { PRoute } from './PL';
import { PCMRoute } from './PCM';
import { CMRoute } from './CM';
import { CARoute } from './CA';
import { CAMRoute } from './CAM';
import { ACRoute } from './AC';
import { PPRoute } from './PP';
import { RPRoute } from './RP';
import { WorklistRoute } from './Worklist';
import { useMenuStore } from '@/stores/menu';
import { storeToRefs } from 'pinia';
import cookie from '@/configs/cookie';
import authenticationService from '@/services/authentication';
import { HttpStatusCode } from 'axios';
import { getIsRefreshing, setIsRefreshing, subscribeTokenRefresh } from '@/utils/tokenRefresh';
import { ANNRoute } from './ANN';

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      component: () => import('@/layouts/Main.vue'),
      meta: { requiresAuth: true },
      children: [
        {
          path: 'demo',
          name: 'demo',
          component: () => import('@/views/others/demo.vue'),
        },
        DashboardRoute,
        PCMRoute,
        STRoute,
        PRoute,
        CARoute,
        CAMRoute,
        CMRoute,
        ACRoute,
        PPRoute,
        RPRoute,
        WorklistRoute,
        ANNRoute,
        {
          path: 'search-all',
          name: 'search-all',
          component: () => import('@/views/SearchAll/Index.vue'),
          meta: { skipPermission: true },
        },
        {
          path: 'attachment-file-all/procurement/:procurementId',
          name: 'attachment-file-all',
          component: () => import('@/views/AttachmentFileAll/attachmentFiles.vue'),
          meta: { skipPermission: true },
        },
        {
          path: 'manuals',
          name: 'manuals',
          component: () => import('@/views/others/manuals.vue'),
          meta: { skipPermission: true },
        },
      ],
    },
    {
      path: '/public/procurement-tracking',
      name: 'public-procurement-tracking',
      component: () => import('@/views/PublicDashboard/ProcurementTracking.vue'),
    },
    {
      path: '/login',
      name: 'login',
      component: () => import('@/views/Authentication/login.vue'),
    },
    {
      path: '/forbidden',
      name: 'forbidden',
      component: () => import('@/views/others/notPermission.vue'),
    },
    {
      path: '/:pathMatch(.*)*',
      name: 'notFound',
      component: () => import('@/views/others/notfound.vue'),
    },
  ],
});

const initMenu = async (path: string) => {
  const menuStore = useMenuStore();

  await menuStore.getMenuAsyncAsync();
  menuStore.getCurrentMenuPermission(path);
};

router.beforeEach(async (to, _, next) => {
  const menuStore = useMenuStore();
  const { hasPermission } = storeToRefs(menuStore);

  if (to.meta.requiresAuth) {
    let accessToken = cookie.get("accessToken");

    // ถ้าไม่มี accessToken แต่มี refreshToken ให้ลอง refresh token ก่อน
    if (accessToken == null) {
      const refreshToken = cookie.get("refreshToken");
      const userId = cookie.get("userLogin");

      if (refreshToken && userId) {
        // ถ้ากำลัง refresh อยู่ ให้รอ token ใหม่
        if (getIsRefreshing()) {
          try {
            accessToken = await new Promise<string>((resolve, reject) => {
              subscribeTokenRefresh(
                (newToken: string) => resolve(newToken),
                (error: unknown) => reject(error instanceof Error ? error : new Error(String(error)))
              );
            });
            cookie.set("accessToken", accessToken, 1);
          } catch {
            // Token refresh failed, will redirect to login
          }
        } else {
          // ยังไม่มีใคร refresh ให้เริ่ม refresh
          setIsRefreshing(true);
          try {
            const { data, status } = await authenticationService.refreshTokenAsync(userId, refreshToken);

            if (status === HttpStatusCode.Ok) {
              cookie.set("accessToken", data.accessToken, 1);
              cookie.set("refreshToken", data.refreshToken);
              accessToken = data.accessToken;
            }
          } catch {
            // Token refresh failed, will redirect to login
          } finally {
            setIsRefreshing(false);
          }
        }
      }
    }

    if (accessToken != null) {
      if (to.meta.skipPermission) {
        return next();
      }

      await initMenu(to.path);

      if (!hasPermission.value && to.path != '/') {
        return next({ name: 'forbidden' });
      }

      next();
    } else {
      next({ name: 'login' });
    }
  } else {
    next();
  }
});

export default router;