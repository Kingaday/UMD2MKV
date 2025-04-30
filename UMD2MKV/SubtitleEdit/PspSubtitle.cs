using System.Text;
using SkiaSharp;
using StreamWriter = System.IO.StreamWriter;

namespace UMD2MKV.SubtitleEdit;

public class PspSubtitle(string outputPath, string srtfilename)
{
    private List<SubtitleRecord> _srtRecords = [];
    private byte[]? _buf;

    private static byte[] ReadBytes(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var index = 0;
        var fileLength = fs.Length;
        var count = (int)fileLength;
        var bytes = new byte[count];
        while (count > 0)
        {
            var n = fs.Read(bytes, index, count);
            if (n == 0)
                throw new InvalidOperationException("eof");
            index += n;
            count -= n;
        }
        return bytes;
    }
    public async Task<bool> Process(string fileName, OutputSubtitleType outputSubtitleType)
    {
        try
        { 
            _buf = ReadBytes(fileName);
            if (_buf[30] == 0x89 && _buf[31] == 0x50 && _buf[32] == 0x4E && _buf[33] == 0x47)
            { 
                GenerateSubtitleRecords();
                foreach (var srtrecord in _srtRecords)
                    WriteSubtitlePng(srtrecord.Index);

                switch (outputSubtitleType)
                {
                    case OutputSubtitleType.Srt:
                        await WriteSrt();
                        break;
                    case OutputSubtitleType.VobSub:
                        await WriteVobSub();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(outputSubtitleType), outputSubtitleType, null);
                }
                return true;
            }
        }
        catch
        {
            return false;
        }
        return false;
    }

    private async Task WriteSrt()
    {
        //write srt
        //TODO OCR here
        await using var writer = new StreamWriter(Path.Combine(outputPath,srtfilename+".srt"));
        foreach (var srtrecord in _srtRecords)
        {
            await writer.WriteLineAsync($"{srtrecord.Index}");
            await writer.WriteLineAsync($"{FormatTime(srtrecord.StartTime)} --> {FormatTime(srtrecord.EndTime)}");
            await writer.WriteLineAsync($"{srtrecord.Index+".png"}\n");
        }
    }

    private async Task<bool> WriteVobSub()
    {
        try
        {
            // add all as english for now ... find way to retrieve language from psp sub files
            var language = new SubtitleLanguage("en-US","English", "English");
            using var vobSubWriter = new VobSubWriter(Path.Combine(outputPath, "vobsub.sub"), 720, 480, 20, 20, 32, SKColors.White, SKColors.Red, true, language);
            for (var index = 0; index < _srtRecords.Count; index++)
            {
                using var skBitmap = SKBitmap.Decode(Path.Combine(outputPath, $"{index+1}.png"));
                if( skBitmap != null)
                    vobSubWriter.WriteParagraph(_srtRecords[index], skBitmap, VobSubWriter.ContentAlignment.BottomCenter);
            }
            vobSubWriter.WriteIdxFile();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
       
        return true;
    }
    private void GenerateSubtitleRecords()
    {
        var index = 30;
        while (_buf != null && index < _buf.Length - 14)
        {
            // PNG header: 89 50 4E 47 0D 0A 1A 0A
            if (_buf[index] == 0x89 && 
                _buf[index + 1] == 0x50 && 
                _buf[index + 2] == 0x4E &&
                _buf[index + 3] == 0x47 &&
                _buf[index + 4] == 0x0D &&
                _buf[index + 5] == 0x0A &&
                _buf[index + 6] == 0x1A &&
                _buf[index + 7] == 0x0A)
            {
                var startHour = _buf[index - 29];
                var startMinutes = _buf[index - 28];
                var startSeconds = _buf[index - 27];
                var startMilliseconds = _buf[index - 26];

                var durationHour = _buf[index - 20];
                var durationMinutes = _buf[index - 19];
                var durationSeconds = _buf[index - 18];
                var durationMilliseconds = _buf[index - 17];
                var start = (startHour * 60 * 60 * 1000.0 + startMinutes * 60 * 1000.0 + startSeconds * 1000.0 +
                             startMilliseconds)/100;
                var duration = (durationHour * 60 * 60 * 1000.0 + durationMinutes * 60 * 1000.0 +
                                durationSeconds * 1000.0 + durationMilliseconds);
                var p = new SubtitleRecord()
                {
                    Index = index,
                    StartTime =   start,
                    EndTime = start + (duration),
                    Duration = duration
                };

                index += 8;
                    
                _srtRecords.Add(p);

                while (index - 12 < _buf.Length - 14)
                {
                    if (_buf[index] == 0 &&
                        _buf[index + 1] == 0 &&
                        _buf[index + 2] == 0 &&
                        _buf[index + 3] == 0)
                    {
                        // read chunk type
                        var chunkType = Encoding.ASCII.GetString(_buf, index + 4, 4);
                        if (chunkType == "IEND")
                        {
                            index += 8;
                            break;
                        }
                    }
                    index++;
                }
            }
            else
                index++;
        }
        _srtRecords = _srtRecords.OrderBy(p => p.StartTime).ToList();
        ReOrder();
        
    }
    private static string FormatTime(double milliseconds)
    {
        var time = TimeSpan.FromMilliseconds(milliseconds);
        return $"{time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2},{time.Milliseconds:D3}";
    }
    private void ReOrder(int startNumber = 1)
    {
        var number = startNumber;
        var l = _srtRecords.Count + number;
        while (number < l)
        {
            var p = _srtRecords[number - startNumber];
            p.Index = number++;
        }
    }
    private void WriteSubtitlePng(int index2)
    {
        var filename = index2 + ".png";
        var index = 30;
        var srtIndex = 0;
        while (_buf != null && index < _buf.Length - 14)
        {
            // PNG header: 89 50 4E 47 0D 0A 1A 0A
            if (_buf[index] == 0x89 &&
                _buf[index + 1] == 0x50 &&
                _buf[index + 2] == 0x4E &&
                _buf[index + 3] == 0x47 &&
                _buf[index + 4] == 0x0D &&
                _buf[index + 5] == 0x0A &&
                _buf[index + 6] == 0x1A &&
                _buf[index + 7] == 0x0A)
            {
                var start = index;
                index += 8;

                while (index - 12 < _buf.Length - 14)
                {
                    if (_buf[index] == 0 &&
                        _buf[index + 1] == 0 &&
                        _buf[index + 2] == 0 &&
                        _buf[index + 3] == 0)
                    {
                        // read chunk type
                        var chunkType = Encoding.ASCII.GetString(_buf, index + 4, 4);
                        if (chunkType == "IEND")
                        {
                            index += 8;

                            if (srtIndex == index2)
                            {
                                index += 4; // CRC
                                var b = new byte[index - start];
                                Array.Copy(_buf, start, b, 0, b.Length);
                                File.WriteAllBytes(Path.Combine(outputPath,filename), b);
                                return;
                            }
                            srtIndex++;
                            break;
                        }
                    }
                    index++;
                }
            }
            else
                index++;
        }
    }
}