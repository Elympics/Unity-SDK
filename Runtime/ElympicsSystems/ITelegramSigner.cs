namespace Elympics
{
    public interface ITelegramSigner
    {
        /// <summary>
        /// Method for retrieving InitData.
        /// It is called by Elympics in authentication process.
        /// </summary>
        string GetInitData();
    }
}
