<template>
  <div v-if="asset">
    <!-- Breadcrumb -->
    <div class="flex items-center gap-2 text-sm text-gray-500 mb-4">
      <router-link to="/assets" class="hover:text-blue-400">دارایی‌ها</router-link>
      <span>/</span>
      <span class="text-white">{{ asset.name }}</span>
    </div>

    <!-- Header -->
    <div class="cyber-card mb-4 flex flex-wrap items-start justify-between gap-4">
      <div class="flex items-center gap-4">
        <div class="w-12 h-12 rounded-xl bg-blue-900/30 flex items-center justify-center text-2xl">
          {{ typeIcon(asset.assetType) }}
        </div>
        <div>
          <h2 class="text-xl font-bold text-white">{{ asset.name }}</h2>
          <p class="text-gray-400 text-sm">{{ asset.hostname }} · {{ asset.ipAddress }}</p>
        </div>
      </div>
      <div class="flex gap-2">
        <span :class="`badge-${asset.criticality}`">{{ criticalityFa(asset.criticality) }}</span>
        <span class="text-xs px-2 py-0.5 rounded-full" :class="statusClass(asset.status)">{{ statusFa(asset.status) }}</span>
      </div>
    </div>

    <!-- Tabs -->
    <div class="flex gap-2 mb-4 border-b border-gray-800">
      <button v-for="tab in tabs" :key="tab.key"
        @click="activeTab = tab.key"
        :class="['px-4 py-2 text-sm font-medium border-b-2 transition-colors',
          activeTab === tab.key ? 'border-blue-500 text-blue-400' : 'border-transparent text-gray-500 hover:text-gray-300']">
        {{ tab.label }}
      </button>
    </div>

    <!-- Info Tab -->
    <div v-if="activeTab === 'info'" class="grid grid-cols-1 md:grid-cols-2 gap-4">
      <div class="cyber-card">
        <h3 class="text-sm font-semibold text-gray-300 mb-4">اطلاعات پایه</h3>
        <dl class="space-y-3">
          <div v-for="field in baseFields" :key="field.label" class="flex justify-between">
            <dt class="text-sm text-gray-500">{{ field.label }}</dt>
            <dd class="text-sm text-white font-medium">{{ field.value || '-' }}</dd>
          </div>
        </dl>
      </div>
      <div class="cyber-card">
        <h3 class="text-sm font-semibold text-gray-300 mb-4">اطلاعات سیستم</h3>
        <dl class="space-y-3">
          <div v-for="field in sysFields" :key="field.label" class="flex justify-between">
            <dt class="text-sm text-gray-500">{{ field.label }}</dt>
            <dd class="text-sm text-white font-medium">{{ field.value || '-' }}</dd>
          </div>
        </dl>
      </div>
    </div>

    <!-- Ports Tab -->
    <div v-if="activeTab === 'ports'" class="cyber-card">
      <h3 class="text-sm font-semibold text-gray-300 mb-4">پورت‌ها و سرویس‌ها ({{ asset.ports?.length || 0 }})</h3>
      <table class="cyber-table">
        <thead><tr><th>پورت</th><th>پروتکل</th><th>وضعیت</th><th>سرویس</th><th>نسخه</th></tr></thead>
        <tbody>
          <tr v-if="!asset.ports?.length">
            <td colspan="5" class="text-center text-gray-500 py-6">پورت باز یافت نشد</td>
          </tr>
          <tr v-for="port in asset.ports" :key="`${port.port}${port.protocol}`">
            <td class="font-mono font-bold text-blue-300">{{ port.port }}</td>
            <td class="uppercase text-xs">{{ port.protocol }}</td>
            <td><span class="badge-low">{{ port.state }}</span></td>
            <td>{{ port.service || '-' }}</td>
            <td class="text-xs text-gray-400">{{ port.version || '-' }}</td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Vulnerabilities Tab -->
    <div v-if="activeTab === 'vulns'" class="cyber-card">
      <h3 class="text-sm font-semibold text-gray-300 mb-4">آسیب‌پذیری‌ها ({{ asset.vulnerabilities?.length || 0 }})</h3>
      <table class="cyber-table">
        <thead><tr><th>CVE</th><th>عنوان</th><th>شدت</th><th>CVSS</th><th>وضعیت</th><th>اکسپلویت</th><th>تاریخ کشف</th></tr></thead>
        <tbody>
          <tr v-if="!asset.vulnerabilities?.length">
            <td colspan="7" class="text-center text-green-400 py-6">✅ آسیب‌پذیری‌ای یافت نشد</td>
          </tr>
          <tr v-for="vuln in asset.vulnerabilities" :key="vuln.vulnerabilityId">
            <td class="font-mono text-xs text-blue-300">{{ vuln.cveId || '-' }}</td>
            <td class="max-w-xs truncate text-sm">{{ vuln.title }}</td>
            <td><span :class="`badge-${vuln.severity}`">{{ severityFa(vuln.severity) }}</span></td>
            <td class="font-bold" :class="cvssColor(vuln.cvssV3Score)">{{ vuln.cvssV3Score?.toFixed(1) || '-' }}</td>
            <td><span class="text-xs px-2 py-0.5 rounded-full bg-gray-800">{{ statusVulnFa(vuln.status) }}</span></td>
            <td>{{ vuln.exploitAvailable ? '⚠️ بله' : '✓ خیر' }}</td>
            <td class="text-xs text-gray-500">{{ formatDate(vuln.detectedAt) }}</td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Graph Tab -->
    <div v-if="activeTab === 'graph'" class="cyber-card">
      <h3 class="text-sm font-semibold text-gray-300 mb-4">نقشه شبکه دارایی</h3>
      <div ref="graphContainer" class="w-full h-96 bg-gray-900 rounded-lg border border-gray-800"></div>
    </div>
  </div>

  <div v-else-if="loading" class="flex items-center justify-center py-20">
    <div class="animate-spin w-8 h-8 border-2 border-blue-500 border-t-transparent rounded-full"></div>
  </div>

  <div v-else class="text-center py-20 text-gray-500">دارایی یافت نشد</div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import api from '@/stores/api'

const route = useRoute()
const asset = ref(null)
const loading = ref(true)
const activeTab = ref('info')

const tabs = [
  { key: 'info', label: 'اطلاعات' },
  { key: 'ports', label: 'پورت‌ها' },
  { key: 'vulns', label: 'آسیب‌پذیری‌ها' },
  { key: 'graph', label: 'نقشه شبکه' },
]

const baseFields = computed(() => asset.value ? [
  { label: 'نوع', value: assetTypeFa(asset.value.assetType) },
  { label: 'آدرس IP', value: asset.value.ipAddress },
  { label: 'MAC', value: asset.value.macAddress },
  { label: 'دپارتمان', value: asset.value.department },
  { label: 'مالک', value: asset.value.ownerName },
  { label: 'GLPI ID', value: asset.value.glpiId },
  { label: 'اولین مشاهده', value: formatDate(asset.value.firstSeen) },
  { label: 'آخرین مشاهده', value: formatDate(asset.value.lastSeen) },
] : [])

const sysFields = computed(() => asset.value ? [
  { label: 'سیستم‌عامل', value: asset.value.osName },
  { label: 'نسخه OS', value: asset.value.osVersion },
  { label: 'خانواده OS', value: asset.value.osFamily },
  { label: 'سازنده', value: asset.value.manufacturer },
  { label: 'مدل', value: asset.value.model },
  { label: 'شماره سریال', value: asset.value.serialNumber },
  { label: 'نسخه Firmware', value: asset.value.firmwareVersion },
  { label: 'CPE', value: asset.value.cpe },
] : [])

function typeIcon(t) { return { server: '🖥️', workstation: '💻', network: '🌐', iot: '📡', mobile: '📱', security: '🔒' }[t] || '📦' }
function assetTypeFa(t) { return { server: 'سرور', workstation: 'ایستگاه کاری', network: 'شبکه', iot: 'IoT', mobile: 'موبایل' }[t] || t }
function statusFa(s) { return { active: 'فعال', inactive: 'غیرفعال', maintenance: 'تعمیر' }[s] || s }
function statusClass(s) { return { active: 'bg-green-900/50 text-green-400', inactive: 'bg-gray-800 text-gray-400', maintenance: 'bg-yellow-900/50 text-yellow-400' }[s] || '' }
function criticalityFa(c) { return { critical: 'بحرانی', high: 'بالا', medium: 'متوسط', low: 'پایین' }[c] || c }
function severityFa(s) { return { critical: 'بحرانی', high: 'بالا', medium: 'متوسط', low: 'پایین', info: 'اطلاعاتی' }[s] || s }
function statusVulnFa(s) { return { open: 'باز', in_progress: 'در حال انجام', mitigated: 'کاهش یافته', false_positive: 'مثبت کاذب' }[s] || s }
function cvssColor(score) { return !score ? '' : score >= 9 ? 'text-red-400' : score >= 7 ? 'text-orange-400' : score >= 4 ? 'text-yellow-400' : 'text-green-400' }
function formatDate(d) { return d ? new Date(d).toLocaleDateString('fa-IR') : '-' }

onMounted(async () => {
  try {
    const res = await api.get(`/assets/${route.params.id}`)
    asset.value = res.data.data
  } finally {
    loading.value = false
  }
})
</script>
