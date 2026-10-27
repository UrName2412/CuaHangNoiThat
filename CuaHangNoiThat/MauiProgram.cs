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

#if MAUI_DEVFLOW
            builder.AddMauiDevFlowAgent();
#endif

            builder.Services.AddSingleton<Controllers.HomeController>();
            builder.Services.AddSingleton<Controllers.CatalogController>();
            builder.Services.AddSingleton<Controllers.CartController>();
            builder.Services.AddSingleton<Controllers.OrdersController>();
            builder.Services.AddSingleton<Controllers.AccountController>();
            builder.Services.AddSingleton<Controllers.CheckoutController>();
            builder.Services.AddSingleton<Controllers.AddressController>();
            builder.Services.AddSingleton<Controllers.PromotionController>();
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