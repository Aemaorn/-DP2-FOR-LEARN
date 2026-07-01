<script setup lang="ts">
import { RouterView } from 'vue-router'
import { onMounted } from 'vue';
import CustomToast from './components/toasts/CustomToast.vue';
import ConfirmDialog from './components/Dialog/ConfirmDialog.vue';
import ReasonDialog from './components/Dialog/ReasonDialog.vue';
import UserDialog from './components/Dialog/UserDialog.vue';
import PartnerDialog from './components/Dialog/PartnerDialog.vue';
import WinnerDialog from './components/Dialog/WinnerDialog.vue';
import HistoryDialog from './components/Dialog/HistoryDialog.vue';
import SaveOptionDialog from './helpers/SaveOptionDialog.vue';
import Loading from './components/Loading.vue';
import cookie from './configs/cookie';
import { useAuthenticationStore } from './stores/authentication';
import { useMenuStore } from './stores/menu';

const store = useAuthenticationStore();
const menuStore = useMenuStore();

onMounted(async () => {
  const accessToken = cookie.get('accessToken');

  if (accessToken) {
    await store.getProfileAsync();
    await menuStore.getMenuAsyncAsync();
  }
});
</script>

<template>
  <RouterView />
  <CustomToast />
  <ConfirmDialog />
  <ReasonDialog />
  <UserDialog />
  <PartnerDialog />
  <WinnerDialog />
  <HistoryDialog />
  <SaveOptionDialog />
  <Loading />
</template>
