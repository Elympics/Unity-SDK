using System;
using System.Threading;

#nullable enable

namespace Elympics
{
    internal static class ActionExtensions
    {
        public static Action WithCancellation(this Action action, CancellationToken ct) =>
            () =>
            {
                if (!ct.IsCancellationRequested)
                    action.Invoke();
            };
    }
}
