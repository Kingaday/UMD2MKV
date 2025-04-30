using SkiaSharp;

namespace UMD2MKV.SubtitleEdit;

    public class RunLengthTwoParts
    {
        public byte[]? Buffer1 { get; set; }
        public byte[]? Buffer2 { get; set; }
        public int? Length => Buffer1?.Length + Buffer2?.Length;
    }

    //based on NikseBitmap from subtitleedit but using skiasharp instead of system.imaging.bitmap for cross compatibility
    public class Bitmap
    {
        private readonly SKBitmap _bitmap;
        private readonly byte[] _bitmapData;
        private int _pixelAddress;
        public int Width => _bitmap.Width;
        public int Height => _bitmap.Height;

        public Bitmap(SKBitmap bitmap)
        {
            _bitmap = bitmap.Copy();
            _bitmapData = _bitmap.Bytes;
        }

        private SKColor GetPixel(int x, int y)
        {
            return _bitmap.GetPixel(x, y);
        }
        private void SetPixel(int x, int y, SKColor color)
        {
            _bitmap.SetPixel(x, y, color);
        }
        private static SKColor GetOutlineColor(SKColor borderColor)
        {
            return borderColor.Red + borderColor.Green + borderColor.Blue < 30 ? new SKColor(75, 75, 75,200) : new SKColor( borderColor.Red, borderColor.Green, borderColor.Blue,150);
        }

        /// <summary>
        /// Convert an x-color image to four colors, for e.g. DVD sub pictures.
        /// </summary>
        /// <param name="background">Background color</param>
        /// <param name="pattern">Pattern color, normally white or yellow</param>
        /// <param name="emphasis1">Emphasis 1, normally black or near black (border)</param>
        /// <param name="useInnerAntialize"></param>
        public SKColor ConvertToFourColors(SKColor background, SKColor pattern, SKColor emphasis1, bool useInnerAntialize)
        {
            var backgroundBuffer = new byte[4];
            backgroundBuffer[0] = background.Blue;
            backgroundBuffer[1] = background.Green;
            backgroundBuffer[2] = background.Red;
            backgroundBuffer[3] = background.Green;

            var patternBuffer = new byte[4];
            patternBuffer[0] = pattern.Blue;
            patternBuffer[1] = pattern.Green;
            patternBuffer[2] = pattern.Red;
            patternBuffer[3] = pattern.Alpha;

            var emphasis1Buffer = new byte[4];
            emphasis1Buffer[0] = emphasis1.Blue;
            emphasis1Buffer[1] = emphasis1.Green;
            emphasis1Buffer[2] = emphasis1.Red;
            emphasis1Buffer[3] = emphasis1.Alpha;

            var emphasis2Buffer = new byte[4];
            var emphasis2 = GetOutlineColor(emphasis1);
            if (!useInnerAntialize)
            {
                emphasis2Buffer[0] = emphasis2.Blue;
                emphasis2Buffer[1] = emphasis2.Green;
                emphasis2Buffer[2] = emphasis2.Red;
                emphasis2Buffer[3] = emphasis2.Alpha;
            }

            for (var i = 0; i < _bitmapData.Length; i += 4)
            {
                var smallestDiff = 10000;
                var buffer = backgroundBuffer;
                if (backgroundBuffer[3] == 0 && _bitmapData[i + 3] < 10) // transparent
                {
                }
                else
                {
                    var patternDiff = Math.Abs(patternBuffer[0] - _bitmapData[i]) + Math.Abs(patternBuffer[1] - _bitmapData[i + 1]) + Math.Abs(patternBuffer[2] - _bitmapData[i + 2]) + Math.Abs(patternBuffer[3] - _bitmapData[i + 3]);
                    if (patternDiff < smallestDiff)
                    {
                        smallestDiff = patternDiff;
                        buffer = patternBuffer;
                    }

                    var emphasis1Diff = Math.Abs(emphasis1Buffer[0] - _bitmapData[i]) + Math.Abs(emphasis1Buffer[1] - _bitmapData[i + 1]) + Math.Abs(emphasis1Buffer[2] - _bitmapData[i + 2]) + Math.Abs(emphasis1Buffer[3] - _bitmapData[i + 3]);
                    if (useInnerAntialize)
                    {
                        if (emphasis1Diff - 20 < smallestDiff)
                            buffer = emphasis1Buffer;
                    }
                    else
                    {
                        if (emphasis1Diff < smallestDiff)
                        {
                            smallestDiff = emphasis1Diff;
                            buffer = emphasis1Buffer;
                        }

                        var emphasis2Diff = Math.Abs(emphasis2Buffer[0] - _bitmapData[i]) + Math.Abs(emphasis2Buffer[1] - _bitmapData[i + 1]) + Math.Abs(emphasis2Buffer[2] - _bitmapData[i + 2]) + Math.Abs(emphasis2Buffer[3] - _bitmapData[i + 3]);
                        if (emphasis2Diff < smallestDiff)
                        {
                            buffer = emphasis2Buffer;
                        }
                        else if (_bitmapData[i + 3] >= 10 && _bitmapData[i + 3] < 90) // anti-alias
                        {
                            buffer = emphasis2Buffer;
                        }
                    }
                }
                Buffer.BlockCopy(buffer, 0, _bitmapData, i, 4);
            }

            return useInnerAntialize ? VobSubAntialize(pattern, emphasis1) : emphasis2;
        }

        private SKColor VobSubAntialize(SKColor pattern, SKColor emphasis1)
        {
            var r = (byte)Math.Round(((pattern.Red * 2.0 + emphasis1.Red) / 3.0));
            var g = (byte)Math.Round(((pattern.Green * 2.0 + emphasis1.Green) / 3.0));
            var b = (byte)Math.Round(((pattern.Blue * 2.0 + emphasis1.Blue) / 3.0));
            var antializeColor = new SKColor(r, g, b);

            for (var y = 1; y < Height - 1; y++)
            {
                for (var x = 1; x < Width - 1; x++)
                {
                    if (GetPixel(x, y) == pattern)
                    {
                        if (GetPixel(x - 1, y) == emphasis1 && GetPixel(x, y - 1) == emphasis1)
                        {
                            SetPixel(x, y, antializeColor);
                        }
                        else if (GetPixel(x - 1, y) == emphasis1 && GetPixel(x, y + 1) == emphasis1)
                        {
                            SetPixel(x, y, antializeColor);
                        }
                        else if (GetPixel(x + 1, y) == emphasis1 && GetPixel(x, y + 1) == emphasis1)
                        {
                            SetPixel(x, y, antializeColor);
                        }
                        else if (GetPixel(x + 1, y) == emphasis1 && GetPixel(x, y - 1) == emphasis1)
                        {
                            SetPixel(x, y, antializeColor);
                        }
                    }
                }
            }

            return antializeColor;
        }

        public RunLengthTwoParts RunLengthEncodeForDvd(SKColor background, SKColor pattern, SKColor emphasis1, SKColor emphasis2)
        {
            /*var backgroundBuffer = new byte[4];
            backgroundBuffer[0] = background.Blue;
            backgroundBuffer[1] = background.Green;
            backgroundBuffer[2] = background.Red;
            backgroundBuffer[3] = background.Alpha;*/

            var patternBuffer = new byte[4];
            patternBuffer[0] = pattern.Blue;
            patternBuffer[1] = pattern.Green;
            patternBuffer[2] = pattern.Red;
            patternBuffer[3] = pattern.Alpha;

            var emphasis1Buffer = new byte[4];
            emphasis1Buffer[0] = emphasis1.Blue;
            emphasis1Buffer[1] = emphasis1.Green;
            emphasis1Buffer[2] = emphasis1.Red;
            emphasis1Buffer[3] = emphasis1.Alpha;

            var emphasis2Buffer = new byte[4];
            emphasis2Buffer[0] = emphasis2.Blue;
            emphasis2Buffer[1] = emphasis2.Green;
            emphasis2Buffer[2] = emphasis2.Red;
            emphasis2Buffer[3] = emphasis2.Alpha;

            var bufferEqual = new byte[Width * Height];
            var bufferUnEqual = new byte[Width * Height];
            var indexBufferEqual = 0;
            var indexBufferUnEqual = 0;

            _pixelAddress = -4;
            for (var y = 0; y < Height; y++)
            {
                int index;
                byte[] buffer;
                if (y % 2 == 0)
                {
                    index = indexBufferEqual;
                    buffer = bufferEqual;
                }
                else
                {
                    index = indexBufferUnEqual;
                    buffer = bufferUnEqual;
                }

                var indexHalfNibble = false;
                var lastColor = -1;
                var count = 0;

                for (var x = 0; x < Width; x++)
                {
                    var color = GetDvdColor(patternBuffer, emphasis1Buffer, emphasis2Buffer);

                    if (lastColor == -1)
                    {
                        lastColor = color;
                        count = 1;
                    }
                    else if (lastColor == color && count < 64) // only allow up to 63 run-length (for SubtitleCreator compatibility)
                        count++;
                    else
                    {
                        WriteRle(ref indexHalfNibble, lastColor, count, ref index, buffer);
                        lastColor = color;
                        count = 1;
                    }
                }

                if (count > 0)
                    WriteRle(ref indexHalfNibble, lastColor, count, ref index, buffer);

                if (indexHalfNibble)
                    index++;

                if (y % 2 == 0)
                {
                    indexBufferEqual = index;
                    bufferEqual = buffer;
                }
                else
                {
                    indexBufferUnEqual = index;
                    bufferUnEqual = buffer;
                }
            }

            var twoParts = new RunLengthTwoParts { Buffer1 = new byte[indexBufferEqual] };
            Buffer.BlockCopy(bufferEqual, 0, twoParts.Buffer1, 0, indexBufferEqual);
            twoParts.Buffer2 = new byte[indexBufferUnEqual + 2];
            Buffer.BlockCopy(bufferUnEqual, 0, twoParts.Buffer2, 0, indexBufferUnEqual);
            return twoParts;
        }

        private static void WriteRle(ref bool indexHalfNibble, int lastColor, int count, ref int index, byte[] buffer)
        {
            switch (count)
            {
                // 1-3 repetitions
                case <= 0b00000011:
                    WriteOneNibble(buffer, count, lastColor, ref index, ref indexHalfNibble);
                    break;
                // 4-15 repetitions
                case <= 0b00001111:
                    WriteTwoNibbles(buffer, count, lastColor, ref index, indexHalfNibble);
                    break;
                // 4-15 repetitions
                case <= 0b00111111:
                    WriteThreeNibbles(buffer, count, lastColor, ref index, ref indexHalfNibble); // 16-63 repetitions
                    break;
                // 64-255 repetitions
                default:
                {
                    var factor = count / 255;
                    for (var i = 0; i < factor; i++)
                    {
                        WriteFourNibbles(buffer, 0xff, lastColor, ref index, indexHalfNibble);
                    }

                    var rest = count % 255;
                    if (rest > 0)
                    {
                        WriteFourNibbles(buffer, rest, lastColor, ref index, indexHalfNibble);
                    }

                    break;
                }
            }
        }
        private static void WriteFourNibbles(byte[] buffer, int count, int color, ref int index, bool indexHalfNibble)
        {
            var n = (count << 2) + color;
            if (indexHalfNibble)
            {
                index++;
                var firstNibble = (byte)(n >> 4);
                buffer[index] = firstNibble;
                index++;
                var secondNibble = (byte)((n & 0b00001111) << 4);
                buffer[index] = secondNibble;
            }
            else
            {
                var firstNibble = (byte)(n >> 8);
                buffer[index] = firstNibble;
                index++;
                var secondNibble = (byte)(n & 0b11111111);
                buffer[index] = secondNibble;
                index++;
            }
        }
        private static void WriteThreeNibbles(byte[] buffer, int count, int color, ref int index, ref bool indexHalfNibble)
        {
            //Value     Bits   n=length, c=color
            //16-63     12     0 0 0 0 n n n n n n c c           (one and a half byte)
            var n = (ushort)((count << 2) + color);
            if (indexHalfNibble)
            {
                index++; // there should already be zeroes in last nibble
                buffer[index] = (byte)n;
                index++;
            }
            else
            {
                buffer[index] = (byte)(n >> 4);
                index++;
                buffer[index] = (byte)((n & 0b00011111) << 4);
            }

            indexHalfNibble = !indexHalfNibble;
        }

        private static void WriteTwoNibbles(byte[] buffer, int count, int color, ref int index, bool indexHalfNibble)
        {
            //Value      Bits   n=length, c=color
            //4-15       8      0 0 n n n n c c                   (one byte)
            var n = (byte)((count << 2) + color);
            if (indexHalfNibble)
            {
                var firstNibble = (byte)(n >> 4);
                buffer[index] = (byte)(buffer[index] | firstNibble);
                var secondNibble = (byte)((n & 0b00001111) << 4);
                index++;
                buffer[index] = secondNibble;
            }
            else
            {
                buffer[index] = n;
                index++;
            }
        }

        private static void WriteOneNibble(byte[] buffer, int count, int color, ref int index, ref bool indexHalfNibble)
        {
            var n = (byte)((count << 2) + color);
            if (indexHalfNibble)
            {
                buffer[index] = (byte)(buffer[index] | n);
                index++;
            }
            else
            {
                buffer[index] = (byte)(n << 4);
            }

            indexHalfNibble = !indexHalfNibble;
        }

        private int GetDvdColor(byte[] pattern, byte[] emphasis1, byte[] emphasis2)
        {
            _pixelAddress += 4;
            int a = _bitmapData[_pixelAddress + 3];
            int r = _bitmapData[_pixelAddress + 2];
            int g = _bitmapData[_pixelAddress + 1];
            int b = _bitmapData[_pixelAddress];

            if (pattern[0] == b && pattern[1] == g && pattern[2] == r && pattern[3] == a)
            {
                return 1;
            }

            if (emphasis1[0] == b && emphasis1[1] == g && emphasis1[2] == r && emphasis1[3] == a)
            {
                return 2;
            }

            if (emphasis2[0] == b && emphasis2[1] == g && emphasis2[2] == r && emphasis2[3] == a)
            {
                return 3;
            }

            return 0;
        }
    }
