import http from '@/configs/axios';
import type { Menu } from '@/models/menu';
import type { AxiosResponse } from 'axios';

const getMenusAsync = async (): Promise<AxiosResponse<Menu[]>> =>
  await http.get('/api/menus', { headers: { isDisabledLoad: true } });

const menuService = {
  getMenusAsync,
};

export default menuService