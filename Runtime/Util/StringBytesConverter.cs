using System.Text;

namespace Elympics
{
	public static class StringBytesConverter
	{
		public static string BytesToString(byte[] bytes) => Encoding.ASCII.GetString(bytes);
		public static byte[] StringToBytes(string str)   => Encoding.ASCII.GetBytes(str);
	}
}
