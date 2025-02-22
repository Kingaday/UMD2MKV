namespace VGMToolbox.format
{
    public static class Atrac3Plus
    {
        public const string FileExtensionPsp = ".oma";

        private static readonly byte[] Aa3HeaderChunk =
        [
            0x65, 0x61, 0x33, 0x03, 0x00, 0x00, 0x00, 0x00, 0x07, 0x76, 0x47, 0x45, 0x4F, 0x42, 0x00, 0x00,
                        0x01, 0xC6, 0x00, 0x00, 0x02, 0x62, 0x69, 0x6E, 0x61, 0x72, 0x79, 0x00, 0x00, 0x00, 0x00, 0x4F, 
                        0x00, 0x4D, 0x00, 0x47, 0x00, 0x5F, 0x00, 0x4C, 0x00, 0x53, 0x00, 0x49, 0x00, 0x00, 0x00, 0x01,
                        0x00, 0x40, 0x00, 0xDC, 0x00, 0x70, 0x00, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x4B, 0x45,
                        0x59, 0x52, 0x49, 0x4E, 0x47
        ];

        private static readonly byte[] Ea3HeaderChunk = [0x45, 0x41, 0x33, 0x01, 0x00, 0x60, 0xFF, 0xFF];

        private const long aa3HeaderSize = 0x460;
        private const long aa3FormatStringLocation = 0x420;
        private const long aa3HeaderLocation = 0x00;
        private const long ea3HeaderLocation = 0x400;
        private static byte[] GetFormatBytes(uint headerBlockValue)
        {
            uint formatValue = 0x01000000;
            formatValue |= (0xFFFF & headerBlockValue); // Thanks to FastElbJa for this info.
            var formatBytes = BitConverter.GetBytes(formatValue);
            Array.Reverse(formatBytes);
            return formatBytes;
        }
        public static byte[] GetAa3Header(uint headerBlockValue)
        {
            var headerBytes = GetFormatBytes(headerBlockValue);
            return GetAa3Header(headerBytes);
        }

        private static byte[] GetAa3Header(byte[] formatString)
        {
            var headerBytes = new byte[aa3HeaderSize];
            // copy AA3 header
            Array.Copy(Aa3HeaderChunk, 0, headerBytes, aa3HeaderLocation, Aa3HeaderChunk.Length);
            // copy EA3 header
            Array.Copy(Ea3HeaderChunk, 0, headerBytes, ea3HeaderLocation, Ea3HeaderChunk.Length);
            // insert format string
            Array.Copy(formatString, 0, headerBytes, aa3FormatStringLocation, formatString.Length);
            return headerBytes;
        }
    }
}