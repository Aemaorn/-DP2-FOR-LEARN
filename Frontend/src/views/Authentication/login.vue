<script setup lang="ts">
import { Card, Image, Button } from 'primevue';
import { InputPassword, InputField } from '@/components/forms';
import { Form as VeeForm } from 'vee-validate';
import { useAuthenticationStore } from '@/stores/authentication';
import version from '@/_version';
import view_version from '@/_view-version';
import ToastHelper from '@/helpers/toast';
import { onMounted } from 'vue';
import { useRouter } from 'vue-router';
import cookie from '@/configs/cookie';
import logoText from '@/assets/images/logo-text.png';

const store = useAuthenticationStore();
const router = useRouter();

// eslint-disable-next-line @typescript-eslint/no-unused-vars
const onOpenManuals = (): void => {
  router.push({ name: 'manuals' });
};

onMounted(() => {
  console.info("Tag Version: ", version);

  cookie.remove('accessToken');
  cookie.remove('refreshToken');

  store.username = '';
  store.password = '';
});

const onSubmit = async (): Promise<void> => {
  await store.loginAsync();
  await store.loginDp1Async();
};
</script>

<template>
  <div class="fixed inset-0 grid place-items-center overflow-hidden bg-login">
    <Card class="rounded-xl w-[70vw] lg:w-[40vw] 2xl:w-[30vw] py-4">
      <template #content>
        <Image :src="logoText" width="124" class="flex justify-center" />
        <VeeForm @submit="onSubmit" @invalid-submit="() => ToastHelper.invalidMessageToast()">
          <div class="my-4 p-4">
            <div class="grid grid-cols-1 gap-2 gap-y-8">
              <InputField v-model.trim="store.username" label="ชื่อผู้ใช้งาน" rules="required" />
              <InputPassword v-model.trim="store.password" label="รหัสผ่าน" rules="required" />
              <Button label="เข้าสู่ระบบ" fluid type="submit" />
            </div>
          </div>
        </VeeForm>
      </template>
    </Card>

    <div class="absolute bottom-5 right-8 font-bold">
      <p>{{ view_version }}</p>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.bg-login {
  background-image: url('/images/bg-login.png');
  background-size: cover;
  background-position: top;
  background-repeat: round;
}
</style>