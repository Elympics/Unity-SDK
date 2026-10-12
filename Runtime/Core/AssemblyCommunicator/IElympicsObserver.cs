#nullable enable

namespace Elympics.AssemblyCommunicator
{
    public interface IElympicsObserver<T>
    {
        void OnEvent(T argument);
    }
}
