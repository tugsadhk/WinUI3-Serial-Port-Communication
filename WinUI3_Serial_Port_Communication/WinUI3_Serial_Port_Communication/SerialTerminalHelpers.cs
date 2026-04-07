namespace WinUI3_Serial_Port_Communication
{
    internal static class SerialTerminalHelpers
    {
        /// <summary>
        /// Trims <paramref name="text"/> so that it contains at most <paramref name="maxLines"/> lines,
        /// keeping the tail (most recent data).
        /// </summary>
        internal static string TrimToMaxLines(string text, int maxLines)
        {
            int count = 0;
            for (int i = text.Length - 1; i >= 0; i--)
            {
                if (text[i] == '\n' && ++count > maxLines)
                    return text[(i + 1)..];
            }
            return text;
        }

        /// <summary>
        /// Returns a human-readable byte count (B / KB / MB).
        /// </summary>
        internal static string FormatBytes(int bytes)
        {
            if (bytes < 1024)        return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            return                          $"{bytes / (1024.0 * 1024):F1} MB";
        }
    }
}
