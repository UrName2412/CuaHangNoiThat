using Android.App;
using Android.Runtime;

namespace CuaHangNoiThat
{
#if DEBUG
    [Application(NetworkSecurityConfig = "@xml/mock_bank_network_security")]
#else
    [Application]
#endif
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
