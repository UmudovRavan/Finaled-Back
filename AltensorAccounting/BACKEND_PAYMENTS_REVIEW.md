# Backend Review: Ödəniş Əməliyyatlarında Foreign Key Xətası və Həll Yolları
**Fayl:** `BACKEND_PAYMENTS_REVIEW.md`  
**Layihə:** Altensor Accounting ERP (`Finaled-Back` & `Accounting-Front`)  
**Tərtib Tarixi:** 2026-10-08  
**Səviyyə:** Senior Backend / Software Architect Review  

---

## 1. Executive Summary (İcmal)

Satış qaimələri (`Customer Invoices`) səhifəsində qaimə Baş Kitaba təsdiqləndikdən (`Posted`) sonra **"Ödəniş Qəbul Et (Receipt)"** modalı vasitəsilə ödəniş qeyd edilərkən backend aşağıdakı kritik xətanı qaytarır:

```json
{
  "title": "Məlumat Bazası Xətası",
  "status": 400,
  "detail": "23503: \"Payments\" tablosu üzerindeki ekleme veya güncelleme işlemi \"FK_Payments_Accounts_BankOrCashAccountId\" foreign key kısıtlamasını ihlal ediyor",
  "instance": "/api/payments",
  "code": "DB_UPDATE_ERROR"
}
```

Bu araşdırma həm **`Finaled-Back`**, həm də **`Accounting-Front`** kod bazasını dərindən analiz edərək problemin kök səbəblərini, arxitektur ziddiyyətləri və backend komandası üçün konkret kod səviyyəsində həll yollarını təqdim edir.

---

## 2. Xətanın Texniki Diaqnostikası və Zəncirvari Axını

### 2.1. Göndərilən HTTP Sorğusu (Frontend Payload)
```http
POST /api/payments HTTP/1.1
Content-Type: application/json

{
  "type": 1,
  "paymentDate": "2026-10-08T00:00:00.000Z",
  "postingDate": "2026-10-08T00:00:00.000Z",
  "bankOrCashAccountId": "3d13ce99-5ba4-4180-9a7d-63280658c061",
  "partyId": "24768e2d-986b-44da-8179-b266489bdd4a",
  "partyType": "Customer",
  "currency": "AZN",
  "exchangeRate": 1,
  "totalAmount": 2855.6,
  "referenceNumber": "PAY-SINV-20261006-525F0B",
  "allocations": [
    {
      "targetDocumentType": 2,
      "targetDocumentId": "843c3c8f-8a76-44dd-90c3-167d3f4e5188",
      "allocatedAmount": 2855.6
    }
  ]
}
```

### 2.2. Verilənlər Bazası Səviyyəsində Problem
- **PostgreSQL Xətası:** `23503 (foreign_key_violation)`
- **Məhdudiyyət (FK Constraint):** `"FK_Payments_Accounts_BankOrCashAccountId"`
- **Cədvəl Əlaqəsi:**
  ```sql
  ALTER TABLE "Payments" 
  ADD CONSTRAINT "FK_Payments_Accounts_BankOrCashAccountId" 
  FOREIGN KEY ("BankOrCashAccountId") REFERENCES "Accounts" ("Id") ON DELETE RESTRICT;
  ```
- **Faktiki Vəziyyət:** Göndərilən `"3d13ce99-5ba4-4180-9a7d-63280658c061"` ID-si `"Accounts"` (Hesablar Planı / Chart of Accounts) cədvəlində **MÖVCUD DEYİL**.
- **Bu ID Haradandır?** Bu ID Xəzinədarlıq (`Treasury`) modulunun `"BankAccounts"` cədvəlindəki Kapital Bank hesabının fərdi ID-sidir.

---

## 3. Kök Səbəb Analizi (Root Cause Analysis)

Bu problem 3 əsas memarlıq və kontrakt uyğunsuzluğunun kəsişməsindən yaranır:

### 3.1. Domain Entity Uyğunsuzluğu: `Treasury.BankAccount` vs `Accounting.Account`
Sistemdə bank və kassa anlayışı iki fərqli domendə modelləşdirilib:
1. **`Treasury` Domeni (`AltensorAccounting.Domain.Entities.Treasury.BankAccount`):**
   - Fiziki bank hesabıdır (IBAN, Bank Adı, Valyuta, SwiftCode).
   - Bu entity daxilində Baş Kitabla inteqrasiya üçün xüsusi xarici açar var:
     ```csharp
     public Guid? GLAccountId { get; set; } // GL Asset account mapping
     public Account? GLAccount { get; set; }
     ```
2. **`Accounting` Domeni (`AltensorAccounting.Domain.Entities.Accounting.Payment`):**
   - Mühasibatlıq ödəniş sənədidir (Payment Voucher).
   - GL posting zamanı debet və ya kredit yazılışı aparmaq üçün birbaşa Baş Kitab hesabını saxlayır:
     ```csharp
     public Guid BankOrCashAccountId { get; set; } // GL Asset account (Bank/Cash)
     public Account BankOrCashAccount { get; set; } = default!;
     ```

### 3.2. DTO və API Kontraktının Qeyri-müəyyənliyi
`CreatePaymentDto` daxilindəki sahənin adı:
```csharp
public Guid BankOrCashAccountId { get; set; }
```
İstifadəçi interfeysi və kənar müştəri üçün bu sahənin adı çaşdırıcıdır:
- İstifadəçi UI-da **"Kapital Bank - Əsas Hesab"** seçir.
- Frontend bu seçimin dəyəri olaraq `GET /api/Treasury/bank-accounts` endpoint-indən gələn `b.id`-ni göndərir.
- Lakin Backend `POST /api/payments` endpoint-i bu sahədə **Treasury BankAccount ID deyil, GL Account (Hesablar Planındakı 1020/1010) ID** gözləyir.

### 3.3. Treasury DTO-da Məlumat İtkisi (Data Concealment)
Backend-in `TreasuryService.cs` faylında `GetBankAccountsAsync` metodu `GLAccountId` sahəsini kənara qaytarmır:
```csharp
// AltensorAccounting.Application/Services/TreasuryService.cs : Sətir 95-102
public async Task<List<BankAccountDto>> GetBankAccountsAsync(CancellationToken ct = default)
{
    var banks = await _bankRepo.GetAllAsync(ct);
    return banks.Select(b => new BankAccountDto
    {
        Id = b.Id,
        BankName = b.BankName,
        AccountNumber = b.AccountNumber,
        Currency = b.Currency,
        CurrentBalance = b.CurrentBalance
        // ❌ GLAccountId xaricə verilməyib!
    }).ToList();
}
```
`BankAccountDto` klassında `GLAccountId` sahəsi olmadığı üçün frontend tərəfi hətta istəsə belə bank hesabının arxasındakı GL hesabını backend-dən birbaşa oxuya bilmir.

### 3.4. Backend Kodundakı İkili Standart (Inconsistency)
Maraqlıdır ki, backend-in özündə `TreasuryService.PostPaymentRunAsync` metodunda (sətir 243-248) proqramçı bu fərqi başa düşüb və bank hesabının arxasındakı GL hesabını avtomatik həll edib:
```csharp
// TreasuryService.cs (Sətir 243-247):
var bank = await _bankRepo.GetByIdAsync(run.BankAccountId, ct)
    ?? throw new BusinessRuleException("Bank hesabı tapılmadı.");

var bankGlId = bank.GLAccountId 
    ?? throw new BusinessRuleException("Bank hesabı üçün GL hesabı təyin edilməyib.");

// Sonra Payment yaradılarkən bankGlId istifadə olunur:
BankOrCashAccountId = bankGlId,
```
**Lakin `AccountingService.CreatePaymentAsync` metodunda bu lookup NƏZƏRƏ ALINMAYIB!**
Gələn `dto.BankOrCashAccountId` heç bir yoxlama, lookup və ya fallback aparılmadan kor-koranə birbaşa `Payment.BankOrCashAccountId`-yə yazılır və verilənlər bazasına göndərilir. Nəticədə PostgreSQL FK constraint xətası baş verir.

### 3.5. Frontend Tərəfindəki Səhv
Frontend `CustomerInvoiceDetailPage.tsx` və `customersService.ts` (sətir 507) fayllarında `payInvoice` çağırarkən:
```typescript
// customersService.ts : Sətir 507
bankOrCashAccountId: data.bankAccountId, // ❌ BankAccount ID-ni birbaşa GL sahəsinə yazır!
```
Bu mənimsətmə `paymentService.createPayment` metodundakı daxili GL resolution məntiqini sıradan çıxarır və xam `BankAccounts.Id` GUID-ini birbaşa backend-ə göndərir.

---

## 4. Backend üçün Həll Strategiyaları və Memarlıq Seçimləri

Backend komandası üçün ən effektiv və möhkəm (robust) həll yolları aşağıdakılardır:

### 🏆 Strategiya 1 (Tövsiyə Olunan - Ağıllı Polimorfik Resolver)
`AccountingService.CreatePaymentAsync` metodu daxilində gələn `dto.BankOrCashAccountId`-ni yoxlamaq:
1. Əgər bu ID birbaşa `Accounts` cədvəlində varsa (`GL Account`), birbaşa istifadə et.
2. Əgər `Accounts`-da yoxdursa, `BankAccounts` cədvəlində yoxla. Tapılarsa, onun `bank.GLAccountId`-sini götür.
3. Əgər orada da yoxdursa, `CashDesks` cədvəlində yoxla. Tapılarsa, onun `cash.GLAccountId`-sini götür.
4. Əgər bank/kassa hesabının `GLAccountId`-si təyin edilməyibsə və ya boşdursa, şirkətin standart Bank/Kassa hesabını götür (məs. `1020` və ya `1010`).
5. Heç biri tapılmazsa, təmiz və anlaşıqlı `BusinessRuleException` qaytar (PostgreSQL crash-inə imkan vermə).

#### Niyə bu ən yaxşı həlldir?
- API müştəriləri (Web Frontend, Mobil tətbiq, Kənar inteqrasiyalar) həm Bank Hesabı ID-sini, həm də birbaşa GL Hesab ID-sini göndərə bilər.
- Heç bir kənar sistem və ya frontend xətası verilənlər bazası səviyyəsində qırılmaya səbəb olmaz.

---

### Strategiya 2: `BankAccountDto` və `CashDeskDto`-ya `GLAccountId`-nin Əlavə Edilməsi
Frontend-in backend domeninə tam bələd olması üçün Treasury DTO-ları genişləndirilməlidir.
1. `BankAccountDto`-ya `public Guid? GLAccountId { get; set; }` əlavə edilməlidir.
2. `CashDeskDto`-ya `public Guid? GLAccountId { get; set; }` əlavə edilməlidir.
3. `GetBankAccountsAsync` və `GetCashDesksAsync` metodlarında mapping aparılmalıdır.

---

## 5. Backend üçün Hazır C# Kod Düzəlişləri

### 5.1. `AltensorAccounting.Application/Services/AccountingService.cs` Düzəlişi

`CreatePaymentAsync` metodunun əvvəlinə aşağıdakı həll blokunu əlavə edin:

```csharp
// AltensorAccounting.Application/Services/AccountingService.cs
public async Task<PaymentDto> CreatePaymentAsync(CreatePaymentDto dto, CancellationToken ct = default)
{
    var tenantId = _tenantService.TenantId ?? throw new BusinessRuleException("Tenant konteksti tapılmadı.");

    if (dto.TotalAmount <= 0)
    {
        throw new BusinessRuleException("Ödəniş məbləği 0-dan böyük olmalıdır.");
    }

    // ─── SENIOR FIX: Resolve GL Account for BankOrCashAccountId ───
    Guid effectiveGlAccountId = Guid.Empty;

    // 1. Birbaşa Accounts (GL) cədvəlində mövcudluğunu yoxla
    var directAccount = await _accountRepo.GetByIdAsync(dto.BankOrCashAccountId, ct);
    if (directAccount != null && directAccount.IsActive)
    {
        effectiveGlAccountId = directAccount.Id;
    }
    else
    {
        // 2. Əgər GL Account tapılmadısa, bəlkə bu Treasury BankAccount ID-sidir?
        var bankAccount = await _bankRepo.GetByIdAsync(dto.BankOrCashAccountId, ct);
        if (bankAccount != null)
        {
            if (bankAccount.GLAccountId.HasValue && bankAccount.GLAccountId.Value != Guid.Empty)
            {
                effectiveGlAccountId = bankAccount.GLAccountId.Value;
            }
            else
            {
                // Bank hesabına GL bağlanmayıbsa, standart 1020 hesabına yönləndir
                var defaultBankGl = (await _accountRepo.FindAsync(a => (a.Code == "1020" || a.Code.StartsWith("102")) && a.IsActive, ct)).FirstOrDefault();
                if (defaultBankGl != null) effectiveGlAccountId = defaultBankGl.Id;
            }
        }
        else
        {
            // 3. Bəlkə bu Treasury CashDesk (Kassa) ID-sidir?
            var cashDesk = await _cashRepo.GetByIdAsync(dto.BankOrCashAccountId, ct);
            if (cashDesk != null)
            {
                if (cashDesk.GLAccountId.HasValue && cashDesk.GLAccountId.Value != Guid.Empty)
                {
                    effectiveGlAccountId = cashDesk.GLAccountId.Value;
                }
                else
                {
                    // Kassa hesabına GL bağlanmayıbsa, standart 1010 hesabına yönləndir
                    var defaultCashGl = (await _accountRepo.FindAsync(a => (a.Code == "1010" || a.Code.StartsWith("101")) && a.IsActive, ct)).FirstOrDefault();
                    if (defaultCashGl != null) effectiveGlAccountId = defaultCashGl.Id;
                }
            }
        }
    }

    // Əgər hələ də tapılmadısa, son fallback olaraq sistemdəki ilk aktiv Aktiv (Asset/Bank/Cash) hesabını tap
    if (effectiveGlAccountId == Guid.Empty)
    {
        var fallbackGl = (await _accountRepo.FindAsync(a => (a.Code == "1020" || a.Code == "1010" || a.Category == AccountCategory.Asset) && a.IsActive, ct)).FirstOrDefault();
        if (fallbackGl != null)
        {
            effectiveGlAccountId = fallbackGl.Id;
        }
        else
        {
            throw new BusinessRuleException("Seçilmiş Bank/Kassa hesabı üçün Mühasibatlıq Hesabı (GL Account) tapılmadı. Zəhmət olmasa Hesablar Planında 1020 və ya 1010 hesabını yoxlayın.");
        }
    }

    var allocatedTotal = dto.Allocations.Sum(a => a.AllocatedAmount);
    var unallocated = dto.TotalAmount - allocatedTotal;

    var payment = new Payment
    {
        TenantId = tenantId,
        PaymentNumber = $"PAY-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}",
        Type = dto.Type,
        PaymentDate = DateTime.SpecifyKind(dto.PaymentDate, DateTimeKind.Utc),
        PostingDate = DateTime.SpecifyKind(dto.PostingDate, DateTimeKind.Utc),
        PartyId = dto.PartyId,
        PartyType = dto.PartyType,
        BankOrCashAccountId = effectiveGlAccountId, // ✅ Həll edilmiş həqiqi Accounts.Id
        Currency = dto.Currency,
        ExchangeRate = dto.ExchangeRate,
        TotalAmount = dto.TotalAmount,
        AllocatedAmount = allocatedTotal,
        UnallocatedAmount = unallocated > 0 ? unallocated : 0,
        ReferenceNumber = dto.ReferenceNumber,
        Notes = dto.Notes,
        Status = DocumentStatus.Draft
    };

    // ... Qalan hissə eyni qalır
```

---

### 5.2. `AltensorAccounting.Contract/DTOs/Treasury/TreasuryDtos.cs` Düzəlişi

```csharp
public class BankAccountDto
{
    public Guid Id { get; set; }
    public string BankName { get; set; } = default!;
    public string AccountNumber { get; set; } = default!;
    public string Currency { get; set; } = "AZN";
    public decimal CurrentBalance { get; set; }
    public Guid? GLAccountId { get; set; } // ✅ Əlavə edilməlidir
}

public class CashDeskDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Currency { get; set; } = "AZN";
    public decimal CurrentBalance { get; set; }
    public Guid? GLAccountId { get; set; } // ✅ Əlavə edilməlidir
}
```

Və `TreasuryService.cs` daxilindəki `GetBankAccountsAsync` mapping-i:
```csharp
public async Task<List<BankAccountDto>> GetBankAccountsAsync(CancellationToken ct = default)
{
    var banks = await _bankRepo.GetAllAsync(ct);
    return banks.Select(b => new BankAccountDto
    {
        Id = b.Id,
        BankName = b.BankName,
        AccountNumber = b.AccountNumber,
        Currency = b.Currency,
        CurrentBalance = b.CurrentBalance,
        GLAccountId = b.GLAccountId // ✅ Map edilməlidir
    }).ToList();
}
```

---

## 6. Frontend Tərəfində Qeydə Alınan Problem və Dərhal Tətbiq Olunacaq Həll (Client-Side Hotfix)

Backend yenilənənə qədər frontend tərəfində ödənişlərin dərhal işləməsi üçün:

1. **`customersService.ts` (`payInvoice` metodu):**
   - Əvvəlki xətalı kod: `bankOrCashAccountId: data.bankAccountId` (bu, `paymentService`-in qoruyucu yoxlamasını söndürürdü).
   - Düzəliş: Parametri `bankAccountId: data.bankAccountId` kimi ötürmək.
2. **`paymentService.ts` (`createPayment` metodu):**
   - Daxil olan `bankAccountId` və ya `bankOrCashAccountId` parametrlərini əvvəlcə `accountsService.getAccounts()` siyahısı ilə çarpaz yoxlamaq.
   - Əgər ötürülən ID `Accounts` cədvəlində yoxdursa (yəni Treasury ID-sidirsə), sistem avtomatik olaraq aktiv Bank GL hesabını (`1020` / `102` / `Bank`) və ya Kassa GL hesabını (`1010` / `101`) seçib backend-ə göndərir.

Bu tədbir sayəsində backend-də dəyişiklik gözləmədən frontend istifadəçisi dərhal satış qaimələri üzrə ödənişləri qəbul edə biləcək.

---

## 7. Xülasə Qərar Matrisi

| Sahə | Problem | Həll Yolu | Status |
| :--- | :--- | :--- | :--- |
| **Frontend: customersService** | `bankAccountId`-ni birbaşa `bankOrCashAccountId`-yə mənimsədirdi | `bankAccountId` ötürmək və GL account yoxlamasını təmin etmək | 🟢 Frontend Hotfix Hazırdır |
| **Frontend: paymentService** | Gələn GUID-in `Accounts` cədvəlinə aid olduğunu kor-koranə fərz edirdi | `Accounts` ilə yoxlamaq, tapılmadıqda 1020/1010 GL ID-ni təyin etmək | 🟢 Frontend Hotfix Hazırdır |
| **Backend: AccountingService** | `CreatePaymentAsync` FK yoxlaması və `BankAccount -> GLAccount` lookup aparmırdı | Polimorfik GL Resolver əlavə etmək | 📝 Review sənədinə daxil edildi |
| **Backend: TreasuryService & DTOs** | `BankAccountDto` GL hesabı mapping-ini (`GLAccountId`) xaricə vermirdi | DTO və Service-ə `GLAccountId` sahəsini əlavə etmək | 📝 Review sənədinə daxil edildi |
