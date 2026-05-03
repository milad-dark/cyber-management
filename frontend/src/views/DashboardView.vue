<template>
  <div>
    <!-- KPI Cards -->
    <div class="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
      <div v-for="kpi in kpiCards" :key="kpi.label" class="cyber-card">
        <div class="flex items-start justify-between">
          <div>
            <p class="text-xs text-gray-500 mb-1">{{ kpi.label }}</p>
            <p class="text-2xl font-bold" :class="kpi.color">
              {{ loading ? '...' : kpi.value }}
            </p>
          </div>
          <span class="text-2xl">{{ kpi.icon }}</span>
        </div>
        <p class="text-xs text-gray-500 mt-2">{{ kpi.sub }}</p>
      </div>
    </div>

    <!-- Charts row -->
    <div class="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
      <!-- Assets by type -->
      <div class="cyber-card">
        <h3 class="text-sm font-semibold text-gray-300 mb-4">دارایی‌ها بر اساس نوع</h3>
        <Doughnut v-if="assetTypeData.datasets[0].data.length" :data="assetTypeData" :options="doughnutOpts" />
        <p v-else class="text-gray-500 text-sm text-center py-8">داده‌ای موجود نیست</p>
      </div>

      <!-- Vulns by severity -->
      <div class="cyber-card">
        <h3 class="text-sm font-semibold text-gray-300 mb-4">آسیب‌پذیری‌ها بر اساس شدت</h3>
        <Bar v-if="vulnSeverityData.datasets[0].data.length" :data="vulnSeverityData" :options="barOpts" />
        <p v-else class="text-gray-500 text-sm text-center py-8">داده‌ای موجود نیست</p>
      </div>

      <!-- Assets by status -->
      <div class="cyber-card">
        <h3 class="text-sm font-semibold text-gray-300 mb-4">وضعیت دارایی‌ها</h3>
        <Doughnut v-if="assetStatusData.datasets[0].data.length" :data="assetStatusData" :options="doughnutOpts" />
        <p v-else class="text-gray-500 text-sm text-center py-8">داده‌ای موجود نیست</p>
      </div>
    </div>

    <!-- Bottom row -->
    <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
      <!-- Top risk assets -->
      <div class="cyber-card">
        <h3 class="text-sm font-semibold text-gray-300 mb-4">🔥 پرریسک‌ترین دارایی‌ها</h3>
        <div v-if="stats?.topRiskAssets?.length" class="space-y-2">
          <div v-for="asset in stats.topRiskAssets" :key="asset.assetId"
            class="flex items-center justify-between py-2 px-3 rounded-lg bg-gray-900/50 hover:bg-gray-900 transition-colors cursor-pointer"
            @click="$router.push(`/assets/${asset.assetId}`)">
            <div>
              <div class="text-sm font-medium text-white">{{ asset.assetName }}</div>
              <div class="text-xs text-gray-500">{{ asset.ipAddress }}</div>
            </div>
            <div class="text-left">
              <div class="text-lg font-bold" :class="riskColor(asset.riskScore)">
                {{ asset.riskScore.toFixed(0) }}
              </div>
              <div class="text-xs" :class="criticalityClass(asset.criticality)">{{ criticalityFa(asset.criticality) }}</div>
            </div>
          </div>
        </div>
        <p v-else class="text-gray-500 text-sm text-center py-4">داده‌ای موجود نیست</p>
      </div>

      <!-- Recent activities -->
      <div class="cyber-card">
        <h3 class="text-sm font-semibold text-gray-300 mb-4">📋 فعالیت‌های اخیر</h3>
        <div v-if="stats?.recentActivities?.length" class="space-y-2">
          <div v-for="act in stats.recentActivities" :key="act.at" class="flex gap-3 py-2 border-b border-gray-800/50">
            <div class="w-8 h-8 rounded-full bg-blue-900/40 flex items-center justify-center flex-shrink-0">
              <span class="text-xs">{{ actionIcon(act.action) }}</span>
            </div>
            <div class="flex-1 min-w-0">
              <div class="text-sm text-gray-300">{{ act.description || act.action }}</div>
              <div class="text-xs text-gray-500">{{ act.username }} · {{ formatDate(act.at) }}</div>
            </div>
          </div>
        </div>
        <p v-else class="text-gray-500 text-sm text-center py-4">فعالیتی ثبت نشده</p>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { Doughnut, Bar } from 'vue-chartjs'
import { Chart as ChartJS, ArcElement, Tooltip, Legend, BarElement, CategoryScale, LinearScale } from 'chart.js'
import api from '@/stores/api'

ChartJS.register(ArcElement, Tooltip, Legend, BarElement, CategoryScale, LinearScale)

const stats = ref(null)
const loading = ref(true)

const kpiCards = computed(() => [
  { label: 'کل دارایی‌ها', value: stats.value?.totalAssets ?? 0, icon: '🖥️', color: 'text-blue-400', sub: `${stats.value?.activeAssets ?? 0} دارایی فعال` },
  { label: 'آسیب‌پذیری‌ها', value: stats.value?.totalVulnerabilities ?? 0, icon: '🛡️', color: 'text-orange-400', sub: `${stats.value?.criticalVulnerabilities ?? 0} بحرانی` },
  { label: 'تهدیدات فعال', value: stats.value?.activeThreats ?? 0, icon: '🎯', color: 'text-red-400', sub: 'IOC‌های فعال' },
  { label: 'میانگین ریسک', value: stats.value?.averageRiskScore?.toFixed(1) ?? '0', icon: '⚠️', color: 'text-yellow-400', sub: 'امتیاز ریسک' },
])

const typeColors = ['#4c6ef5', '#51cf66', '#ffa94d', '#ff6b6b', '#cc5de8', '#20c997', '#f06595']
const severityColors = { critical: '#ff4757', high: '#ff6b6b', medium: '#ffa94d', low: '#51cf66', info: '#74c0fc' }

const assetTypeData = computed(() => {
  const labels = Object.keys(stats.value?.assetsByType ?? {})
  return {
    labels,
    datasets: [{ data: labels.map(k => stats.value.assetsByType[k]), backgroundColor: typeColors }]
  }
})

const vulnSeverityData = computed(() => {
  const order = ['critical', 'high', 'medium', 'low', 'info']
  const d = stats.value?.vulnsBySeverity ?? {}
  return {
    labels: order.filter(k => d[k]).map(k => severityFa(k)),
    datasets: [{
      label: 'تعداد',
      data: order.filter(k => d[k]).map(k => d[k]),
      backgroundColor: order.filter(k => d[k]).map(k => severityColors[k])
    }]
  }
})

const assetStatusData = computed(() => {
  const d = stats.value?.assetsByStatus ?? {}
  const labels = Object.keys(d)
  return {
    labels: labels.map(l => statusFa(l)),
    datasets: [{ data: labels.map(k => d[k]), backgroundColor: ['#51cf66', '#adb5bd', '#ffa94d', '#ff4757'] }]
  }
})

const doughnutOpts = { responsive: true, plugins: { legend: { position: 'bottom', labels: { color: '#8fa8c9', font: { family: 'Vazirmatn' } } } } }
const barOpts = { responsive: true, plugins: { legend: { display: false } }, scales: { x: { ticks: { color: '#8fa8c9' }, grid: { color: '#1e2d4a' } }, y: { ticks: { color: '#8fa8c9' }, grid: { color: '#1e2d4a' } } } }

function severityFa(s) { return { critical: 'بحرانی', high: 'بالا', medium: 'متوسط', low: 'پایین', info: 'اطلاعاتی' }[s] || s }
function statusFa(s) { return { active: 'فعال', inactive: 'غیرفعال', maintenance: 'تعمیرات', decommissioned: 'بازنشسته' }[s] || s }
function criticalityFa(c) { return { critical: 'بحرانی', high: 'بالا', medium: 'متوسط', low: 'پایین' }[c] || c }
function criticalityClass(c) { return { critical: 'text-red-400', high: 'text-orange-400', medium: 'text-yellow-400', low: 'text-green-400' }[c] || 'text-gray-400' }
function riskColor(score) { return score >= 75 ? 'text-red-400' : score >= 50 ? 'text-orange-400' : score >= 25 ? 'text-yellow-400' : 'text-green-400' }
function actionIcon(a) { return { LOGIN: '🔑', CREATE: '➕', UPDATE: '✏️', DELETE: '🗑️', SCAN_START: '🔍', SCAN_CREATE: '📡', EXPORT: '📤' }[a] || '📝' }
function formatDate(d) { return new Date(d).toLocaleDateString('fa-IR', { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' }) }

onMounted(async () => {
  try {
    const res = await api.get('/dashboard/stats')
    stats.value = res.data.data
  } catch (e) {
    console.error(e)
  } finally {
    loading.value = false
  }
})
</script>
