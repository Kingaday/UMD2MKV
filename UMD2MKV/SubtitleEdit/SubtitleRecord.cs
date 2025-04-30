namespace UMD2MKV.SubtitleEdit;

public class SubtitleRecord
{
    public double StartTime { get; init; }
    public double EndTime { get; init; }
    public double Duration { get; set; }
    public int Index { get; set; }
}