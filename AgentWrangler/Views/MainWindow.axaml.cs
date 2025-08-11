using AgentWrangler.Services;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tmds.DBus.Protocol;

namespace AgentWrangler.Views;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    public ObservableCollection<string> ModelList { get; } = new()
    {
        "llama-3.1-8b-instant",
        "llama-3.3-70b-versatile",
        "meta-llama/llama-guard-4-12b",
        "whisper-large-v3",
        "whisper-large-v3-turbo",
        "deepseek-r1-distill-llama-70b (Preview Model)",
        "meta-llama/llama-4-maverick-17b-128e-instruct (Preview Model)",
        "meta-llama/llama-4-scout-17b-16e-instruct (Preview Model)",
        "meta-llama/llama-prompt-guard-2-22m (Preview Model)",
        "meta-llama/llama-prompt-guard-2-86m (Preview Model)",
        "moonshotai/kimi-k2-instruct (Preview Model)",
        "openai/gpt-oss-120b (Preview Model)",
        "openai/gpt-oss-20b (Preview Model)",
        "playai-tts (Preview Model)",
        "playai-tts-arabic (Preview Model)",
        "qwen/qwen3-32b (Preview Model)",
        "compound-beta (Preview System)",
        "compound-beta-mini (Preview System)"
    };

    private string? _selectedModel;
    public string? SelectedModel
    {
        get => _selectedModel;
        set { _selectedModel = value; OnPropertyChanged(); }
    }

    public MainWindow()
    {
        DataContext = this;
        InitializeComponent();
        SelectedModel = ModelList.FirstOrDefault(m => m.StartsWith("compound-beta-mini"));
        var sendButton = this.FindControl<Button>("SendButton");
        sendButton.Click += SendButton_Click;
        var ocrButton = this.FindControl<Button>("OcrButton");
        ocrButton.Click += OcrButton_Click;
        var micToggle = this.FindControl<ToggleButton>("MicTranscribeToggle");
        micToggle.Checked += MicTranscribeToggle_Checked;
        micToggle.Unchecked += MicTranscribeToggle_Unchecked;
    }

    private bool _isMicTranscribing = false;
    private System.Threading.CancellationTokenSource? _micTranscribeCts;

    public void AppendStatus(string message)
    {
        var statusLabel = this.FindControl<TextBlock>("StatusLabel");
        if (statusLabel != null)
        {
            statusLabel.Text = message;
        }
    }

    private async void MicTranscribeToggle_Checked(object? sender, RoutedEventArgs e)
    {
        _isMicTranscribing = true;
        _micTranscribeCts = new System.Threading.CancellationTokenSource();
        AppendStatus("Listening to microphone...");
        var apiKey = ConfigHelper.ReadApiKey();
        var model = CleanModelString(SelectedModel);
        var client = new GroqApiClient(apiKey, model: model);
        var leftTextBox = this.FindControl<TextBox>("LeftTextBox");
        string tempAudioFile = Path.Combine(Path.GetTempPath(), $"mic_record_{Guid.NewGuid()}.wav");
        try
        {
            // Record audio (10 seconds or until cancelled)
            var recordedPath = await AgentWrangler.Services.AudioRecorder.RecordAudioAsync(tempAudioFile, 10, _micTranscribeCts.Token);
            //if (!_isMicTranscribing) return;
            if (string.IsNullOrEmpty(recordedPath) || !File.Exists(recordedPath))
            {
                AppendStatus($"Recording failed or file not found: {recordedPath}");
                return;
            }
            AppendStatus("Transcribing audio...");
            var transcript = await client.TranscribeAudioAsync(recordedPath, model: model, language: "en");
            if (leftTextBox != null)
            {
                leftTextBox.Text += (leftTextBox.Text?.Length > 0 ? "\n" : "") + (transcript ?? "No transcription result.");
            }
            AppendStatus("Transcription done.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "MicTranscribeToggle_Checked");
            AppendStatus($"Mic transcription error: {ex.Message}");
        }
        finally
        {
            try { if (File.Exists(tempAudioFile)) File.Delete(tempAudioFile); } catch { }
        }
    }

    private void MicTranscribeToggle_Unchecked(object? sender, RoutedEventArgs e)
    {
        _isMicTranscribing = false;
        _micTranscribeCts?.Cancel();
        AppendStatus("(Microphone transcription stopped.)");
    }

    private async void OcrButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Minimize the window
        this.WindowState = WindowState.Minimized;
        
        string tempFile = Path.Combine(Path.GetTempPath(), $"ocr_screenshot_{Guid.NewGuid()}.jpg");
        bool screenshotSuccess = false;
        string debugOutput = "";
        try
        {
            if (OperatingSystem.IsWindows())
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = $"-Command Add-Type -AssemblyName System.Windows.Forms; Add-Type -AssemblyName System.Drawing; $bmp = New-Object Drawing.Bitmap([Windows.Forms.Screen]::PrimaryScreen.Bounds.Width, [Windows.Forms.Screen]::PrimaryScreen.Bounds.Height); $graphics = [Drawing.Graphics]::FromImage($bmp); $graphics.CopyFromScreen(0, 0, 0, 0, $bmp.Size); $bmp.Save('{tempFile}', [Drawing.Imaging.ImageFormat]::Jpeg); $graphics.Dispose(); $bmp.Dispose();",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                var proc = System.Diagnostics.Process.Start(psi);
                string stdOut = proc.StandardOutput.ReadToEnd();
                string stdErr = proc.StandardError.ReadToEnd();
                proc.WaitForExit();
                screenshotSuccess = File.Exists(tempFile);
                debugOutput = $"STDOUT:\n{stdOut}\nSTDERR:\n{stdErr}";
            }
            else if (OperatingSystem.IsLinux())
            {
                // Use 'import' (ImageMagick) or 'gnome-screenshot' if available
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "bash",
                    Arguments = $"-c \"which import && import -window root '{tempFile}' || (which gnome-screenshot && gnome-screenshot -f '{tempFile}')\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                var proc = System.Diagnostics.Process.Start(psi);
                proc.WaitForExit();
                screenshotSuccess = File.Exists(tempFile);
            }
            else if (OperatingSystem.IsMacOS())
            {
                // Use 'screencapture' on macOS
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "screencapture",
                    Arguments = $"-x '{tempFile}'",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                var proc = System.Diagnostics.Process.Start(psi);
                proc.WaitForExit();
                screenshotSuccess = File.Exists(tempFile);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "OcrButton_Click screenshot");
            this.WindowState = WindowState.Normal;
            AppendStatus($"Screenshot failed: {ex.Message}\n{debugOutput}");
            return;
        }

        if (!screenshotSuccess)
        {
            this.WindowState = WindowState.Normal;
            AppendStatus($"Screenshot is not supported or failed on this platform.\n{debugOutput}");
            return;
        }

        // Send to OCR
        AppendStatus("Processing OCR...");
        var apiKey = ConfigHelper.ReadApiKey();
        var model = CleanModelString(SelectedModel);
        var client = new GroqApiClient(apiKey, model: model);
        var ocrResult = await client.OcrImageAsync(tempFile);
        AppendStatus("OCR processing done.");
        var leftTextBox = this.FindControl<TextBox>("LeftTextBox");
        if (leftTextBox != null)
        {
            leftTextBox.Text += (leftTextBox.Text?.Length > 0 ? "\n" : "") + (ocrResult ?? "No OCR result.");
        }
        this.WindowState = WindowState.Normal;
        try { File.Delete(tempFile); } catch { Logger.LogError($"Failed to delete temp file: {tempFile}", "OcrButton_Click cleanup"); }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    private async Task<string?> OpenGroqKeyModal()
    {
        var modal = new GroqKeyModal();
        var result = await modal.ShowDialog<string?>(this);
        return result;
    }

    public static string CleanModelString(string model)
    {
        if (string.IsNullOrWhiteSpace(model)) return "compound-beta-mini";
        int idx = model.IndexOf(" (");
        return idx > 0 ? model.Substring(0, idx) : model;
    }

    private async void SendButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var leftTextBox = this.FindControl<TextBox>("LeftTextBox");
        var rightTextBox = this.FindControl<TextBox>("RightTextBox");
        var input = leftTextBox.Text;
        if (string.IsNullOrWhiteSpace(input)) return;
        var apiKey = ConfigHelper.ReadApiKey();
        var model = CleanModelString(SelectedModel);
        var client = new GroqApiClient(apiKey, model: model);
        rightTextBox.Text = "Sending...";
        var result = await client.SendOcrResultAsync(input, "C#");
        rightTextBox.Text = result ?? "No response.";
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
