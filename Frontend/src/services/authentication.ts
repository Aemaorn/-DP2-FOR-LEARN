import http from '@/configs/axios';
import type { LoginResponse, UserProfile } from '@/models/authentication';
import type { AxiosResponse } from 'axios';
import axios from 'axios';

const apiDp1Url = import.meta.env.VITE_APP_API_DP1_URL;

const axiosDp1Instance = axios.create({
  baseURL: apiDp1Url,
});

const loginAsync = async (username: string, password: string): Promise<AxiosResponse<LoginResponse>> => {
  const data = new URLSearchParams();
  data.append('username', username);
  data.append('password', password);

  return await http.post('/api/user/signin', data, { headers: { 'Content-Type': 'application/x-www-form-urlencoded' } });
};

const loginDp1Async = async (username: string, password: string) => {
  const formData = new FormData();
  formData.set('Username', username);
  formData.set('Password', password);

  return await axiosDp1Instance.post('/account/signin', formData);
};

const getProfileAsync = async (): Promise<AxiosResponse<UserProfile>> =>
  await http.get('/api/user');

const signOutAsync = async (): Promise<AxiosResponse<void>> =>
  await http.post('/api/user/signout');

const refreshTokenAsync = async (userId: string, refreshToken: string) => {
  const apiUrl = (import.meta.env.VITE_APP_API_URL as string).replace(/\/$/, ''); // ลบ trailing slash
  const body = {
    userId,
    refreshToken
  };

  // ใช้ raw axios instance เพื่อหลีกเลี่ยง infinite loop เมื่อ refresh API return 401
  return await axios.post(`${apiUrl}/api/user/refresh`, body);
}

const refreshTokenDp1Async = async (refreshToken: string) => {
  return await axiosDp1Instance.post('/account/refresh-token', { refreshToken });
};

const setTokenCachAsync = async (userName: string, accessToken: string, refreshToken: string): Promise<AxiosResponse<void>> => {
  const body = {
    userName,
    accessToken,
    refreshToken
  };

  return await http.post('/api/user/token-cache', body);
}


const authenticationService = {
  loginAsync,
  loginDp1Async,
  getProfileAsync,
  signOutAsync,
  refreshTokenAsync,
  refreshTokenDp1Async,
  setTokenCachAsync
};

export default authenticationService