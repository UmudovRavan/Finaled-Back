using System;
using AltensorAccounting.Application.Interfaces;
using AltensorAccounting.Application.Services;
using AltensorAccounting.Application.Services.Posting;
using AltensorAccounting.Application.Services.Procurement;
using AltensorAccounting.Application.Services.Valuation;
using AltensorAccounting.Persistence.Data;
using AltensorAccounting.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AltensorAccounting.Persistence.Extensions;

public static class PersistenceServiceRegistration
{
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Repositories
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Domain Engines
        services.AddScoped<IPostingEngine, PostingEngine>();
        services.AddScoped<IStockValuationEngine, StockValuationEngine>();
        services.AddScoped<IThreeWayMatchService, ThreeWayMatchService>();

        // Application Services
        services.AddScoped<IAccountingService, AccountingService>();
        services.AddScoped<IProcurementService, ProcurementService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<ITreasuryService, TreasuryService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IUserSyncService, UserSyncService>();

        return services;
    }
}
