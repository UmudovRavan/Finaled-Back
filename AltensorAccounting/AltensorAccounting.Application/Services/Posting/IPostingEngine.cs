using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AltensorAccounting.Contract.Services;
using AltensorAccounting.Domain.Entities.Accounting;
using AltensorAccounting.Domain.Enums;
using AltensorAccounting.Domain.Exceptions;

namespace AltensorAccounting.Application.Services.Posting;

public interface IPostingEngine
{
    Task<PostingBatch> PostBatchAsync(PostingBatch batch, CancellationToken ct = default);
    Task<PostingBatch> ReverseBatchAsync(Guid originalBatchId, string reason, DateTime reversalDate, CancellationToken ct = default);
}
