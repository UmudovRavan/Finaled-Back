using AltensorAuthService.Domain.Entities;
using AltensorAuthService.Domain.Enums;
using AltensorAuthService.Persistence.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AltensorAuthService.Persistence.Data.Seed
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

            try
            {
                logger.LogInformation("Verilənlər bazası seed prosesi başlayır...");

                // 1. Seed System Modules
                var modules = new List<SistemModule>
                {
                    new SistemModule
                    {
                        Code = "crm",
                        Name = "CRM Modulu",
                        Description = "Müştəri münasibətlərinin və satışların idarə edilməsi",
                        IsActive = true
                    },
                    new SistemModule
                    {
                        Code = "inventory",
                        Name = "Anbar (Inventory) Modulu",
                        Description = "Məhsul, anbar qalıqları və transferlərin idarə edilməsi",
                        IsActive = true
                    },
                    new SistemModule
                    {
                        Code = "hr",
                        Name = "İnsan Resursları (HR) Modulu",
                        Description = "İşçilər, davamiyyət və məzuniyyətlərin idarə edilməsi",
                        IsActive = true
                    },
                    new SistemModule
                    {
                        Code = "accounting",
                        Name = "Mühasibatlıq (Accounting) Modulu",
                        Description = "Maliyyə əməliyyatları, hesab-fakturalar və hesabatlar",
                        IsActive = true
                    },
                    new SistemModule
                    {
                        Code = "tms",
                        Name = "Task Management (TMS) Modulu",
                        Description = "Tapşırıqların, iş qruplarının və icraçıların idarə edilməsi",
                        IsActive = true
                    }
                };

                foreach (var module in modules)
                {
                    var existingModule = await context.Modules.FirstOrDefaultAsync(m => m.Code == module.Code);
                    if (existingModule == null)
                    {
                        await context.Modules.AddAsync(module);
                    }
                }
                await context.SaveChangesAsync();

                // Reload modules to get database IDs
                var crmModule = await context.Modules.FirstAsync(m => m.Code == "crm");
                var invModule = await context.Modules.FirstAsync(m => m.Code == "inventory");
                var hrModule = await context.Modules.FirstAsync(m => m.Code == "hr");
                var accModule = await context.Modules.FirstAsync(m => m.Code == "accounting");
                var tmsModule = await context.Modules.FirstAsync(m => m.Code == "tms");

                // 2. Seed Permissions
                var permissions = new List<Permission>
                {
                    // CRM - Broad / Fallback
                    new Permission { Code = "crm.read", Name = "CRM Baxış", Description = "CRM məlumatlarını oxumaq", ModuleId = crmModule.Id },
                    new Permission { Code = "crm.write", Name = "CRM Redaktə", Description = "CRM məlumatları yaratmaq/dəyişmək", ModuleId = crmModule.Id },
                    new Permission { Code = "crm.delete", Name = "CRM Silmə", Description = "CRM qeydlərini silmək", ModuleId = crmModule.Id },

                    // CRM - Granular Contacts
                    new Permission { Code = "crm.contacts.view", Name = "Kontaktlara Baxış", Description = "Kontakt məlumatlarını oxumaq", ModuleId = crmModule.Id },
                    new Permission { Code = "crm.contacts.create", Name = "Kontakt Yaratma", Description = "Yeni kontakt yaratmaq", ModuleId = crmModule.Id },
                    new Permission { Code = "crm.contacts.update", Name = "Kontakt Redaktə", Description = "Kontakt məlumatlarını yeniləmək", ModuleId = crmModule.Id },
                    new Permission { Code = "crm.contacts.delete", Name = "Kontakt Silmə", Description = "Kontaktı sistemdən silmək", ModuleId = crmModule.Id },

                    // CRM - Granular Leads
                    new Permission { Code = "crm.leads.view", Name = "Leydlərə Baxış", Description = "Leyd məlumatlarını oxumaq", ModuleId = crmModule.Id },
                    new Permission { Code = "crm.leads.create", Name = "Leyd Yaratma", Description = "Yeni leyd yaratmaq", ModuleId = crmModule.Id },
                    new Permission { Code = "crm.leads.update", Name = "Leyd Redaktə", Description = "Leyd məlumatlarını yeniləmək və sövdələşməyə çevirmək", ModuleId = crmModule.Id },
                    new Permission { Code = "crm.leads.delete", Name = "Leyd Silmə", Description = "Leydi sistemdən silmək", ModuleId = crmModule.Id },

                    // CRM - Granular Deals
                    new Permission { Code = "crm.deals.view", Name = "Sövdələşmələrə Baxış", Description = "Sövdələşmələri oxumaq", ModuleId = crmModule.Id },
                    new Permission { Code = "crm.deals.create", Name = "Sövdələşmə Yaratma", Description = "Yeni sövdələşmə yaratmaq", ModuleId = crmModule.Id },
                    new Permission { Code = "crm.deals.update", Name = "Sövdələşmə Redaktə", Description = "Sövdələşmə məlumatlarını və mərhələsini yeniləmək", ModuleId = crmModule.Id },
                    new Permission { Code = "crm.deals.delete", Name = "Sövdələşmə Silmə", Description = "Sövdələşməni sistemdən silmək", ModuleId = crmModule.Id },

                    // CRM - Granular Organizations
                    new Permission { Code = "crm.organizations.view", Name = "Təşkilatlara Baxış", Description = "Təşkilat məlumatlarını oxumaq", ModuleId = crmModule.Id },
                    new Permission { Code = "crm.organizations.create", Name = "Təşkilat Yaratma", Description = "Yeni təşkilat yaratmaq", ModuleId = crmModule.Id },
                    new Permission { Code = "crm.organizations.update", Name = "Təşkilat Redaktə", Description = "Təşkilat məlumatlarını yeniləmək", ModuleId = crmModule.Id },
                    new Permission { Code = "crm.organizations.delete", Name = "Təşkilat Silmə", Description = "Təşkilatı sistemdən silmək", ModuleId = crmModule.Id },

                    // CRM - Granular Products
                    new Permission { Code = "crm.products.view", Name = "Məhsullara Baxış", Description = "Məhsul kataloqunu oxumaq", ModuleId = crmModule.Id },
                    new Permission { Code = "crm.products.create", Name = "Məhsul Yaratma", Description = "Yeni məhsul yaratmaq", ModuleId = crmModule.Id },
                    new Permission { Code = "crm.products.update", Name = "Məhsul Redaktə", Description = "Məhsul məlumatlarını yeniləmək", ModuleId = crmModule.Id },
                    new Permission { Code = "crm.products.delete", Name = "Məhsul Silmə", Description = "Məhsulu sistemdən silmək", ModuleId = crmModule.Id },

                    // CRM - Granular Tasks
                    new Permission { Code = "crm.tasks.view", Name = "CRM Tapşırıqlara Baxış", Description = "CRM daxili tapşırıqları oxumaq", ModuleId = crmModule.Id },
                    new Permission { Code = "crm.tasks.create", Name = "CRM Tapşırıq Yaratma", Description = "CRM daxili tapşırıq yaratmaq", ModuleId = crmModule.Id },
                    new Permission { Code = "crm.tasks.update", Name = "CRM Tapşırıq Redaktə", Description = "CRM daxili tapşırıq və yoxlama siyahısını yeniləmək", ModuleId = crmModule.Id },
                    new Permission { Code = "crm.tasks.delete", Name = "CRM Tapşırıq Silmə", Description = "CRM daxili tapşırıq silmək", ModuleId = crmModule.Id },

                    // CRM - Granular Notes
                    new Permission { Code = "crm.notes.view", Name = "CRM Qeydlərə Baxış", Description = "CRM qeydlərini oxumaq", ModuleId = crmModule.Id },
                    new Permission { Code = "crm.notes.create", Name = "CRM Qeyd Yaratma", Description = "Yeni CRM qeydi yaratmaq", ModuleId = crmModule.Id },
                    new Permission { Code = "crm.notes.update", Name = "CRM Qeyd Redaktə", Description = "CRM qeydini yeniləmək", ModuleId = crmModule.Id },
                    new Permission { Code = "crm.notes.delete", Name = "CRM Qeyd Silmə", Description = "CRM qeydini silmək", ModuleId = crmModule.Id },

                    // CRM - Settings
                    new Permission { Code = "crm.settings.manage", Name = "CRM Parametrlərini İdarəetmə", Description = "CRM sistem konfiqurasiyalarını redaktə etmək", ModuleId = crmModule.Id },

                    // Inventory
                    new Permission { Code = "inventory.read", Name = "Anbar Baxış", Description = "Anbar və məhsul qalıqlarını oxumaq", ModuleId = invModule.Id },
                    new Permission { Code = "inventory.write", Name = "Anbar Redaktə", Description = "Məhsul və anbar əməliyyatları yaratmaq", ModuleId = invModule.Id },
                    new Permission { Code = "inventory.delete", Name = "Anbar Silmə", Description = "Anbar qeydlərini silmək", ModuleId = invModule.Id },
                    new Permission { Code = "inventory.approve_transfer", Name = "Transfer Təsdiqi", Description = "Anbarlararası transferləri təsdiq etmək", ModuleId = invModule.Id },

                    // HR
                    new Permission { Code = "hr.read", Name = "HR Baxış", Description = "İşçi məlumatlarına baxış", ModuleId = hrModule.Id },
                    new Permission { Code = "hr.write", Name = "HR Redaktə", Description = "İşçi məlumatlarını redaktə etmək", ModuleId = hrModule.Id },
                    new Permission { Code = "hr.delete", Name = "HR Silmə", Description = "İşçi qeydlərini silmək", ModuleId = hrModule.Id },
                    new Permission { Code = "hr.approve_leave", Name = "Məzuniyyət Təsdiqi", Description = "Məzuniyyət müraciətlərini təsdiq etmək", ModuleId = hrModule.Id },

                    // Accounting
                    new Permission { Code = "accounting.read", Name = "Mühasibatlıq Baxış", Description = "Maliyyə qeydlərinə baxış", ModuleId = accModule.Id },
                    new Permission { Code = "accounting.write", Name = "Mühasibatlıq Redaktə", Description = "Maliyyə qeydlərini redaktə etmək", ModuleId = accModule.Id },
                    new Permission { Code = "accounting.delete", Name = "Mühasibatlıq Silmə", Description = "Maliyyə qeydlərini silmək", ModuleId = accModule.Id },
                    new Permission { Code = "accounting.approve_payment", Name = "Ödəniş Təsdiqi", Description = "Ödənişləri təsdiq etmək", ModuleId = accModule.Id },

                    // TMS (Task Management) - Granular Tasks
                    new Permission { Code = "tms.tasks.view", Name = "Tapşırıqlara Baxış", Description = "Tapşırıqları oxumaq və görüntüləmək", ModuleId = tmsModule.Id },
                    new Permission { Code = "tms.tasks.create", Name = "Tapşırıq Yaratma", Description = "Yeni tapşırıq yaratmaq", ModuleId = tmsModule.Id },
                    new Permission { Code = "tms.tasks.update", Name = "Tapşırıq Redaktə", Description = "Tapşırıq məlumatlarını yeniləmək", ModuleId = tmsModule.Id },
                    new Permission { Code = "tms.tasks.delete", Name = "Tapşırıq Silmə", Description = "Tapşırıqları sistemdən silmək", ModuleId = tmsModule.Id },
                    new Permission { Code = "tms.tasks.assign", Name = "Tapşırıq Təyin Etmə", Description = "Tapşırığı istifadəçiyə təyin etmək və ya geri almaq", ModuleId = tmsModule.Id },
                    new Permission { Code = "tms.tasks.status_manage", Name = "Tapşırıq Statusunu İdarəetmə", Description = "Tapşırığı qəbul etmək, rədd etmək, bitirmək və yenidən açmaq", ModuleId = tmsModule.Id },
                    new Permission { Code = "tms.tasks.comment", Name = "Tapşırıq Şərhləri", Description = "Tapşırığa şərh yazmaq və müzakirə aparmaq", ModuleId = tmsModule.Id },
                    new Permission { Code = "tms.tasks.attachment.view", Name = "Fayl Əlavələrinə Baxış", Description = "Tapşırıq fayllarını yükləmək və önizləmək", ModuleId = tmsModule.Id },
                    new Permission { Code = "tms.tasks.attachment.upload", Name = "Fayl Əlavəsi Yükləmə", Description = "Tapşırığa yeni fayllar əlavə etmək", ModuleId = tmsModule.Id },

                    // TMS - Granular WorkGroups
                    new Permission { Code = "tms.workgroups.view", Name = "İş Qruplarına Baxış", Description = "İş qruplarını oxumaq və detallarına baxmaq", ModuleId = tmsModule.Id },
                    new Permission { Code = "tms.workgroups.create", Name = "İş Qrupu Yaratma", Description = "Yeni iş qrupu yaratmaq", ModuleId = tmsModule.Id },
                    new Permission { Code = "tms.workgroups.update", Name = "İş Qrupu Redaktə", Description = "İş qrupu məlumatlarını dəyişmək", ModuleId = tmsModule.Id },
                    new Permission { Code = "tms.workgroups.delete", Name = "İş Qrupu Silmə", Description = "İş qrupunu silmək", ModuleId = tmsModule.Id },
                    new Permission { Code = "tms.workgroups.members_manage", Name = "İş Qrupu Üzvlərini İdarəetmə", Description = "İş qrupuna istifadəçi əlavə etmək və ya çıxarmaq", ModuleId = tmsModule.Id },
                    new Permission { Code = "tms.workgroups.task_assign", Name = "İş Qrupuna Tapşırıq Təyini", Description = "İş qrupuna tapşırıq yönləndirmək", ModuleId = tmsModule.Id },

                    // TMS - Performance & Notifications
                    new Permission { Code = "tms.performance.view", Name = "Performansa Baxış", Description = "Performans statistikasına və lider lövhəsinə baxış", ModuleId = tmsModule.Id },
                    new Permission { Code = "tms.notifications.view", Name = "Bildirişlərə Baxış", Description = "İstifadəçi bildirişlərinə baxış və oxundu qeyd etmə", ModuleId = tmsModule.Id }
                };

                foreach (var perm in permissions)
                {
                    var existing = await context.Permissions.FirstOrDefaultAsync(p => p.Code == perm.Code);
                    if (existing == null)
                    {
                        await context.Permissions.AddAsync(perm);
                    }
                }
                await context.SaveChangesAsync();

                // 3. Seed System Roles
                var allPermissions = await context.Permissions.ToListAsync();

                // PlatformSuperAdmin
                const string superAdminRoleName = "PlatformSuperAdmin";
                var superAdminRole = await roleManager.FindByNameAsync(superAdminRoleName);
                if (superAdminRole == null)
                {
                    superAdminRole = new ApplicationRole
                    {
                        Name = superAdminRoleName,
                        NormalizedName = superAdminRoleName.ToUpper(),
                        IsSystemRole = true,
                        Description = "Platforma Super Administratoru — tam qlobal idarəetmə hüququ.",
                        TenantId = null
                    };
                    await roleManager.CreateAsync(superAdminRole);

                    // Assign all permissions
                    foreach (var p in allPermissions)
                    {
                        await context.RolePermissions.AddAsync(new RolePermission
                        {
                            RoleId = superAdminRole.Id,
                            PermissionId = p.Id
                        });
                    }
                    await context.SaveChangesAsync();
                }

                // TenantAdmin
                const string tenantAdminRoleName = "TenantAdmin";
                var tenantAdminRole = await roleManager.FindByNameAsync(tenantAdminRoleName);
                if (tenantAdminRole == null)
                {
                    tenantAdminRole = new ApplicationRole
                    {
                        Name = tenantAdminRoleName,
                        NormalizedName = tenantAdminRoleName.ToUpper(),
                        IsSystemRole = true,
                        Description = "Tenant Administratoru — öz müştərisi daxilində tam idarəetmə hüququ.",
                        TenantId = null
                    };
                    await roleManager.CreateAsync(tenantAdminRole);

                    // Assign all permissions to default TenantAdmin
                    foreach (var p in allPermissions)
                    {
                        await context.RolePermissions.AddAsync(new RolePermission
                        {
                            RoleId = tenantAdminRole.Id,
                            PermissionId = p.Id
                        });
                    }
                    await context.SaveChangesAsync();
                }

                // 4. Seed Platform System Tenant & Platform Super Admin User
                var platformTenantSlug = "platform";
                var platformTenant = await context.Tenants.FirstOrDefaultAsync(t => t.Slug == platformTenantSlug);
                if (platformTenant == null)
                {
                    platformTenant = new Tenant
                    {
                        Name = "Altensor Platform HQ",
                        Slug = platformTenantSlug,
                        Domain = "platform.altensor.io",
                        Status = TenantStatus.Active
                    };
                    await context.Tenants.AddAsync(platformTenant);
                    await context.SaveChangesAsync();

                    // Subscribe to all modules
                    foreach (var m in modules)
                    {
                        var dbMod = await context.Modules.FirstAsync(x => x.Code == m.Code);
                        await context.TenantModuleSubscriptions.AddAsync(new TenantModuleSubscription
                        {
                            TenantId = platformTenant.Id,
                            ModuleId = dbMod.Id,
                            Status = SubscriptionStatus.Active,
                            StartsAt = DateTime.UtcNow
                        });
                    }
                    await context.SaveChangesAsync();
                }

                // Platform Super Admin User
                var superAdminEmail = configuration["Platform:SuperAdminEmail"] ?? "superadmin@altensor.io";
                var superAdminPassword = configuration["Platform:SuperAdminPassword"] ?? "SuperAdmin@2026!";

                var superAdminUser = await userManager.FindByEmailAsync(superAdminEmail);
                if (superAdminUser == null)
                {
                    superAdminUser = new ApplicationUser
                    {
                        UserName = superAdminEmail,
                        Email = superAdminEmail,
                        FullName = "Platform Super Admin",
                        TenantId = platformTenant.Id,
                        IsActive = true
                    };

                    var result = await userManager.CreateAsync(superAdminUser, superAdminPassword);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(superAdminUser, superAdminRoleName);
                        logger.LogInformation("Platform Super Admin istifadəçisi uğurla yaradıldı: {Email}", superAdminEmail);
                    }
                    else
                    {
                        logger.LogError("Super Admin yaradılarkən xəta: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }

                logger.LogInformation("Verilənlər bazası seed prosesi uğurla tamamlandı.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Seed prosesi zamanı xəta baş verdi: {Message}", ex.Message);
            }
        }
    }
}
