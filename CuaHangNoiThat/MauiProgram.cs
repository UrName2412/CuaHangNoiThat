using Microsoft.Extensions.Logging;

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
            builder.Services.AddSingleton<Controllers.RoomStagerController>();
            builder.Services.AddSingleton<Controllers.CartController>();
            builder.Services.AddSingleton<Controllers.OrdersController>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}