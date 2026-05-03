# سامانه مدیریت دارایی‌های سایبری

## 🛡️ Cyber Asset Management Platform

یک پلتفرم جامع مدیریت دارایی‌های سایبری با رابط کاربری فارسی (RTL).

## 🏗️ معماری سیستم

```
Nginx (Reverse Proxy: 80/443)
  ├── Vue.js Frontend (Persian RTL)
  └── .NET 8 ASP.NET Core API
        ├── PostgreSQL 16
        ├── Redis 7
        ├── Neo4j 5
        └── Python FastAPI Discovery Engine
```

## 🚀 راه‌اندازی

```bash
cp .env.example .env
docker compose up -d
```

## دسترسی
- رابط کاربری: http://localhost
- Swagger API: http://localhost/swagger
- کاربر: `admin` / رمز: `Admin@1234`

## قابلیت‌ها
- داشبورد با KPI و نمودارها
- مدیریت دارایی‌ها (CRUD)
- کشف خودکار شبکه (nmap/SNMP)
- مدیریت آسیب‌پذیری‌ها (CVE/CVSS)
- تحلیل ریسک
- هوش تهدید (IOC)
- یکپارچه‌سازی SIEM (Syslog/Webhook)
- یکپارچه‌سازی GLPI
- لاگ حسابرسی کامل
- گزارش‌های Excel
- نقشه شبکه Neo4j
