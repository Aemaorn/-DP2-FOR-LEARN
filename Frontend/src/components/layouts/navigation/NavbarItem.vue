<script setup lang="ts">
import Menu from 'primevue/menu';
import Button from 'primevue/button';
import type { MenuItem } from 'primevue/menuitem';
import { ref } from 'vue';
import { useRouter } from 'vue-router';

type Prop = {
  name: string;
  active?: boolean;
  subMenu?: MenuItem[];
  path?: string;
  isShow?: boolean;
};

const props = defineProps<Prop>();

const router = useRouter();

const menu = ref();

const hoverDebounce = ref<number | null>(null);
const isMenuVisible = ref(false);

const toggleMenu = (e: Event) => {
  if (menu.value) {
    isMenuVisible.value = !isMenuVisible.value;
    if (isMenuVisible.value) {
      menu.value?.show(e);
    } else {
      menu.value?.hide(e);
    }
  }
};

const onClickMenu = (e: Event, path?: string) => {
  if (path) {
    return router.push(path);
  }

  toggleMenu(e);
};

const startCloseTimer = () => {
  hoverDebounce.value = window.setTimeout(() => {
    if (!isMenuVisible.value) return;
    menu.value?.hide();
    isMenuVisible.value = false;
  }, 100);
};

const cancelCloseMenuTimer = () => {
  if (hoverDebounce.value != null) {
    clearTimeout(hoverDebounce.value);
    hoverDebounce.value = null;
  }
};
</script>

<template>
  <div class="h-full py-2" @mouseleave="() => startCloseTimer()">
    <Button class="px-1.5! gap-1" @click="(e: Event) => onClickMenu(e, props.path)" v-if="props.isShow">
      <p class="font-normal text-nowrap xl:max-w-[80px] 2xl:max-w-none overflow-hidden text-ellipsis">
        {{ props.name }}
      </p>
      <span class="material-symbols-rounded duration-200" :class="isMenuVisible ? 'rotate-180' : ''"
        v-if="props.subMenu?.length">
        keyboard_arrow_down
      </span>
    </Button>
    <Menu @mouseenter="() => cancelCloseMenuTimer()" @mouseleave="() => startCloseTimer()" ref="menu"
      :model="props.subMenu" v-if="props.subMenu?.length" popup class="bg-secondary! shadow-none! !z-[1006]">
      <template #item="{ item }">
        <Button
          class="text-primary-800! bg-transparent! border-0! hover:bg-primary-500! hover:text-white! w-full font-thin! min-w-[324px]"
          @click="
            () => {
              isMenuVisible = false;
              router.push(item?.path);
            }
          ">
          <div class="text-start w-full">
            {{ item.label }}
          </div>
        </Button>
      </template>
    </Menu>
  </div>
</template>
