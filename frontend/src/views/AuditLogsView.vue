<template>
  <div>
    <div class="mb-6">
      <h2 class="text-lg font-semibold text-white">لاگ‌های حسابرسی</h2>
      <p class="text-sm text-gray-500">ثبت کامل عملیات‌های سیستم</p>
    </div>

    <div class="cyber-card mb-4 grid grid-cols-2 md:grid-cols-3 gap-3">
      <input v-model="username" @input="debouncedLoad" class="cyber-input" placeholder="نام کاربری..." />
      <select v-model="action" @change="loadLogs" class="cyber-input">
        <option value="">همه عملیات‌ها</option>
        <option value="LOGIN">ورود</option>
        <option value="LOGOUT">خروج</option>
        <option value="LOGIN_FAILED">ورود ناموفق</option>
        <option value="CREATE">ایجاد</option>
        <option value="UPDATE">بروزرسانی</option>
        <option value="DELETE">حذف</option>
        <option value="SCAN_START">شروع اسکن</option>
        <option value="EXPORT">خروجی</option>
      </select>
      <button @click="username=''; action=''; loadLogs()" class="btn-secondary">پاک کردن</button>
    </div>

    <div class="cyber-card">
      <div v-if="loading" class="flex justify-center py-8">
        <div class="animate-spin w-8 h-8 border-2 border-blue-500 border-t-transparent rounded-full"></div>
      </div>

      <table v-else class="cyber-table">
        <thead>
          <tr>
            <th>کاربر</th>
            <th>عملیات</th>
            <th>نوع منبع</th>
            <th>توضیحات</th>
            <th>آدرس IP</th>
            <th>کد پاسخ</th>
            <th>زمان</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="!logs.length">
            <td colspan="7" class="text-center text-gray-500 py-8">لاگی یافت نشد</td>
          </tr>
          <tr v-for="log in logs" :key="log.id">
            <td class="text-blue-300 font-medium">{{ log.username || 'سیستم' }}</td>
            <td>
              <span :class="actionClass(log.action)" class="text-xs px-2 py-0.5 rounded font-medium">
                {{ actionFa(log.action) }}
              </span>
            </td>
            <td class="text-xs text-gray-400">{{ log.resourceType || '-' }}</td>
            <td class="max-w-xs text-sm text-gray-300 truncate" :title="log.description">{{ log.description || '-' }}</td>
            <td class="font-mono text-xs text-gray-400">{{ log.ipAddress || '-' }}</td>
            <td>
              <span v-if="log.responseCode" :class="log.responseCode < 400 ? 'text-green-400' : 'text-red-400'" class="text-xs font-mono">
                {{ log.responseCode }}
              </span>
            </td>
            <td class="text-xs text-gray-500">{{ formatDateTime(log.createdAt) }}</td>
          </tr>
        </tbody>
      </table>

      <div class="flex justify-between mt-4 pt-4 border-t border-gray-800">
        <span class="text-sm text-gray-500">{{ total }} لاگ</span>
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

const logs = ref([])
const total = ref(0)
const loading = ref(true)
const page = ref(1)
const pageSize = 20
const username = ref('')
const action = ref('')

const totalPages = computed(() => Math.ceil(total.value / pageSize))

let debounceTimer = null
function debouncedLoad() {
  clearTimeout(debounceTimer)
  debounceTimer = setTimeout(loadLogs, 500)
}

async function loadLogs() {
  loading.value = true
  try {
    const res = await api.get('/audit', { params: { page: page.value, pageSize, username: username.value, action: action.value } })
    logs.value = res.data.data.items
    total.value = res.data.data.totalCount
  } finally {
    loading.value = false
  }
}

function prevPage() { if (page.value > 1) { page.value--; loadLogs() } }
function nextPage() { if (page.value < totalPages.value) { page.value++; loadLogs() } }

function actionFa(a) {
  return {
    LOGIN: 'ورود', LOGOUT: 'خروج', LOGIN_FAILED: 'ورود ناموفق',
    CREATE: 'ایجاد', UPDATE: 'بروزرسانی', DELETE: 'حذف',
    SCAN_START: 'شروع اسکن', SCAN_CREATE: 'ایجاد اسکن', EXPORT: 'خروجی'
  }[a] || a
}

function actionClass(a) {
  if (['LOGIN_FAILED', 'DELETE'].includes(a)) return 'bg-red-900/40 text-red-400'
  if (['LOGIN', 'CREATE'].includes(a)) return 'bg-green-900/40 text-green-400'
  if (['UPDATE', 'SCAN_START'].includes(a)) return 'bg-blue-900/40 text-blue-400'
  return 'bg-gray-800 text-gray-400'
}

function formatDateTime(d) { return d ? new Date(d).toLocaleString('fa-IR') : '-' }

onMounted(loadLogs)
</script>
