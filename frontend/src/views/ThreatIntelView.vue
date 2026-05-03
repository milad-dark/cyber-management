<template>
  <div>
    <div class="flex items-center justify-between mb-6">
      <div>
        <h2 class="text-lg font-semibold text-white">هوش تهدید (Threat Intelligence)</h2>
        <p class="text-sm text-gray-500">مدیریت IOC‌ها و شاخص‌های سازش</p>
      </div>
      <div class="flex gap-2">
        <button @click="matchToAssets" class="btn-secondary flex items-center gap-1">🔗 تطابق‌یابی</button>
        <button @click="showCreate = true" class="btn-primary flex items-center gap-2">➕ IOC جدید</button>
      </div>
    </div>

    <!-- Filters -->
    <div class="cyber-card mb-4 grid grid-cols-2 md:grid-cols-3 gap-3">
      <input v-model="search" @input="debouncedLoad" class="cyber-input" placeholder="جستجو IOC..." />
      <select v-model="iocType" @change="loadThreats" class="cyber-input">
        <option value="">همه انواع</option>
        <option value="ip">آدرس IP</option>
        <option value="domain">دامنه</option>
        <option value="hash_md5">Hash MD5</option>
        <option value="hash_sha256">Hash SHA256</option>
        <option value="url">URL</option>
      </select>
      <button @click="search=''; iocType=''; loadThreats()" class="btn-secondary">پاک کردن</button>
    </div>

    <div class="cyber-card">
      <div v-if="loading" class="flex justify-center py-8">
        <div class="animate-spin w-8 h-8 border-2 border-blue-500 border-t-transparent rounded-full"></div>
      </div>

      <table v-else class="cyber-table">
        <thead>
          <tr>
            <th>نوع IOC</th>
            <th>مقدار</th>
            <th>نوع تهدید</th>
            <th>منبع</th>
            <th>شدت</th>
            <th>اطمینان</th>
            <th>آخرین مشاهده</th>
            <th>عملیات</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="!threats.length">
            <td colspan="8" class="text-center text-gray-500 py-8">IOC‌ای یافت نشد</td>
          </tr>
          <tr v-for="t in threats" :key="t.id">
            <td>
              <span class="text-xs px-2 py-0.5 rounded bg-blue-900/30 text-blue-300">{{ iocTypeFa(t.iocType) }}</span>
            </td>
            <td class="font-mono text-sm text-white max-w-xs truncate" :title="t.iocValue">{{ t.iocValue }}</td>
            <td class="text-gray-400">{{ t.threatType || '-' }}</td>
            <td class="text-xs text-gray-500">{{ t.source || '-' }}</td>
            <td><span :class="`badge-${t.severity}`">{{ severityFa(t.severity) }}</span></td>
            <td>
              <div class="flex items-center gap-2">
                <div class="flex-1 bg-gray-800 rounded-full h-1.5">
                  <div class="h-1.5 rounded-full bg-blue-500" :style="{ width: `${t.confidence}%` }"></div>
                </div>
                <span class="text-xs text-gray-400 w-8">{{ t.confidence }}%</span>
              </div>
            </td>
            <td class="text-xs text-gray-500">{{ formatDate(t.lastSeen) }}</td>
            <td>
              <button @click="deleteIoc(t)" class="text-red-500 hover:text-red-400 text-xs px-2 py-1 rounded">حذف</button>
            </td>
          </tr>
        </tbody>
      </table>

      <div class="flex justify-between mt-4 pt-4 border-t border-gray-800">
        <span class="text-sm text-gray-500">{{ total }} IOC</span>
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
        <h3 class="text-lg font-semibold text-white mb-4">IOC جدید</h3>
        <form @submit.prevent="createIoc" class="space-y-3">
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="text-xs text-gray-400">نوع IOC *</label>
              <select v-model="newIoc.iocType" class="cyber-input" required>
                <option value="ip">IP</option>
                <option value="domain">Domain</option>
                <option value="hash_md5">Hash MD5</option>
                <option value="hash_sha256">Hash SHA256</option>
                <option value="url">URL</option>
              </select>
            </div>
            <div>
              <label class="text-xs text-gray-400">شدت</label>
              <select v-model="newIoc.severity" class="cyber-input">
                <option value="critical">بحرانی</option>
                <option value="high">بالا</option>
                <option value="medium">متوسط</option>
                <option value="low">پایین</option>
              </select>
            </div>
          </div>
          <div>
            <label class="text-xs text-gray-400">مقدار IOC *</label>
            <input v-model="newIoc.iocValue" class="cyber-input" required placeholder="192.168.1.100 یا domain.com" />
          </div>
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="text-xs text-gray-400">نوع تهدید</label>
              <input v-model="newIoc.threatType" class="cyber-input" placeholder="malware, botnet..." />
            </div>
            <div>
              <label class="text-xs text-gray-400">منبع</label>
              <input v-model="newIoc.source" class="cyber-input" placeholder="VirusTotal, AlienVault..." />
            </div>
          </div>
          <div>
            <label class="text-xs text-gray-400">اطمینان ({{ newIoc.confidence }}%)</label>
            <input v-model.number="newIoc.confidence" type="range" min="0" max="100" class="w-full" />
          </div>
          <div class="flex gap-3 pt-2">
            <button type="submit" :disabled="creating" class="btn-primary flex-1">{{ creating ? 'ذخیره...' : 'ذخیره' }}</button>
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

const threats = ref([])
const total = ref(0)
const loading = ref(true)
const page = ref(1)
const pageSize = 20
const search = ref('')
const iocType = ref('')
const showCreate = ref(false)
const creating = ref(false)
const newIoc = ref({ iocType: 'ip', iocValue: '', threatType: '', source: '', severity: 'medium', confidence: 75 })

const totalPages = computed(() => Math.ceil(total.value / pageSize))

let debounceTimer = null
function debouncedLoad() {
  clearTimeout(debounceTimer)
  debounceTimer = setTimeout(loadThreats, 500)
}

async function loadThreats() {
  loading.value = true
  try {
    const res = await api.get('/threatintel', { params: { page: page.value, pageSize, search: search.value, iocType: iocType.value } })
    threats.value = res.data.data.items
    total.value = res.data.data.totalCount
  } finally {
    loading.value = false
  }
}

async function createIoc() {
  creating.value = true
  try {
    await api.post('/threatintel', newIoc.value)
    showCreate.value = false
    newIoc.value = { iocType: 'ip', iocValue: '', threatType: '', source: '', severity: 'medium', confidence: 75 }
    await loadThreats()
  } catch (e) {
    alert('خطا در ذخیره IOC')
  } finally {
    creating.value = false
  }
}

async function deleteIoc(t) {
  if (!confirm('آیا از حذف این IOC مطمئن هستید؟')) return
  await api.delete(`/threatintel/${t.id}`)
  await loadThreats()
}

async function matchToAssets() {
  await api.post('/threatintel/match')
  alert('تطابق‌یابی با دارایی‌ها انجام شد')
}

function prevPage() { if (page.value > 1) { page.value--; loadThreats() } }
function nextPage() { if (page.value < totalPages.value) { page.value++; loadThreats() } }

function iocTypeFa(t) { return { ip: 'آدرس IP', domain: 'دامنه', hash_md5: 'MD5', hash_sha256: 'SHA256', url: 'URL' }[t] || t }
function severityFa(s) { return { critical: 'بحرانی', high: 'بالا', medium: 'متوسط', low: 'پایین' }[s] || s }
function formatDate(d) { return d ? new Date(d).toLocaleDateString('fa-IR') : '-' }

onMounted(loadThreats)
</script>
