<template>
  <div>
    <div class="flex items-center justify-between mb-6">
      <div>
        <h2 class="text-lg font-semibold text-white">وضعیت SIEM</h2>
        <p class="text-sm text-gray-500">رویدادهای امنیتی و وضعیت ارسال به SIEM</p>
      </div>
      <button @click="forwardPending" class="btn-secondary flex items-center gap-2">📡 ارسال معلق‌ها</button>
    </div>

    <!-- Filters -->
    <div class="cyber-card mb-4 grid grid-cols-2 md:grid-cols-3 gap-3">
      <select v-model="severity" @change="loadEvents" class="cyber-input">
        <option value="">همه شدت‌ها</option>
        <option value="critical">بحرانی</option>
        <option value="high">بالا</option>
        <option value="medium">متوسط</option>
        <option value="low">پایین</option>
        <option value="info">اطلاعاتی</option>
      </select>
      <select v-model="eventType" @change="loadEvents" class="cyber-input">
        <option value="">همه انواع</option>
        <option value="vulnerability_detected">کشف آسیب‌پذیری</option>
        <option value="threat_match">تطابق تهدید</option>
        <option value="scan_completed">اتمام اسکن</option>
        <option value="asset_added">دارایی جدید</option>
      </select>
      <button @click="severity=''; eventType=''; loadEvents()" class="btn-secondary">پاک کردن</button>
    </div>

    <div class="cyber-card">
      <div v-if="loading" class="flex justify-center py-8">
        <div class="animate-spin w-8 h-8 border-2 border-blue-500 border-t-transparent rounded-full"></div>
      </div>

      <table v-else class="cyber-table">
        <thead>
          <tr>
            <th>نوع رویداد</th>
            <th>عنوان</th>
            <th>شدت</th>
            <th>دارایی</th>
            <th>منبع</th>
            <th>ارسال SIEM</th>
            <th>زمان وقوع</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="!events.length">
            <td colspan="7" class="text-center text-gray-500 py-8">رویدادی یافت نشد</td>
          </tr>
          <tr v-for="ev in events" :key="ev.id">
            <td>
              <span class="text-xs px-2 py-0.5 rounded bg-blue-900/20 text-blue-300">{{ eventTypeFa(ev.eventType) }}</span>
            </td>
            <td class="max-w-xs">
              <div class="text-sm text-white truncate" :title="ev.title">{{ ev.title }}</div>
              <div v-if="ev.description" class="text-xs text-gray-500 truncate">{{ ev.description }}</div>
            </td>
            <td><span :class="`badge-${ev.severity}`">{{ severityFa(ev.severity) }}</span></td>
            <td class="text-sm text-gray-400">{{ ev.assetName || '-' }}</td>
            <td class="text-xs text-gray-500">{{ ev.source || '-' }}</td>
            <td>
              <span v-if="ev.forwarded" class="text-green-400 text-xs">✅ ارسال شد</span>
              <span v-else class="text-yellow-400 text-xs">⏳ معلق</span>
            </td>
            <td class="text-xs text-gray-500">{{ formatDateTime(ev.occurredAt) }}</td>
          </tr>
        </tbody>
      </table>

      <div class="flex justify-between mt-4 pt-4 border-t border-gray-800">
        <span class="text-sm text-gray-500">{{ total }} رویداد</span>
        <div class="flex gap-2">
          <button @click="prevPage" :disabled="page===1" class="btn-secondary text-xs px-3 py-1">قبلی</button>
          <span class="text-sm text-gray-400 px-2">{{ page }}</span>
          <button @click="nextPage" :disabled="page>=totalPages" class="btn-secondary text-xs px-3 py-1">بعدی</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import api from '@/stores/api'

const events = ref([])
const total = ref(0)
const loading = ref(true)
const page = ref(1)
const pageSize = 20
const severity = ref('')
const eventType = ref('')

const totalPages = computed(() => Math.ceil(total.value / pageSize))

async function loadEvents() {
  loading.value = true
  try {
    const res = await api.get('/siem/events', { params: { page: page.value, pageSize, severity: severity.value, eventType: eventType.value } })
    events.value = res.data.data.items
    total.value = res.data.data.totalCount
  } finally {
    loading.value = false
  }
}

async function forwardPending() {
  await api.post('/siem/forward')
  await loadEvents()
}

function prevPage() { if (page.value > 1) { page.value--; loadEvents() } }
function nextPage() { if (page.value < totalPages.value) { page.value++; loadEvents() } }

function severityFa(s) { return { critical: 'بحرانی', high: 'بالا', medium: 'متوسط', low: 'پایین', info: 'اطلاعاتی' }[s] || s }
function eventTypeFa(t) {
  return {
    vulnerability_detected: 'کشف آسیب‌پذیری',
    threat_match: 'تطابق تهدید',
    scan_completed: 'اتمام اسکن',
    asset_added: 'دارایی جدید',
    login: 'ورود',
    login_failed: 'ورود ناموفق',
  }[t] || t
}
function formatDateTime(d) { return d ? new Date(d).toLocaleString('fa-IR') : '-' }

onMounted(loadEvents)
</script>
