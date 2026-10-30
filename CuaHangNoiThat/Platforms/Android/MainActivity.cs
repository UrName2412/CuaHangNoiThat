using Android.App;
using Android.Content.PM;
using Android.OS;

namespace CuaHangNoiThat
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        public override void OnWindowFocusChanged(bool hasFocus)
        {
            base.OnWindowFocusChanged(hasFocus);
            if (hasFocus && Window?.DecorView is { } decor)
                decor.Post(() => StyleBottomNavigation(decor));
        }

        private void StyleBottomNavigation(Android.Views.View view)
        {
            if (view is Google.Android.Material.BottomNavigation.BottomNavigationView bar)
            {
                var resources = Microsoft.Maui.Controls.Application.Current!.Resources;
                var primary = (Microsoft.Maui.Graphics.Color)resources["Primary"];
                var surface = (Microsoft.Maui.Graphics.Color)resources["Surface"];
                Android.Graphics.Color Native(Microsoft.Maui.Graphics.Color color, float alpha = 1) =>
                    new((int)(color.Red * 255), (int)(color.Green * 255), (int)(color.Blue * 255), (int)(alpha * 255));
                int Dp(float value) => (int)(value * Resources!.DisplayMetrics!.Density);
                bar.SetBackgroundColor(Native(surface));
                bar.ItemHorizontalTranslationEnabled = false;
                bar.ItemIconSize = Dp(24);
                bar.ItemActiveIndicatorEnabled = true;
                bar.ItemActiveIndicatorWidth = Dp(56);
                bar.ItemActiveIndicatorHeight = Dp(32);
                bar.ItemActiveIndicatorColor = Android.Content.Res.ColorStateList.ValueOf(Native(primary, 0.12f));
                bar.ItemActiveIndicatorShapeAppearance = Google.Android.Material.Shape.ShapeAppearanceModel.InvokeBuilder().SetAllCornerSizes(Dp(16)).Build();
                bar.ItemRippleColor = Android.Content.Res.ColorStateList.ValueOf(Native(primary, 0.16f));
                bar.Elevation = Dp(6);
            }
            if (view is Android.Views.ViewGroup group)
                for (var i = 0; i < group.ChildCount; i++)
                    if (group.GetChildAt(i) is { } child) StyleBottomNavigation(child);
        }

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (Window is null)
                return;

#pragma warning disable CA1422 // Kept for API 21-35; MAUI's window still owns this view on supported Android versions.
            Window.SetStatusBarColor(Android.Graphics.Color.Rgb(250, 248, 245));
            if (OperatingSystem.IsAndroidVersionAtLeast(23))
                Window.DecorView.SystemUiFlags |= Android.Views.SystemUiFlags.LightStatusBar;
            if (OperatingSystem.IsAndroidVersionAtLeast(30) && Window.InsetsController is { } insetsController)
            {
                var lightStatusBar = (int)Android.Views.WindowInsetsControllerAppearance.LightStatusBars;
                insetsController.SetSystemBarsAppearance(lightStatusBar, lightStatusBar);
            }
#pragma warning restore CA1422
        }
    }
}
