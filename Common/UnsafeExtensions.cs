using System.Runtime.InteropServices;

namespace Common;

public static class UnsafeExtensions
{
    public static unsafe byte* ToPointer(this byte[] str)
    {
        return (byte*)Marshal.UnsafeAddrOfPinnedArrayElement(str, 0).ToPointer();
    }
    
    public static unsafe byte* ToPointer(this string str)
    {
        return (byte*)Marshal.StringToHGlobalAnsi(str).ToPointer();
    }
    
    public static unsafe string ToString(byte* str)
    {
        var stra = Marshal.PtrToStringAnsi((IntPtr)str);
        return stra;
        
    }
    
    public static unsafe byte** ToPointerArray(this string[] str)
    {
        var ptrs = new byte*[str.Length];
        for (int i = 0; i < str.Length; i++)
        {
            ptrs[i] = str[i].ToPointer();
        }
        fixed (byte** p = ptrs)
        {
            return p;
        }
    }
}