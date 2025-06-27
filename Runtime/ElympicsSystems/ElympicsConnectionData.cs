using Elympics.Models.Authentication;
namespace Elympics.ElympicsSystems
{
    public readonly struct ElympicsConnectionData
    {
        public AuthData AuthData { get; init; }
        public bool AutoReconnected { get; init; }

    }
}
