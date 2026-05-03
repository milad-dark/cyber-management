<template>
  <div class="flex h-screen overflow-hidden">
    <!-- Sidebar -->
    <aside class="w-64 flex-shrink-0 bg-cyber-card border-l border-cyber-border flex flex-col">
      <!-- Logo -->
      <div class="p-5 border-b border-cyber-border">
        <div class="flex items-center gap-3">
          <div class="w-9 h-9 rounded-lg bg-blue-600/20 border border-blue-500/30 flex items-center justify-center">
            <svg class="w-5 h-5 text-blue-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
            </svg>
          </div>
          <div>
            <div class="text-sm font-bold text-white leading-tight">مدیریت دارایی</div>
            <div class="text-xs text-gray-500">سایبری</div>
          </div>
        </div>
      </div>

      <!-- Nav -->
      <nav class="flex-1 p-3 space-y-1 overflow-y-auto">
        <router-link v-for="link in navLinks" :key="link.to" :to="link.to"
          class="sidebar-link" :class="{ active: $route.path.startsWith(link.to) }">
          <span class="text-lg">{{ link.icon }}</span>
          <span>{{ link.label }}</span>
        </router-link>
      </nav>

      <!-- User -->
      <div class="p-3 border-t border-cyber-border">
        <div class="flex items-center justify-between px-2 py-2">
          <div>
            <div class="text-sm font-medium text-white">{{ auth.user?.fullName || auth.user?.username }}</div>
            <div class="text-xs text-gray-500">{{ auth.user?.role }}</div>
          </div>
          <button @click="handleLogout" class="text-gray-500 hover:text-red-400 transition-colors" title="خروج">
            <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
            </svg>
          </button>
        </div>
      </div>
    </aside>

    <!-- Main content -->
    <div class="flex-1 flex flex-col overflow-hidden">
      <!-- Topbar -->
      <header class="h-14 bg-cyber-card border-b border-cyber-border flex items-center justify-between px-6">
        <h1 class="text-base font-semibold text-white">{{ currentTitle }}</h1>
        <div class="flex items-center gap-2">
          <div class="w-2 h-2 rounded-full bg-green-400 animate-pulse"></div>
          <span class="text-xs text-gray-400">سیستم فعال</span>
        </div>
      </header>

      <!-- Page content -->
      <main class="flex-1 overflow-y-auto p-6">
        <router-view />
      </main>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()

const navLinks = [
  { to: '/dashboard', icon: '📊', label: 'داشبورد' },
  { to: '/assets', icon: '🖥️', label: 'دارایی‌ها' },
  { to: '/discovery', icon: '🔍', label: 'کشف دارایی‌ها' },
  { to: '/vulnerabilities', icon: '🛡️', label: 'آسیب‌پذیری‌ها' },
  { to: '/risk', icon: '⚠️', label: 'تحلیل ریسک' },
  { to: '/threat-intel', icon: '🎯', label: 'هوش تهدید' },
  { to: '/siem', icon: '📡', label: 'وضعیت SIEM' },
  { to: '/audit', icon: '📋', label: 'لاگ حسابرسی' },
  { to: '/reports', icon: '📄', label: 'گزارش‌ها' },
]

const currentTitle = computed(() => route.meta.title || 'سامانه مدیریت دارایی‌های سایبری')

async function handleLogout() {
  await auth.logout()
  router.push('/login')
}
</script>
