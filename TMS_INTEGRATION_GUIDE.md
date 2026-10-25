# TMS: Üç Analitik Bölgü (Division, Project, Title), Layihəsiz Seçimin Ləğvi və Excel İxracı Bələdçisi

Bu sənəd **Task Management System (TMS)** üzərində edilmiş 3 əsas dəyişikliyi və onların API/Frontend istifadəsini izah edir.

---

## 📌 1. Üç Bölgü (Analitik Ölçülər)

Sistemdə təşkilati və analitik bölgülər vahid standart adlarla təsdiqləndi:
1. **Division (Bölmə / Departament):** Məsələn: *Satış Departamenti*, *IT Departamenti*, *Maliyyə*.
2. **Project (Layihə):** Məsələn: *ERP Tətbiqi*, *CRM İnteqrasiyası*, *Ağ Şəhər Tikintisi*.
3. **Title (Holdinq / Müştəri / Şirkət / MMC):** Məsələn: *A Holdinq*, *B Holdinq*, *X MMC*.

---

## 📌 2. Yeni və Yenilənmiş API Endpoint-ləri

### 2.1. Title (Holdinq / Şirkət / MMC) İdarəetməsi
Baza marşrut: `/api/Title`

* **`GET /api/Title`** — Bütün aktiv Title-ların (holdinqlərin/şirkətlərin) siyahısı (Dropdown üçün).
* **`GET /api/Title/{id}`** — Konkret bir Title məlumatı.
* **`POST /api/Title`** — Yeni Title əlavə etmək.
  ```json
  {
    "name": "A Holdinq",
    "description": "Əsas tərəfdaş holdinq",
    "voen": "1234567890"
  }
  ```
* **`PUT /api/Title/{id}`** — Title məlumatını redaktə etmək.
* **`DELETE /api/Title/{id}`** — Title-ı silmək (Əgər aktiv tapşırığa bağlıdırsa silinməyə qadağa qoyulur).

---

### 2.2. Layihələrin (Project) Siyahısı
Baza marşrut: `/api/Project`

* **`GET /api/Project`** — Layihələr siyahısı.
  * İndi həm `divisionId`, həm də `titleId` üzrə müstəqil filtrləməni dəstəkləyir:
    `GET /api/Project?divisionId={divId}&titleId={titleId}`
  * **Diqqət:** Bu siyahıda heç bir "Layihəsiz" bəndi çıxmır, yalnız real mövcud layihələr qaytarılır.

---

### 2.3. Tapşırıqlarda "Layihəsiz" Seçiminin Aradan Qaldırılması
* **`POST /api/Task/CreateTask`**
  * Əgər `ProjectId` (və ya layihəyə bağlı `LevelId`) göndərilməzsə, backend `400 Bad Request` qaytarır:
    ```json
    {
      "message": "Tapşırıq mütləq bir layihəyə bağlanmalıdır. Layihəsiz seçim mümkün deyil."
    }
    ```
  * Əgər tapşırıq yaradılarkən `DivisionId` və ya `TitleId` boş qalarsa, seçilmiş `Project`-dən avtomatik miras alınır.

---

### 2.4. Müstəqil Filtrləmə və Excel-ə İxrac (Export to Excel)

#### A. Əməliyyatların Müstəqil Filtrlənməsi (JSON)
* **`GET /api/Task/filter`**
* **Query Parametrləri (hamısı optional-dır):**
  * `titleId`: Holdinq / Şirkət ID-si (məs: *A Holdinq*)
  * `divisionId`: Bölmə ID-si (məs: *Satış Departamenti*)
  * `projectId`: Layihə ID-si (məs: *ERP Tətbiqi*)
  * `status`: Status filtri (`Pending`, `Assigned`, `InProgress`, `UnderReview`, `Completed`, `Expired`)
  * `priority`: Prioritet filtri (`Low`, `Normal`, `Urgent`)
  * `fromDeadline` / `toDeadline`: Tarix aralığı
  * `search`: Mətni axtarış

**Nümunə ssenarilər:**
1. Yalnız `A Holdinq`-ə aid bütün işləri görmək:
   `GET /api/Task/filter?titleId=3fa85f64-5717-4562-b3fc-2c963f66afa6`
2. Satış Departamentinin `ERP Tətbiqi` layihəsində `A Holdinq` üzrə işləri:
   `GET /api/Task/filter?titleId={titleId}&divisionId={divId}&projectId={projId}`

---

#### B. Əməliyyatların Excel-ə Yüklənməsi (.xlsx)
* **`GET /api/Task/export-excel`**
* Qəbul edilən parametrlər `GET /api/Task/filter` ilə tamamilə eynidir (`titleId`, `divisionId`, `projectId`, `status` və s.).
* **Geri Dönüş:** `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
* **Avtomatik Fayl Adı:** `Emeliyyatlar_A_Holdinq_20261025_1250.xlsx`
* **Excel Struktur və Formatı:**
  * **Banner Başlıq:** Şirkət adı, hesabat tarixi və tətbiq olunan filtr məlumatları.
  * **Cədvəl Sütunları:**
    1. `№`
    2. `Title (Holdinq / Şirkət)`
    3. `Division (Bölmə)`
    4. `Project (Layihə)`
    5. `Mərhələ (Level)`
    6. `Tapşırıq Adı`
    7. `Təsvir`
    8. `İcraçı Əməkdaş`
    9. `Prioritet`
    10. `Çətinlik`
    11. `Status`
    12. `Son Tarix (Deadline)`
    13. `Yaradılma Tarixi`

---

## 💻 3. Frontend İnteqrasiya Nümunəsi (JavaScript / React / Vue)

```javascript
// 1. Title üzrə Excel faylını endirmək
async function downloadExcelReport(selectedTitleId, selectedDivId, selectedProjId) {
  try {
    const response = await axios.get('/api/Task/export-excel', {
      params: {
        titleId: selectedTitleId,       // Məs: "A Holdinq" ID-si
        divisionId: selectedDivId,      // Optional
        projectId: selectedProjId       // Optional
      },
      responseType: 'blob' // Binary fayl axını üçün mütləqdir
    });

    // Brauzerdə avtomatik fayl kimi endirmək
    const url = window.URL.createObjectURL(new Blob([response.data]));
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', 'Tapsiriqlar_Hesabati.xlsx');
    document.body.appendChild(link);
    link.click();
    link.remove();
  } catch (error) {
    console.error('Excel yüklənərkən xəta baş verdi:', error);
  }
}
```

---

## 📊 4. Çətinlik Kvotası Əsaslı İş Yükü (Workload) İnteqrasiyası

Sistemdə əməkdaşların iş yükünün düzgün tənzimlənməsi üçün **Asan, Orta və Çətin statuslu/dərəcəli tapşırıqların hərəsinə 10 tapşırıq kvotası** tətbiq olunmuşdur:
- **Asan (Easy):** 10 tapşırığa qədər
- **Orta (Medium):** 10 tapşırığa qədər
- **Çətin (Hard):** 10 tapşırığa qədər
- **Ümumi icazə verilən:** 30 tapşırıq (10 Asan + 10 Orta + 10 Çətin). Bu limitlər daxilində əməkdaş **"Overloaded" (Aşırı yüklənmiş) sayılmır**!
- Yalnız hər hansı bir kateqoriyada 10 limiti aşıldıqda (məs. Çətin > 10) və ya ümumi 30-u keçdikdə sistem əməkdaşı **"Overloaded"** statusuna keçirir və tapşırıq təyin edərkən xəbərdarlıq edir.

### API Endpoints:

#### 1. Bütün əməkdaşların iş yükü:
- **GET** `/api/Workload`
- **Response Nümunəsi:**
  ```json
  [
    {
      "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "fullName": "Əli Əliyev",
      "email": "ali@company.az",
      "activeTaskCount": 25,
      "easyTasksCount": 10,
      "mediumTasksCount": 9,
      "hardTasksCount": 6,
      "tasksDueToday": 2,
      "overdueTasks": 0,
      "workloadLevel": "High",
      "workloadScore": 54,
      "isOverloaded": false
    }
  ]
  ```

#### 2. Əməkdaşa tapşırıq təyin etməzdən əvvəl yoxlama:
- **GET** `/api/Workload/check-assign/{userId}`
- **Response Nümunəsi (Limit aşıldıqda):**
  ```json
  {
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "fullName": "Əli Əliyev",
    "activeTaskCount": 31,
    "easyTasksCount": 10,
    "mediumTasksCount": 10,
    "hardTasksCount": 11,
    "tasksDueToday": 1,
    "workloadLevel": "Overloaded",
    "isWarning": true,
    "warningMessage": "⚠️ DİQQƏT: Əməkdaş (Əli Əliyev) hal-hazırda həddindən artıq yüklənib! Aşılmış limitlər: Çətin (11/10), Ümumi (31/30). Aktiv cəmi: 31 (Asan: 10/10, Orta: 10/10, Çətin: 11/10). Yeni tapşırıq təyin etməzdən əvvəl nəzərə alın."
  }
  ```

#### 3. Frontend Təyinat Modalı Yoxlaması:
```javascript
// Əməkdaş seçildikdə iş yükünü yoxlayıb xəbərdarlıq pəncərəsi çıxarmaq
async function onEmployeeSelected(userId) {
  const res = await axios.get(`/api/Workload/check-assign/${userId}`);
  if (res.data.isWarning) {
    // İstifadəçiyə xəbərdarlıq göstər (məs. Toast və ya Modal)
    toast.warning(res.data.warningMessage);
  }
}
```

