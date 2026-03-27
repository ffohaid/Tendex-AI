import { createApp } from 'vue'
import App from './App.vue'

/* Plugins */
import pinia from '@/stores'
import router from '@/router'
import i18n, { DEFAULT_LOCALE, applyLocaleDirection } from '@/plugins/i18n'
import { setupPrimeVue } from '@/plugins/primevue'

/* Styles */
import '@/assets/css/main.css'
import 'primeicons/primeicons.css'

const app = createApp(App)

/* Register plugins */
app.use(pinia)
app.use(router)
app.use(i18n)
setupPrimeVue(app)

/* Apply initial locale direction (RTL for Arabic) */
applyLocaleDirection(DEFAULT_LOCALE)

app.mount('#app')
