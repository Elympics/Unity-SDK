namespace Elympics
{
    internal interface IBinaryInputReader : IInputReader
    {
        void FeedDataForReading(byte[] data);
        bool AllBytesRead();
    }
}
