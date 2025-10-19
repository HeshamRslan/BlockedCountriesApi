# 🌍 Blocked Countries API (.NET 8)

## 📘 Overview
**Blocked Countries API** is a clean and efficient **.NET 8 Web API** that helps manage restricted access by countries and IP addresses.

It allows:
    - Blocking and unblocking countries (permanently or temporarily)
    - Checking if an IP belongs to a blocked country
    - Logging all blocked access attempts
    - Integration with a third-party Geo IP API to identify user location and ISP

---

##  Features

    ✅ Add or remove blocked countries  
    ✅ Temporarily block countries with auto-expiry  
    ✅ Check if a visitor’s IP is blocked  
    ✅ Fetch IP geo-location and ISP details via Geo API  
    ✅ Paginated logs of blocked access attempts  
    ✅ Fully in-memory storage (no database)  
    ✅ Thread-safe with `ConcurrentDictionary`  
    ✅ Swagger UI support for live testing  

---

## Tech Stack
    - **.NET 8 Web API**
    - **HttpClientFactory**
    - **In-Memory Caching (IMemoryCache & ConcurrentDictionary)**
    - **BackgroundService (Hosted Service)** for auto-unblocking
    - **Swagger / OpenAPI**

---

## 📂 Project Structure
    📁 BlockedCountriesApi
    ┣ 📂 Controllers
    ┣ 📂 Models
    ┣ 📂 Services
    ┣ 📄 Program.cs
    ┣ 📄 appsettings.json
    ┣ 📄 README.md                
