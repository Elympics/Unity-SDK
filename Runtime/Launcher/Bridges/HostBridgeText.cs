#nullable enable

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Elympics
{
    /// <summary>Both host bridges receive null-terminated UTF-8 strings.</summary>
    internal static class HostBridgeText
    {
        public static string FromUtf8(IntPtr pointer)
        {
            if (pointer == IntPtr.Zero)
                return string.Empty;
            var length = 0;
            while (Marshal.ReadByte(pointer, length) != 0)
                length++;
            var bytes = new byte[length];
            Marshal.Copy(pointer, bytes, 0, length);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
