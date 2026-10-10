# Frontend Developer üçün İnteqrasiya və Dəyişikliklər Bələdçisi (Frontend Review)

Bu sənəd son arxitektura və funksionallıq yeniləmələri nəticəsində backend tərəfdə edilmiş bütün dəyişiklikləri, yeni modulları, API endpoint-ləri və onların frontend tərəfində necə istifadə olunacağını izah edir.

---

## 📌 Mündəricat
1. [Satış Modulu (Sales Module) - Sifariş, Təhvil və Qaimə](#1-satış-modulu-sales-module)
2. [Master Data və İlkin Qalıqlar (Master Data Hub)](#2-master-data-və-i̇lkin-qalıqlar)
3. [Hesablar Planı (Chart of Accounts - Ağac və Balanslar)](#3-hesablar-planı-chart-of-accounts)
4. [Şirkət Profili və Dinamik Header](#4-şirkət-profili-və-dinamik-header)
5. [Maliyyə Hesabatları (4 Əsas Maliyyə Hesabatı)](#5-maliyyə-hesabatları)
6. [Frontend Komponentləri və UX Tövsiyələri](#6-frontend-komponentləri-və-ux-tövsiyələri)

---

## 1. Satış Modulu (Sales Module)

Əvvəl yalnız müstəqil qaimə kəsilməsi var idisə, artıq tam dövrəli **Satış Zənciri** tətbiq olunub:
```text
[Satış Sifarişi (Sales Order)] 
       ⬇ (Təsdiqlənmə)
[Mal Göndərişi / Qaimə-Faktura (Delivery Note)] 
       ⬇ (Anbar çıxışı və COGS silinməsi)
[Satış Qaiməsi (Customer Invoice / e-Qaimə)] 
       ⬇ (Debitor borc və Gəlir qeydiyyatı)
```

### 1.1. Satış Sifarişləri (Sales Orders)
Baza marşrut: `/api/sales/orders`

* **`GET /api/sales/orders`** — Bütün satış sifarişlərinin siyahısını qaytarır.
* **`GET /api/sales/orders/{orderId}`** — Seçilmiş sifarişin detallarını və sətirlərini qaytarır.
* **`POST /api/sales/orders`** — Yeni sifariş yaradır.
  * **Status:** İlkin olaraq `Draft` (0) statusunda yaranır.
  * **Request Body:**
  ```json
  {
    "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "orderDate": "2026-10-10T12:00:00Z",
    "expectedDeliveryDate": "2026-10-15T12:00:00Z",
    "currency": "AZN",
    "customerReference": "PO-CLIENT-9921",
    "notes": "Təcili çatdırılma",
    "lines": [
      {
        "itemId": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
        "description": "Noutbuk Lenovo Legion",
        "quantity": 5,
        "unitPrice": 1850.00,
        "discountPercent": 5,
        "taxCodeId": "5fa85f64-5717-4562-b3fc-2c963f66afa8",
        "taxRate": 18.0
      }
    ]
  }
  ```
* **`POST /api/sales/orders/{orderId}/confirm`** — Sifarişi təsdiqləyir (`Confirmed`). Çatdırılmaya hazır vəziyyətə keçir.
* **`POST /api/sales/orders/{orderId}/cancel`** — Sifarişi ləğv edir (`Cancelled`).

---

### 1.2. Mal Göndərişi / İrsaliyyə (Delivery Notes / Goods Issue)
Baza marşrut: `/api/sales/deliveries`

* **`GET /api/sales/deliveries`** — Bütün mal göndərişlərinin siyahısı.
* **`GET /api/sales/deliveries/{deliveryId}`** — Göndərişin detalları və sətirləri.
* **`POST /api/sales/deliveries`** — Yeni mal göndərişi sənədi (qaiməsiz irsaliyyə) yaradır.
  * Sifarişə bağlı və ya birbaşa müştəriyə ola bilər (`salesOrderId` optional-dır).
  * **Request Body:**
  ```json
  {
    "salesOrderId": "6fa85f64-5717-4562-b3fc-2c963f66afa9",
    "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "warehouseId": "7fa85f64-5717-4562-b3fc-2c963f66afb0",
    "deliveryDate": "2026-10-10T14:00:00Z",
    "driverName": "Əli Hüseynov",
    "vehicleNumber": "99-AB-123",
    "trackingNumber": "TRK-88129",
    "notes": "1-ci partiya çatdırılması",
    "lines": [
      {
        "salesOrderLineId": "8fa85f64-5717-4562-b3fc-2c963f66afb1",
        "itemId": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
        "description": "Noutbuk Lenovo Legion",
        "quantity": 3
      }
    ]
  }
  ```
* **`POST /api/sales/deliveries/{deliveryId}/post`** — **Ən vacib addım!**
  * Sənədi təsdiqləyir və uçota alır (`Posted`).
  * Göstərilən anbardan real stoku azaldır.
  * Maya dəyərini (AVCO / Standart maya dəyəri) hesablayır.
  * Avtomatik jurnal qeydi yazır:
    * **DR:** 7010 - Satışın Maya Dəyəri (COGS)
    * **CR:** 1100 - Mallar və Materiallar (Stok)
  * Əgər sifarişlə bağlıdırsa, həmin sifariş sətirinin `DeliveredQuantity` sayını artırır.

---

### 1.3. Satış Qaimələri (Sales Invoices / Customer Invoices)
Baza marşrut: `/api/sales/invoices` və ya `/api/invoicing/customer-invoices`

* **`GET /api/sales/invoices`** — Satış qaimələri siyahısı.
* **`POST /api/sales/invoices`** — Yeni satış qaiməsi yaradır.
  * Artıq `salesOrderId` və `deliveryNoteId` sahələri əlavə olunub!
  ```json
  {
    "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "salesOrderId": "6fa85f64-5717-4562-b3fc-2c963f66afa9",
    "deliveryNoteId": "9fa85f64-5717-4562-b3fc-2c963f66afb2",
    "invoiceDate": "2026-10-10T12:00:00Z",
    "dueDate": "2026-10-25T12:00:00Z",
    "postingDate": "2026-10-10T12:00:00Z",
    "currency": "AZN",
    "exchangeRate": 1.0,
    "lines": [
      {
        "description": "Noutbuk Lenovo Legion",
        "quantity": 3,
        "unitPrice": 1850.00,
        "discountPercent": 5,
        "taxRate": 18.0
      }
    ]
  }
  ```
* **`POST /api/sales/invoices/{invoiceId}/post`** — Qaiməni uçota alır. AR (1200) debitor borcunu və Satış Gəlirini (6010) formalaşdırır.

---

## 2. Master Data və İlkin Qalıqlar

Tənzimləmələr / Əsas Məlumatlar bölməsi üçün vahid mərkəz:
Baza marşrut: `/api/master-data`

* **`GET /api/master-data/summary`** — Kartlar və dashboard üçün bütün obyektlərin sayını verir:
  ```json
  {
    "customersCount": 14,
    "suppliersCount": 8,
    "warehousesCount": 3,
    "itemsCount": 45,
    "fixedAssetsCount": 12,
    "bankAccountsCount": 2,
    "taxCodesCount": 4,
    "accountsCount": 32
  }
  ```
* **`GET /api/master-data/tabs`** — UI tab menyusu üçün dinamik struktur:
  * Hər tab üçün `key`, `titleAz`, `titleEn`, `count`, `apiEndpoint` qaytarır.
* **Qısa Yol Endpoint-ləri:**
  * `GET /api/master-data/customers`
  * `GET /api/master-data/suppliers`
  * `GET /api/master-data/items`
  * `GET /api/master-data/warehouses`
  * `GET /api/master-data/bank-accounts`
  * `GET /api/master-data/cash-desks`

### 2.1. İlkin Qalıqların Təsdiqi və Audit Jurnalı
* **`POST /api/master-data/initial-balances`** (və ya `/api/accounts/initial-balances`)
  * Sistemə ilkin daxilolma aktı əsasında ilkin qalıqları daxil edir.
  * Yalnız mühasibatlıq balansı qorunmaqla (DR cəmi == CR cəmi) qəbul edilir.
  * Audit jurnalında (`InitialBalanceAuditLog`) akt nömrəsi, tarixi, təsdiqləyən şəxs və əlavə edilmiş sənəd faylı URL-i saxlanılır.
  * **Request Body:**
  ```json
  {
    "actNumber": "AKT-2026/01",
    "actDate": "2026-01-01T00:00:00Z",
    "attachmentUrl": "https://storage.altensor.az/docs/initial_balance_akt_signed.pdf",
    "notes": "01.01.2026 tarixinə inventarizasiya və təhvil-təslim aktı əsasında",
    "lines": [
      {
        "accountId": "203a3d54-a690-4a8d-b0ad-e7706d967e81",
        "debit": 50000.00,
        "credit": 0.00,
        "description": "Kassa qalığı"
      },
      {
        "accountId": "304b4e65-b701-4b9e-c1be-f8817e078f92",
        "debit": 0.00,
        "credit": 50000.00,
        "description": "Nizamnamə kapitalı"
      }
    ]
  }
  ```

---

## 3. Hesablar Planı (Chart of Accounts)

Standart hesablar planı Azərbaycan Milli Mühasibat Uçotu Standartlarına (MMUS / İFRS) tam uyğunlaşdırılıb:
* **0100..1400** — Aktivlər
* **2000..2400** — Öhdəliklər
* **3000..3200** — Kapital
* **6000..6300** — Gəlirlər
* **7000..7600** — Xərclər

### 3.1. Yeni Endpoint-lər:
* **`GET /api/accounts/tree`** — Hesablar planının ağac (Tree view) strukturu.
  * Hər düyündə `children: []`, `level`, `isLeaf`, `hasChildren`, `subcategoryName` və 3 mərhələli qalıqlar (`openingDebit`, `turnoverCredit`, `closingDebit` və s.) mövcuddur.
* **`GET /api/accounts/balances`** — Dövriyyə cədvəli cərgələri:
  * Parametrlər: `?fromDate=2026-01-01&toDate=2026-10-10&search=1010&includeZeroBalance=false`
* **`GET /api/accounts/types`** — Hesab tipləri siyahısı (Standart, AR, AP, Bank, Kassa, Stok, GRNI, COGS, Vergi və s.).
* **`GET /api/accounts/subcategories`** — Kateqoriyaya uyğun alt kateqoriyalar siyahısı (məs: Aktiv üçün "Uzunmüddətli" və "Dövriyyə aktivləri").

---

## 4. Şirkət Profili və Dinamik Header

### 4.1. Dinamik Navbar / Topbar Məlumatı
* **`GET /api/company/header`**
  * Hər səhifənin yuxarı sağ/sol küncündə şirkət rekvizitlərini və cari aktiv maliyyə dövrünü göstərmək üçün xüsusi yüngül DTO:
  ```json
  {
    "companyName": "Altensor MMC",
    "taxNumber": "1401234567",
    "baseCurrency": "AZN",
    "logoUrl": "https://cdn.altensor.az/logos/company.png",
    "activeFiscalYear": "2026 Maliyyə İli",
    "activeFiscalPeriod": "2026-Q4 / Oktyabr"
  }
  ```

### 4.2. Şirkət Tənzimləmələri Səhifəsi
* **`GET /api/company`** — Tam profil:
  * VÖEN, Hüquqi forma (MMC/ASC/F/Ş), ƏDV ödəyicisi olub-olmaması (`isVatPayer`), ƏDV dərəcəsi (`vatRate`: 18%), Direktor, Baş Mühasib, İBAN, SWIFT/BIC, Müxbir hesab, Statistika kodu, ASAN Login ID.
* **`PUT /api/company`** — Profil məlumatlarını redaktə etmək.

---

## 5. Maliyyə Hesabatları

Baza marşrut: `/api/reports`

Backend tərəfdə beynəlxalq standartlara cavab verən 4 əsas maliyyə hesabatı və dövriyyə cədvəli tam hazır vəziyyətə gətirilib:

1. **`GET /api/reports/trial-balance`** (Dövriyyə-Qalıq Cədvəli)
   * Parametrlər: `?fromDate=...&toDate=...&search=...&includeZeroBalance=false`
   * Hər sətirdə: `openingDebit`, `openingCredit`, `turnoverDebit`, `turnoverCredit`, `closingDebit`, `closingCredit`.
2. **`GET /api/reports/financial-position`** (Maliyyə Vəziyyəti Haqqında Hesabat / Balans)
   * Parametrlər: `?asOfDate=2026-10-10`
   * Bölmələr: Uzunmüddətli Aktivlər, Dövriyyə Aktivləri, Uzunmüddətli Öhdəliklər, Qısamüddətli Öhdəliklər, Kapital, Cari Dövrün Mənfəəti/Zərəri.
   * `equationHolds`: Balans tənliyinin (Aktiv = Öhdəlik + Kapital) düzgünlüyünü göstərən boolean bayraq.
3. **`GET /api/reports/profit-or-loss`** (Mənfəət və Zərər Hesabatı)
   * Parametrlər: `?fromDate=2026-01-01&toDate=2026-10-10`
   * Addımlar: Əsas Gəlirlər - COGS = Ümumi Mənfəət (Gross Profit) -> İnzibati və Satış Xərcləri çıxılır = Əməliyyat Mənfəəti (EBIT) -> Maliyyə gəlir/xərcləri -> Vergi -> Xalis Mənfəət/Zərər (Net Profit).
4. **`GET /api/reports/changes-in-equity`** (Kapitalda Dəyişikliklər Hesabatı)
   * Parametrlər: `?fromDate=2026-01-01&toDate=2026-10-10`
   * Nizamnamə kapitalı, Bölüşdürülməmiş mənfəət və digər ehtiyatlar üzrə hərəkət.
5. **`GET /api/reports/cash-flow`** (Pul Vəsaitlərinin Hərəkəti Hesabatı)
   * Parametrlər: `?fromDate=2026-01-01&toDate=2026-10-10`
   * Əməliyyat, İnvestisiya və Maliyyələşdirmə fəaliyyətləri üzrə pul giriş-çıxışları.

---

## 6. Frontend Komponentləri və UX Tövsiyələri

### 6.1. Satış Modulu Səhifəsi (Workflow)
* **Sales Order View:** Sifariş statusları üçün badge-lər qoyun:
  * `Draft` (Boz), `Confirmed` (Mavi), `PartiallyDelivered` (Sarı), `Delivered` (Yaşıl), `Cancelled` (Qırmızı).
  * Əgər status `Confirmed`-dirsə, cədvəldə və ya detallar səhifəsində **"Təhvil Aktı / İrsaliyyə Yarat"** düyməsi çıxsın (`Create Delivery Note` modalı açsın və sifariş sətirlərini avtomatik doldursun).
* **Delivery Note View:**
  * Əgər status `Draft`-dırsa, **"Təsdiqlə və Anbardan Sil"** (`POST /api/sales/deliveries/{id}/post`) düyməsi yerləşdirin.
  * Təsdiqləndikdən sonra **"e-Qaimə Kəs"** düyməsi çıxsın (`POST /api/sales/invoices` -ə referans versin).

### 6.2. Hesablar Planı (Tree View)
* Ant Design `Tree` və ya Tailwind ilə qatlanan ierarxik komponent istifadə edin.
* Hər hesabın yanında `Type` və `Subcategory` etiketləri göstərilsin.
* Qalıq cədvəlində `includeZeroBalance` checkbox filtri qoyun ki, qalıqsız hesablar gizlədilə bilsin.

### 6.3. Xətaların İdarə Edilməsi
* Əgər anbarda kifayət qədər qalıq yoxdursa və ya balans tənliyi pozularsa, backend `400 Bad Request` ilə `{ "message": "..." }` qaytarır.
* `axios` interceptor-da `error.response?.data?.message` sahəsini toast/notification olaraq istifadəçiyə çatdırın.
