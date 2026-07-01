type RefreshSubscriber = {
  resolve: (token: string) => void;
  reject: (error: any) => void;
};

let isRefreshing = false;
let refreshSubscribers: RefreshSubscriber[] = [];

export const getIsRefreshing = () => isRefreshing;
export const setIsRefreshing = (value: boolean) => { isRefreshing = value; };

export const subscribeTokenRefresh = (
  resolve: (token: string) => void,
  reject: (error: any) => void
) => {
  refreshSubscribers.push({ resolve, reject });
};

export const onTokenRefreshed = (newToken: string) => {
  refreshSubscribers.forEach(({ resolve }) => resolve(newToken));
  refreshSubscribers = [];
};

export const onTokenRefreshFailed = (error: any) => {
  refreshSubscribers.forEach(({ reject }) => reject(error));
  refreshSubscribers = [];
};

export const clearRefreshSubscribers = () => {
  refreshSubscribers = [];
};
