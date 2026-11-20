using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Pip
{
    internal enum CopyKind { None, Link, Number, Code, LongText }

    internal sealed class ClipboardListener : IDisposable
    {
        private const uint UnicodeText = 13;
        private readonly IntPtr window;
        private readonly bool listening;
        private uint sequence;
        private int retries;
        private double nextTry;

        internal ClipboardListener(IntPtr window)
        {
            this.window = window;
            sequence = Native.GetClipboardSequenceNumber();
            listening = Native.AddClipboardFormatListener(window);
        }

        internal void Changed()
        {
            uint latest = Native.GetClipboardSequenceNumber();
            if (sequence == latest) return;
            sequence = latest;
            nextTry = 0;
            retries = 8;
        }

        internal CopyKind Check(double now)
        {
            if (retries == 0 || now < nextTry) return CopyKind.None;
            retries--;
            nextTry = now + .045;
            if (!Native.OpenClipboard(window)) return CopyKind.None;

            string text = null;
            try
            {
                retries = 0;
                if (!Native.IsClipboardFormatAvailable(UnicodeText)) return CopyKind.None;
                IntPtr memory = Native.GetClipboardData(UnicodeText);
                if (memory == IntPtr.Zero) return CopyKind.None;
                IntPtr chars = Native.GlobalLock(memory);
                if (chars == IntPtr.Zero) return CopyKind.None;
                try
                {
                    int length = (int)Math.Min(4096ul, Native.GlobalSize(memory).ToUInt64() / 2);
                    text = Marshal.PtrToStringUni(chars, length);
                    int terminator = text.IndexOf('\0');
                    if (terminator >= 0) text = text.Substring(0, terminator);
                }
                finally { Native.GlobalUnlock(memory); }
            }
            finally { Native.CloseClipboard(); }

            return CopyClassifier.Classify(text);
        }

        public void Dispose()
        {
            if (listening) Native.RemoveClipboardFormatListener(window);
        }
    }

    internal static class CopyClassifier
    {
        private static readonly Regex BareLink = new Regex(
            @"^(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+(?:ai|app|co|com|dev|edu|gov|info|io|me|net|org|test)(?::\d{1,5})?(?:[/?#].*)?$",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        private static readonly Regex Number = new Regex(
            @"^[+-]?(?:(?:(?:\d{1,3}(?:[,_ .\u00a0\u202f]\d{3})+|\d+)(?:[.,]\d+)?|[.,]\d+)(?:[eE][+-]?\d+)?%?|0[xX][\dA-Fa-f]+|0[bB][01]+)$",
            RegexOptions.CultureInvariant);

        private static readonly Regex Code = new Regex(
            @"(^\s*(?:```|#!|#include\s|<[/!\?]?[a-z][\w:-]*(?:\s[^>\r\n]*)?/?>))|" +
            @"(^\s*(?:async\s+)?(?:function\s+\w+\s*\(|def\s+\w+\s*\(|class\s+\w+\s*[:{(]|(?:let|const|var|fn)\s+\w+\s*(?:[:=(])|(?:from\s+\w[\w.]*\s+)?import\s+[\w{*]|using\s+[\w.]+\s*;|public\s+(?:static|class|struct|void)\b))|" +
            @"(^\s*(?:SELECT\b[^\r\n]+\bFROM\b|INSERT\s+INTO\b|UPDATE\b[^\r\n]+\bSET\b|CREATE\s+TABLE\b))|" +
            @"(^\s*(?:\{\s*""[^""]+""\s*:|\[\s*\{\s*""|(?:\$?[\w.-]+\s*=\s*[^=\s])|(?:Get|Set|New|Write|Remove|Invoke)-[A-Z]\w+\b))|" +
            @"(^\s*[\w.]+\([^\r\n]*\)\s*;?\s*$)|(=>|\breturn\s+[^\r\n]+;|\b(?:if|for|while|switch)\s*\([^\r\n]*\)\s*\{)",
            RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnoreCase);

        internal static CopyKind Classify(string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return CopyKind.None;
            string text = value.Trim();
            if (IsLink(text)) return CopyKind.Link;
            if (text.Length <= 100 && Number.IsMatch(text)) return CopyKind.Number;
            if (Code.IsMatch(text)) return CopyKind.Code;
            return text.Length >= 180 ? CopyKind.LongText : CopyKind.None;
        }

        private static bool IsLink(string text)
        {
            if (text.Length > 2048) return false;
            foreach (char character in text)
                if (Char.IsWhiteSpace(character) || Char.IsControl(character)) return false;
            bool bare = text.StartsWith("www.", StringComparison.OrdinalIgnoreCase) || BareLink.IsMatch(text);
            string target = bare ? "https://" + text : text;
            Uri address;
            if (!Uri.TryCreate(target, UriKind.Absolute, out address)) return false;
            return ((address.Scheme == "https" || address.Scheme == "http" || address.Scheme == "ftp") &&
                    !String.IsNullOrEmpty(address.Host)) || (address.Scheme == "mailto" && target.IndexOf('@') > 7);
        }
    }
}
