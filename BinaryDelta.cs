using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

internal static class BinaryDelta
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        internal IntPtr Start;
        internal UIntPtr Size;
        internal int Editable;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Output
    {
        internal IntPtr Start;
        internal UIntPtr Size;
    }

    [DllImport("msdelta.dll", SetLastError = true)]
    private static extern bool ApplyDeltaB(long flags, Input source, Input delta, out Output target);

    [DllImport("msdelta.dll")]
    private static extern bool DeltaFree(IntPtr address);

    internal static void Apply(string sourceFile, Stream deltaStream, string targetFile)
    {
        byte[] source = File.ReadAllBytes(sourceFile);
        byte[] delta;
        using (var buffer = new MemoryStream())
        {
            deltaStream.CopyTo(buffer);
            delta = buffer.ToArray();
        }
        GCHandle sourceHandle = GCHandle.Alloc(source, GCHandleType.Pinned);
        try
        {
            GCHandle deltaHandle = GCHandle.Alloc(delta, GCHandleType.Pinned);
            try
            {
                var input = new Input { Start = sourceHandle.AddrOfPinnedObject(), Size = new UIntPtr((uint)source.Length) };
                var patch = new Input { Start = deltaHandle.AddrOfPinnedObject(), Size = new UIntPtr((uint)delta.Length) };
                Output result;
                if (!ApplyDeltaB(0, input, patch, out result))
                    throw new IOException("Windows could not apply the engine patch.", new Win32Exception());
                try
                {
                    byte[] bytes = new byte[checked((int)result.Size.ToUInt64())];
                    Marshal.Copy(result.Start, bytes, 0, bytes.Length);
                    File.WriteAllBytes(targetFile, bytes);
                }
                finally { DeltaFree(result.Start); }
            }
            finally { deltaHandle.Free(); }
        }
        finally { sourceHandle.Free(); }
    }
}
