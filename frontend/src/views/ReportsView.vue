<template>
  <div>
    <div class="flex items-center justify-between mb-6">
      <div>
        <h2 class="text-lg font-semibold text-white">گزارش‌های مدیریتی</h2>
        <p class="text-sm text-gray-500">تولید و دانلود گزارش‌های امنیتی</p>
      </div>
      <button @click="showCreate = true" class="btn-primary flex items-center gap-2">📄 گزارش جدید</button>
    </div>

    <!-- Report types info -->
    <div class="grid grid-cols-2 md:grid-cols-4 gap-3 mb-6">
      <div v-for="t in reportTypes" :key="t.type" class="cyber-card cursor-pointer hover:border-blue-700 transition-colors"
        @click="openCreate(t.type)">
        <div class="text-2xl mb-2">{{ t.icon }}</div>
        <div class="text-sm font-medium text-white">{{ t.label }}</div>
        <div class="text-xs text-gray-500 mt-1">{{ t.desc }}</div>
      </div>
    </div>

    <div class="cyber-card">
      <div v-if="loading" class="flex justify-center py-8">
        <div class="animate-spin w-8 h-8 border-2 border-blue-500 border-t-transparent rounded-full"></div>
      </div>

      <table v-else class="cyber-table">
        <thead>
          <tr>
            <th>عنوان</th>
            <th>نوع گزارش</th>
            <th>فرمت</th>
            <th>وضعیت</th>
            <th>ایجادکننده</th>
            <th>حجم</th>
            <th>تاریخ</th>
            <th>دانلود</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="!reports.length">
            <td colspan="8" class="text-center text-gray-500 py-8">گزارشی یافت نشد</td>
          </tr>
          <tr v-for="report in reports" :key="report.id">
            <td class="font-medium text-white">{{ report.title }}</td>
            <td class="text-gray-400">{{ reportTypeFa(report.reportType) }}</td>
            <td class="uppercase text-xs font-mono text-blue-300">{{ report.format }}</td>
            <td>
              <span :class="statusClass(report.status)" class="text-xs px-2 py-0.5 rounded-full">
                {{ statusFa(report.status) }}
              </span>
            </td>
            <td class="text-xs text-gray-400">{{ report.createdByName || '-' }}</td>
            <td class="text-xs text-gray-500">{{ formatSize(report.fileSize) }}</td>
            <td class="text-xs text-gray-500">{{ formatDate(report.createdAt) }}</td>
            <td>
              <button v-if="report.status === 'completed'"
                @click="downloadReport(report)"
                class="text-xs text-blue-400 hover:text-blue-300 px-2 py-1 rounded border border-blue-800 hover:border-blue-600">
                ⬇ دانلود
              </button>
              <span v-else-if="report.status === 'generating'" class="text-xs text-yellow-400">
                <span class="animate-spin inline-block w-3 h-3 border border-yellow-400 border-t-transparent rounded-full mr-1"></span>
                در حال تولید
              </span>
            </td>
          </tr>
        </tbody>
      </table>

      <div class="flex justify-between mt-4 pt-4 border-t border-gray-800">
        <span class="text-sm text-gray-500">{{ total }} گزارش</span>
        <div class="flex gap-2">
          <button @click="prevPage" :disabled="page===1" class="btn-secondary text-xs px-3 py-1">قبلی</button>
          <span class="text-sm text-gray-400 px-2">{{ page }}</span>
          <button @click="nextPage" :disabled="page>=totalPages" class="btn-secondary text-xs px-3 py-1">بعدی</button>
        </div>
      </div>
    </div>

    <!-- Create Modal -->
    <div v-if="showCreate" class="fixed inset-0 bg-black/70 flex items-center justify-center z-50 p-4">
      <div class="cyber-card w-full max-w-lg">
        <h3 class="text-lg font-semibold text-white mb-4">گزارش جدید</h3>
        <form @submit.prevent="createReport" class="space-y-3">
          <div>
            <label class="text-xs text-gray-400">عنوان گزارش *</label>
            <input v-model="newReport.title" class="cyber-input" required placeholder="گزارش امنیتی ماهانه" />
          </div>
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="text-xs text-gray-400">نوع گزارش</label>
              <select v-model="newReport.reportType" class="cyber-input">
                <option value="summary">خلاصه مدیریتی</option>
                <option value="inventory">موجودی دارایی‌ها</option>
                <option value="vulnerability">آسیب‌پذیری‌ها</option>
                <option value="risk">تحلیل ریسک</option>
                <option value="compliance">انطباق</option>
              </select>
            </div>
            <div>
              <label class="text-xs text-gray-400">فرمت خروجی</label>
              <select v-model="newReport.format" class="cyber-input">
                <option value="xlsx">Excel (XLSX)</option>
                <option value="csv">CSV</option>
              </select>
            </div>
          </div>
          <div class="flex gap-3 pt-2">
            <button type="submit" :disabled="creating" class="btn-primary flex-1">
              {{ creating ? 'در حال ایجاد...' : '📄 تولید گزارش' }}
            </button>
            <button type="button" @click="showCreate = false" class="btn-secondary flex-1">انصراف</button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import api from '@/stores/api'

const reports = ref([])
const total = ref(0)
const loading = ref(true)
const page = ref(1)
const pageSize = 20
const showCreate = ref(false)
const creating = ref(false)
const newReport = ref({ title: '', reportType: 'summary', format: 'xlsx', filters: {} })

const totalPages = computed(() => Math.ceil(total.value / pageSize))

const reportTypes = [
  { type: 'summary', icon: '📊', label: 'خلاصه مدیریتی', desc: 'گزارش کلی وضعیت امنیتی' },
  { type: 'inventory', icon: '🖥️', label: 'موجودی دارایی‌ها', desc: 'فهرست کامل دارایی‌ها' },
  { type: 'vulnerability', icon: '🛡️', label: 'آسیب‌پذیری‌ها', desc: 'گزارش CVE و CVSS' },
  { type: 'risk', icon: '⚠️', label: 'تحلیل ریسک', desc: 'امتیازبندی ریسک دارایی‌ها' },
]

function openCreate(type) {
  newReport.value.reportType = type
  showCreate.value = true
}

async function loadReports() {
  loading.value = true
  try {
    const res = await api.get('/reports', { params: { page: page.value, pageSize } })
    reports.value = res.data.data.items
    total.value = res.data.data.totalCount
  } finally {
    loading.value = false
  }
}

async function createReport() {
  creating.value = true
  try {
    await api.post('/reports', newReport.value)
    showCreate.value = false
    newReport.value = { title: '', reportType: 'summary', format: 'xlsx', filters: {} }
    await loadReports()
  } catch (e) {
    alert('خطا در ایجاد گزارش')
  } finally {
    creating.value = false
  }
}

async function downloadReport(report) {
  const res = await api.get(`/reports/${report.id}/download`, { responseType: 'blob' })
  const url = window.URL.createObjectURL(new Blob([res.data]))
  const a = document.createElement('a')
  a.href = url
  a.download = `report_${report.id}.${report.format}`
  a.click()
}

function prevPage() { if (page.value > 1) { page.value--; loadReports() } }
function nextPage() { if (page.value < totalPages.value) { page.value++; loadReports() } }

function reportTypeFa(t) { return { summary: 'خلاصه مدیریتی', inventory: 'موجودی', vulnerability: 'آسیب‌پذیری', risk: 'ریسک', compliance: 'انطباق' }[t] || t }
function statusFa(s) { return { pending: 'معلق', generating: 'در حال تولید', completed: 'آماده', failed: 'خطا' }[s] || s }
function statusClass(s) { return { pending: 'bg-gray-800 text-gray-400', generating: 'bg-yellow-900/50 text-yellow-400', completed: 'bg-green-900/50 text-green-400', failed: 'bg-red-900/50 text-red-400' }[s] || '' }
function formatDate(d) { return d ? new Date(d).toLocaleDateString('fa-IR') : '-' }
function formatSize(b) { if (!b) return '-'; return b > 1048576 ? `${(b / 1048576).toFixed(1)} MB` : `${(b / 1024).toFixed(0)} KB` }

onMounted(loadReports)
</script>
