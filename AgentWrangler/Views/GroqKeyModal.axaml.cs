using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.Threading.Tasks;
using AgentWrangler.Services;

namespace AgentWrangler.Views;

public partial class GroqKeyModal : Window
{
    public string? ApiKey { get; private set; }
    public GroqKeyModal()
    {
        InitializeComponent();
        var submitButton = this.FindControl<Button>("SubmitButton");
        submitButton.Click += SubmitButton_Click;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async void SubmitButton_Click(object? sender, RoutedEventArgs e)
    {
        var apiKeyBox = this.FindControl<TextBox>("ApiKeyBox");
        var errorText = this.FindControl<TextBlock>("ErrorText");
        var key = apiKeyBox.Text;
        if (string.IsNullOrWhiteSpace(key))
        {
            errorText.Text = "Key cannot be empty.";
            errorText.IsVisible = true;
            return;
        }
        var client = new GroqApiClient(key);
        var result = await client.SendOcrResultAsync("ping", "C#");
        if (result != null)
        {
            ApiKey = key;
            this.Close(ApiKey);
        }
        else
        {
            errorText.Text = "Invalid key. Please try again.";
            errorText.IsVisible = true;
        }
    }
}
