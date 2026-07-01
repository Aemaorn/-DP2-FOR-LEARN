import axios, { HttpStatusCode, type AxiosInstance, type InternalAxiosRequestConfig } from 'axios';
import { useLoadingStore } from '@/stores/loading';
import { decodeUserId, isNullorEmpty } from "@/utils/validate";
import cookie from './cookie';
import ToastHelper from '@/helpers/toast';
import router from '@/router';
import { errorMessageHandler } from '@/helpers/error';
import authenticationService from '@/services/authentication';
import {
  getIsRefreshing,
  setIsRefreshing,
  subscribeTokenRefresh,
  onTokenRefreshed,
  onTokenRefreshFailed,
} from '@/utils/tokenRefresh';

const apiUrl = import.meta.env.VITE_APP_API_URL;

const axiosInstance = axios.create({
  baseURL: apiUrl,
});

const http = axios.create({
  baseURL: apiUrl,
});

// Paths that should not trigger global loading indicator
const IGNORE_LOADING_PATHS = ['api/su/notifications', '/api/dropdown/parameter'];

// Paths that handle their own error toasts — skip global 500 toast
const IGNORE_SERVER_ERROR_PATHS = ['api/coi/', 'api/watchlist/'];

const shouldIgnoreServerError = (url?: string): boolean => {
  if (!url) return false;
  return IGNORE_SERVER_ERROR_PATHS.some(path => url.includes(path));
};

const shouldIgnoreLoading = (url?: string): boolean => {
  if (!url) return false;
  return IGNORE_LOADING_PATHS.some(
    (path) => url.startsWith(path) || url.includes(`/${path}`)
  );
};

// Retry flag key to prevent infinite retry loop
const RETRY_FLAG = '__isRetry';

/**
 * Shared token refresh handler for both axios instances
 * Prevents code duplication and ensures consistent behavior
 */
const handleUnauthorizedError = async (
  error: any,
  instance: AxiosInstance,
): Promise<any> => {
  const originalRequest = error.config;

  if (originalRequest?.url?.includes('/user/signin')) {
    throw error;
  }

  // ป้องกัน infinite retry - ถ้า retry แล้วยัง 401 ให้หยุด
  if (originalRequest[RETRY_FLAG]) {
    return alertSessionExpired();
  }

  const refreshToken = cookie.get("refreshToken");
  const accessToken = cookie.get("accessToken");
  const userId = cookie.get('userLogin') || decodeUserId(accessToken);

  if (!isNullorEmpty(refreshToken) && userId) {
    // ถ้ากำลัง refresh อยู่ ให้รอ token ใหม่
    if (getIsRefreshing()) {
      return new Promise((resolve, reject) => {
        subscribeTokenRefresh(
          (newToken: string) => {
            originalRequest.headers.Authorization = `Bearer ${newToken}`;
            originalRequest.transformResponse = axios.defaults.transformResponse;
            originalRequest.transformRequest = axios.defaults.transformRequest;
            originalRequest[RETRY_FLAG] = true;
            resolve(instance(originalRequest));
          },
          (err: unknown) => {
            reject(err instanceof Error ? err : new Error(String(err)));
          }
        );
      });
    }

    setIsRefreshing(true);

    try {
      const { data, status } = await authenticationService.refreshTokenAsync(userId, refreshToken);

      if (status !== HttpStatusCode.Ok) {
        setIsRefreshing(false);
        onTokenRefreshFailed(new Error('Token refresh failed'));
        return alertSessionExpired();
      }

      clearCookie();
      cookie.set("accessToken", data.accessToken, 1);
      cookie.set("refreshToken", data.refreshToken);

      onTokenRefreshed(data.accessToken);
      setIsRefreshing(false);

      originalRequest.headers.Authorization = `Bearer ${data.accessToken}`;
      originalRequest.transformResponse = axios.defaults.transformResponse;
      originalRequest.transformRequest = axios.defaults.transformRequest;
      originalRequest[RETRY_FLAG] = true;

      return instance(originalRequest);
    } catch (err) {
      setIsRefreshing(false);
      onTokenRefreshFailed(err);
      return alertSessionExpired();
    }
  }

  return alertSessionExpired();
};

/**
 * Handle 403 Forbidden errors
 */
const handleForbiddenError = (error: any): never => {
  ToastHelper.error('ไม่ได้รับอนุญาต', 'คุณไม่มีสิทธิ์เข้าถึงข้อมูลนี้');
  router.push({ name: 'forbidden' });
  throw error;
};

axiosInstance.interceptors.request.use(
  (requestConfig: InternalAxiosRequestConfig): InternalAxiosRequestConfig<any> => {
    const loadingStore = useLoadingStore();

    // determine if this request should skip global loading
    const shouldSkip = !!requestConfig.headers.isDisabledLoad || shouldIgnoreLoading(requestConfig.url);

    // persist flag on config (not sent over the wire)
    (requestConfig as any).__skipLoading = shouldSkip;

    if (!shouldSkip) {
      loadingStore.setIsLoading(true);
    }

    // do not send custom header to backend
    delete requestConfig.headers['isDisabledLoad'];

    const accessToken = cookie.get('accessToken');

    if (accessToken != null) {
      requestConfig.headers.Authorization = `Bearer ${accessToken}`;
    }

    return requestConfig;
  },
  async (error) => {
    const loadingStore = useLoadingStore();
    loadingStore.setIsLoading(false);

    throw error;
  }
);

axiosInstance.interceptors.response.use(
  (resp) => {
    const loadingStore = useLoadingStore();

    if (!(resp.config as any).__skipLoading) {
      loadingStore.setIsLoading(false);
    }

    return resp;
  },
  async (error) => {
    const loadingStore = useLoadingStore();

    if (!(error?.config as any)?.__skipLoading) {
      loadingStore.setIsLoading(false);
    }

    // Handle network errors that might be 403 responses with protocol errors
    if (error?.code === 'ERR_NETWORK' &&
      (error?.request?.status === 403 ||
        error?.request?.responseText?.includes('403') ||
        error?.request?.responseText?.includes('Forbidden'))) {
      return handleForbiddenError(error);
    }

    if (error?.response?.status === HttpStatusCode.InternalServerError &&
        !shouldIgnoreServerError(error?.config?.url)) {
      ToastHelper.error('เกิดข้อผิดพลาด', 'เกิดข้อผิดพลาดเซิฟเวอร์');
    }

    if (error?.response?.status === HttpStatusCode.RequestTimeout) {
      ToastHelper.error("Request Timeout", "การเชื่อมต่อล้มเหลวเนื่องจากใช้เวลานานเกินไป กรุณาลองใหม่อีกครั้ง");
    }

    if (error?.response?.status === HttpStatusCode.NotFound) {
      ToastHelper.error('ไม่พบข้อมูล', errorMessageHandler(error?.response.data));
    }

    if (error?.response?.status === HttpStatusCode.BadRequest) {
      ToastHelper.errorDescription(errorMessageHandler(error?.response.data));
    }

    if (error?.response?.status === HttpStatusCode.Conflict) {
      const conflictMessage = errorMessageHandler(error?.response?.data);
      ToastHelper.warning('ข้อมูลขัดแย้ง', conflictMessage || 'ข้อมูลถูกแก้ไขโดยผู้อื่น กรุณาโหลดหน้าใหม่อีกครั้ง');
    }

    if (error?.response?.status === HttpStatusCode.TooManyRequests) {
      const url = error?.config?.url ?? '';
      const message = url.includes('/signin')
        ? 'ลองเข้าสู่ระบบบ่อยเกินไป'
        : 'คำขอมากเกินไป';
      ToastHelper.error(message, 'กรุณารอสักครู่แล้วลองใหม่อีกครั้ง');
      throw error;
    }

    if (error?.response?.status === HttpStatusCode.Forbidden || error?.response?.status === 403) {
      return handleForbiddenError(error);
    }

    if (error?.response?.status === HttpStatusCode.Unauthorized) {
      return handleUnauthorizedError(error, axiosInstance);
    }

    return error.response;
  }
);

// Request interceptor สำหรับ http - เพิ่ม Authorization header
http.interceptors.request.use(
  (requestConfig: InternalAxiosRequestConfig): InternalAxiosRequestConfig<any> => {
    const accessToken = cookie.get('accessToken');

    if (accessToken != null) {
      requestConfig.headers.Authorization = `Bearer ${accessToken}`;
    }

    return requestConfig;
  }
);

http.interceptors.response.use(
  (resp) => resp,
  async (error) => {
    // Handle network errors that might be 403 responses with protocol errors
    if (error?.code === 'ERR_NETWORK' && error?.request?.status === 403) {
      return handleForbiddenError(error);
    }

    if (error?.response?.status === HttpStatusCode.Forbidden || error?.response?.status === 403) {
      return handleForbiddenError(error);
    }

    // Handle 401 Unauthorized - refresh token
    if (error?.response?.status === HttpStatusCode.Unauthorized) {
      return handleUnauthorizedError(error, http);
    }

    return error.response;
  });

const clearCookie = () => {
  cookie.remove("accessToken");
  cookie.remove("refreshToken");
};

const alertSessionExpired = () => {
  ToastHelper.error("Session หมดอายุ", "โปรดเข้าสู่ระบบใหม่อีกครั้ง");
  router.replace({ name: "login" });

  return clearCookie();
};

export default axiosInstance;
