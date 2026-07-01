<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { useRoute } from 'vue-router';
import { tailwindBreakpoint } from '@/helpers/breakpoint';
import { Logo } from '@/components/icons/index';
import { Vue3Marquee } from 'vue3-marquee';
import { useAuthenticationStore } from '@/stores/authentication';
import NavbarItem from '@/components/layouts/navigation/NavbarItem.vue';
import Notification from '@/components/layouts/navigation/Notification.vue';
import Divider from 'primevue/divider';
import Drawer from 'primevue/drawer';
import Button from 'primevue/button';
import router from '@/router';
import { useMenuStore } from '@/stores/menu';
import { PermissionStrType } from '@/enums/role';
import { UAParser } from 'ua-parser-js';
import type { Menu } from '@/models/menu';

const { browser } = UAParser(navigator.userAgent);

const authenticationStore = useAuthenticationStore();
const menuStore = useMenuStore();

const { xlAndUp, xxlAndUp } = tailwindBreakpoint();
const isDrawer = ref<boolean>(false);
const route = useRoute();
const ip = ref('');
const time = ref('');
const menuScrollContainer = ref<HTMLElement>();
const scrollLeft = ref(0);
const containerWidth = ref(0);
const scrollWidth = ref(0);

let intervalId: ReturnType<typeof setInterval>;

const scrollButtonsState = computed(() => {
  const hasMenus = menuStore.menus.length > 0;
  const needsScroll = scrollWidth.value > containerWidth.value;

  return {
    needsScrollButtons: hasMenus && needsScroll,
    canScrollLeft: scrollLeft.value > 0,
    canScrollRight: scrollLeft.value < scrollWidth.value - containerWidth.value
  };
});

const needsScrollButtons = computed(() => scrollButtonsState.value.needsScrollButtons);
const canScrollLeft = computed(() => scrollButtonsState.value.canScrollLeft);
const canScrollRight = computed(() => scrollButtonsState.value.canScrollRight);

const updateScrollValues = () => {
  if (menuScrollContainer.value) {
    const el = menuScrollContainer.value;
    scrollLeft.value = el.scrollLeft;
    containerWidth.value = el.clientWidth;
    scrollWidth.value = el.scrollWidth;
  }
};

const scrollLeftAction = () => {
  if (menuScrollContainer.value) {
    menuScrollContainer.value.scrollBy({ left: -200, behavior: 'smooth' });
    setTimeout(updateScrollValues, 300);
  }
};

const scrollRightAction = () => {
  if (menuScrollContainer.value) {
    menuScrollContainer.value.scrollBy({ left: 200, behavior: 'smooth' });
    setTimeout(updateScrollValues, 300);
  }
};


onMounted(() => {
  timesFn();

  intervalId = setInterval(timesFn, 1000);

  fetch('https://api.ipify.org?format=json')
    .then(res => res.json())
    .then(data => {
      ip.value = data.ip;
    });

  nextTick(() => {
    updateScrollValues();
    if (menuScrollContainer.value) {
      menuScrollContainer.value.addEventListener('scroll', updateScrollValues);
    }
    window.addEventListener('resize', updateScrollValues);
  });
});

onBeforeUnmount(() => {
  if (menuScrollContainer.value) {
    menuScrollContainer.value.removeEventListener('scroll', updateScrollValues);
  }
  window.removeEventListener('resize', updateScrollValues);

  clearInterval(intervalId);
});

const timesFn = () => {
  time.value = Intl.DateTimeFormat('th', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
    second: 'numeric',
    hour12: false,
  }).format(new Date());
};

const checkHasPermissionMenu = (menu: Menu) => {
  if (menu.children && menu.children?.length > 0 && menu.children.some(a => a.permission === PermissionStrType.View || a.permission === PermissionStrType.Manage)) {
    return true;
  }

  return menu.permission !== PermissionStrType.None;
};

watch(
  () => xlAndUp.value,
  (val) => {
    if (val) {
      isDrawer.value = false;
    }
  }
);

watch(isDrawer, (val) => {
  if (val) {
    const keys: Record<string, boolean> = {};
    for (const menu of drawerMenus.value) {
      if (menu.items?.some(child => child.path === route.path)) {
        keys[menu.key] = true;
      }
    }
    drawerExpandedKeys.value = keys;
  }
});

watch(() => menuStore.menus, () => {
  nextTick(updateScrollValues);
}, { deep: true });

const navigateDrawer = (path: string) => {
  router.push(path);
  isDrawer.value = false;
};

const drawerMenus = computed(() =>
  menuStore.menus
    .filter(menu => checkHasPermissionMenu(menu))
    .map(menu => ({
      ...menu,
      key: menu.id,
      isRoot: true,
      ...(menu.path && !menu.children?.length ? { command: () => navigateDrawer(menu.path) } : {}),
      items: menu.children
        ?.filter(f => f.permission !== PermissionStrType.None)
        .map(child => ({ ...child, key: child.id, command: () => navigateDrawer(child.path) }))
    }))
);

const drawerExpandedKeys = ref<Record<string, boolean>>({});

const onOpenManuals = (): void => {
  const route = router.resolve({ name: 'manuals' });
  window.open(route.href, '_blank');
};
</script>

<template>
  <nav>
    <div class="fixed top-0 w-full shadow-md bg-primary-500 z-[1005]">
      <div class="w-full relative">
        <div class="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 lg:hidden">
          <Logo color="white" :height="36" />
        </div>

        <div class="flex justify-between items-center gap-2 lg:gap-4 px-4 lg:px-8">
          <!-- left nav -->
          <div class="flex gap-2 lg:gap-4 items-center flex-1 min-w-0">
            <Button class="grid place-items-center lg:hidden border-none aspect-square" @click="isDrawer = true">
              <i class="pi pi-bars text-xl" />
            </Button>

            <div class="my-3 invisible w-0 lg:w-auto lg:visible cursor-pointer" @click="() => router.push('/wl')">
              <Logo color="white" :height="36" />
            </div>

            <div class="hidden lg:flex items-center flex-1 min-w-0">

              <Button v-show="needsScrollButtons && canScrollLeft" @click="scrollLeftAction"
                class="flex-shrink-0 p-1 border-none bg-transparent hover:bg-white/10">
                <i class="pi pi-chevron-left text-white text-sm" />
              </Button>

              <div ref="menuScrollContainer" class="flex gap-1 flex-1 min-w-0 overflow-x-hidden scroll-smooth"
                @scroll="updateScrollValues">
                <NavbarItem v-for="(menu, i) in menuStore.menus" :key="i" :name="menu.label" :path="menu.path"
                  :sub-menu="menu.children?.filter(f => f.permission !== PermissionStrType.None)" :active="false"
                  :is-show="checkHasPermissionMenu(menu)" />
              </div>

              <Button v-show="needsScrollButtons && canScrollRight" @click="scrollRightAction"
                class="flex-shrink-0 p-1 border-none bg-transparent hover:bg-white/10">
                <i class="pi pi-chevron-right text-white text-sm" />
              </Button>
            </div>
          </div>

          <!-- right nav -->
          <div class="gap-1 lg:gap-2 hidden lg:flex flex-shrink-0 min-w-0">
            <div class="justify-center items-end flex flex-col text-white min-w-0 cursor-pointer"
              @click="router.push({ name: 'search-all' })">
              <div class="flex items-center gap-1 hover:bg-primary-600 px-4 py-2 rounded-lg">
                <span class="material-symbols-outlined text-sm lg:text-base">
                  search
                </span>
                <small class="leading-5 truncate max-w-[120px] lg:max-w-[200px]"> ค้นหาทั้งหมด</small>
              </div>
            </div>
            <div class="py-1.5 hidden xl:block">
              <Divider layout="vertical" class="mx-1! rounded-full" />
            </div>

            <div class="justify-center items-end flex flex-col text-white min-w-0">
              <div class="flex items-center gap-1">
                <span class="material-symbols-rounded text-sm lg:text-base"> account_circle </span>
                <small class="leading-5 truncate max-w-[120px] lg:max-w-[200px]">{{ authenticationStore.profile.name
                }}</small>
              </div>
              <div class="flex justify-end font-thin!">
                <div class="leading-4 max-w-[150px] lg:max-w-[200px] whitespace-nowrap overflow-hidden">
                  <Vue3Marquee class="overflow-y-hidden flex gap-2" :duration="50">
                    <small class="mr-2 text-xs lg:text-sm">{{ time }}</small>
                    <small class="mr-2 text-xs lg:text-sm">IP {{ ip }}</small>
                    <small class="mr-2 text-xs lg:text-sm">{{ browser.name }}</small>
                  </Vue3Marquee>
                </div>
              </div>
            </div>

            <div class="py-1.5 hidden xl:block">
              <Divider layout="vertical" class="mx-1! rounded-full" />
            </div>

            <div class="flex items-center text-white">
              <Button @click="onOpenManuals" class="px-1 lg:px-1.5!">
                <i class="pi pi-book text-sm lg:text-base" />
                <small v-if="xxlAndUp" class="hidden xl:inline">คู่มือ</small>
              </Button>
            </div>

            <div class="py-1.5 hidden xl:block">
              <Divider layout="vertical" class="mx-1! rounded-full" />
            </div>

            <div class="flex items-center">
              <Notification />
            </div>

            <div class="py-1.5 hidden xl:block">
              <Divider layout="vertical" class="mx-1! rounded-full" />
            </div>

            <div class="flex items-center gap-1 lg:gap-2 text-white">
              <Button @click="() => authenticationStore.signOutAsync()" class="px-1 lg:px-1.5!">
                <span class="material-symbols-outlined text-sm lg:text-md!"> logout </span>
                <small v-if="xxlAndUp" class="hidden xl:inline"> ออกจากระบบ</small>
              </Button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <Drawer v-model:visible="isDrawer" class="border-0">
      <template #container>
        <div class="flex flex-col h-full">
          <!-- top drawer -->
          <div class="flex items-center justify-center px-4 py-3 shrink-0 border-b border-gray-200">
            <Logo color="var(--color-primary)" :height="50" />
          </div>

          <div class="px-3 pt-3 shrink-0">
            <a class="flex items-center gap-2 px-3 py-2.5 rounded-lg text-sm cursor-pointer bg-gray-100 hover:bg-gray-200 text-gray-600 transition-colors duration-150"
              @click="() => { router.push({ name: 'search-all' }); isDrawer = false; }">
              <span class="material-symbols-outlined text-base">search</span>
              <span>ค้นหาทั้งหมด</span>
            </a>
          </div>

          <!-- menu -->
          <div class="overflow-y-auto px-3 py-2 h-full ghb-drawer-scroll">
            <div v-for="menu in drawerMenus" :key="menu.id" class="mb-1">
              <!-- top-level item with path (no children, e.g. Worklist) -->
              <a v-if="menu.path && !menu.items?.length" class="ghb-drawer-parent"
                :class="route.path == menu.path ? 'active' : ''" @click="navigateDrawer(menu.path)">
                <p>{{ menu.label }}</p>
              </a>

              <!-- parent item (has children) -->
              <template v-else>
                <a class="ghb-drawer-parent"
                  :class="{ 'active': menu.items?.some((s: any) => s.path == route.path), 'expanded': drawerExpandedKeys[menu.key] }"
                  @click="drawerExpandedKeys[menu.key] = !drawerExpandedKeys[menu.key]">
                  <p>{{ menu.label }}</p>
                  <i class="pi pi-chevron-down text-xs transition-transform duration-200"
                    :class="drawerExpandedKeys[menu.key] ? 'rotate-180' : ''" />
                </a>

                <div v-show="drawerExpandedKeys[menu.key]" class="mt-1">
                  <a v-for="child in menu.items" :key="child.id" class="ghb-drawer-child"
                    :class="route.path == child.path ? 'active' : ''" @click="navigateDrawer(child.path)">
                    {{ child.label }}
                  </a>
                </div>
              </template>
            </div>
          </div>

          <!-- bottom -->
          <div class="mt-auto px-4 py-3 flex flex-col gap-2 border-t border-gray-200">
            <div class="flex items-center justify-between">
              <div class="flex items-center gap-2 min-w-0">
                <span class="material-symbols-rounded text-primary-500 text-2xl"> account_circle </span>
                <div class="flex flex-col min-w-0">
                  <p class="text-sm font-medium truncate">{{ authenticationStore.profile.name }}</p>
                  <p class="text-xs text-gray-400 truncate">{{ authenticationStore.profile.departmentName }}</p>
                </div>
              </div>
              <Button text rounded class="grid place-items-center w-9 h-9 text-red-500 hover:bg-red-50"
                @click="() => authenticationStore.signOutAsync()">
                <span class="material-symbols-outlined text-xl"> logout </span>
              </Button>
            </div>
          </div>
        </div>
      </template>
    </Drawer>
  </nav>
</template>

<style>
.ghb-drawer-scroll {
  scrollbar-width: none;
  scrollbar-color: var(--color-gray-300) transparent;
  scrollbar-arrow-color: transparent;
}

.ghb-drawer-scroll::-webkit-scrollbar-button {
  display: none;
}
</style>

<style lang="css" scoped>
@reference "tailwindcss";

.ghb-drawer-parent {
  @apply flex items-center justify-between cursor-pointer py-2.5 px-3 mb-1 rounded-lg text-sm font-medium transition-colors duration-150 hover:bg-[var(--color-primary-50)];
}

.ghb-drawer-parent.active {
  @apply text-[var(--color-primary-600)];
}

.ghb-drawer-parent.expanded {
  @apply bg-[var(--color-primary-50)];
}

.ghb-drawer-child {
  @apply block cursor-pointer py-2 px-3 ml-3 rounded-lg border-l-2 border-transparent transition-colors duration-150 hover:bg-[var(--color-primary-50)] hover:text-[var(--color-primary-900)];
}

.ghb-drawer-child.active {
  @apply bg-[var(--color-primary-500)] text-white border-[var(--color-primary-500)] hover:bg-[var(--color-primary-600)] hover:text-white;
}
</style>
