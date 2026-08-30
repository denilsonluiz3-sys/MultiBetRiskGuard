using MULTI_Bet_playing_Demo.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using System;
using System.Linq;

namespace MULTI_Bet_playing_Demo.Pages;

public partial class WebViewPage : ContentPage
{
    private readonly string _homeUrl;
    private readonly string _title;
    private bool _desktopUa;
    private bool _isNavigating;

    private sealed record ImageSearchProvider(string Name, string UrlTemplate);
    private static readonly ImageSearchProvider[] ImageSearchProviders =
    {
        new("Google Imagens", "https://www.google.com/search?tbm=isch&q={0}"),
        new("Bing Imagens", "https://www.bing.com/images/search?q={0}"),
        new("DuckDuckGo Imagens", "https://duckduckgo.com/?iax=images&ia=images&q={0}"),
        new("Yahoo Imagens", "https://images.search.yahoo.com/search/images?p={0}"),
        new("Yandex Imagens", "https://yandex.com/images/search?text={0}")
    };

    public WebViewPage(string url, string title = "Site")
    {
        InitializeComponent();
        _title = title;
        if (!UrlValidator.TryNormalize(url, out var safe, out var err))
        {
            TitleLabel.Text = "URL bloqueada"; Title = "Erro"; _homeUrl = string.Empty;
            MainWebView.Source = null; AddressEntry.Text = string.Empty;
            _ = DisplayAlertAsync("Bloqueado", err, "OK"); return;
        }
        _homeUrl = safe; TitleLabel.Text = title; Title = title;
        SetCurrentUrl(safe); MainWebView.Source = safe;
    }

    protected override bool OnBackButtonPressed()
    {
        try { if (MainWebView.CanGoBack) { MainWebView.GoBack(); return true; } } catch { }
        return base.OnBackButtonPressed();
    }
    private void OnBack(object? sender, EventArgs e) { try { if (MainWebView.CanGoBack) MainWebView.GoBack(); } catch { } }
    private void OnForward(object? sender, EventArgs e) { try { if (MainWebView.CanGoForward) MainWebView.GoForward(); } catch { } }
    private void OnReload(object? sender, EventArgs e) { try { MainWebView.Reload(); } catch { } }
    private void OnHome(object? sender, EventArgs e) { if (!string.IsNullOrEmpty(_homeUrl)) NavigateTo(_homeUrl); }
    private async void OnClose(object? sender, EventArgs e) { try { await Navigation.PopAsync(); } catch { } }
    private void OnAddressCompleted(object? sender, EventArgs e) => NavigateFromAddressBar();
    private void OnGo(object? sender, EventArgs e) => NavigateFromAddressBar();

    private void NavigateFromAddressBar()
    {
        if (!UrlValidator.TryNormalize(AddressEntry.Text, out var safe, out var err)) { _ = DisplayAlertAsync("URL bloqueada", err, "OK"); return; }
        NavigateTo(safe); AddressEntry.Unfocus();
    }

    private void NavigateTo(string url)
    {
        if (!UrlValidator.TryNormalize(url, out var safe, out _)) return;
        try { SetCurrentUrl(safe); MainWebView.Source = safe; } catch { }
    }

    private void OnNavigating(object? sender, WebNavigatingEventArgs e)
    {
        try
        {
            // about:blank is permitted only as the WebView's inert internal blank page.
            if (string.Equals(e.Url, "about:blank", StringComparison.OrdinalIgnoreCase))
            {
                _isNavigating = false; LoadingProgress.IsVisible = false; return;
            }
            if (!UrlValidator.TryNormalize(e.Url, out var safe, out _)) { e.Cancel = true; _isNavigating = false; LoadingProgress.IsVisible = false; return; }
            _isNavigating = true; LoadingProgress.Progress = 0.15; LoadingProgress.IsVisible = true; SetCurrentUrl(safe);
        }
        catch { e.Cancel = true; }
    }

    private void OnNavigated(object? sender, WebNavigatedEventArgs e)
    {
        try
        {
            _isNavigating = false; LoadingProgress.Progress = 1;
            if (!string.IsNullOrEmpty(e.Url) && !e.Url.Equals("about:blank", StringComparison.OrdinalIgnoreCase)) SetCurrentUrl(e.Url);
            Dispatcher.StartTimer(TimeSpan.FromMilliseconds(180), () => { LoadingProgress.IsVisible = false; LoadingProgress.Progress = 0; return false; });
        }
        catch { }
    }

    private void SetCurrentUrl(string url) => AddressEntry.Text = url;

    private async void OnMenu(object? sender, EventArgs e)
    {
        try
        {
            var choice = await DisplayActionSheetAsync("Navegador", "Cancelar", null,
                _isNavigating ? "Parar carregamento" : "Recarregar", "Pesquisar imagens", "Pesquisa reversa por URL de imagem",
                "Abrir no Chrome / navegador do sistema", _desktopUa ? "Modo mobile (UA)" : "Modo desktop (UA)",
                "Copiar URL", "Compartilhar URL", "Limpar cookies deste app");
            var url = AddressEntry.Text ?? _homeUrl;
            switch (choice)
            {
                case "Parar carregamento": try { MainWebView.StopLoading(); } catch { } _isNavigating = false; LoadingProgress.IsVisible = false; break;
                case "Recarregar": OnReload(sender, e); break;
                case "Pesquisar imagens": await SearchImagesAsync(); break;
                case "Pesquisa reversa por URL de imagem": await ReverseImageSearchAsync(); break;
                case "Abrir no Chrome / navegador do sistema": if (UrlValidator.TryNormalize(url, out var safe, out _)) await Launcher.Default.OpenAsync(new Uri(safe)); break;
                case "Modo desktop (UA)": case "Modo mobile (UA)": _desktopUa = !_desktopUa; ApplyUserAgent(_desktopUa); MainWebView.Reload(); break;
                case "Copiar URL": await Clipboard.Default.SetTextAsync(url); break;
                case "Compartilhar URL": await Share.Default.RequestAsync(new ShareTextRequest { Text = url, Title = _title }); break;
                case "Limpar cookies deste app": WebViewSecurity.ClearCookiesAndCache(); break;
            }
        }
        catch (Exception ex) { try { await DisplayAlertAsync("Erro", ex.Message, "OK"); } catch { } }
    }

    private async Task SearchImagesAsync()
    {
        var query = await DisplayPromptAsync("Pesquisa de imagens", "Digite o que deseja pesquisar:", "Pesquisar", "Cancelar", "ex.: GPT", 200, Keyboard.Text);
        if (string.IsNullOrWhiteSpace(query)) return;
        var selected = await DisplayActionSheetAsync("Escolha o provedor", "Cancelar", null, ImageSearchProviders.Select(p => p.Name).ToArray());
        var provider = ImageSearchProviders.FirstOrDefault(p => p.Name == selected);
        if (provider == null) return;
        NavigateTo(string.Format(provider.UrlTemplate, Uri.EscapeDataString(query.Trim())));
    }

    private async Task ReverseImageSearchAsync()
    {
        var imageUrl = await DisplayPromptAsync("Pesquisa reversa", "Cole a URL pública da imagem:", "Pesquisar", "Cancelar", "https://...", 1000, Keyboard.Url);
        if (string.IsNullOrWhiteSpace(imageUrl)) return;
        if (!UrlValidator.TryNormalize(imageUrl.Trim(), out var safeImage, out var error)) { await DisplayAlertAsync("URL bloqueada", error, "OK"); return; }
        var selected = await DisplayActionSheetAsync("Escolha o provedor", "Cancelar", null, "Google Lens", "Yandex Imagens");
        string? searchUrl = selected switch
        {
            "Google Lens" => $"https://lens.google.com/uploadbyurl?url={Uri.EscapeDataString(safeImage)}",
            "Yandex Imagens" => $"https://yandex.com/images/search?rpt=imageview&url={Uri.EscapeDataString(safeImage)}",
            _ => null
        };
        if (searchUrl != null) NavigateTo(searchUrl);
    }

    private void ApplyUserAgent(bool desktop)
    {
#if ANDROID
        try { if (MainWebView.Handler?.PlatformView is Android.Webkit.WebView aw) aw.Settings.UserAgentString = desktop ? WebViewSecurity.ChromeDesktopUa : WebViewSecurity.ChromeMobileUa; } catch { }
#endif
    }
}