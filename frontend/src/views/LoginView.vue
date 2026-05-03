<template>
  <div class="min-h-screen bg-cyber-dark flex items-center justify-center p-4">
    <div class="w-full max-w-md">
      <!-- Logo -->
      <div class="text-center mb-8">
        <div class="inline-flex items-center justify-center w-16 h-16 rounded-2xl bg-blue-600/20 border border-blue-500/30 mb-4">
          <svg class="w-8 h-8 text-blue-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
          </svg>
        </div>
        <h1 class="text-2xl font-bold text-white">سامانه مدیریت دارایی‌های سایبری</h1>
        <p class="text-gray-400 text-sm mt-1">Cyber Asset Management Platform</p>
      </div>

      <!-- Form -->
      <div class="cyber-card">
        <h2 class="text-lg font-semibold text-white mb-6">ورود به سامانه</h2>

        <form @submit.prevent="handleLogin" class="space-y-4">
          <div>
            <label class="block text-sm text-gray-400 mb-1">نام کاربری</label>
            <input
              v-model="form.username"
              type="text"
              class="cyber-input"
              placeholder="نام کاربری را وارد کنید"
              required
              autocomplete="username"
            />
          </div>

          <div>
            <label class="block text-sm text-gray-400 mb-1">رمز عبور</label>
            <input
              v-model="form.password"
              type="password"
              class="cyber-input"
              placeholder="رمز عبور را وارد کنید"
              required
              autocomplete="current-password"
            />
          </div>

          <div v-if="error" class="text-red-400 text-sm bg-red-900/20 rounded-lg p-3 border border-red-800">
            {{ error }}
          </div>

          <button
            type="submit"
            :disabled="loading"
            class="btn-primary w-full flex items-center justify-center gap-2"
          >
            <svg v-if="loading" class="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
            </svg>
            {{ loading ? 'در حال ورود...' : 'ورود' }}
          </button>
        </form>

        <p class="text-xs text-gray-500 text-center mt-4">
          نام کاربری پیش‌فرض: <span class="text-blue-400">admin</span> | رمز: <span class="text-blue-400">Admin@1234</span>
        </p>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const auth = useAuthStore()

const form = ref({ username: '', password: '' })
const loading = ref(false)
const error = ref('')

async function handleLogin() {
  loading.value = true
  error.value = ''
  try {
    await auth.login(form.value.username, form.value.password)
    router.push('/dashboard')
  } catch (e) {
    error.value = e.response?.data?.message || 'خطا در ورود به سامانه'
  } finally {
    loading.value = false
  }
}
</script>
