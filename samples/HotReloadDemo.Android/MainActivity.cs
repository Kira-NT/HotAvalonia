using Android.Content.PM;
using Avalonia;
using Avalonia.Android;

namespace HotReloadDemo.Android;

[Activity(
    Label = "HotReloadDemo.Android",
    Theme = "@style/HotReloadDemoTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        => base.CustomizeAppBuilder(builder)
            .LogToTrace()
            .WithInterFont();
}
