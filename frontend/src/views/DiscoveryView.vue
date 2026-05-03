<template>
  <div>
    <div class="flex items-center justify-between mb-6">
      <div>
        <h2 class="text-lg font-semibold text-white">کشف دارایی‌ها</h2>
        <p class="text-sm text-gray-500">اسکن شبکه و کشف خودکار دارایی‌ها</p>
      </div>
      <button @click="showCreate = true" class="btn-primary flex items-center gap-2">
        <span>🔍</span> اسکن جدید
      </button>
    </div>

    <!-- Jobs list -->
    <div class="cyber-card">
      <div v-if="loading" class="flex justify-center py-8">
        <div class="animate-spin w-8 h-8 border-2 border-blue-500 border-t-transparent rounded-full"></div>
      </div>

      <table v-else class="cyber-table">
        <thead>
          <tr>
            <th>نام</th>
            <th>هدف</th>
            <th>نوع اسکن</th>
            <th>اسکنر</th>
            <th>وضعیت</th>
            <th>دارایی‌های کشف‌شده</th>
            <th>زمان شروع</th>
            <th>عملیات</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="!jobs.length">
            <td colspan="8" class="text-center text-gray-500 py-8">کاری یافت نشد</td>
          </tr>
          <tr v-for="job in jobs" :key="job.id">
            <td class="font-medium text-white">{{ job.name }}</td>
            <td class="font-mono text-xs text-blue-300">{{ job.target }}</td>
            <td>{{ scanTypeFa(job.scanType) }}</td>
            <td class="uppercase text-xs text-gray-400">{{ job.scanner }}</td>
            <td>
              <span class="flex items-center gap-1 text-xs">
                <span :class="statusDot(job.status)"></span>
                {{ statusFa(job.status) }}
              </span>
            </td>
            <td class="font-medium" :class="job.assetsFound > 0 ? 'text-green-400' : 'text-gray-500'">
              {{ job.assetsFound }}
            </td>
            <td class="text-xs text-gray-500">{{ formatDate(job.startedAt || job.createdAt) }}</td>
            <td>
              <button v-if="job.status === 'pending'"
                @click="startJob(job)"
                class="text-xs text-blue-400 hover:text-blue-300 px-2 py-1 rounded border border-blue-800 hover:border-blue-600 transition-colors">
                ▶ شروع
              </button>
              <button v-if="job.status === 'running'"
                @click="cancelJob(job)"
                class="text-xs text-red-400 hover:text-red-300 px-2 py-1 rounded border border-red-800">
                ■ لغو
              </button>
              <span v-if="job.status === 'running'" class="inline-flex items-center gap-1 text-xs text-blue-400 mr-2">
                <span class="animate-spin w-3 h-3 border border-blue-400 border-t-transparent rounded-full"></span>
                در حال اجرا
              </span>
            </td>
          </tr>
        </tbody>
      </table>

      <div class="flex justify-between mt-4 pt-4 border-t border-gray-800">
        <span class="text-sm text-gray-500">{{ total }} کار</span>
        <div class="flex gap-2">
          <button @click="prevPage" :disabled="page===1" class="btn-secondary text-xs px-3 py-1">قبلی</button>
          <span class="text-sm text-gray-400 px-2">{{ page }} / {{ totalPages }}</span>
          <button @click="nextPage" :disabled="page>=totalPages" class="btn-secondary text-xs px-3 py-1">بعدی</button>
        </div>
      </div>
    </div>

    <!-- Create Modal -->
    <div v-if="showCreate" class="fixed inset-0 bg-black/70 flex items-center justify-center z-50 p-4">
      <div class="cyber-card w-full max-w-lg">
        <h3 class="text-lg font-semibold text-white mb-4">اسکن جدید</h3>
        <form @submit.prevent="createAndStart" class="space-y-3">
          <div>
            <label class="text-xs text-gray-400">نام کار *</label>
            <input v-model="newJob.name" class="cyber-input" placeholder="اسکن شبکه داخلی" required />
          </div>
          <div>
            <label class="text-xs text-gray-400">هدف (IP/CIDR) *</label>
            <input v-model="newJob.target" class="cyber-input" placeholder="192.168.1.0/24" required />
          </div>
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="text-xs text-gray-400">نوع اسکن</label>
              <select v-model="newJob.scanType" class="cyber-input">
                <option value="quick">سریع (top 100)</option>
                <option value="full">کامل (همه پورت‌ها)</option>
                <option value="deep">عمیق (OS detection)</option>
                <option value="passive">غیرفعال (ping)</option>
              </select>
            </div>
            <div>
              <label class="text-xs text-gray-400">اسکنر</label>
              <select v-model="newJob.scanner" class="cyber-input">
                <option value="nmap">Nmap</option>
                <option value="snmp">SNMP</option>
                <option value="arp">ARP Scan</option>
                <option value="masscan">Masscan</option>
              </select>
            </div>
          </div>
          <div class="flex gap-3 pt-2">
            <button type="submit" :disabled="creating" class="btn-primary flex-1">
              {{ creating ? 'در حال ایجاد...' : '🔍 ایجاد و شروع اسکن' }}
            </button>
            <button type="button" @click="showCreate = false" class="btn-secondary flex-1">انصراف</button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import api from '@/stores/api'

const jobs = ref([])
const total = ref(0)
const loading = ref(true)
const page = ref(1)
const pageSize = 20
const showCreate = ref(false)
const creating = ref(false)
const newJob = ref({ name: '', target: '', scanType: 'full', scanner: 'nmap' })

const totalPages = computed(() => Math.ceil(total.value / pageSize))

let refreshTimer = null

async function loadJobs() {
  try {
    const res = await api.get('/discovery/jobs', { params: { page: page.value, pageSize } })
    jobs.value = res.data.data.items
    total.value = res.data.data.totalCount
  } finally {
    loading.value = false
  }
}

async function createAndStart() {
  creating.value = true
  try {
    const res = await api.post('/discovery/jobs', newJob.value)
    await api.post(`/discovery/jobs/${res.data.data.id}/start`)
    showCreate.value = false
    newJob.value = { name: '', target: '', scanType: 'full', scanner: 'nmap' }
    await loadJobs()
  } catch (e) {
    alert('خطا در ایجاد کار')
  } finally {
    creating.value = false
  }
}

async function startJob(job) {
  await api.post(`/discovery/jobs/${job.id}/start`)
  await loadJobs()
}

async function cancelJob(job) {
  await api.post(`/discovery/jobs/${job.id}/cancel`)
  await loadJobs()
}

function prevPage() { if (page.value > 1) { page.value--; loadJobs() } }
function nextPage() { if (page.value < totalPages.value) { page.value++; loadJobs() } }

function scanTypeFa(t) { return { quick: 'سریع', full: 'کامل', deep: 'عمیق', passive: 'غیرفعال' }[t] || t }
function statusFa(s) { return { pending: 'منتظر', running: 'در حال اجرا', completed: 'تکمیل', failed: 'خطا', cancelled: 'لغو شده' }[s] || s }
function statusDot(s) {
  return `w-2 h-2 rounded-full inline-block ${{ pending: 'bg-gray-500', running: 'bg-blue-500 animate-pulse', completed: 'bg-green-500', failed: 'bg-red-500', cancelled: 'bg-gray-600' }[s]}`
}
function formatDate(d) { return d ? new Date(d).toLocaleString('fa-IR') : '-' }

onMounted(() => {
  loadJobs()
  refreshTimer = setInterval(loadJobs, 10000)
})
onUnmounted(() => clearInterval(refreshTimer))
</script>
