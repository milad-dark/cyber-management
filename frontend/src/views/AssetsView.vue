<template>
  <div>
    <!-- Header -->
    <div class="flex flex-wrap items-center justify-between gap-4 mb-6">
      <div>
        <h2 class="text-lg font-semibold text-white">فهرست دارایی‌ها</h2>
        <p class="text-sm text-gray-500">مدیریت و مشاهده دارایی‌های سایبری</p>
      </div>
      <button @click="showCreate = true" class="btn-primary flex items-center gap-2">
        <span>➕</span> دارایی جدید
      </button>
    </div>

    <!-- Filters -->
    <div class="cyber-card mb-4">
      <div class="grid grid-cols-2 md:grid-cols-5 gap-3">
        <input v-model="filter.search" @input="debouncedLoad" class="cyber-input" placeholder="جستجو..." />
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
        <button @click="resetFilter" class="btn-secondary">پاک کردن فیلترها</button>
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
            <th>نام / Hostname</th>
            <th>آدرس IP</th>
            <th>نوع</th>
            <th>سیستم‌عامل</th>
            <th>وضعیت</th>
            <th>اهمیت</th>
            <th>آسیب‌پذیری</th>
            <th>امتیاز ریسک</th>
            <th>آخرین مشاهده</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="!assets.length">
            <td colspan="10" class="text-center text-gray-500 py-8">دارایی‌ای یافت نشد</td>
          </tr>
          <tr v-for="asset in assets" :key="asset.id" class="cursor-pointer" @click="$router.push(`/assets/${asset.id}`)">
            <td>
              <div class="font-medium text-white">{{ asset.name }}</div>
              <div class="text-xs text-gray-500">{{ asset.hostname }}</div>
            </td>
            <td class="font-mono text-blue-300">{{ asset.ipAddress || '-' }}</td>
            <td>{{ typeIcon(asset.assetType) }} {{ assetTypeFa(asset.assetType) }}</td>
            <td class="text-gray-400">{{ asset.osName || '-' }}</td>
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

const filter = ref({ search: '', assetType: '', status: '', criticality: '' })
const newAsset = ref({ name: '', hostname: '', ipAddress: '', assetType: 'server', osName: '', criticality: 'medium', description: '' })

const totalPages = computed(() => Math.ceil(total.value / pageSize))

let debounceTimer = null
function debouncedLoad() {
  clearTimeout(debounceTimer)
  debounceTimer = setTimeout(() => loadAssets(), 500)
}

async function loadAssets() {
  loading.value = true
  try {
    const params = { page: page.value, pageSize, ...filter.value }
    const res = await api.get('/assets', { params })
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
    await loadAssets()
  } catch (e) {
    alert('خطا در ایجاد دارایی')
  } finally {
    creating.value = false
  }
}

async function deleteAsset(asset) {
  if (!confirm(`آیا از حذف "${asset.name}" مطمئن هستید؟`)) return
  await api.delete(`/assets/${asset.id}`)
  await loadAssets()
}

function resetFilter() {
  filter.value = { search: '', assetType: '', status: '', criticality: '' }
  loadAssets()
}

function prevPage() { if (page.value > 1) { page.value--; loadAssets() } }
function nextPage() { if (page.value < totalPages.value) { page.value++; loadAssets() } }

function assetTypeFa(t) { return { server: 'سرور', workstation: 'ایستگاه کاری', network: 'شبکه', iot: 'IoT', mobile: 'موبایل', security: 'امنیتی' }[t] || t }
function typeIcon(t) { return { server: '🖥️', workstation: '💻', network: '🌐', iot: '📡', mobile: '📱', security: '🔒' }[t] || '📦' }
function statusFa(s) { return { active: 'فعال', inactive: 'غیرفعال', maintenance: 'تعمیر', decommissioned: 'بازنشسته' }[s] || s }
function criticalityFa(c) { return { critical: 'بحرانی', high: 'بالا', medium: 'متوسط', low: 'پایین' }[c] || c }
function statusClass(s) { return { active: 'bg-green-900/50 text-green-400', inactive: 'bg-gray-800 text-gray-400', maintenance: 'bg-yellow-900/50 text-yellow-400', decommissioned: 'bg-red-900/50 text-red-400' }[s] || '' }
function riskColor(score) { return score >= 75 ? 'text-red-400' : score >= 50 ? 'text-orange-400' : score >= 25 ? 'text-yellow-400' : 'text-green-400' }
function formatDate(d) { return d ? new Date(d).toLocaleDateString('fa-IR') : '-' }

onMounted(loadAssets)
</script>
