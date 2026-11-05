using Microsoft.Extensions.Logging;
using CuaHangNoiThat.Services;

#if MAUI_DEVFLOW
using Microsoft.Maui.DevFlow.Agent;
#endif

namespace CuaHangNoiThat
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if ANDROID
            // Inputs already sit inside app-styled surfaces; hide Android's extra underline.
            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("MokhaEntrySurface", (handler, _) =>
                handler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent));
#endif

#if MAUI_DEVFLOW
            builder.AddMauiDevFlowAgent();
#endif

            builder.Services.AddSingleton<Controllers.HomeController>();
            builder.Services.AddSingleton<Controllers.CatalogController>();
            builder.Services.AddSingleton<Controllers.CartController>();
            builder.Services.AddTransient<Controllers.OrdersController>();
            builder.Services.AddSingleton<Controllers.AccountController>();
            builder.Services.AddTransient<Controllers.ProfileController>();
            // Each checkout visit owns its order result and address/voucher selection.
            builder.Services.AddTransient<Controllers.CheckoutController>();
            builder.Services.AddTransient<Controllers.AddressController>();
            builder.Services.AddSingleton(sp => new ProvinceService(new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(20)
            }));
            builder.Services.AddTransient<Controllers.PromotionController>();
            builder.Services.AddTransient<Controllers.ProductDetailController>();
            builder.Services.AddSingleton(sp => new SupabaseService(new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(15)
            }));

#if DEBUG
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();
            App.SetServices(app.Services);
            return app;
        }
    }
}
