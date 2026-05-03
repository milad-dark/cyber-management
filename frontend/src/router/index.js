import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const routes = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/LoginView.vue'),
    meta: { public: true }
  },
  {
    path: '/',
    component: () => import('@/components/Layout.vue'),
    meta: { requiresAuth: true },
    children: [
      {
        path: '',
        redirect: '/dashboard'
      },
      {
        path: 'dashboard',
        name: 'Dashboard',
        component: () => import('@/views/DashboardView.vue'),
        meta: { title: 'داشبورد' }
      },
      {
        path: 'assets',
        name: 'Assets',
        component: () => import('@/views/AssetsView.vue'),
        meta: { title: 'فهرست دارایی‌ها' }
      },
      {
        path: 'assets/:id',
        name: 'AssetDetail',
        component: () => import('@/views/AssetDetailView.vue'),
        meta: { title: 'جزئیات دارایی' }
      },
      {
        path: 'discovery',
        name: 'Discovery',
        component: () => import('@/views/DiscoveryView.vue'),
        meta: { title: 'کشف دارایی‌ها' }
      },
      {
        path: 'vulnerabilities',
        name: 'Vulnerabilities',
        component: () => import('@/views/VulnerabilitiesView.vue'),
        meta: { title: 'آسیب‌پذیری‌ها' }
      },
      {
        path: 'risk',
        name: 'Risk',
        component: () => import('@/views/RiskView.vue'),
        meta: { title: 'تحلیل ریسک' }
      },
      {
        path: 'threat-intel',
        name: 'ThreatIntel',
        component: () => import('@/views/ThreatIntelView.vue'),
        meta: { title: 'هوش تهدید' }
      },
      {
        path: 'siem',
        name: 'Siem',
        component: () => import('@/views/SiemView.vue'),
        meta: { title: 'وضعیت SIEM' }
      },
      {
        path: 'audit',
        name: 'AuditLogs',
        component: () => import('@/views/AuditLogsView.vue'),
        meta: { title: 'لاگ‌های حسابرسی' }
      },
      {
        path: 'reports',
        name: 'Reports',
        component: () => import('@/views/ReportsView.vue'),
        meta: { title: 'گزارش‌ها' }
      }
    ]
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

router.beforeEach((to, from, next) => {
  const auth = useAuthStore()
  if (!to.meta.public && !auth.isLoggedIn) {
    next('/login')
  } else if (to.path === '/login' && auth.isLoggedIn) {
    next('/dashboard')
  } else {
    next()
  }
})

export default router
