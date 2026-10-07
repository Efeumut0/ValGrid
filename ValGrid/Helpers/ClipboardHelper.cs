using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ValGrid.Helpers;

public static class ClipboardHelper
{
    private const uint CF_UNICODETEXT = 13;
    private const uint GMEM_MOVEABLE = 0x0002;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalFree(IntPtr hMem);

    /// <summary>
    /// Asynchronously copies text to clipboard on a background thread without freezing the UI.
    /// </summary>
    public static async Task<bool> SetTextAsync(string text, int retries = 6, int delayMs = 30)
    {
        if (text == null) text = "";

        return await Task.Run(async () =>
        {
            // 1. Try Win32 direct clipboard API first (fastest, no OLE COM overhead)
            for (int i = 0; i < retries; i++)
            {
                if (TrySetTextWin32(text))
                    return true;

                if (i < retries - 1)
                    await Task.Delay(delayMs).ConfigureAwait(false);
            }

            // 2. Fallback to WPF Clipboard in isolated STA thread with timeout
            if (TrySetTextWpf(text))
                return true;

            return false;
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Synchronously copies text to clipboard (safe fallback).
    /// </summary>
    public static bool SetText(string text, int retries = 5, int delayMs = 25)
    {
        if (text == null) text = "";

        // 1. Try Win32 direct clipboard API
        for (int i = 0; i < retries; i++)
        {
            if (TrySetTextWin32(text))
                return true;

            if (i < retries - 1)
                Thread.Sleep(delayMs);
        }

        // 2. Fallback to WPF Clipboard in STA thread with timeout
        return TrySetTextWpf(text);
    }

    private static bool TrySetTextWin32(string text)
    {
        try
        {
            if (!OpenClipboard(IntPtr.Zero))
                return false;

            try
            {
                if (!EmptyClipboard())
                    return false;

                // Length + 1 for null terminator, 2 bytes per char (Unicode)
                int bytesCount = (text.Length + 1) * 2;
                IntPtr hGlobal = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)bytesCount);
                if (hGlobal == IntPtr.Zero)
                    return false;

                IntPtr target = GlobalLock(hGlobal);
                if (target == IntPtr.Zero)
                {
                    GlobalFree(hGlobal);
                    return false;
                }

                try
                {
                    Marshal.Copy(text.ToCharArray(), 0, target, text.Length);
                    Marshal.WriteInt16(target, text.Length * 2, 0); // null terminator
                }
                finally
                {
                    GlobalUnlock(hGlobal);
                }

                if (SetClipboardData(CF_UNICODETEXT, hGlobal) == IntPtr.Zero)
                {
                    GlobalFree(hGlobal);
                    return false;
                }

                return true;
            }
            finally
            {
                CloseClipboard();
            }
        }
        catch
        {
            return false;
        }
    }

    private static bool TrySetTextWpf(string text)
    {
        var success = false;
        var thread = new Thread(() =>
        {
            try
            {
                Clipboard.SetText(text);
                success = true;
            }
            catch
            {
                try
                {
                    Clipboard.SetDataObject(text, false);
                    success = true;
                }
                catch { }
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();

        // Hard timeout so caller never freezes indefinitely
        if (!thread.Join(350))
        {
            try { thread.Interrupt(); } catch { }
            return false;
        }

        return success;
    }
}

