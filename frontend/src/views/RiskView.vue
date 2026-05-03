<template>
  <div>
    <div class="flex items-center justify-between mb-6">
      <div>
        <h2 class="text-lg font-semibold text-white">تحلیل ریسک</h2>
        <p class="text-sm text-gray-500">امتیازبندی و اولویت‌بندی ریسک دارایی‌ها</p>
      </div>
      <button @click="recalculate" :disabled="recalculating" class="btn-secondary flex items-center gap-2">
        <span :class="recalculating ? 'animate-spin' : ''">🔄</span>
        {{ recalculating ? 'در حال محاسبه...' : 'محاسبه مجدد' }}
      </button>
    </div>

    <!-- Summary cards -->
    <div class="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
      <div v-for="card in summaryCards" :key="card.label" class="cyber-card text-center">
        <div class="text-3xl font-bold mb-1" :class="card.color">{{ card.value }}</div>
        <div class="text-xs text-gray-500">{{ card.label }}</div>
      </div>
    </div>

    <div class="cyber-card">
      <div v-if="loading" class="flex justify-center py-8">
        <div class="animate-spin w-8 h-8 border-2 border-blue-500 border-t-transparent rounded-full"></div>
      </div>

      <table v-else class="cyber-table">
        <thead>
          <tr>
            <th>رتبه</th>
            <th>نام دارایی</th>
            <th>آدرس IP</th>
            <th>امتیاز کلی</th>
            <th>سطح ریسک</th>
            <th>آسیب‌پذیری</th>
            <th>نمایش</th>
            <th>اهمیت</th>
            <th>آخرین محاسبه</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="!risks.length">
            <td colspan="9" class="text-center text-gray-500 py-8">امتیاز ریسکی محاسبه نشده</td>
          </tr>
          <tr v-for="(risk, idx) in risks" :key="risk.assetId">
            <td class="font-bold text-gray-400">{{ idx + 1 }}</td>
            <td>
              <router-link :to="`/assets/${risk.assetId}`" class="text-white hover:text-blue-400 transition-colors font-medium">
                {{ risk.assetName }}
              </router-link>
            </td>
            <td class="font-mono text-xs text-blue-300">{{ risk.ipAddress || '-' }}</td>
            <td>
              <div class="flex items-center gap-2">
                <div class="flex-1 bg-gray-800 rounded-full h-2">
                  <div class="h-2 rounded-full transition-all"
                    :class="riskBarColor(risk.overallScore)"
                    :style="{ width: `${Math.min(risk.overallScore, 100)}%` }"></div>
                </div>
                <span class="font-bold text-sm w-8 text-right" :class="riskTextColor(risk.overallScore)">
                  {{ risk.overallScore.toFixed(0) }}
                </span>
              </div>
            </td>
            <td><span :class="riskLevelClass(risk.riskLevel)">{{ riskLevelFa(risk.riskLevel) }}</span></td>
            <td>{{ risk.vulnerabilityScore.toFixed(0) }}</td>
            <td>{{ risk.exposureScore.toFixed(0) }}</td>
            <td>{{ risk.criticalityScore.toFixed(0) }}</td>
            <td class="text-xs text-gray-500">{{ formatDate(risk.calculatedAt) }}</td>
          </tr>
        </tbody>
      </table>

      <div class="flex justify-between mt-4 pt-4 border-t border-gray-800">
        <span class="text-sm text-gray-500">{{ total }} دارایی</span>
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

const risks = ref([])
const total = ref(0)
const loading = ref(true)
const page = ref(1)
const pageSize = 20
const recalculating = ref(false)

const totalPages = computed(() => Math.ceil(total.value / pageSize))

const summaryCards = computed(() => {
  const critical = risks.value.filter(r => r.overallScore >= 75).length
  const high = risks.value.filter(r => r.overallScore >= 50 && r.overallScore < 75).length
  const medium = risks.value.filter(r => r.overallScore >= 25 && r.overallScore < 50).length
  const low = risks.value.filter(r => r.overallScore < 25).length
  return [
    { label: 'ریسک بحرانی', value: critical, color: 'text-red-400' },
    { label: 'ریسک بالا', value: high, color: 'text-orange-400' },
    { label: 'ریسک متوسط', value: medium, color: 'text-yellow-400' },
    { label: 'ریسک پایین', value: low, color: 'text-green-400' },
  ]
})

async function loadRisks() {
  loading.value = true
  try {
    const res = await api.get('/risk', { params: { page: page.value, pageSize } })
    risks.value = res.data.data.items
    total.value = res.data.data.totalCount
  } finally {
    loading.value = false
  }
}

async function recalculate() {
  recalculating.value = true
  try {
    await api.post('/risk/recalculate')
    setTimeout(loadRisks, 2000)
  } finally {
    recalculating.value = false
  }
}

function prevPage() { if (page.value > 1) { page.value--; loadRisks() } }
function nextPage() { if (page.value < totalPages.value) { page.value++; loadRisks() } }

function riskBarColor(s) { return s >= 75 ? 'bg-red-500' : s >= 50 ? 'bg-orange-500' : s >= 25 ? 'bg-yellow-500' : 'bg-green-500' }
function riskTextColor(s) { return s >= 75 ? 'text-red-400' : s >= 50 ? 'text-orange-400' : s >= 25 ? 'text-yellow-400' : 'text-green-400' }
function riskLevelFa(l) { return { critical: 'بحرانی', high: 'بالا', medium: 'متوسط', low: 'پایین' }[l] || l }
function riskLevelClass(l) {
  return `text-xs px-2 py-0.5 rounded-full ${{ critical: 'bg-red-900/50 text-red-400', high: 'bg-orange-900/50 text-orange-400', medium: 'bg-yellow-900/50 text-yellow-400', low: 'bg-green-900/50 text-green-400' }[l] || ''}`
}
function formatDate(d) { return d ? new Date(d).toLocaleDateString('fa-IR') : '-' }

onMounted(loadRisks)
</script>
