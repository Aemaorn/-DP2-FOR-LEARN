import MenuPermission from '@/enums/menu';
import ToastHelper from '@/helpers/toast';
import type { Menu } from '@/models/menu';
import menuService from '@/services/menu';
import { HttpStatusCode } from 'axios';
import { defineStore } from 'pinia';
import { ref } from 'vue';

export const useMenuStore = defineStore(
  'menu-store',
  () => {
    const menus = ref<Menu[]>([] as Menu[]);
    const hasPermission = ref<boolean>(false);
    const hasManage = ref<boolean>(false);
    const hasView = ref<boolean>(false);

    const getMenuAsyncAsync = async (): Promise<void> => {
      const { data, status } = await menuService.getMenusAsync();

      if (status === HttpStatusCode.Ok) {
        menus.value = data;

        return;
      }

      if (status === HttpStatusCode.Unauthorized) {
        return ToastHelper.warning('เมนู', 'ไม่สามารถโหลดเมนูได้');
      }
    };

    const findMenuByPath = (menuList: Menu[], currentPath: string): Menu | undefined => {
      // Strip trailing /detail or /detail/:id so both edit (with id) and create (without id)
      // navigations resolve to the parent menu's permission.
      const splitCurrentPath = currentPath.replace(/\/detail(\/.*)?$/i, "");

      for (const menu of menuList) {
        if (menu.path.toLowerCase() === splitCurrentPath.toLowerCase()) {
          return menu;
        }

        if (menu.children && menu.children.length > 0) {
          const found = findMenuByPath(menu.children, splitCurrentPath);
          if (found) return found;
        }
      }

      return undefined;
    };

    const getCurrentMenuPermission = (to?: string) => {
      const currentRoute = to ?? window.location.pathname;
      let currentMenu = findMenuByPath(menus.value, currentRoute);

      if (!currentMenu) {
        // Fallback: strip path segments from the right to handle routes nested
        // beyond /module/detail/:id (e.g. /module/segment/:id/detail/:id, /module/contract-selected)
        let fallbackPath = currentRoute.replace(/\/detail(\/.*)?$/i, "");
        while (!currentMenu && fallbackPath.lastIndexOf('/') > 0) {
          fallbackPath = fallbackPath.substring(0, fallbackPath.lastIndexOf('/'));
          currentMenu = findMenuByPath(menus.value, fallbackPath);
        }
      }

      if (currentMenu) {
        hasPermission.value =
          currentMenu.permission == MenuPermission.Manage ||
          currentMenu.permission == MenuPermission.View;

        hasManage.value = currentMenu.permission == MenuPermission.Manage;
        hasView.value = currentMenu.permission == MenuPermission.View;
      } else {
        hasPermission.value = false;
        hasManage.value = false;
        hasView.value = false;
      }
    };

    return { menus, hasPermission, getMenuAsyncAsync, getCurrentMenuPermission, hasManage, hasView };
  },
  {
    persist: {
      key: 'menu-store',
      storage: sessionStorage, // or sessionStorage
    },
  }
);
