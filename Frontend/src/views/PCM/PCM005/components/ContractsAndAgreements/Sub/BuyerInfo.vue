<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { InputField, InputArea } from '@/components/forms';
import { usePcmContractDraftStore } from '@/stores/PCM/PCM005/pcmContractDraft';

const store = usePcmContractDraftStore();
</script>

<template>
  <Card>
    <template #content>
      <TitleHeader label="ข้อมูลผู้ซื้อ" />
      <div class="grid lg:grid-cols-3">
        <InputField label="ชื่อ-ที่อยู่" v-model="store.body.detail.buyer.name" class="mt-5" />
      </div>
      <InputArea label="สัญญาฉบับนี้ทำขึ้น ณ" v-model="store.body.detail.buyer.address" />
    </template>
  </Card>
</template>