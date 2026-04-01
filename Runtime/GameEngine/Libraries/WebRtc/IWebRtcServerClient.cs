using System.Threading.Tasks;

namespace WebRtcWrapper
{
    public interface IWebRtcServerClient : IWebRtcCommunication
    {
        Task<string> CreateAnswerAsync(string offerJson);

        /// <summary>
        /// Use to store this in WebRtcServer dictionary for collective receiving ~pprzestrzelski 26.09.2022
        /// </summary>
        void ReceiveReliable();

        /// <summary>
        /// Use to store this in WebRtcServer dictionary for collective receiving ~pprzestrzelski 26.09.2022
        /// </summary>
        void ReceiveUnreliable();

        bool ReceiveReliableOnce();
        bool ReceiveUnreliableOnce();
        void Close();
    }
}
