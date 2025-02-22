using CommunityToolkit.Maui.Storage;
using UMD2MKV.FFmpeg;
using UMD2MKV.VGMToolbox;

namespace UMD2MKV;
public partial class MainPage
{
    public MainPage()
    {
        IsoSelected = false;
        OutputSelected = false;
        InitializeComponent();
        BindingContext = this;
    }

    private bool _lossy = true;
    private bool _uiEnabled = true;
    private bool _isoSelected;
    private bool _outputSelected;

    public bool UiEnabled
    {
        get => _uiEnabled;
        set
        {
            _uiEnabled = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsConvertButtonEnabled));
        }
    }
    public bool IsoSelected
    {
        get => _isoSelected;
        set
        {
            _isoSelected = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsConvertButtonEnabled));
        }
    }
    public bool OutputSelected
    {
        get => _outputSelected;
        set
        {
            _outputSelected = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsConvertButtonEnabled));
        }
    }
    public bool IsConvertButtonEnabled => IsoSelected && OutputSelected && UiEnabled;
    private async void OnISOSelectClicked(object sender, EventArgs e)
    {
        try
        {
            var customFileType = new FilePickerFileType(
                new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, [".iso"] },
                    { DevicePlatform.MacCatalyst, ["public.iso-image"] }
                });
            PickOptions options = new()
            {
                PickerTitle = "Please select an UMD movie iso",
                FileTypes = customFileType
            };
            var path = await PickFileAsync(options); 
            IsoPath.Text = path?.FullPath??string.Empty;
            if(IsoPath.Text != string.Empty)
                IsoSelected = true;
        }
        catch (Exception ex)
        {
            ProgressTxt.Text = ex.Message;
            IsoSelected = false;
        }
    }
    private async void OnOutputSelectClicked(object? sender, EventArgs e)
    {
        try
        {
            var path = await PickFolderAsync();
            if (FileUtils.FileUtils.DirectoryHasFiles(path?.Folder?.Path??string.Empty))
            {
                ProgressTxt.Text = "The output folder needs to be empty";
                return;
            }
            OutputPath.Text = path?.Folder?.Path??string.Empty;
            if(OutputPath.Text != string.Empty)
                OutputSelected = true;
        }
        catch (Exception ex)
        {
            ProgressTxt.Text = ex.Message;
            OutputSelected = false;
        }
    }
    private async void OnConvertClicked(object? sender, EventArgs e)
    {
        try
        {
            //await Subtitles.ExtractPngFromSubtitles(OutputPath.Text);
            UiEnabled = false;
            var start = TimeSpan.Zero;
            var end = TimeSpan.Zero;

            if (SegmentChk.IsChecked)
            {
                if (!TimeSpan.TryParse(StartTime.Text, out start))
                {
                    ProgressTxt.Text = "Invalid start time format.";
                    UiEnabled = true;
                    return;
                }

                if (!TimeSpan.TryParse(EndTime.Text, out end))
                {
                    ProgressTxt.Text = "Invalid start time format.";
                    UiEnabled = true;
                    return;
                }
            }
            if (start <= end)
            {
                var progress = new Progress<int>(percent =>
                {
                    MainThread.BeginInvokeOnMainThread(() => { ProgressBar.Progress = percent / 100.0; });
                });

                //1 find largest .mps file in the iso and copy to output folder using discutils
                ProgressTxt.Text = "Find and copy mps file (slow if you select ISO directly on PSP)";
                var successCopy = await FileUtils.FileUtils.CopyLargestMps(IsoPath.Text, OutputPath.Text, progress);
                if (successCopy)
                {
                    //2 demux audio files  from .mps file using cleaned up vgmtoolbox based code
                    ProgressTxt.Text = "Demuxing audio (atrac3) using code based on VgmToolbox";
                    var successMps = await MpegDemuxWorker.Demux(OutputPath.Text + "/movie.mps", OutputPath.Text, progress);
                    if (successMps)
                    {
                        //3 reencode .oma container (atrac3) audio files using ffmpeg (xabe.ffmpeg)
                        ProgressTxt.Text = "Converting atrac3 using Ffmpeg";
                        var successConvert = await Ffmpeg.ConvertOma(FileUtils.FileUtils.GetFilesWithExtension(OutputPath.Text, "*.oma"), OutputPath.Text, _lossy,CancellationToken.None, progress);
                        if (successConvert)
                        {
                            //4 mux video and encoded audio files into new mkv using ffmpeg (xabe.ffmpeg)
                            //4 if split is selected cut part of video using ffmpeg (xabe.ffmpeg)
                            ProgressTxt.Text = "Muxing video (mps) and audio (aac/flc) in mkv...";
                            var successMux = await Ffmpeg.MergeMpsWithFlacAsync(
                                FileUtils.FileUtils.GetFilesWithExtension(OutputPath.Text, "*.mps").FirstOrDefault(),
                                FileUtils.FileUtils.GetFilesWithExtension(OutputPath.Text, _lossy?"*.aac":"*.flac"), OutputPath.Text,
                                SegmentChk.IsChecked, start, end, progress);
                            if (successMux)
                            {
                                // Demux subtitles, convert to vobsub, mux into mkv using vgmtoolbox incomplete code + xabe.ffmpeg
                                if (SubtitletChk.IsChecked)
                                {
                                    var successSubtitle = await Subtitles.ConvertAndMuxSubtitles(OutputPath.Text);
                                    if (successSubtitle)
                                        Done();
                                    else
                                        ProgressTxt.Text = "Subtitle conversion failed. Halting ... movie without subtitles is available.";
                                }
                                else
                                {
                                    FileUtils.FileUtils.DeleteFilesWithExtension(OutputPath.Text,"*.subs");
                                    Done();
                                }
                            }
                            else
                                ProgressTxt.Text = "Muxing mkv failed. Halting, please restart and try again";
                        }
                        else
                            ProgressTxt.Text = "Converting audio tracks failed. Halting, please restart and try again\"";
                    }
                    else
                        ProgressTxt.Text = "Demuxing mps file failed. Halting, please restart and try again\"";
                }
                else
                    ProgressTxt.Text = "Copying mps file failed. Halting, please restart and try again\"";
            }
            else
            {
                ProgressTxt.Text = "Start/End segment time not correct";
                UiEnabled = true;
            }
        }
        catch (Exception ex)
        {
            ProgressTxt.Text = ex.Message;
        }
    }

    private void Done()
    {
        ProgressTxt.Text = "Done!";
        ProgressBar.Progress = 1;
        UiEnabled = true;
    }

    private async Task<FileResult?> PickFileAsync(PickOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options), "PickOptions cannot be null.");
        try
        {
            var result = await FilePicker.Default.PickAsync(options);
            return result;
        }
        catch (OperationCanceledException)
        {
            ProgressTxt.Text = "File picking was canceled by the user.";
        }
        catch (Exception ex)
        {
            ProgressTxt.Text = $"An error occurred while picking a file: {ex.Message}";
        }
        return null;
    }
    private async Task<FolderPickerResult?> PickFolderAsync()
    {
        try
        {
            var result = await FolderPicker.Default.PickAsync();
            return result;
        }
        catch (OperationCanceledException)
        {
            ProgressTxt.Text = "Folder picking was canceled by the user.";
        }
        catch (Exception ex)
        {
            ProgressTxt.Text = $"An error occurred while picking a folder: {ex.Message}";
        }
        return null;
    }
    private void OnCodecChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (AacRadioButton.IsChecked)
            _lossy = true;
        else if (FlacRadioButton.IsChecked)
            _lossy = false;
    }
}