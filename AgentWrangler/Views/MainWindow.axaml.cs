using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Threading.Tasks;
using AgentWrangler.Services;

namespace AgentWrangler.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
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
}
