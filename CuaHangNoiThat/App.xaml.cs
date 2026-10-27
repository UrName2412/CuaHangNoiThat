using Microsoft.Extensions.DependencyInjection;
using CuaHangNoiThat.Services;

namespace CuaHangNoiThat
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; } = null!;

        public App()
        {
            InitializeComponent();
        }

        public static void SetServices(IServiceProvider services) => Services = services;

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}