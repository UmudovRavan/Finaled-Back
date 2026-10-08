# Altensor Accounting – Review

> Tarix: 2026-10-07 · Scope: **yalnız backend** – `Finaled-Back/AltensorAccounting` (+ `AltensorAuthService` webhook göndərişi)
> Frontend qeydləri ayrıca faylda: [FRONTEND_REVIEW.md](FRONTEND_REVIEW.md)
> Status: backend **deploy olunub** – bütün həllər "canlı mühitdə necə tətbiq olunur" nəzərə alınaraq yazılıb.
> Bu fayl problemlər gəldikcə genişlənəcək. Hazırda: **Problem 1–4 (+ ortaq kök səbəb)**.

---

## 0. Qısa xülasə (TL;DR)

| # | Ekran | Xəta | Yer (backend) |
|---|-------|------|-------------------|
| 1 | Satış qaimələri → Baş kitaba keçir | `Hesablanmış ƏDV üçün default hesab təyin edilməyib.` | `AccountingService.cs:703` |
| 2 | Mal qəbulu → Sənədi icra et | `GRNI / Accrued Purchases hesabı təyin edilməyib.` | `ProcurementService.cs:331` |
| 3 | Alış qaimələri → Qaiməni icra et | `Kreditor borclar (AP) hesabı təyin edilməyib.` | `ProcurementService.cs:614` |
| 4 | Ehtiyyat hərəkəti → Sənədi icra et | `Maya dəyəri (COGS) hesabı təyin edilməyib.` | `InventoryService.cs:252` |

**Dörd xəta ayrı-ayrı bug deyil. Hamısı eyni kök səbəbdən gəlir:** bu tenant üçün **Hesab Planı (Chart of Accounts) tam qurulmayıb və `Company.Default*AccountId` sahələri boşdur.** Posting servisləri hesabı tapa bilmir və `BusinessRuleException` atır.

Üstəlik 5-ci bir xəta (`DB_UPDATE_ERROR 23505 PK_Users` – `/internal/webhooks/user-created`) məhz **bu qurulumun niyə heç vaxt düzəlmədiyini** izah edir – o da eyni zəncirin bir hissəsidir (bax: Bölmə 3).

Sorğular backend-ə düzgün çatır və xəta backend məntiqindən gəlir. Frontend ilə bağlı qeydlər: [FRONTEND_REVIEW.md](FRONTEND_REVIEW.md).

---

## 1. Frontend yoxlaması

Bu bölmə [FRONTEND_REVIEW.md](FRONTEND_REVIEW.md) faylına köçürülüb.

---

## 2. Kök səbəb – backend hesabları necə tapır

Hər posting metodu eyni 3 pilləli qaydanı izləyir:

```
1) Company.DefaultXxxAccountId        (şirkət parametri)
2) Hesab planında kod/tip üzrə axtarış (məs: Code == "2250" || Type == Tax)
3) Tapılmadısa → throw BusinessRuleException("... təyin edilməyib")
```

Yəni xəta o deməkdir ki, **bu tenant üçün həm `Company.DefaultXxx` boşdur, həm də hesab planında uyğun hesab yoxdur.**

### Hansı hesablar harada tələb olunur

| Əməliyyat | Tələb olunan hesablar | Axtarış qaydası |
|-----------|----------------------|-----------------|
| Satış qaiməsi post | AR, **Output VAT**, Revenue | Output VAT: `DefaultOutputVatAccountId` → kod `2250` → `Type=Tax & Liability` |
| Mal qəbulu post | Stock, **GRNI** | GRNI: `DefaultGRNIAccountId` → kod `2200` → `Type=GRNI` |
| Alış qaiməsi post | **AP**, Input VAT, GRNI | AP: `Supplier.PayableAccountId` → `DefaultPayableAccountId` → kod `2100` / `Type=Payable` / hər hansı Liability yarpaq |
| Stok hərəkəti post | Stock, **COGS** | COGS: `DefaultCOGSAccountId` → kod `7010` / `Type=COGS` / hər hansı Expense yarpaq |

### Əsas müşahidə – hesab planı "yarımçıqdır"

Satış qaiməsində **AR (Debitor) hesabı tapıldı, amma Output VAT tapılmadı.** Mal qəbulunda Stock tapıldı, GRNI tapılmadı. Alış qaiməsində AP tapılmadı. Stok hərəkətində COGS tapılmadı (hətta "hər hansı Expense yarpaq hesab" fallback-ı belə).

Bu o deməkdir ki, tenant-da **bəzi hesablar var, amma standart plan (1100, 1200, 1250, 2100, 2200, 2250, 6010, 7010 ...) yoxdur.** Yəni hesab planı heç vaxt tam seed olunmayıb, yalnız istifadəçi/frontend tərəfindən əl ilə bir-iki hesab yaranıb (məs: `setInitialBalance` içində avtomatik `3100` yaradılır, istifadəçi əl ilə hesablar əlavə edir).

---

## 3. Niyə seed işləmir? (ən vacib hissə)

Seed məntiqi `DbSeeder.SeedTenantAccountingDefaultsAsync` içindədir. Onu **yalnız bir yerdən** çağırırlar: `InternalWebhooksController.HandleUserCreated`.

### Problem 3.1 – Seed "ya hamısı, ya heç nə" məntiqindədir

`DbSeeder.cs:33-34`:

```csharp
var existingAccounts = await context.Accounts.IgnoreQueryFilters().Where(a => a.TenantId == tenantId).ToListAsync();
if (!existingAccounts.Any())
{
    // tam standart plan seed olunur (1000..7400)
}
else
{
    // yalnız mövcud hesablardan Company.Default* "yamaqlanır"
}
```

Əgər tenant-da **bir dənə belə hesab varsa** (istifadəçi əl ilə yaradıbsa, və ya webhook-dan əvvəl hər hansı əməliyyat hesab yaradıbsa), tam plan **heç vaxt seed olunmur.** `else` şaxəsi isə yalnız **mövcud** hesabları tapıb `Company.Default*`-a bağlayır. Olmayan hesab (`2250`, `2200`, `2100`, `7010`) **yaradılmır**, `FindAccId` `null` qaytarır və default boş qalır.

> Bu, 4 xətanın ən ehtimal olunan birbaşa səbəbidir.

### Problem 3.2 – Startup patch-ı da həll etmir

`Program.cs:105` → `EnsureAllCompaniesHaveDefaultAccountsAsync`:

- `if (!tenantAccounts.Any()) continue;` – hesabı olmayan tenant atlanır.
- Yalnız **mövcud** hesabları bağlayır, **çatışan hesabları yaratmır.**
- Company olmayan tenant (yalnız `User` var) heç görünmür.

Deməli deploy zamanı restart etməklə də problem həll olmur.

### Problem 3.3 – Webhook `PK_Users` xətası ilə **seed addımına çatmır**

`InternalWebhooksController.cs:76-83`:

```csharp
await _userSyncService.SyncUserCreatedAsync(@event, cancellationToken);   // (1) burada exception atılır
try { await DbSeeder.SeedTenantAccountingDefaultsAsync(...); }             // (2) heç vaxt işləmir
```

`UserSyncService.cs:25`:

```csharp
var existing = await _userRepo.GetByIdAsync(@event.UserId, ct);
```

Webhook **anonimdir** (JWT yoxdur) → `CurrentTenantService.TenantId == null`.
`AppDbContext.cs:115-117` global query filter-i:

```csharp
_tenantService.IsPlatformSuperAdmin || (_tenantService.TenantId != null && e.TenantId == _tenantService.TenantId)
```

Webhook kontekstində `TenantId == null` olduğu üçün filter **həmişə `false`** olur → `GetByIdAsync` **heç vaxt mövcud istifadəçini tapmır** → kod "istifadəçi yoxdur" hesab edib `INSERT` edir → PostgreSQL `23505 duplicate key PK_Users` → `DB_UPDATE_ERROR` (400).

Nəticə: **istifadəçi bir dəfə sync olunduqdan sonra, webhook hər çağırılanda 400 qaytarır və seed addımına heç vaxt çatmır.** Yəni:

1. İlk dəfə webhook işləyəndə (və ya seed kodu deploy olunmamışdan əvvəl) istifadəçi yazılır.
2. Seed ya işləməyib, ya da tam işləməyib (3.1).
3. Sonrakı hər webhook çağırışı 3.3 səbəbindən yıxılır → **tenant əbədi "yarımçıq" qalır.**

Bu, sizin screenshot-dakı `user-created` 400 cavabıdır (payload-da `userId d5dd24d3...`, `tenantId 9dd2fd4d...`).

### Problem 3.4 – Auth Service tərəfi (bonus risklər)

`HttpIntegrationEventPublisher` / `PersistenceServiceRegistration.cs`:

- `client.Timeout = 5s` – seed (hesab planı + fiskal il + 12 dövr) yavaş DB-də 5 saniyədən uzun çəkə bilər. Auth tərəfi timeout alır, xətanı **yalnız loglayır**, retry yoxdur. Accounting tərəfi isə işini bitirə də bilər, bitirməyə də.
- Retry / outbox yoxdur. Webhook bir dəfə uğursuz olarsa tenant əbədi seed olunmur.
- `DangerousAcceptAnyServerCertificateValidator` – production üçün təhlükəlidir (TLS yoxlaması söndürülüb).
- Modul endpoint-i `ModuleEndpointRegistry`-də konfiqurasiya olunmayıbsa, event ümumiyyətlə göndərilmir (`endpoints.Count == 0 → return`).

---

## 4. Hər problem üzrə detallı analiz və həll

### Problem 1 – Satış qaiməsi `Hesablanmış ƏDV üçün default hesab təyin edilməyib`

- **Endpoint:** `POST /api/customer-invoices/{id}/post`
- **Kod:** `AccountingService.PostCustomerInvoiceAsync` (`AccountingService.cs:699-703`)
- **Səbəb:** `company.DefaultOutputVatAccountId == null` və tenant-da nə kod `2250`, nə də `Type=Tax & Category=Liability` hesab var.
- **Qeyd:** Qaimədə `AZN 2,855.60` yekun məbləğ və vergi var (`TaxTotal > 0`), ona görə VAT hesabı məcburidir.
- **Qeyd 2:** Növbəti addımda Revenue da yoxlanacaq (`6010` və ya `Type=Revenue`). Seed düzgün olmasa, VAT-dan sonra növbəti xəta `Gəlir (Revenue) hesabı...` olacaq.

### Problem 2 – Mal qəbulu `GRNI / Accrued Purchases hesabı təyin edilməyib`

- **Endpoint:** `POST /api/Procurement/goods-receipts/{id}/post`
- **Kod:** `ProcurementService.PostGoodsReceiptAsync` (`ProcurementService.cs:329-331`)
- **Səbəb:** `DefaultGRNIAccountId == null`, kod `2200` və `Type=GRNI` hesabı yoxdur.
- **Əlavə müşahidə (UI):** Screenshot-da "Qəbul miqdarı **0 ədəd**" göstərilir, amma "Yekun dəyər **110.00**". Backend `line.ReceivedQuantity` ilə stok mühərrikini çağırır, `grn.TotalValue` ilə yazılış edir. Bu uyğunsuzluq hesab xətası aradan qalxdıqdan sonra növbəti problem ola bilər (0 miqdarla stok hərəkəti və 110 dəyərlik jurnal). **Ayrıca yoxlanmalıdır** (DB-də `GoodsReceiptLines.ReceivedQuantity` dəyərinə baxın).

### Problem 3 – Alış qaiməsi `Kreditor borclar (AP) hesabı təyin edilməyib`

- **Endpoint:** `POST /api/Procurement/supplier-invoices/{id}/post`
- **Kod:** `ProcurementService.PostSupplierInvoiceAsync` (`ProcurementService.cs:611-622`)
- **Səbəb:** `Supplier.PayableAccountId`, `DefaultPayableAccountId` boşdur; kod `2100` / `Type=Payable` / Liability yarpaq hesab yoxdur.
- **Növbəti xətalar (zəncir):** AP düzəldildikdən sonra kod eyni metodda ardıcıl yoxlayır:
  1. `Əvəzləşdirilən ƏDV hesabı` (`1250`/Tax) – `:618`
  2. `GRNI hesabı` (`2200`) – `:622`

  **Diqqət:** `:620-622` GRNI hesabını **qaimə GRNI-based olmasa belə** məcburi tələb edir. Bu mantiqi bugdur (bax: Bölmə 6.4).

### Problem 4 – Stok hərəkəti `Maya dəyəri (COGS) hesabı təyin edilməyib`

- **Endpoint:** `POST /api/Inventory/stock-transactions/{id}/post`
- **Kod:** `InventoryService.PostStockTransactionAsync` (`InventoryService.cs:250-252`)
- **Səbəb:** `DefaultCOGSAccountId == null`, kod `7010` / `Type=COGS` / hər hansı Expense yarpaq hesab yoxdur.
- **Bu sənəd tipi `Mədaxil (Receipt)`-dir – COGS ona lazım deyil!** COGS yalnız `Issue` (məxaric) tipində istifadə olunur (`:318`). Receipt üçün `:353-355` yalnız `cogsAccountId`-ni **GRNI tapılmayanda fallback** kimi istifadə edir. Amma metodun başında COGS **bütün tiplər üçün** məcburi yoxlanır və Receipt-i də bloklayır. Bu ayrıca kod bugdur (bax: Bölmə 6.3).

---

## 5. Frontend

Frontend tapıntıları (hardcoded gizli açar, brauzerdən `/internal/*` çağırışı, `localStorage` fallback-ları və s.) [FRONTEND_REVIEW.md](FRONTEND_REVIEW.md) faylına köçürülüb.

Backend baxımından vacib qeyd: `Webhook:SharedSecret` / `InternalCommunication:ApiKey` açarı brauzer bundle-ında açıq göründüyü üçün **kompromis olunmuş sayılır** və rotate edilməlidir (B5).

---

## 6. Həll planı

### Həll A – Dərhal (canlı mühit, kod dəyişmədən): data düzəlişi

> ⚠️ Əvvəl DB backup alın. SQL-i yalnız ziyanlı tenant üçün icra edin.

**Addım 1 – Diaqnostika (hansı hesab və default var?)**

```sql
-- 1) Tenant-ın hesabları
SELECT "Code","Name","Category","Type","IsLeaf","IsActive"
FROM "Accounts"
WHERE "TenantId" = '9dd2fd4d-32a6-4319-815e-64710257d82a' AND NOT "IsDeleted"
ORDER BY "Code";

-- 2) Company default-ları
SELECT "Id","Name",
       "DefaultReceivableAccountId","DefaultPayableAccountId","DefaultStockAccountId",
       "DefaultGRNIAccountId","DefaultCOGSAccountId","DefaultRevenueAccountId",
       "DefaultInputVatAccountId","DefaultOutputVatAccountId",
       "DefaultRetainedEarningsAccountId","DefaultFXGainLossAccountId"
FROM "Companies"
WHERE "TenantId" = '9dd2fd4d-32a6-4319-815e-64710257d82a';

-- 3) Maliyyə ili / dövr
SELECT y."Name", p."Name", p."StartDate", p."EndDate", p."Status"
FROM "FiscalYears" y LEFT JOIN "AccountingPeriods" p ON p."FiscalYearId" = y."Id"
WHERE y."TenantId" = '9dd2fd4d-32a6-4319-815e-64710257d82a';
```

(Cədvəl/sütun adları EF migration-a görə fərqlənə bilər – `\dt` ilə yoxlayın.)

**Addım 2 – Çatışan hesabları əlavə et və `Company.Default*` bağla.** Əgər hesab planı boşdursa və ya yarımçıqdırsa, aşağıdakı hesablar tenant-da olmalıdır (kod ilə eyni `DbSeeder`-dəki plan):

| Kod | Ad | Category | Type | Leaf | Company sahəsi |
|-----|----|----------|------|------|----------------|
| 1100 | Mallar və Materiallar (Stok) | Asset | Stock | ✔ | `DefaultStockAccountId` |
| 1200 | Alıcıların Debitor Borcları (AR) | Asset | Receivable | ✔ | `DefaultReceivableAccountId` |
| 1250 | Əvəzləşdirilən ƏDV (Input VAT) | Asset | Tax | ✔ | `DefaultInputVatAccountId` |
| 2100 | Kreditor Borclar (AP) | Liability | Payable | ✔ | `DefaultPayableAccountId` |
| 2200 | GRNI | Liability | GRNI | ✔ | `DefaultGRNIAccountId` |
| 2250 | Hesablanmış ƏDV (Output VAT) | Liability | Tax | ✔ | `DefaultOutputVatAccountId` |
| 3100 | Bölüşdürülməmiş Mənfəət | Equity | RetainedEarnings | ✔ | `DefaultRetainedEarningsAccountId` |
| 6010 | Satış Gəliri | Income | Revenue | ✔ | `DefaultRevenueAccountId` |
| 7010 | COGS | Expense | COGS | ✔ | `DefaultCOGSAccountId` |
| 7400 | Məzənnə Fərqi | Expense | Expense | ✔ | `DefaultFXGainLossAccountId` |

Ən təhlükəsiz yol SQL ilə əl ilə hesab insert etmək yerinə **Həll B-dən sonra** idempotent seed-i çağırmaqdır (SQL ilə `Category/Type` enum rəqəmlərini səhv yazmaq asandır). Əgər təcili lazımdırsa, SQL-i `Category/Type` rəqəmlərini `AltensorAccounting.Domain.Enums`-dan yoxlayaraq yazın.

> Qeyd: hesab əlavə etdikdən sonra yalnız `Company.Default*` sahələrini UPDATE etmək kifayətdir, restart lazım deyil (hər sorğuda DB-dən oxunur).

### Həll B – Düzgün (kod) həlli – prioritet sırası ilə

#### B1. Seed-i **idempotent** et (əsas düzəliş)

`DbSeeder.SeedTenantAccountingDefaultsAsync` – "əgər heç hesab yoxdursa seed et" əvəzinə **hər kod üçün çatışanı yarat**:

```csharp
var existing = await context.Accounts.IgnoreQueryFilters()
    .Where(a => a.TenantId == tenantId && !a.IsDeleted).ToListAsync();
var byCode = existing.ToDictionary(a => a.Code, a => a);

foreach (var def in StandardChart)            // statik siyahı: Code, Name, Category, Type, IsLeaf, IsControl
{
    if (!byCode.ContainsKey(def.Code))
    {
        var acc = new Account { TenantId = tenantId, Code = def.Code, Name = def.Name,
            Category = def.Category, Type = def.Type, IsLeaf = def.IsLeaf,
            IsControlAccount = def.IsControl };
        context.Accounts.Add(acc);
        byCode[def.Code] = acc;
    }
}
await context.SaveChangesAsync();

// Default-ları YALNIZ boşdursa, KOD ilə dəqiq bağla
company.DefaultOutputVatAccountId ??= byCode["2250"].Id;
company.DefaultGRNIAccountId      ??= byCode["2200"].Id;
company.DefaultPayableAccountId   ??= byCode["2100"].Id;
company.DefaultCOGSAccountId      ??= byCode["7010"].Id;
// ... qalanları da
await context.SaveChangesAsync();
```

Eyni məntiqi `EnsureAllCompaniesHaveDefaultAccountsAsync`-də də tətbiq et və `if (!tenantAccounts.Any()) continue;` sətrini sil. Tenant siyahısını `Companies ∪ Users ∪ Accounts` birləşməsindən götür.

**Gözlənilən nəticə:** istənilən tenant (boş, yarımçıq və ya tam) bir çağırışdan sonra tam plan + bağlı default-lara malik olur; təkrar çağırış zərərsizdir.

#### B2. Webhook-un `PK_Users` xətasını düzəlt

`UserSyncService`:

```csharp
var existing = await _userRepo.Query()          // və ya DbContext
    .IgnoreQueryFilters()
    .FirstOrDefaultAsync(u => u.Id == @event.UserId, ct);
```

(`GenericRepository.Query()` filtersiz versiyası olmalıdır, ya da servis `AppDbContext`-i birbaşa istifadə etsin.) Əlavə təhlükəsizlik: `DbUpdateException` + PostgreSQL `23505` tutulub **update**-ə çevrilsin (yarış vəziyyətinə qarşı).

`InternalWebhooksController`: **seed addımı user sync-dən asılı olmamalıdır**:

```csharp
try { await _userSyncService.SyncUserCreatedAsync(@event, ct); }
catch (Exception ex) { _logger.LogError(ex, "User sync failed"); /* seed-ə mane olmasın */ }

await DbSeeder.SeedTenantAccountingDefaultsAsync(db, @event.TenantId, _logger);  // həmişə işləsin
```

**Gözlənilən nəticə:** webhook istənilən sayda çağırılanda 200 qaytarır, istifadəçi təkrar yaranmır, seed hər dəfə yoxlanılır/tamamlanır.

#### B3. Posting servislərində səhv məntiqləri düzəlt

1. **`InventoryService.cs:250-252`** – COGS yalnız `Issue` üçün tələb olunsun:

   ```csharp
   Guid? cogsAccountId = null;
   if (tx.Type == StockTransactionType.Issue)
       cogsAccountId = company.DefaultCOGSAccountId
           ?? allAccounts.FirstOrDefault(a => a.Type == AccountType.COGS)?.Id
           ?? throw new BusinessRuleException("Maya dəyəri (COGS) hesabı təyin edilməyib.");
   ```

   Receipt üçün `:353-355` fallback-dakı `?? cogsAccountId` sil – düzgün muhasibatlıq baxımından məxaric hesabı mədaxil korrespondensiyası ola bilməz; GRNI/Adjustment hesabı yoxdursa, aydın xəta ver.

2. **`ProcurementService.cs:620-622`** – GRNI yalnız `invoice.IsGRNIBased == true` olduqda məcburi olsun. Non-GRNI qaimə GRNI hesabı olmadığına görə bloklanmamalıdır. `:666`-dakı `?? grniAccountId` fallback-ı da silinməlidir.

3. **Təhlükəli "hər hansı hesab" fallback-ları** – aşağıdakı sətirlər səhv hesaba jurnal yazır (heç xəta vermədən!), bu da maliyyə hesabatlarını pozur:
   - `ProcurementService.cs:326` və `InventoryService.cs:247` – "hər hansı Asset yarpaq" → Stock
   - `ProcurementService.cs:613` – "hər hansı Liability yarpaq" → AP
   - `InventoryService.cs:251` – "hər hansı Expense yarpaq" → COGS
   - `AccountingService.cs:706` – Revenue fallback `Category == Income`

   Tövsiyə: yalnız `Company.Default*` və **dəqiq `Type`** ilə axtarış; tapılmasa xəta. Yəni "təxmini hesab" qaydası ləğv olunsun.

4. Mərkəzi `IAccountResolver` yaradın (`ResolveAsync(AccountRole role)`):
   - Sıra: entity-specific (Customer/Supplier) → `Company.Default*` → (lazım olarsa) **lazy-seed** (B1-i çağırıb bir dəfə yenidən cəhd) → aydın xəta.
   - Bu qaydanı 4 servisdə ayrı-ayrı təkrar yazmaq əvəzinə bir yerdə saxlayın.

   **Gözlənilən nəticə:** gələcəkdə hər hansı tenant "yarımçıq" qalsa belə, ilk posting özü-özünü düzəldir; səhv hesaba yazılış mümkünsüz olur.

#### B4. Admin üçün idarəetmə API-si

- `POST /api/accounts/seed-template` (JWT, Admin/Accountant rolu) → B1-dəki idempotent seed-i **cari tenant** üçün çağırır. Hazırda belə endpoint backend-də **yoxdur** (axtarış nəticəsi boşdur).
- `GET/PUT /api/company/default-accounts` → Company default hesablarının oxunması/dəyişdirilməsi. Hazırda **heç bir API yoxdur** (yalnız `DbSeeder` bu sahələri yazır) → istifadəçi problemi öz tərəfindən həll edə bilmir.
- Bu 4 post xətası üçün `BusinessRuleException` cavabına ayrıca xəta kodu əlavə et (məs. `MISSING_DEFAULT_ACCOUNT`), hazırda hamısı ümumi `BUSINESS_RULE_ERROR` qaytarır və mətn ilə ayırd olunur.
- Bu endpoint-lərdən frontend-in necə istifadə edəcəyi: [FRONTEND_REVIEW.md](FRONTEND_REVIEW.md) (F1, F2).

#### B5. Auth → Accounting inteqrasiyasını möhkəmləndir

- Webhook timeout 5s → 15–30s və ya seed-i arxa planda işlət (`202 Accepted` + background job).
- Retry/outbox (Polly exponential backoff və ya DB-based outbox).
- `DangerousAcceptAnyServerCertificateValidator`-u production-da sil.
- `Webhook:SharedSecret` / `InternalCommunication:ApiKey` açarını **rotate et** (brauzer bundle-ında açıq göründü, bax Bölmə 5), env/secret manager-də saxla və hər modul üçün ayrı açar istifadə et.
- `TenantStatusMiddleware` / tenant yaradılma axınında "Accounting modulu aktivdirsə, `user-created` göndərilib və seed uğurla tamamlanıb" yoxlaması + monitorinq.

---

## 7. Tətbiq sırası (canlı mühit üçün)

| Addım | İş | Risk | Nəticə |
|-------|----|------|--------|
| 1 | Webhook açarını rotate et (B5) | Aşağı | Təhlükəsizlik boşluğu bağlanır |
| 2 | Həll A: bu tenant üçün hesabları/default-ları əl ilə düzəlt | Orta (backup şərt) | **4 xəta dərhal aradan qalxır** |
| 3 | B1 + B2: idempotent seed + webhook düzəlişi, deploy | Orta | Yeni/yarımçıq tenant-lər avtomatik düzəlir |
| 4 | B3: posting məntiq düzəlişləri (COGS/GRNI/fallback) | Orta | Yanlış blok və yanlış hesaba yazılış aradan qalxır |
| 5 | B4: admin endpoint-ləri və xəta kodu | Aşağı | Problemi istifadəçi özü həll edə bilir |
| 6 | B5: Auth tərəfi təkmilləşdirmələri | Aşağı | Təkrar baş verməz |

---

## 8. Həll sonrası gözlənilən davranış və test planı

Tenant-ın hesab planı tam və `Company.Default*` bağlı olduqdan sonra:

| Test | Gözlənilən nəticə |
|------|-------------------|
| Satış qaiməsi post | `Dr 1200 AR (grand total)` / `Cr 6010 Revenue (subtotal)` / `Cr 2250 Output VAT (tax)`; status → `Posted` |
| Mal qəbulu post | `Dr 1100 Stock` / `Cr 2200 GRNI`; stok mühərriki hərəkət yazır; PO `FullyReceived` |
| Alış qaiməsi post (GRNI-based) | `Dr 2200 GRNI (subtotal)` + `Dr 1250 Input VAT` / `Cr 2100 AP (grand total)` |
| Alış qaiməsi post (birbaşa xərc) | `Dr Expense/Asset` + `Dr 1250` / `Cr 2100` (**GRNI tələb olunmur** – B3.2) |
| Stok mədaxili (Receipt) | `Dr 1100` / `Cr 2200`; **COGS tələb olunmur** (B3.1) |
| Stok məxarici (Issue) | `Dr 7010 COGS` / `Cr 1100 Stock` |
| İkinci dəfə post | `DuplicatePostingException` (mövcud davranış) |
| Webhook-u 10 dəfə çağır | Hər dəfə `200`, `Users` cədvəlində 1 sətir, hesab planında dublikat yoxdur |
| Hesabı olmayan yeni tenant | İlk post-dan əvvəl (və ya yaradılma anında) seed işləyir, post uğurlu olur |

**Növbəti ehtimal olunan problemlər (post xətası aradan qalxanda ortaya çıxa bilər):**

1. **Fiskal dövr:** Seeder yalnız `DateTime.UtcNow.Year` üçün il yaradır. Keçmiş/gələcək ilə post cəhdi `Posting tarixi üçün heç bir maliyyə dövrü təyin edilməyib` xətası verir. Backend-də posting zamanı il yoxdursa avtomatik yaratmaq (və ya aydın xəta kodu qaytarmaq) lazımdır. Frontend-in müvəqqəti workaround-u: [FRONTEND_REVIEW.md](FRONTEND_REVIEW.md), F3.4.
2. **GRN `Qəbul miqdarı 0`** (Problem 2-də qeyd olundu).
3. **`LedgerEntry.TransactionCurrency`** GRN və stok jurnallarında doldurulmur (`ProcurementService.cs:361-380`, `InventoryService.cs:330-347`) – çox valyutalı hesabatlarda boş valyuta görünə bilər.
4. **Tək `SaveChanges` / atomiklik:** `PostGoodsReceiptAsync`-də stok mühərriki `PostBatchAsync`-dən **əvvəl** işləyir. Əgər sonrakı addım yıxılarsa və transaction yoxdursa, stok hərəkəti yazılıb jurnal yazılmamış qala bilər. Posting hər halda bir DB transaction-ı daxilində olmalıdır (bu review-ın növbəti hissəsində ayrıca yoxlanacaq).

---

## 9. Növbəti addım

Növbəti problemləri bir-bir göndər, hər birini bu fayla ayrı bölmə kimi əlavə edəcəyəm.
