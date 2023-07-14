namespace Elympics
{
    public class HalfRemoteLagConfig
    {
        public int DelayMs = 0;
        public float PacketLoss = 0f;
        public int JitterMs = 0;
        public int RandomSeed = 4206969;

        public void LoadLan()
        {
            DelayMs = 5;
            PacketLoss = 0;
            JitterMs = 5;
        }

        public void LoadBroadband()
        {
            DelayMs = 25;
            PacketLoss = 0.01f;
            JitterMs = 8;
        }

        public void LoadSlowBroadband()
        {
            DelayMs = 40;
            PacketLoss = 0.02f;
            JitterMs = 10;
        }

        public void LoadLTE()
        {
            DelayMs = 80;
            PacketLoss = 0.05f;
            JitterMs = 15;
        }

        public void Load3G()
        {
            DelayMs = 160;
            PacketLoss = 0.1f;
            JitterMs = 40;
        }

        public void LoadTotalMess()
        {
            DelayMs = 100;
            PacketLoss = 0.4f;
            JitterMs = 100;
        }
    }
}
