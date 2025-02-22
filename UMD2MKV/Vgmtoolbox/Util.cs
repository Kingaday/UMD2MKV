using System.Collections;

namespace UMD2MKV.VGMToolbox
{    
    /// <summary>
    /// Class for Parsing Files.
    /// </summary>
    public static class ParseFile
    {
        /// <summary>
        /// Extract a section from the incoming byte array.
        /// </summary>
        /// <param name="sourceArray">Bytes to extract from.</param>
        /// <param name="startingOffset">Offset to begin cutting from.</param>
        /// <param name="lengthToCut">Number of bytes to cut.</param>
        /// <returns>Byte array containing the extracted section.</returns>
        public static byte[] ParseSimpleOffset(byte[] sourceArray, int startingOffset, int lengthToCut)
        {
            var ret = new byte[lengthToCut];
            uint j = 0;

            for (var i = startingOffset; i < startingOffset + lengthToCut; i++)
            {
                ret[j] = sourceArray[i];
                j++;
            }

            return ret;
        }

        /// <summary>
        /// Extract a section from the incoming stream.
        /// </summary>
        /// <param name="stream">Stream to extract the chunk from.</param>
        /// <param name="startingOffset">Offset to begin cutting from.</param>
        /// <param name="lengthToCut">Number of bytes to cut.</param>
        /// <returns>Byte array containing the extracted section.</returns>
        public static byte[] ParseSimpleOffset(Stream stream, int startingOffset, int lengthToCut)
        {
            var currentStreamPosition = stream.Position;

            stream.Seek(startingOffset, SeekOrigin.Begin);
            var br = new BinaryReader(stream);
            var ret = br.ReadBytes(lengthToCut);

            stream.Position = currentStreamPosition;

            return ret;
        }

        /// <summary>
        /// Extract a section from the incoming stream.
        /// </summary>
        /// <param name="stream">Stream to extract the chunk from.</param>
        /// <param name="startingOffset">Offset to begin cutting from.</param>
        /// <param name="lengthToCut">Number of bytes to cut.</param>
        /// <returns>Byte array containing the extracted section.</returns>
        public static byte[] ParseSimpleOffset(Stream stream, long startingOffset, int lengthToCut)
        {
            var currentStreamPosition = stream.Position;

            stream.Seek(startingOffset, SeekOrigin.Begin);
            var br = new BinaryReader(stream);
            var ret = br.ReadBytes(lengthToCut);

            stream.Position = currentStreamPosition;

            return ret;
        }
        
        /// <summary>
        /// Get the offset of the first instance of pSearchBytes after the input offset.
        /// </summary>
        /// <param name="stream">Stream to search.</param>
        /// <param name="startingOffset">Offset to begin searching from.</param>
        /// <param name="searchBytes">Bytes to search for.</param>
        /// <returns>Returns the offset of the first instance of pSearchBytes after the input offset or -1 otherwise.</returns>
        public static long GetNextOffset(Stream stream, long startingOffset, byte[] searchBytes)=>GetNextOffset(stream, startingOffset, searchBytes, true);

        private static long GetNextOffset(Stream stream, long startingOffset, 
            byte[] searchBytes, bool returnStreamToIncomingPosition)
        {
            long initialStreamPosition = 0;

            if (returnStreamToIncomingPosition)
                initialStreamPosition = stream.Position;

            var itemFound = false;
            var absoluteOffset = startingOffset;
            var checkBytes = new byte[Constants.fileReadChunkSize];

            long ret = -1;

            while (!itemFound && (absoluteOffset < stream.Length))
            {
                stream.Position = absoluteOffset;
                stream.ReadExactly(checkBytes, 0, Constants.fileReadChunkSize);
                long relativeOffset = 0;

                while (!itemFound && (relativeOffset < Constants.fileReadChunkSize))
                {
                    if ((relativeOffset + searchBytes.Length) < checkBytes.Length)
                    {
                        var compareBytes = new byte[searchBytes.Length];
                        Array.Copy(checkBytes, relativeOffset, compareBytes, 0, searchBytes.Length);

                        if (CompareSegment(compareBytes, 0, searchBytes))
                        {
                            itemFound = true;
                            ret = absoluteOffset + relativeOffset;
                            break;
                        }
                    }

                    relativeOffset++;
                }

                absoluteOffset += Constants.fileReadChunkSize - searchBytes.Length;
            }

            // return stream to incoming position
            if (returnStreamToIncomingPosition)
                stream.Position = initialStreamPosition;

            return ret;
        }

        public static long[] GetAllOffsets(Stream stream, long startingOffset,
            byte[] searchBytes, bool doOffsetModulo, long offsetModuloDivisor,
            long offsetModuloResult, bool returnStreamToIncomingPosition)
        {
            long initialStreamPosition = 0;

            if (returnStreamToIncomingPosition)
                initialStreamPosition = stream.Position;

            var absoluteOffset = startingOffset;
            var checkBytes = new byte[Constants.fileReadChunkSize];
            var offsetList = new ArrayList();

            while (absoluteOffset < stream.Length)
            {
                stream.Position = absoluteOffset;
                var checkBytesRead = stream.Read(checkBytes, 0, Constants.fileReadChunkSize);
                long relativeOffset = 0;

                while (relativeOffset < checkBytesRead)
                {
                    var actualOffset = absoluteOffset + relativeOffset;

                    if ((!doOffsetModulo) ||
                        (actualOffset % offsetModuloDivisor == offsetModuloResult))
                    {
                        if ((relativeOffset + searchBytes.Length) < checkBytes.Length)
                        {
                            var compareBytes = new byte[searchBytes.Length];
                            Array.Copy(checkBytes, relativeOffset, compareBytes, 0, searchBytes.Length);

                            if (CompareSegment(compareBytes, 0, searchBytes))
                                offsetList.Add(actualOffset);
                        }
                    }

                    relativeOffset++;
                }

                absoluteOffset += Constants.fileReadChunkSize - searchBytes.Length;
            }

            // return stream to incoming position
            if (returnStreamToIncomingPosition)
                stream.Position = initialStreamPosition;

            var ret = (long[])offsetList.ToArray(typeof(long));

            return ret;
        }
        /// <summary>
        /// Get the offset of the first instance of pSearchBytes after the input offset.
        /// </summary>
        /// <param name="bufferToSearch">Byte array to search.</param>
        /// <param name="offset">Offset to begin searching from.</param>
        /// <param name="searchValue">Bytes to search for.</param>
        /// <returns>Returns the offset of the first instance of pSearchBytes after the input offset or -1 otherwise.</returns>
        public static long GetNextOffset(byte[] bufferToSearch, long offset, byte[] searchValue)
        {
            var itemFound = false;
            var absoluteOffset = offset;

            long ret = -1;

            while (!itemFound && (absoluteOffset < (bufferToSearch.Length - searchValue.Length)))
            {
                var compareBytes = new byte[searchValue.Length];
                Array.Copy(bufferToSearch, absoluteOffset, compareBytes, 0, searchValue.Length);

                if (CompareSegment(compareBytes, 0, searchValue))
                {
                    itemFound = true;
                    ret = absoluteOffset;
                    break;
                }

                absoluteOffset++;
            }

            return ret;
        }
        /// <summary>
        /// Compare bytes at input offset to target bytes.
        /// </summary>
        /// <param name="sourceArray">Bytes to compare.</param>
        /// <param name="offset">Offset to begin comparison of pBytes to pTarget.</param>
        /// <param name="target">Target bytes to compare.</param>
        /// <returns>True if the bytes at pOffset match the pTarget bytes.</returns>
        public static bool CompareSegment(byte[] sourceArray, int offset, byte[] target)
        {
            var ret = true;
            uint j = 0;

            if (sourceArray.Length > 0)
            {
                for (var i = offset; i < target.Length; i++)
                {
                    if (sourceArray[i] != target[j])
                    {
                        ret = false;
                        break;
                    }

                    j++;
                }
            }
            else
                ret = false;
            
            return ret;
        }
        public static void ExtractChunkToFile(Stream stream, long startingOffset, long length, string filePath)=>ExtractChunkToFile(stream, startingOffset, length, filePath, false, false);

        /// <summary>
        /// Extracts a section of the incoming stream to a file.
        /// </summary>
        /// <param name="stream">Stream to extract from.</param>
        /// <param name="startingOffset">Offset to begin the cut.</param>
        /// <param name="length">Number of bytes to cut.</param>
        /// <param name="filePath">File path to output the extracted chunk to.</param>
        /// <param name="outputLogFile"></param>
        /// <param name="outputSnakebiteBatchFile"></param>
        private static void ExtractChunkToFile(Stream stream, long startingOffset, long length, string filePath, bool outputLogFile, bool outputSnakebiteBatchFile)
        {
            BinaryWriter? bw = null;
            var fullFilePath = Path.GetFullPath(filePath);
            var fullOutputDirectory = Path.GetDirectoryName(fullFilePath);            

            // create output folder if needed
            if (!Directory.Exists(fullOutputDirectory))
                Directory.CreateDirectory(fullOutputDirectory!);

            // check if file exists and change name as needed
            if (File.Exists(fullFilePath))
            {
                var fileCount = Directory.GetFiles(fullOutputDirectory!, (Path.GetFileNameWithoutExtension(fullFilePath) + "*" + Path.GetExtension(fullFilePath)), SearchOption.TopDirectoryOnly).Length;
                fullFilePath = Path.Combine(fullOutputDirectory!, $"{Path.GetFileNameWithoutExtension(fullFilePath)}_{fileCount:X3}{Path.GetExtension(fullFilePath)}");
            }

            try
            {
                bw = new BinaryWriter(File.Open(fullFilePath, FileMode.Create, FileAccess.Write));

                int read;
                long totalBytes = 0;
                var bytes = new byte[Constants.fileReadChunkSize];
                stream.Seek(startingOffset, SeekOrigin.Begin);

                var maxread = length > bytes.Length ? bytes.Length : (int)length;

                while ((read = stream.Read(bytes, 0, maxread)) > 0)
                {
                    bw.Write(bytes, 0, read);
                    totalBytes += read;

                    maxread = (length - totalBytes) > bytes.Length ? bytes.Length : (int)(length - totalBytes);
                }
            }
            finally
            {
                bw?.Close();
            }
        }
        
        public static long GetVaryingByteValueAtRelativeOffset(Stream inStream, OffsetDescription offsetInfo, long currentOffset)
        {
            var newValueOffset = currentOffset + Byteconversion.GetLongValueFromString(offsetInfo.OffsetValue);
            var newValueLength = Byteconversion.GetLongValueFromString(offsetInfo.OffsetSize);

            return GetVaryingByteValueAtOffset(inStream, newValueOffset, newValueLength, offsetInfo.OffsetByteOrder.Equals(Constants.littleEndianByteOrder));
        }

        private static long GetVaryingByteValueAtOffset(Stream inStream, long valueOffset, long valueLength,
            bool valueIsLittleEndian)=>GetVaryingByteValueAtOffset(inStream, valueOffset, valueLength, valueIsLittleEndian, false);

        private static long GetVaryingByteValueAtOffset(Stream inStream, long valueOffset, long valueLength,
            bool valueIsLittleEndian, bool allowNegativeOffset)
        {
            if (allowNegativeOffset && (valueOffset < 0))
                valueOffset = inStream.Length + valueOffset;

            var newValueBytes = ParseSimpleOffset(inStream, valueOffset, (int)valueLength);

            if (!valueIsLittleEndian)
                Array.Reverse(newValueBytes);

            long newValue = newValueBytes.Length switch
            {
                1 => newValueBytes[0],
                2 => BitConverter.ToUInt16(newValueBytes, 0),
                4 => BitConverter.ToUInt32(newValueBytes, 0),
                _ => -1
            };

            return newValue;
        }
        public static ushort ReadUshortBe(Stream inStream, long offset)
        {
            var val = ParseSimpleOffset(inStream, offset, 2);
            Array.Reverse(val);
            
            return BitConverter.ToUInt16(val, 0);
        }
    }
}