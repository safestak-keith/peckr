using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Peckr.Abstractions
{   
    public interface IPeckResultPoller<T>
    {
        IAsyncEnumerable<PeckResult<T>> PollAsync(
            PeckrSettings settings,
            [EnumeratorCancellation] CancellationToken ct);
    }
}