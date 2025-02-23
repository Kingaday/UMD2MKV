namespace UMD2MKV.VGMToolbox;
public abstract class MpegDemuxWorker
{                
    public static async Task<bool> Demux(string mpsPath,string workingDirectory,IProgress<int>? progress = null)
    {
        var demuxOptions = new Mpegstream.DemuxOptionsStruct
        {
            ExtractAudio = true,
            ExtractVideo = false,
            AddHeader = true
        };

        var mpsStream = new SonyPspMpsStream(mpsPath);
        await Task.Run(async () => await mpsStream.DemultiplexStreams(demuxOptions, workingDirectory, progress));
        return true;
    }               
}