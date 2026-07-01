<script setup lang="ts">
import { useRouter } from 'vue-router';

const router = useRouter();
</script>

<template>
  <div class="h-dvh text-center">
    <div class="grid grid-cols-3 h-full">
      <div class="flex flex-col justify-center items-center col-span-2 leading-[0.75] px-10 lg:px-5 xl:px-0">
        <p class="text-[5rem] md:text-[10rem] xl:text-[12rem] 2xl:text-[20rem] text-shadow-sm text-primary mb-6">404</p>
        <p class="text-[2.5rem] 2xl:text-[4rem] mb-5 xl:mb-10">ไม่พบหน้าค้นหา</p>
        <p class="text-[1.7rem] 2xl:text-[2rem] leading-[1.25] xl:leading-[0.75]">ขออภัย ไม่พบหน้าที่คุณกำลังค้นหา
          อาจถูกลบ เปลี่ยนชื่อ
          หรือไม่พร้อมให้ใช้งานในขณะนี้</p>
        <div class="mt-10 w-full xl:px-10">
          <Button label="กลับสู่หน้าหลัก" @click="router.replace('/')" class="w-full xl:text-[1.5rem]" />
        </div>
      </div>
      <div class="flex flex-col justify-center items-center bg-primary">
        <img src="/images/icons/ghb.svg" class="w-[50%]" alt="ghb-icon" />
      </div>
    </div>
  </div>
</template>
