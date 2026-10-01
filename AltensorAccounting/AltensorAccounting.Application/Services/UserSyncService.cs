using System.Threading;
using System.Threading.Tasks;
using AltensorAccounting.Application.Interfaces;
using AltensorAccounting.Contract.DTOs.Webhooks;
using AltensorAccounting.Domain.Entities;

namespace AltensorAccounting.Application.Services;

public class UserSyncService : IUserSyncService
{
    private readonly IGenericRepository<User> _userRepo;
    private readonly IUnitOfWork _unitOfWork;

    public UserSyncService(IGenericRepository<User> userRepo, IUnitOfWork unitOfWork)
    {
        _userRepo = userRepo;
        _unitOfWork = unitOfWork;
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
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
