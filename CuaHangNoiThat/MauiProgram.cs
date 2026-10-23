using Microsoft.Extensions.Logging;

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

            builder.Services.AddSingleton<ViewModels.HomeViewModel>();
            builder.Services.AddSingleton<ViewModels.CatalogViewModel>();
            builder.Services.AddSingleton<ViewModels.RoomStagerViewModel>();
            builder.Services.AddSingleton<ViewModels.CartViewModel>();
            builder.Services.AddSingleton<ViewModels.OrdersViewModel>();

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
