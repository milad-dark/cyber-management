<template>
  <div>
    <div class="flex items-center justify-between mb-6">
      <div>
        <h2 class="text-lg font-semibold text-white">آسیب‌پذیری‌ها</h2>
        <p class="text-sm text-gray-500">مدیریت و پایش آسیب‌پذیری‌های دارایی‌ها</p>
      </div>
    </div>

    <!-- Filters -->
    <div class="cyber-card mb-4 grid grid-cols-2 md:grid-cols-4 gap-3">
      <input v-model="filter.search" @input="debouncedLoad" class="cyber-input" placeholder="جستجو CVE/عنوان..." />
      <select v-model="filter.severity" @change="loadVulns" class="cyber-input">
        <option value="">همه شدت‌ها</option>
        <option value="critical">بحرانی</option>
        <option value="high">بالا</option>
        <option value="medium">متوسط</option>
        <option value="low">پایین</option>
      </select>
      <select v-model="filter.exploitAvailable" @change="loadVulns" class="cyber-input">
        <option value="">همه</option>
        <option value="true">دارای اکسپلویت</option>
        <option value="false">بدون اکسپلویت</option>
      </select>
      <button @click="resetFilter" class="btn-secondary">پاک کردن</button>
    </div>

    <div class="cyber-card">
      <div v-if="loading" class="flex justify-center py-8">
        <div class="animate-spin w-8 h-8 border-2 border-blue-500 border-t-transparent rounded-full"></div>
      </div>

      <table v-else class="cyber-table">
        <thead>
          <tr>
            <th>CVE ID</th>
            <th>عنوان</th>
            <th>شدت</th>
            <th>امتیاز CVSS</th>
            <th>دارایی‌های آسیب‌دیده</th>
            <th>اکسپلویت</th>
            <th>وصله</th>
            <th>تاریخ انتشار</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="!vulns.length">
            <td colspan="8" class="text-center text-gray-500 py-8">آسیب‌پذیری‌ای یافت نشد</td>
          </tr>
          <tr v-for="vuln in vulns" :key="vuln.id">
            <td>
              <a v-if="vuln.cveId" :href="`https://nvd.nist.gov/vuln/detail/${vuln.cveId}`" target="_blank"
                class="font-mono text-xs text-blue-400 hover:text-blue-300">{{ vuln.cveId }}</a>
              <span v-else class="text-gray-500 text-xs">-</span>
            </td>
            <td class="max-w-xs">
              <div class="text-sm text-white truncate" :title="vuln.title">{{ vuln.title }}</div>
              <div v-if="vuln.description" class="text-xs text-gray-500 truncate">{{ vuln.description }}</div>
            </td>
            <td><span :class="`badge-${vuln.severity}`">{{ severityFa(vuln.severity) }}</span></td>
            <td>
              <span v-if="vuln.cvssV3Score" class="font-bold text-lg" :class="cvssColor(vuln.cvssV3Score)">
                {{ vuln.cvssV3Score.toFixed(1) }}
              </span>
              <span v-else class="text-gray-500">-</span>
            </td>
            <td class="font-medium" :class="vuln.affectedAssetsCount > 0 ? 'text-orange-400' : 'text-gray-500'">
              {{ vuln.affectedAssetsCount }}
            </td>
            <td>
              <span v-if="vuln.exploitAvailable" class="text-red-400 text-sm">⚠️ بله</span>
              <span v-else class="text-gray-500 text-sm">-</span>
            </td>
            <td>
              <span v-if="vuln.patchAvailable" class="text-green-400 text-sm">✅ موجود</span>
              <span v-else class="text-gray-500 text-sm">-</span>
            </td>
            <td class="text-xs text-gray-500">{{ formatDate(vuln.publishedAt) }}</td>
          </tr>
        </tbody>
      </table>

      <div class="flex justify-between mt-4 pt-4 border-t border-gray-800">
        <span class="text-sm text-gray-500">{{ total }} آسیب‌پذیری</span>
        <div class="flex gap-2">
          <button @click="prevPage" :disabled="page===1" class="btn-secondary text-xs px-3 py-1">قبلی</button>
          <span class="text-sm text-gray-400 px-2">{{ page }} / {{ totalPages }}</span>
          <button @click="nextPage" :disabled="page>=totalPages" class="btn-secondary text-xs px-3 py-1">بعدی</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import api from '@/stores/api'

const vulns = ref([])
const total = ref(0)
const loading = ref(true)
const page = ref(1)
const pageSize = 20
const filter = ref({ search: '', severity: '', exploitAvailable: '' })

const totalPages = computed(() => Math.ceil(total.value / pageSize))

let debounceTimer = null
function debouncedLoad() {
  clearTimeout(debounceTimer)
  debounceTimer = setTimeout(loadVulns, 500)
}

async function loadVulns() {
  loading.value = true
  try {
    const params = { page: page.value, pageSize, ...filter.value }
    if (params.exploitAvailable === '') delete params.exploitAvailable
    const res = await api.get('/vulnerabilities', { params })
    vulns.value = res.data.data.items
    total.value = res.data.data.totalCount
  } finally {
    loading.value = false
  }
}

function resetFilter() {
  filter.value = { search: '', severity: '', exploitAvailable: '' }
  loadVulns()
}

function prevPage() { if (page.value > 1) { page.value--; loadVulns() } }
function nextPage() { if (page.value < totalPages.value) { page.value++; loadVulns() } }

function severityFa(s) { return { critical: 'بحرانی', high: 'بالا', medium: 'متوسط', low: 'پایین', info: 'اطلاعاتی' }[s] || s }
function cvssColor(score) { return score >= 9 ? 'text-red-400' : score >= 7 ? 'text-orange-400' : score >= 4 ? 'text-yellow-400' : 'text-green-400' }
function formatDate(d) { return d ? new Date(d).toLocaleDateString('fa-IR') : '-' }

onMounted(loadVulns)
</script>
