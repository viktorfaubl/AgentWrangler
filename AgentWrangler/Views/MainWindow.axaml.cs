using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AgentWrangler.Services;

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
    }
    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        var apiKey = ConfigHelper.ReadApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            apiKey = await OpenGroqKeyModal();
            if (!string.IsNullOrWhiteSpace(apiKey))
                ConfigHelper.SaveApiKey(apiKey);
        }
        // You can now use apiKey for further logic
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
