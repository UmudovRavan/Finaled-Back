using System.Threading;
using System.Threading.Tasks;
using AltensorAccounting.Application.Interfaces;
using AltensorAccounting.Contract.DTOs.Webhooks;
using AltensorAccounting.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AltensorAccounting.Application.Services;

public class UserSyncService : IUserSyncService
{
    private readonly IGenericRepository<User> _userRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserSyncService> _logger;

    public UserSyncService(IGenericRepository<User> userRepo, IUnitOfWork unitOfWork, ILogger<UserSyncService> logger)
    {
        _userRepo = userRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task SyncUserCreatedAsync(UserCreatedIntegrationEvent @event, CancellationToken ct = default)
    {
        var existing = await _userRepo.GetByIdAsync(@event.UserId, ct);
        if (existing != null)
        {
            existing.Email = @event.Email;
            existing.FullName = @event.FullName;
            existing.UserName = @event.UserName;
            await _userRepo.UpdateAsync(existing, ct);
            _logger.LogInformation("[AltensorAccounting] Mövcud istifadəçi yeniləndi: UserId={UserId}, TenantId={TenantId}", @event.UserId, @event.TenantId);
        }
        else
        {
            var user = new User
            {
                Id = @event.UserId,
                TenantId = @event.TenantId,
                Email = @event.Email,
                FullName = @event.FullName,
                UserName = @event.UserName,
                IsActive = true
            };
            await _userRepo.AddAsync(user, ct);
            _logger.LogInformation("[AltensorAccounting] Yeni istifadəçi əlavə edildi: UserId={UserId}, TenantId={TenantId}", @event.UserId, @event.TenantId);
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
