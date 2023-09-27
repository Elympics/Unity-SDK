using System;

#nullable enable

namespace Elympics
{
    public interface IAsyncEventsDispatcher
    {
        void Enqueue(Action action);
    }
}
