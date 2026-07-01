import './assets/styles/font.css';
import 'material-symbols';
import './assets/styles/main.css';

import { createApp } from 'vue';
import { createPinia } from 'pinia';
import piniaPluginPersistedstate from 'pinia-plugin-persistedstate'
import PrimeVue from 'primevue/config';
import { GHBPreset, GHBThemeOption } from './configs/primeVue';
import { ToastService, KeyFilter } from 'primevue';
import Vue3Marquee from 'vue3-marquee';

import '@/configs/globalValidate';

import App from './App.vue';
import router from './router';

const app = createApp(App);

const pinia = createPinia();
pinia.use(piniaPluginPersistedstate);

app
  .use(pinia)
  .use(router)
  .use(PrimeVue, {
    locale: GHBThemeOption.locale,
    theme: {
      preset: GHBPreset,
      options: GHBThemeOption,
    },
    pt: {
      button: {
        root: {
          class: 'rounded-lg py-1.5 font-semibold text-xl',
        },
      },
      card: {
        root: {
          class: 'rounded-sm',
        },
      },
    },
  })
  .use(ToastService)
  .directive('keyfilter', KeyFilter)
  .use(Vue3Marquee);

app.mount('#app');
