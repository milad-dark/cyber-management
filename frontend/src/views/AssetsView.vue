<template>
  <div>
    <!-- Header -->
    <div class="flex flex-wrap items-center justify-between gap-4 mb-6">
      <div>
        <h2 class="text-lg font-semibold text-white">فهرست دارایی‌ها</h2>
        <p class="text-sm text-gray-500">مدیریت و مشاهده دارایی‌های سایبری</p>
      </div>
      <div class="flex gap-2">
        <button @click="toggleAdvancedSearch" class="btn-secondary flex items-center gap-2 text-sm">
          <span>🔍</span> {{ showAdvanced ? 'جستجوی ساده' : 'جستجوی پیشرفته' }}
        </button>
        <button @click="showCreate = true" class="btn-primary flex items-center gap-2">
          <span>➕</span> دارایی جدید
        </button>
      </div>
    </div>

    <!-- Basic Filters -->
    <div v-if="!showAdvanced" class="cyber-card mb-4">
      <div class="grid grid-cols-2 md:grid-cols-5 gap-3">
        <input v-model="filter.search" @input="debouncedLoad" class="cyber-input" placeholder="جستجوی سریع..." />
        <select v-model="filter.assetType" @change="loadAssets" class="cyber-input">
          <option value="">همه انواع</option>
          <option value="server">سرور</option>
          <option value="workstation">ایستگاه کاری</option>
          <option value="network">شبکه</option>
          <option value="iot">IoT</option>
          <option value="mobile">موبایل</option>
        </select>
        <select v-model="filter.status" @change="loadAssets" class="cyber-input">
          <option value="">همه وضعیت‌ها</option>
          <option value="active">فعال</option>
          <option value="inactive">غیرفعال</option>
          <option value="maintenance">تعمیرات</option>
        </select>
        <select v-model="filter.criticality" @change="loadAssets" class="cyber-input">
          <option value="">همه سطوح</option>
          <option value="critical">بحرانی</option>
          <option value="high">بالا</option>
          <option value="medium">متوسط</option>
          <option value="low">پایین</option>
        </select>
        <button @click="resetFilter" class="btn-secondary">پاک کردن</button>
      </div>
    </div>

    <!-- Advanced Search Panel -->
    <div v-else class="cyber-card mb-4">
      <div class="flex items-center justify-between mb-3">
        <h3 class="text-sm font-semibold text-blue-400">🔍 جستجوی پیشرفته</h3>
        <button @click="resetAdvancedSearch" class="text-xs text-gray-400 hover:text-white">پاک کردن همه</button>
      </div>
      <div class="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-3 mb-3">
        <div>
          <label class="text-xs text-gray-400 block mb-1">کلیدواژه (جستجوی کلی)</label>
          <input v-model="advSearch.keyword" @input="debouncedAdvSearch" class="cyber-input" placeholder="نام، IP، hostname، توضیحات..." />
        </div>
        <div>
          <label class="text-xs text-gray-400 block mb-1">Hostname</label>
          <input v-model="advSearch.hostname" @input="debouncedAdvSearch" class="cyber-input" placeholder="server01.local" />
        </div>
        <div>
          <label class="text-xs text-gray-400 block mb-1">آدرس IP</label>
          <input v-model="advSearch.ipAddress" @input="debouncedAdvSearch" class="cyber-input" placeholder="192.168.1.1" />
        </div>
        <div>
          <label class="text-xs text-gray-400 block mb-1">آدرس MAC</label>
          <input v-model="advSearch.macAddress" @input="debouncedAdvSearch" class="cyber-input" placeholder="AA:BB:CC:DD:EE:FF" />
        </div>
        <div>
          <label class="text-xs text-gray-400 block mb-1">نوع دارایی</label>
          <select v-model="advSearch.assetType" @change="runAdvancedSearch" class="cyber-input">
            <option value="">همه انواع</option>
            <option value="server">سرور</option>
            <option value="workstation">ایستگاه کاری</option>
            <option value="network">تجهیزات شبکه</option>
            <option value="iot">IoT</option>
            <option value="mobile">موبایل</option>
          </select>
        </div>
        <div>
          <label class="text-xs text-gray-400 block mb-1">سیستم‌عامل</label>
          <input v-model="advSearch.osName" @input="debouncedAdvSearch" class="cyber-input" placeholder="Linux، Windows..." />
        </div>
        <div>
          <label class="text-xs text-gray-400 block mb-1">مالک / کاربر</label>
          <input v-model="advSearch.owner" @input="debouncedAdvSearch" class="cyber-input" placeholder="نام یا نام کاربری" />
        </div>
        <div>
          <label class="text-xs text-gray-400 block mb-1">وضعیت</label>
          <select v-model="advSearch.status" @change="runAdvancedSearch" class="cyber-input">
            <option value="">همه وضعیت‌ها</option>
            <option value="active">فعال</option>
            <option value="inactive">غیرفعال</option>
            <option value="maintenance">تعمیرات</option>
            <option value="decommissioned">بازنشسته</option>
          </select>
        </div>
        <div>
          <label class="text-xs text-gray-400 block mb-1">اهمیت</label>
          <select v-model="advSearch.criticality" @change="runAdvancedSearch" class="cyber-input">
            <option value="">همه سطوح</option>
            <option value="critical">بحرانی</option>
            <option value="high">بالا</option>
            <option value="medium">متوسط</option>
            <option value="low">پایین</option>
          </select>
        </div>
        <div>
          <label class="text-xs text-gray-400 block mb-1">سطح ریسک</label>
          <select v-model="advSearch.riskLevel" @change="runAdvancedSearch" class="cyber-input">
            <option value="">همه سطوح</option>
            <option value="critical">بحرانی (≥75)</option>
            <option value="high">بالا (50–74)</option>
            <option value="medium">متوسط (25–49)</option>
            <option value="low">پایین (&lt;25)</option>
          </select>
        </div>
        <div>
          <label class="text-xs text-gray-400 block mb-1">کشف‌شده از تاریخ</label>
          <input v-model="advSearch.discoveredFrom" @change="runAdvancedSearch" type="date" class="cyber-input" />
        </div>
        <div>
          <label class="text-xs text-gray-400 block mb-1">کشف‌شده تا تاریخ</label>
          <input v-model="advSearch.discoveredTo" @change="runAdvancedSearch" type="date" class="cyber-input" />
        </div>
        <div>
          <label class="text-xs text-gray-400 block mb-1">CPE</label>
          <input v-model="advSearch.cpe" @input="debouncedAdvSearch" class="cyber-input" placeholder="cpe:/o:..." />
        </div>
        <div>
          <label class="text-xs text-gray-400 block mb-1">نام نرم‌افزار</label>
          <input v-model="advSearch.softwareName" @input="debouncedAdvSearch" class="cyber-input" placeholder="Apache، OpenSSH..." />
        </div>
        <div>
          <label class="text-xs text-gray-400 block mb-1">نسخه نرم‌افزار</label>
          <input v-model="advSearch.softwareVersion" @input="debouncedAdvSearch" class="cyber-input" placeholder="2.4.51" />
        </div>
        <div>
          <label class="text-xs text-gray-400 block mb-1">سازنده نرم‌افزار</label>
          <input v-model="advSearch.softwareVendor" @input="debouncedAdvSearch" class="cyber-input" placeholder="Microsoft، Apache..." />
        </div>
      </div>
      <div class="flex items-center gap-4 pt-2 border-t border-gray-800">
        <label class="flex items-center gap-2 text-sm text-gray-300 cursor-pointer">
          <input type="checkbox" v-model="advSearch.includeGlpi" @change="runAdvancedSearch" class="rounded border-gray-600 bg-gray-800 text-blue-500" />
          <span>شامل نتایج GLPI</span>
        </label>
        <button @click="runAdvancedSearch" class="btn-primary text-sm px-4 py-1.5">اجرای جستجو</button>
        <span v-if="advSearchActive" class="text-xs text-blue-400">{{ total }} نتیجه یافت شد</span>
      </div>
    </div>

    <!-- Table -->
    <div class="cyber-card">
      <div v-if="loading" class="flex items-center justify-center py-12">
        <div class="animate-spin w-8 h-8 border-2 border-blue-500 border-t-transparent rounded-full"></div>
      </div>

      <table v-else class="cyber-table">
        <thead>
          <tr>
            <th @click="sortBy('name')" class="cursor-pointer select-none">
              نام / Hostname <span class="text-gray-500 text-xs">{{ sortIcon('name') }}</span>
            </th>
            <th @click="sortBy('ipAddress')" class="cursor-pointer select-none">
              آدرس IP <span class="text-gray-500 text-xs">{{ sortIcon('ipAddress') }}</span>
            </th>
            <th>نوع</th>
            <th @click="sortBy('osName')" class="cursor-pointer select-none">
              سیستم‌عامل <span class="text-gray-500 text-xs">{{ sortIcon('osName') }}</span>
            </th>
            <th @click="sortBy('status')" class="cursor-pointer select-none">
              وضعیت <span class="text-gray-500 text-xs">{{ sortIcon('status') }}</span>
            </th>
            <th @click="sortBy('criticality')" class="cursor-pointer select-none">
              اهمیت <span class="text-gray-500 text-xs">{{ sortIcon('criticality') }}</span>
            </th>
            <th>آسیب‌پذیری</th>
            <th @click="sortBy('riskScore')" class="cursor-pointer select-none">
              امتیاز ریسک <span class="text-gray-500 text-xs">{{ sortIcon('riskScore') }}</span>
            </th>
            <th @click="sortBy('lastSeen')" class="cursor-pointer select-none">
              آخرین مشاهده <span class="text-gray-500 text-xs">{{ sortIcon('lastSeen') }}</span>
            </th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="!assets.length">
            <td colspan="10" class="text-center text-gray-500 py-8">دارایی‌ای یافت نشد</td>
          </tr>
          <tr v-for="asset in assets" :key="asset.id" class="cursor-pointer" @click="$router.push(`/assets/${asset.id}`)">
            <td>
              <div class="font-medium text-white" v-html="highlight(asset.name)"></div>
              <div class="text-xs text-gray-500" v-html="highlight(asset.hostname || '')"></div>
              <span v-if="asset.source === 'glpi'" class="text-xs bg-purple-900/40 text-purple-300 px-1.5 py-0.5 rounded mr-1">GLPI</span>
            </td>
            <td class="font-mono text-blue-300" v-html="highlight(asset.ipAddress || '-')"></td>
            <td>{{ typeIcon(asset.assetType) }} {{ assetTypeFa(asset.assetType) }}</td>
            <td class="text-gray-400" v-html="highlight(asset.osName || '-')"></td>
            <td>
              <span class="text-xs px-2 py-0.5 rounded-full" :class="statusClass(asset.status)">
                {{ statusFa(asset.status) }}
              </span>
            </td>
            <td>
              <span :class="`badge-${asset.criticality}`">{{ criticalityFa(asset.criticality) }}</span>
            </td>
            <td>
              <span v-if="asset.vulnerabilityCount > 0" class="text-orange-400 font-medium">{{ asset.vulnerabilityCount }}</span>
              <span v-else class="text-green-400">✓</span>
              <span v-if="asset.criticalVulnCount > 0" class="text-red-400 text-xs mr-1">({{ asset.criticalVulnCount }} بحرانی)</span>
            </td>
            <td>
              <span v-if="asset.riskScore" :class="riskColor(asset.riskScore)" class="font-bold">
                {{ asset.riskScore.toFixed(0) }}
              </span>
              <span v-else class="text-gray-500">-</span>
            </td>
            <td class="text-xs text-gray-500">{{ formatDate(asset.lastSeen) }}</td>
            <td @click.stop>
              <button @click="deleteAsset(asset)" class="text-red-500 hover:text-red-400 text-xs px-2 py-1 rounded">حذف</button>
            </td>
          </tr>
        </tbody>
      </table>

      <!-- Pagination -->
      <div class="flex items-center justify-between mt-4 pt-4 border-t border-gray-800">
        <span class="text-sm text-gray-500">{{ total }} دارایی</span>
        <div class="flex gap-2">
          <button @click="prevPage" :disabled="page === 1" class="btn-secondary text-xs px-3 py-1">قبلی</button>
          <span class="text-sm text-gray-400 px-2 py-1">{{ page }} / {{ totalPages }}</span>
          <button @click="nextPage" :disabled="page >= totalPages" class="btn-secondary text-xs px-3 py-1">بعدی</button>
        </div>
      </div>
    </div>

    <!-- Create Modal -->
    <div v-if="showCreate" class="fixed inset-0 bg-black/70 flex items-center justify-center z-50 p-4">
      <div class="cyber-card w-full max-w-lg">
        <h3 class="text-lg font-semibold text-white mb-4">دارایی جدید</h3>
        <form @submit.prevent="createAsset" class="space-y-3">
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="text-xs text-gray-400">نام *</label>
              <input v-model="newAsset.name" class="cyber-input" required />
            </div>
            <div>
              <label class="text-xs text-gray-400">Hostname</label>
              <input v-model="newAsset.hostname" class="cyber-input" />
            </div>
            <div>
              <label class="text-xs text-gray-400">آدرس IP</label>
              <input v-model="newAsset.ipAddress" class="cyber-input" placeholder="192.168.1.1" />
            </div>
            <div>
              <label class="text-xs text-gray-400">نوع دارایی</label>
              <select v-model="newAsset.assetType" class="cyber-input">
                <option value="server">سرور</option>
                <option value="workstation">ایستگاه کاری</option>
                <option value="network">تجهیزات شبکه</option>
                <option value="iot">IoT</option>
                <option value="mobile">موبایل</option>
              </select>
            </div>
            <div>
              <label class="text-xs text-gray-400">سیستم‌عامل</label>
              <input v-model="newAsset.osName" class="cyber-input" placeholder="Linux Ubuntu 22.04" />
            </div>
            <div>
              <label class="text-xs text-gray-400">اهمیت</label>
              <select v-model="newAsset.criticality" class="cyber-input">
                <option value="critical">بحرانی</option>
                <option value="high">بالا</option>
                <option value="medium">متوسط</option>
                <option value="low">پایین</option>
              </select>
            </div>
          </div>
          <div>
            <label class="text-xs text-gray-400">توضیحات</label>
            <textarea v-model="newAsset.description" class="cyber-input h-20 resize-none"></textarea>
          </div>
          <div class="flex gap-3 pt-2">
            <button type="submit" :disabled="creating" class="btn-primary flex-1">
              {{ creating ? 'در حال ذخیره...' : 'ذخیره' }}
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

const assets = ref([])
const total = ref(0)
const loading = ref(true)
const page = ref(1)
const pageSize = 20
const showCreate = ref(false)
const creating = ref(false)
const showAdvanced = ref(false)
const advSearchActive = ref(false)

// Sort state
const currentSort = ref('name')
const currentSortDir = ref('asc')

// Basic filter
const filter = ref({ search: '', assetType: '', status: '', criticality: '' })

// Advanced search
const advSearch = ref({
  keyword: '', hostname: '', ipAddress: '', macAddress: '',
  assetType: '', osName: '', owner: '', status: '', criticality: '',
  riskLevel: '', discoveredFrom: '', discoveredTo: '',
  cpe: '', softwareName: '', softwareVersion: '', softwareVendor: '',
  includeGlpi: false
})

const newAsset = ref({ name: '', hostname: '', ipAddress: '', assetType: 'server', osName: '', criticality: 'medium', description: '' })

const totalPages = computed(() => Math.ceil(total.value / pageSize))

let debounceTimer = null
function debouncedLoad() {
  clearTimeout(debounceTimer)
  debounceTimer = setTimeout(() => loadAssets(), 500)
}

function debouncedAdvSearch() {
  clearTimeout(debounceTimer)
  debounceTimer = setTimeout(() => runAdvancedSearch(), 500)
}

function toggleAdvancedSearch() {
  showAdvanced.value = !showAdvanced.value
  page.value = 1
  if (showAdvanced.value) {
    runAdvancedSearch()
  } else {
    advSearchActive.value = false
    loadAssets()
  }
}

async function loadAssets() {
  loading.value = true
  try {
    const params = {
      page: page.value,
      pageSize,
      sortBy: currentSort.value,
      sortDir: currentSortDir.value,
      ...filter.value
    }
    const res = await api.get('/assets', { params })
    assets.value = res.data.data.items
    total.value = res.data.data.totalCount
  } catch (e) {
    console.error(e)
  } finally {
    loading.value = false
  }
}

async function runAdvancedSearch() {
  loading.value = true
  advSearchActive.value = true
  try {
    const params = {
      page: page.value,
      pageSize,
      sortBy: currentSort.value,
      sortDir: currentSortDir.value,
      ...advSearch.value
    }
    const res = await api.get('/assets/search', { params })
    assets.value = res.data.data.items
    total.value = res.data.data.totalCount
  } catch (e) {
    console.error(e)
  } finally {
    loading.value = false
  }
}

async function createAsset() {
  creating.value = true
  try {
    await api.post('/assets', newAsset.value)
    showCreate.value = false
    newAsset.value = { name: '', hostname: '', ipAddress: '', assetType: 'server', osName: '', criticality: 'medium', description: '' }
    await (showAdvanced.value ? runAdvancedSearch() : loadAssets())
  } catch (e) {
    alert('خطا در ایجاد دارایی')
  } finally {
    creating.value = false
  }
}

async function deleteAsset(asset) {
  if (!confirm(`آیا از حذف "${asset.name}" مطمئن هستید؟`)) return
  await api.delete(`/assets/${asset.id}`)
  await (showAdvanced.value ? runAdvancedSearch() : loadAssets())
}

function resetFilter() {
  filter.value = { search: '', assetType: '', status: '', criticality: '' }
  page.value = 1
  loadAssets()
}

function resetAdvancedSearch() {
  advSearch.value = {
    keyword: '', hostname: '', ipAddress: '', macAddress: '',
    assetType: '', osName: '', owner: '', status: '', criticality: '',
    riskLevel: '', discoveredFrom: '', discoveredTo: '',
    cpe: '', softwareName: '', softwareVersion: '', softwareVendor: '',
    includeGlpi: false
  }
  page.value = 1
  runAdvancedSearch()
}

function sortBy(field) {
  if (currentSort.value === field) {
    currentSortDir.value = currentSortDir.value === 'asc' ? 'desc' : 'asc'
  } else {
    currentSort.value = field
    currentSortDir.value = 'asc'
  }
  page.value = 1
  showAdvanced.value ? runAdvancedSearch() : loadAssets()
}

function sortIcon(field) {
  if (currentSort.value !== field) return '↕'
  return currentSortDir.value === 'asc' ? '↑' : '↓'
}

// Highlight matched keyword in text
function highlight(text) {
  if (!text) return ''
  const kw = showAdvanced.value ? advSearch.value.keyword : filter.value.search
  if (!kw || !kw.trim()) return text
  const escaped = kw.replace(/[.*+?^${}()|[\]\\-]/g, '\\$&')
  return String(text).replace(
    new RegExp(`(${escaped})`, 'gi'),
    '<mark class="bg-yellow-500/30 text-yellow-200 rounded px-0.5">$1</mark>'
  )
}

function prevPage() { if (page.value > 1) { page.value--; showAdvanced.value ? runAdvancedSearch() : loadAssets() } }
function nextPage() { if (page.value < totalPages.value) { page.value++; showAdvanced.value ? runAdvancedSearch() : loadAssets() } }

function assetTypeFa(t) { return { server: 'سرور', workstation: 'ایستگاه کاری', network: 'شبکه', iot: 'IoT', mobile: 'موبایل', security: 'امنیتی' }[t] || t }
function typeIcon(t) { return { server: '🖥️', workstation: '💻', network: '🌐', iot: '📡', mobile: '📱', security: '🔒' }[t] || '📦' }
function statusFa(s) { return { active: 'فعال', inactive: 'غیرفعال', maintenance: 'تعمیر', decommissioned: 'بازنشسته' }[s] || s }
function criticalityFa(c) { return { critical: 'بحرانی', high: 'بالا', medium: 'متوسط', low: 'پایین' }[c] || c }
function statusClass(s) { return { active: 'bg-green-900/50 text-green-400', inactive: 'bg-gray-800 text-gray-400', maintenance: 'bg-yellow-900/50 text-yellow-400', decommissioned: 'bg-red-900/50 text-red-400' }[s] || '' }
function riskColor(score) { return score >= 75 ? 'text-red-400' : score >= 50 ? 'text-orange-400' : score >= 25 ? 'text-yellow-400' : 'text-green-400' }
function formatDate(d) { return d ? new Date(d).toLocaleDateString('fa-IR') : '-' }

onMounted(loadAssets)
</script>
