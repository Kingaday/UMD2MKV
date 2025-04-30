using System.Text;
using SkiaSharp;

namespace UMD2MKV.SubtitleEdit;

public sealed class VobSubWriter : IDisposable
{
    public enum ContentAlignment
    {
        TopLeft,
        TopCenter,
        TopRight,
        MiddleLeft,
        MiddleCenter,
        MiddleRight,
        BottomLeft,
        BottomCenter,
        BottomRight
    }

    private class MemWriter(long size)
    {
        private readonly byte[] _buf = new byte[size];
        private long _pos;

        public byte[] GetBuf()
        {
            return _buf;
        }

        public long GetPosition()
        {
            return _pos;
        }

        public void GotoBegin()
        {
            _pos = 0;
        }

        public void WriteByte(byte val)
        {
            _buf[_pos++] = val;
        }
    }

    private readonly string _subFileName;
    private FileStream? _subFile;
    private readonly StringBuilder _idx;
    private readonly int _screenWidth;
    private readonly int _screenHeight;
    private readonly int _bottomMargin;
    private readonly int _leftRightMargin;
    private readonly int _languageStreamId;
    private readonly SKColor _background = SKColors.Transparent;
    private readonly SKColor _pattern;
    private readonly SKColor _emphasis1;
    private readonly bool _useInnerAntiAliasing;
    private SKColor _emphasis2 = SKColors.Black;
    private readonly string _languageName;
    private readonly string _languageNameShort;

    public VobSubWriter(string subFileName, int screenWidth, int screenHeight, int bottomMargin, int leftRightMargin,
        int languageStreamId, SKColor pattern, SKColor emphasis1, bool useInnerAntiAliasing, SubtitleLanguage language)
    {
        _subFileName = subFileName;
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
        _bottomMargin = bottomMargin;
        _leftRightMargin = leftRightMargin;
        _languageStreamId = languageStreamId;
        _pattern = pattern;
        _emphasis1 = emphasis1;
        _useInnerAntiAliasing = useInnerAntiAliasing;
        _languageName = language.NativeName;
        _languageNameShort = language.Code;
        _idx = CreateIdxHeader();
        _subFile = new FileStream(subFileName, FileMode.Create);
    }

    private static void WriteEndianWord(int? i, Stream stream)
    {
#pragma warning disable CS8629 // Nullable value type may be null.
        stream.WriteByte((byte)(i / 256));
#pragma warning restore CS8629 // Nullable value type may be null.
#pragma warning disable CS8629 // Nullable value type may be null.
        stream.WriteByte((byte)(i % 256));
#pragma warning restore CS8629 // Nullable value type may be null.
    }

    private byte[] GetSubImageBuffer(RunLengthTwoParts twoPartBuffer, Bitmap nbmp, SubtitleRecord p,
        ContentAlignment alignment, Point? overridePosition)
    {
        var ms = new MemoryStream();

        // sup picture data size
        WriteEndianWord(twoPartBuffer.Length + 34, ms);

        // first display control sequence table address
        var startDisplayControlSequenceTableAddress = twoPartBuffer.Length + 4;
        WriteEndianWord(startDisplayControlSequenceTableAddress, ms);

        // Write image
        const int imageTopFieldDataAddress = 4;
        if (twoPartBuffer.Buffer1 != null)
        {
            ms.Write(twoPartBuffer.Buffer1, 0, twoPartBuffer.Buffer1.Length);
            var imageBottomFieldDataAddress = 4 + twoPartBuffer.Buffer1.Length;
            if (twoPartBuffer.Buffer2 != null) ms.Write(twoPartBuffer.Buffer2, 0, twoPartBuffer.Buffer2.Length);

            // Write zero delay
            ms.WriteByte(0);
            ms.WriteByte(0);

            // next display control sequence table address (use current is last)
            WriteEndianWord(startDisplayControlSequenceTableAddress + 24,
                ms); // start of display control sequence table address

            // Control command start
            ms.WriteByte(false ? (byte)0 : (byte)1);

            // Control command 3 = SetColor
            WriteColors(ms); // 3 bytes

            // Control command 4 = SetContrast
            WriteContrast(ms); // 3 bytes

            // Control command 5 = SetDisplayArea
            WriteDisplayArea(ms, nbmp, alignment, overridePosition); // 7 bytes

            // Control command 6 = SetPixelDataAddress
            WritePixelDataAddress(ms, imageTopFieldDataAddress, imageBottomFieldDataAddress); // 5 bytes
        }

        // Control command exit
        ms.WriteByte(255); // 1 byte

        // Control Sequence Table
        // Write delay - subtitle duration
        WriteEndianWord(Convert.ToInt32(p.Duration * 90.0) >> 10, ms);

        // next display control sequence table address (use current is last)
        WriteEndianWord(startDisplayControlSequenceTableAddress + 24, ms); // start of display control sequence table address

        // Control command 2 = StopDisplay
        ms.WriteByte(2);

        // extra byte - for compatibility with gpac/MP4BOX
        ms.WriteByte(255); // 1 byte

        return ms.ToArray();
    }

    public void WriteParagraph(SubtitleRecord p, SKBitmap bmp, ContentAlignment alignment, Point? overridePosition = null) // inspired by code from SubtitleCreator
    {
        var startTime = TimeSpan.FromMilliseconds((long)p.StartTime);
        // timestamp: 00:00:33:900, filepos: 000000000
        _idx.AppendLine($"timestamp: {startTime.Hours:00}:{startTime.Minutes:00}:{startTime.Seconds:00}:{startTime.Milliseconds:000}, filepos: {_subFile?.Position.ToString("X").PadLeft(9, '0').ToLowerInvariant()}");

        var nbmp = new Bitmap(bmp);
        _emphasis2 = nbmp.ConvertToFourColors(_background, _pattern, _emphasis1, _useInnerAntiAliasing);
        var twoPartBuffer = nbmp.RunLengthEncodeForDvd(_background, _pattern, _emphasis1, _emphasis2);
        var imageBuffer = GetSubImageBuffer(twoPartBuffer, nbmp, p, alignment, overridePosition);

        var bufferIndex = 0;
        var vobSubId = (byte)_languageStreamId;
        var mwsub = new MemWriter(200000);
        var subHeader = new byte[30];
        var ts = new byte[4];

        // Lent from "Son2VobSub" by Alain Vielle and Petr Vyskocil
        // And also from Sup2VobSub by Emmel
        subHeader[0] = 0x00; // MPEG 2 PACK HEADER
        subHeader[1] = 0x00;
        subHeader[2] = 0x01;
        subHeader[3] = 0xba;
        subHeader[4] = 0x44;
        subHeader[5] = 0x02;
        subHeader[6] = 0xc4;
        subHeader[7] = 0x82;
        subHeader[8] = 0x04;
        subHeader[9] = 0xa9;
        subHeader[10] = 0x01;
        subHeader[11] = 0x89;
        subHeader[12] = 0xc3;
        subHeader[13] = 0xf8;
        subHeader[14] = 0x00; // PES
        subHeader[15] = 0x00;
        subHeader[16] = 0x01;
        subHeader[17] = 0xbd;

        var packetSize = imageBuffer.Length;
        long toWrite = packetSize; // Image buffer + control sequence length
        var header0 = true;

        while (toWrite > 0)
        {
            long headerSize;
            if (header0)
            {
                header0 = false;

                // This is only for first packet
                subHeader[20] = 0x81; // mark as original
                subHeader[21] = 0x80; // first packet: PTS
                subHeader[22] = 0x05; // PES header data length

                // PTS (90kHz):
                //--------------
                subHeader[23] = (byte)((ts[3] & 0xc0) >> 5 | 0x21);
                subHeader[24] = (byte)((ts[3] & 0x3f) << 2 | (ts[2] & 0xc0) >> 6);
                subHeader[25] = (byte)((ts[2] & 0x3f) << 2 | (ts[1] & 0x80) >> 6 | 0x01);
                subHeader[26] = (byte)((ts[1] & 0x7f) << 1 | (ts[0] & 0x80) >> 7);
                subHeader[27] = (byte)((ts[0] & 0x7f) << 1 | 0x01);

                const string pre = "0010"; // 0011 or 0010 ? (KMPlayer will not understand 0011!!!)
                var newPts = (long)(startTime.TotalSeconds * 90000.0);
                var bString = Convert.ToString(newPts, 2).PadLeft(33, '0');
                var fiveBytesString = pre + bString.Substring(0, 3) + "1" + bString.Substring(3, 15) + "1" +
                                      bString.Substring(18, 15) + "1";
                for (var i = 0; i < 5; i++)
                    subHeader[23 + i] = Convert.ToByte(fiveBytesString.Substring(i * 8, 8), 2);

                subHeader[28] = vobSubId;
                headerSize = 29;
            }
            else
            {
                subHeader[20] = 0x81; // mark as original
                subHeader[21] = 0x00; // no PTS
                subHeader[22] = 0x00; // header data length
                subHeader[23] = vobSubId;
                headerSize = 24;
            }

            if ((toWrite + headerSize) <= 0x800)
            {
                // write whole image in one 0x800 part
                var j = (headerSize - 20) + toWrite;
                subHeader[18] = (byte)(j / 0x100);
                subHeader[19] = (byte)(j % 0x100);

                // First Write header
                for (var x = 0; x < headerSize; x++)
                    mwsub.WriteByte(subHeader[x]);

                // Write Image Data
                for (var x = 0; x < toWrite; x++)
                    mwsub.WriteByte(imageBuffer[bufferIndex++]);

                // Pad remaining space
                var paddingSize = 0x800 - headerSize - toWrite;
                for (var x = 0; x < paddingSize; x++)
                    mwsub.WriteByte(0xff);

                toWrite = 0;
            }
            else
            {
                // write multiple parts
                var blockSize = 0x800 - headerSize;
                var j = (headerSize - 20) + blockSize;
                subHeader[18] = (byte)(j / 0x100);
                subHeader[19] = (byte)(j % 0x100);

                // First Write header
                for (var x = 0; x < headerSize; x++)
                    mwsub.WriteByte(subHeader[x]);

                // Write Image Data
                for (var x = 0; x < blockSize; x++)
                    mwsub.WriteByte(imageBuffer[bufferIndex++]);

                toWrite -= blockSize;
            }
        }

        // Write whole memory stream to file
        var endPosition = mwsub.GetPosition();
        mwsub.GotoBegin();
        _subFile?.Write(mwsub.GetBuf(), 0, (int)endPosition);
    }

    private static void WritePixelDataAddress(Stream stream, int imageTopFieldDataAddress, int imageBottomFieldDataAddress)
    {
        stream.WriteByte(6);
        WriteEndianWord(imageTopFieldDataAddress, stream);
        WriteEndianWord(imageBottomFieldDataAddress, stream);
    }

    private void WriteDisplayArea(Stream stream, Bitmap nbmp, ContentAlignment alignment, Point? overridePosition)
    {
        stream.WriteByte(5);

        // Write 6 bytes of area - starting X, ending X, starting Y, ending Y, each 12 bits
        var startX = (ushort)((_screenWidth - nbmp.Width) / 2);
        var startY = (ushort)(_screenHeight - nbmp.Height - _bottomMargin);

        if (alignment == ContentAlignment.TopLeft || alignment == ContentAlignment.TopCenter || alignment == ContentAlignment.TopRight)
        {
            startY = (ushort)_bottomMargin;
        }

        if (alignment == ContentAlignment.MiddleLeft || alignment == ContentAlignment.MiddleCenter ||
            alignment == ContentAlignment.MiddleRight)
        {
            startY = (ushort)(_screenHeight / 2 - nbmp.Height / 2);
        }

        if (alignment == ContentAlignment.TopLeft || alignment == ContentAlignment.MiddleLeft ||
            alignment == ContentAlignment.BottomLeft)
        {
            startX = (ushort)_leftRightMargin;
        }

        if (alignment == ContentAlignment.TopRight || alignment == ContentAlignment.MiddleRight ||
            alignment == ContentAlignment.BottomRight)
        {
            startX = (ushort)(_screenWidth - nbmp.Width - _leftRightMargin);
        }

        if (overridePosition != null &&
            overridePosition.Value.X >= 0 && overridePosition.Value.X < _screenWidth &&
            overridePosition.Value.Y >= 0 && overridePosition.Value.Y < _screenHeight)
        {
            startX = (ushort)overridePosition.Value.X;
            startY = (ushort)overridePosition.Value.Y;
        }

        var endX = (ushort)(startX + nbmp.Width - 1);
        var endY = (ushort)(startY + nbmp.Height - 1);

        WriteEndianWord((ushort)(startX << 4 | endX >> 8), stream); // 16 - 12 start x + 4 end x
        WriteEndianWord((ushort)(endX << 8 | startY >> 4), stream); // 16 - 8 endx + 8 starty
        WriteEndianWord((ushort)(startY << 12 | endY), stream); // 16 - 4 start y + 12 end y
    }

    /// <summary>
    /// Directly provides the four contrast (alpha blend) values to associate with the four pixel values. One nibble per pixel value for a total of 2 bytes. 0x0 = transparent, 0xF = opaque
    /// </summary>
    private void WriteContrast(Stream stream)
    {
        stream.WriteByte(4);
        stream.WriteByte((byte)((_emphasis2.Alpha << 4) | _emphasis1.Alpha)); // emphasis2 + emphasis1
        stream.WriteByte((byte)((_pattern.Alpha << 4) | _background.Alpha)); // pattern + background
    }

    /// <summary>
    /// provides four indices into the CLUT for the current PGC to associate with the four pixel values. One nibble per pixel value for a total of 2 bytes.
    /// </summary>
    private static void WriteColors(Stream stream)
    {
        // Index to palette
        const byte emphasis2 = 3;
        const byte emphasis1 = 2;
        const byte pattern = 1;
        const byte background = 0;

        stream.WriteByte(3);
        stream.WriteByte((emphasis2 << 4) | emphasis1); // emphasis2 + emphasis1
        stream.WriteByte((pattern << 4) | background); // pattern + background
    }

    public void WriteIdxFile()
    {
        var idxFileName = _subFileName.Substring(0, _subFileName.Length - 3) + "idx";
        File.WriteAllText(idxFileName, _idx.ToString().Trim());
    }

    private StringBuilder CreateIdxHeader()
    {
        var sb = new StringBuilder();
        sb.AppendLine(@"# VobSub index file, v7 (do not modify this line!)
#
# To repair desynchronization, you can insert gaps this way:
# (it usually happens after vob id changes)
#
#    delay: [sign]hh:mm:ss:ms
#
# Where:
#    [sign]: +, - (optional)
#    hh: hours (0 <= hh)
#    mm/ss: minutes/seconds (0 <= mm/ss <= 59)
#    ms: milliseconds (0 <= ms <= 999)
#
#    Note: You can't position a sub before the previous with a negative value.
#
# You can also modify timestamps or delete a few subs you don't like.
# Just make sure they stay in increasing order.

# Settings

# Original frame size
size: " + _screenWidth + "x" + _screenHeight + @"

# Origin, relative to the upper-left corner, can be overloaded by aligment
org: 0, 0

# Image scaling (hor,ver), origin is at the upper-left corner or at the alignment coord (x, y)
scale: 100%, 100%

# Alpha blending
alpha: 100%

# Smoothing for very blocky images (use OLD for no filtering)
smooth: OFF

# In milliseconds
fadein/out: 50, 50

# Force subtitle placement relative to (org.x, org.y)
align: OFF at LEFT TOP

# For correcting non-progressive desync. (in milliseconds or hh:mm:ss:ms)
# Note: Not effective in DirectVobSub, use 'delay: ... ' instead.
time offset: 0

# ON: displays only forced subtitles, OFF: shows everything
forced subs: OFF

# The original palette of the DVD
palette: 000000, " + ToHexColor(_pattern) + ", " + ToHexColor(_emphasis1) + ", " + ToHexColor(_emphasis2) +
                      @", 828282, 828282, 828282, ffffff, 828282, bababa, 828282, 828282, 828282, 828282, 828282, 828282

# Custom colors (transp idxs and the four colors)
custom colors: OFF, tridx: 0000, colors: 000000, 000000, 000000, 000000

# Language index in use
langidx: 0

# " + _languageName + @"
id: " + _languageNameShort + @", index: 0
# Decomment next line to activate alternative name in DirectVobSub / Windows Media Player 6.x
# alt: " + _languageName + @"
# Vob/Cell ID: 1, 1 (PTS: 0)");
        return sb;
    }

    private static string ToHexColor(SKColor c)
    {
        return (c.Red.ToString("X2") + c.Green.ToString("X2") + c.Blue.ToString("X2")).ToLowerInvariant();
    }

    private void ReleaseManagedResources()
    {
        _subFile?.Dispose();
        _subFile = null;
    }

    public void Dispose()
    {
        Dispose(true);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            ReleaseManagedResources();
        }
    }
}