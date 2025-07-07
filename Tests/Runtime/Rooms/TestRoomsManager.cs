using System;
using System.Reflection;
using System.Threading;
using Castle.Core.Internal;
using Cysharp.Threading.Tasks;
using Elympics.Communication.Utils;
using Elympics.ElympicsSystems.Internal;
using Elympics.Rooms.Models;
using NSubstitute;
using NSubstitute.ClearExtensions;
using NUnit.Framework;

#nullable enable

namespace Elympics.Tests.Rooms
{
    internal abstract class TestRoomsManager
    {
        protected static readonly Guid HostId = new("10100000000000000000000000aaaa01");
        protected static readonly Guid RoomId = new("10100000000000000000000000bbbb01");
        protected static DiscreteTimer Timer = new();

        protected static readonly RoomStateChanged InitialRoomState = Defaults.CreateRoomState(RoomId, HostId)
            .WithLastUpdate(Timer++);

        protected readonly RoomsManager RoomsManager;
        protected readonly IRoomJoiner RoomJoiner;
        protected readonly IMatchLauncher MatchLauncherMock;
        protected readonly IRoomsClient RoomsClientMock;
        protected readonly EventObserver<IRoomsManager> EventRegister;
        private CancellationTokenSource _tearDownCts = new();

        private static readonly TimeSpan StandardResponseDelay = TimeSpan.FromMilliseconds(200);
        protected UniTask GetResponseDelay() => UniTask.Delay(StandardResponseDelay, DelayType.DeltaTime, cancellationToken: _tearDownCts.Token);
        protected UniTask GetEternalDelay() => UniTask.Never(_tearDownCts.Token);

        private readonly FieldInfo _roomsManagerInitialized;
        private void InitializeRoomsManager() => _roomsManagerInitialized.SetValue(RoomsManager, true);

        protected TestRoomsManager()
        {
            MatchLauncherMock = Substitute.For<IMatchLauncher>();
            RoomsClientMock = Substitute.For<IRoomsClient>();
            var logger = new ElympicsLoggerContext(Guid.NewGuid());
            RoomJoiner = new RoomJoiner(RoomsClientMock)
            {
                OperationTimeout = TimeSpan.FromSeconds(1),
            };
            RoomsManager = new RoomsManager(MatchLauncherMock, RoomsClientMock, logger, RoomJoiner);
            ElympicsTimeout.RoomStateChangeConfirmationTimeout = TimeSpan.FromSeconds(1);
            EventRegister = new EventObserver<IRoomsManager>(RoomsManager);

            _roomsManagerInitialized = RoomsManager.GetType()
                .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Find(x => x.Name == "_initialized");
        }

        [SetUp]
        public void SetUp()
        {
            _tearDownCts = new CancellationTokenSource();
            InitializeRoomsManager();
        }

        [TearDown]
        public void Reset()
        {
            _tearDownCts.Cancel();
            ((IRoomsManager)RoomsManager).Reset();
            EventRegister.Reset();
            RoomsClientMock.ClearSubstitute();
            MatchLauncherMock.ClearSubstitute();
        }

        /// <summary>
        /// Emits the <see cref="IRoomsClient.RoomStateChanged"/> event for the current <see cref="IRoomsClient"/> mock (<see cref="RoomsClientMock"/>).
        /// </summary>
        /// <param name="updatedState">The state to be emitted.</param>
        /// <param name="updateLastUpdateTime">Should the emitted room state have its last update time set to the current <see cref="Timer"/> value.</param>
        protected void EmitRoomUpdate(RoomStateChanged updatedState, bool updateLastUpdateTime = true) =>
            RoomsClientMock.RoomStateChanged += Raise.Event<Action<RoomStateChanged>>(updateLastUpdateTime ? updatedState.WithLastUpdate(Timer++) : updatedState);

        /// <summary>
        /// Proceed with room being joined: from JoinedNoTracking to JoinedWithTracking.
        /// This is done by emitting the first room state update message.
        /// </summary>
        /// <remarks>Emitted room state will have its last update time set to the current <see cref="Timer"/> value.</remarks>
        /// <param name="roomState">Room state to be emitted. By default, <see cref="InitialRoomState"/> is used.</param>
        protected void SetRoomAsTrackedWhenItGetsJoined(RoomStateChanged? roomState = null)
        {
            roomState ??= InitialRoomState;
            RoomJoiner.JoiningStateChanged += OnJoiningStateChanged;
            return;

            void OnJoiningStateChanged(RoomJoiningState state)
            {
                if (state is not RoomJoiningState.JoinedNoTracking)
                    return;
                EmitRoomUpdate(roomState);
                RoomJoiner.JoiningStateChanged -= OnJoiningStateChanged;
            }
        }
    }
}
