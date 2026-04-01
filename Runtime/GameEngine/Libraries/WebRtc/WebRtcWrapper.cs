using System;
using System.Runtime.InteropServices;
using System.Text;

namespace WebRtcWrapper
{
    public static class WebRtcWrapper
    {
        private const int SizeOfGuid = 16;

        public delegate void UuidReturnDelegate(IntPtr data);

        public delegate void ReceivedDelegate(IntPtr data, int size);

        public delegate void ReceivingErrorDelegate(IntPtr data, int size);

        public delegate void ReceivingEndedDelegate();

        public delegate void OfferDelegate(IntPtr offer, int offerSize);

        public delegate void AnswerDelegate(IntPtr id, IntPtr answer, int answerSize);

        public delegate void ErrorDelegate(IntPtr error, int errorSize);

        [DllImport("webrtc")]
        private static extern void ClientNew(
            IntPtr uuidReturnCallback,
            IntPtr reliableReceivedCallback,
            IntPtr reliableErrorCallback,
            IntPtr reliableEndedCallback,
            IntPtr unreliableReceivedCallback,
            IntPtr unreliableErrorCallback,
            IntPtr unreliableEndedCallback);

        [DllImport("webrtc")]
        private static extern void ClientRemove(IntPtr id);

        [DllImport("webrtc")]
        private static extern void ClientCreateOffer(IntPtr id, IntPtr offerCallback);

        [DllImport("webrtc")]
        private static extern void ClientOnAnswer(IntPtr id, string answerJson);

        [DllImport("webrtc")]
        private static extern bool ClientSendReliable(IntPtr id, IntPtr data, int dataSize);

        [DllImport("webrtc")]
        private static extern bool ClientSendUnreliable(IntPtr id, IntPtr data, int dataSize);

        [DllImport("webrtc")]
        private static extern bool ClientReceiveReliable(IntPtr id);

        [DllImport("webrtc")]
        private static extern bool ClientReceiveUnreliable(IntPtr id);

        [DllImport("webrtc")]
        private static extern void ClientClose(IntPtr id);

        [DllImport("webrtc")]
        private static extern void ServerNew(IntPtr uuidReturnCallback);

        [DllImport("webrtc")]
        private static extern void ServerRemove(IntPtr id);

        [DllImport("webrtc")]
        private static extern void ServerStart(IntPtr id, int port, string ip);

        [DllImport("webrtc")]
        private static extern void ServerStop(IntPtr id);

        [DllImport("webrtc")]
        private static extern void ServerNewClient(
            IntPtr id,
            string offerJson,
            IntPtr answerCallback,
            IntPtr errorCallback,
            IntPtr reliableReceivedCallback,
            IntPtr reliableErrorCallback,
            IntPtr reliableEndedCallback,
            IntPtr unreliableReceivedCallback,
            IntPtr unreliableErrorCallback,
            IntPtr unreliableEndedCallback);

        [DllImport("webrtc")]
        private static extern void ServerRemoveClient(IntPtr serverId, IntPtr clientId);

        [DllImport("webrtc")]
        private static extern bool ServerClientSendReliable(IntPtr serverId, IntPtr clientId, IntPtr data, int dataSize);

        [DllImport("webrtc")]
        private static extern bool ServerClientSendUnreliable(IntPtr serverId, IntPtr clientId, IntPtr data, int dataSize);

        [DllImport("webrtc")]
        private static extern bool ServerClientReceiveReliable(IntPtr serverId, IntPtr clientId);

        [DllImport("webrtc")]
        private static extern bool ServerClientReceiveUnreliable(IntPtr serverId, IntPtr clientId);

        [DllImport("webrtc")]
        private static extern void ServerClientClose(IntPtr serverId, IntPtr clientId);

        public static Guid ClientNew(
            ReceivedDelegate reliableReceivedDelegate,
            ReceivingErrorDelegate reliableErrorDelegate,
            ReceivingEndedDelegate reliableEndedDelegate,
            ReceivedDelegate unreliableReceivedDelegate,
            ReceivingErrorDelegate unreliableErrorDelegate,
            ReceivingEndedDelegate unreliableEndedDelegate)
        {
            Guid id = default;

            void UuidReturn(IntPtr data)
            {
                id = new Guid(GetByteArray(data, SizeOfGuid));
            }

            var uuidReturnDelegate = new UuidReturnDelegate(UuidReturn);

            ClientNew(
                Marshal.GetFunctionPointerForDelegate(uuidReturnDelegate),
                Marshal.GetFunctionPointerForDelegate(reliableReceivedDelegate),
                Marshal.GetFunctionPointerForDelegate(reliableErrorDelegate),
                Marshal.GetFunctionPointerForDelegate(reliableEndedDelegate),
                Marshal.GetFunctionPointerForDelegate(unreliableReceivedDelegate),
                Marshal.GetFunctionPointerForDelegate(unreliableErrorDelegate),
                Marshal.GetFunctionPointerForDelegate(unreliableEndedDelegate));
            return id;
        }

        public static void ClientRemove(Guid clientId)
        {
            var clientIdUnmanaged = UnmanagedByteArray(clientId.ToByteArray());
            ClientRemove(clientIdUnmanaged);
            Marshal.FreeHGlobal(clientIdUnmanaged);
        }

        public static ReceivedDelegate CreateReceivedDelegate(Action<byte[]> received)
        {
            void Received(IntPtr data, int size) => received(GetByteArray(data, size));
            return Received;
        }

        public static ReceivingErrorDelegate CreateReceivingErrorDelegate(Action<string> receivingError)
        {
            void ReceivingError(IntPtr data, int size) => receivingError(GetString(data, size));
            return ReceivingError;
        }

        public static ReceivingEndedDelegate CreateReceivingEndedDelegate(Action receivingEnded)
        {
            void ReceivingEnded() => receivingEnded();
            return ReceivingEnded;
        }

        public static OfferDelegate CreateOfferDelegate(Action<string> offer)
        {
            void Offer(IntPtr data, int size) => offer(GetString(data, size));
            return Offer;
        }

        public static void ClientCreateOffer(Guid clientId, OfferDelegate offerDelegate)
        {
            var clientIdUnmanaged = UnmanagedByteArray(clientId.ToByteArray());
            ClientCreateOffer(clientIdUnmanaged, Marshal.GetFunctionPointerForDelegate(offerDelegate));
            Marshal.FreeHGlobal(clientIdUnmanaged);
        }

        public static void ClientOnAnswer(Guid clientId, string answer)
        {
            var clientIdUnmanaged = UnmanagedByteArray(clientId.ToByteArray());
            ClientOnAnswer(clientIdUnmanaged, answer);
            Marshal.FreeHGlobal(clientIdUnmanaged);
        }

        public static void ClientSendReliable(Guid clientId, byte[] data)
        {
            var clientIdUnmanaged = UnmanagedByteArray(clientId.ToByteArray());
            var dataUnmanaged = UnmanagedByteArray(data);

            _ = ClientSendReliable(clientIdUnmanaged, dataUnmanaged, data.Length);

            Marshal.FreeHGlobal(clientIdUnmanaged);
            Marshal.FreeHGlobal(dataUnmanaged);
        }

        public static void ClientSendUnreliable(Guid clientId, byte[] data)
        {
            var clientIdUnmanaged = UnmanagedByteArray(clientId.ToByteArray());
            var dataUnmanaged = UnmanagedByteArray(data);

            _ = ClientSendUnreliable(clientIdUnmanaged, dataUnmanaged, data.Length);

            Marshal.FreeHGlobal(clientIdUnmanaged);
            Marshal.FreeHGlobal(dataUnmanaged);
        }

        public static bool ClientReceiveReliable(Guid clientId)
        {
            var clientIdUnmanaged = UnmanagedByteArray(clientId.ToByteArray());
            var result = ClientReceiveReliable(clientIdUnmanaged);
            Marshal.FreeHGlobal(clientIdUnmanaged);

            return result;
        }

        public static bool ClientReceiveUnreliable(Guid clientId)
        {
            var clientIdUnmanaged = UnmanagedByteArray(clientId.ToByteArray());
            var result = ClientReceiveUnreliable(clientIdUnmanaged);
            Marshal.FreeHGlobal(clientIdUnmanaged);

            return result;
        }

        public static void ClientClose(Guid clientId)
        {
            var clientIdUnmanaged = UnmanagedByteArray(clientId.ToByteArray());
            ClientClose(clientIdUnmanaged);
            Marshal.FreeHGlobal(clientIdUnmanaged);
        }

        public static Guid ServerNew()
        {
            Guid id = default;
            void UuidReturn(IntPtr data) => id = new Guid(GetByteArray(data, SizeOfGuid));
            var uuidReturnDelegate = new UuidReturnDelegate(UuidReturn);

            ServerNew(Marshal.GetFunctionPointerForDelegate(uuidReturnDelegate));
            return id;
        }

        public static void ServerStart(Guid id, int port, string ip)
        {
            var idUnmanaged = UnmanagedByteArray(id.ToByteArray());
            ServerStart(idUnmanaged, port, ip);
            Marshal.FreeHGlobal(idUnmanaged);
        }

        public static void ServerStop(Guid id)
        {
            var idUnmanaged = UnmanagedByteArray(id.ToByteArray());
            ServerStop(idUnmanaged);
            Marshal.FreeHGlobal(idUnmanaged);
        }

        public static void ServerRemove(Guid id)
        {
            var idUnmanaged = UnmanagedByteArray(id.ToByteArray());
            ServerRemove(idUnmanaged);
            Marshal.FreeHGlobal(idUnmanaged);
        }

        public static (Guid clientId, string answerJson, string error) ServerNewClient(
            Guid serverId,
            string offer,
            ReceivedDelegate reliableReceivedDelegate,
            ReceivingErrorDelegate reliableErrorDelegate,
            ReceivingEndedDelegate reliableEndedDelegate,
            ReceivedDelegate unreliableReceivedDelegate,
            ReceivingErrorDelegate unreliableErrorDelegate,
            ReceivingEndedDelegate unreliableEndedDelegate)
        {
            (Guid, string, string) ret = default;

            void AnswerCallback(IntPtr clientId, IntPtr answer, int answerSize) => ret = (new Guid(GetByteArray(clientId, SizeOfGuid)), GetString(answer, answerSize), default);
            void ErrorCallback(IntPtr error, int errorSize) => ret = (default, default, GetString(error, errorSize));

            var answerDelegate = new AnswerDelegate(AnswerCallback);
            var errorDelegate = new ErrorDelegate(ErrorCallback);

            var serverIdUnmanaged = UnmanagedByteArray(serverId.ToByteArray());

            ServerNewClient(serverIdUnmanaged,
                offer,
                Marshal.GetFunctionPointerForDelegate(answerDelegate),
                Marshal.GetFunctionPointerForDelegate(errorDelegate),
                Marshal.GetFunctionPointerForDelegate(reliableReceivedDelegate),
                Marshal.GetFunctionPointerForDelegate(reliableErrorDelegate),
                Marshal.GetFunctionPointerForDelegate(reliableEndedDelegate),
                Marshal.GetFunctionPointerForDelegate(unreliableReceivedDelegate),
                Marshal.GetFunctionPointerForDelegate(unreliableErrorDelegate),
                Marshal.GetFunctionPointerForDelegate(unreliableEndedDelegate));

            Marshal.FreeHGlobal(serverIdUnmanaged);

            return ret;
        }

        public static void ServerRemoveClient(Guid serverId, Guid clientId)
        {
            var serverIdUnmanaged = UnmanagedByteArray(serverId.ToByteArray());
            var clientIdUnmanaged = UnmanagedByteArray(clientId.ToByteArray());
            ServerRemoveClient(serverIdUnmanaged, clientIdUnmanaged);
            Marshal.FreeHGlobal(serverIdUnmanaged);
            Marshal.FreeHGlobal(clientIdUnmanaged);
        }

        public static void ServerClientSendReliable(Guid serverId, Guid clientId, byte[] data)
        {
            var serverIdUnmanaged = UnmanagedByteArray(serverId.ToByteArray());
            var clientIdUnmanaged = UnmanagedByteArray(clientId.ToByteArray());
            var dataUnmanaged = UnmanagedByteArray(data);

            _ = ServerClientSendReliable(serverIdUnmanaged, clientIdUnmanaged, dataUnmanaged, data.Length);

            Marshal.FreeHGlobal(serverIdUnmanaged);
            Marshal.FreeHGlobal(clientIdUnmanaged);
            Marshal.FreeHGlobal(dataUnmanaged);
        }

        public static void ServerClientSendUnreliable(Guid serverId, Guid clientId, byte[] data)
        {
            var serverIdUnmanaged = UnmanagedByteArray(serverId.ToByteArray());
            var clientIdUnmanaged = UnmanagedByteArray(clientId.ToByteArray());
            var dataUnmanaged = UnmanagedByteArray(data);

            _ = ServerClientSendUnreliable(serverIdUnmanaged, clientIdUnmanaged, dataUnmanaged, data.Length);

            Marshal.FreeHGlobal(serverIdUnmanaged);
            Marshal.FreeHGlobal(clientIdUnmanaged);
            Marshal.FreeHGlobal(dataUnmanaged);
        }

        public static bool ServerClientReceiveReliable(Guid serverId, Guid clientId)
        {
            var serverIdUnmanaged = UnmanagedByteArray(serverId.ToByteArray());
            var clientIdUnmanaged = UnmanagedByteArray(clientId.ToByteArray());
            var result = ServerClientReceiveReliable(serverIdUnmanaged, clientIdUnmanaged);
            Marshal.FreeHGlobal(serverIdUnmanaged);
            Marshal.FreeHGlobal(clientIdUnmanaged);

            return result;
        }

        public static bool ServerClientReceiveUnreliable(Guid serverId, Guid clientId)
        {
            var serverIdUnmanaged = UnmanagedByteArray(serverId.ToByteArray());
            var clientIdUnmanaged = UnmanagedByteArray(clientId.ToByteArray());
            var result = ServerClientReceiveUnreliable(serverIdUnmanaged, clientIdUnmanaged);
            Marshal.FreeHGlobal(serverIdUnmanaged);
            Marshal.FreeHGlobal(clientIdUnmanaged);

            return result;
        }

        public static void ServerClientClose(Guid serverId, Guid clientId)
        {
            var serverIdUnmanaged = UnmanagedByteArray(serverId.ToByteArray());
            var clientIdUnmanaged = UnmanagedByteArray(clientId.ToByteArray());
            ServerClientClose(serverIdUnmanaged, clientIdUnmanaged);
            Marshal.FreeHGlobal(serverIdUnmanaged);
            Marshal.FreeHGlobal(clientIdUnmanaged);
        }

        private static byte[] GetByteArray(IntPtr data, int size)
        {
            var arr = new byte[size];
            Marshal.Copy(data, arr, 0, size);
            return arr;
        }

        private static string GetString(IntPtr data, int size)
        {
            return Encoding.ASCII.GetString(GetByteArray(data, size));
        }

        private static IntPtr UnmanagedByteArray(byte[] bytes)
        {
            var unmanaged = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, unmanaged, bytes.Length);
            return unmanaged;
        }
    }
}
