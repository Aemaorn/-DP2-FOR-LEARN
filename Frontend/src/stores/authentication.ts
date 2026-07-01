import { HttpStatusCode } from 'axios';
import { defineStore } from 'pinia';
import { ref } from 'vue';
import type { UserProfile } from '@/models/authentication';
import { showConfirmDialogAsync } from '@/helpers/dialog';
import { ConfirmDialogType } from '@/enums/dialog';
import ToastHelper from '@/helpers/toast';
import authenticationService from '@/services/authentication';
import cookie from '@/configs/cookie';
import router from '@/router';
import { useMenuStore } from './menu';

export const useAuthenticationStore = defineStore('authentication-store', () => {
  const menuStore = useMenuStore();
  const username = ref<string>('');
  const password = ref<string>('');
  const profile = ref<UserProfile>({} as UserProfile);

  const loginAsync = async (): Promise<void> => {
    try {
      const { data, status } = await authenticationService.loginAsync(username.value, password.value);

      if (status === HttpStatusCode.Ok) {
        cookie.set('userLogin', data.userId);
        cookie.set('userNameLogin', username.value);
        cookie.set('accessToken', data.accessToken, 1);  // 1 day (access token expires sooner)
        cookie.set('refreshToken', data.refreshToken);   // 7 days (default)

        router.replace('/wl');

        await getProfileAsync();
        await menuStore.getMenuAsyncAsync();

        return ToastHelper.success('เข้าสู่ระบบ', 'เข้าสู่ระบบสำเร็จ');
      }
    } catch (error: any) {
      if (error?.response?.status === HttpStatusCode.Unauthorized) {
        cookie.remove('accessToken');
        cookie.remove('refreshToken');
        const adMessage = typeof error?.response?.data === 'string' && error.response.data
          ? error.response.data
          : error?.response?.data?.message ?? error?.response?.data?.detail ?? null;
        const message = adMessage ?? 'ชื่อผู้ใช้งานหรือรหัสผ่านไม่ถูกต้อง';
        ToastHelper.error('เข้าสู่ระบบล้มเหลว', message);
        return;
      }
      throw error;
    }
  };

  const loginDp1Async = async (): Promise<void> => {
    const { data, status } = await authenticationService.loginDp1Async(username.value, password.value);

    if (status === HttpStatusCode.Ok) {
      cookie.set('accessToken-dp1', data.access_token);
      cookie.set('refreshToken-dp1', data.refresh_token);
    }
  };

  const getProfileAsync = async (): Promise<void> => {
    const { data, status } = await authenticationService.getProfileAsync();

    if (status === HttpStatusCode.Ok) {
      profile.value = data;
    }
  };

  const signOutAsync = async (): Promise<void> => {
    if (!(await showConfirmDialogAsync(ConfirmDialogType.Logout))) return;

    try {
      // Call backend signout API to blacklist the JWT token
      await authenticationService.signOutAsync();
    } catch (error) {
      // Continue with logout even if API call fails
      console.warn('Signout API call failed:', error);
    }

    cookie.remove('accessToken');
    cookie.remove('refreshToken');

    username.value = '';
    password.value = '';
    profile.value = {} as UserProfile;

    router.push('/login');

    ToastHelper.success('ออกจากระบบ', 'ออกจากระบบสำเร็จ');
  };

  const setTokenCachAsync = async (userName: string, accessToken: string, refreshToken: string) => {
    return await authenticationService.setTokenCachAsync(userName, accessToken, refreshToken);
  }

  return {
    username,
    password,
    profile,
    loginAsync,
    loginDp1Async,
    getProfileAsync,
    signOutAsync,
    setTokenCachAsync
  };
}, {
  persist: {
    key: 'authentication-store',
    storage: localStorage,
    pick: ['profile'],
  },
});
