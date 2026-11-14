// uno.css first: its reset must come before main.css, otherwise it overrides the rules
import 'virtual:uno.css'
import './assets/main.css'

import { createApp } from 'vue'
import App from './App.vue'
import { router } from './router'

createApp(App).use(router).mount('#app')
