// uno.css first: its reset must come before main.css, otherwise it overrides the rules
import 'virtual:uno.css'
import './assets/main.css'

import { createApp } from 'vue'
import App from './App.vue'

createApp(App).mount('#app')
