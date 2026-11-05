namespace CuaHangNoiThat
{
    public partial class AppShell : Shell
    {
        private bool isResettingTabStack;

        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("OrdersPage", typeof(Views.OrdersPage));
            Routing.RegisterRoute("CheckoutPage", typeof(Views.CheckoutPage));
            Routing.RegisterRoute("AddressPage", typeof(Views.AddressPage));
            Routing.RegisterRoute("ProfilePage", typeof(Views.ProfilePage));
            Routing.RegisterRoute("PromotionPage", typeof(Views.PromotionPage));
            Routing.RegisterRoute("ProductDetailPage", typeof(Views.ProductDetailPage));

            Navigated += OnNavigated;
        }

        private async void OnNavigated(object? sender, ShellNavigatedEventArgs args)
        {
            if (isResettingTabStack ||
                args.Source != ShellNavigationSource.ShellSectionChanged ||
                CurrentItem?.CurrentItem?.Stack.Count <= 1)
                return;

            var rootRoute = GetRootRouteToRestore();
            if (rootRoute is null)
                return;

            isResettingTabStack = true;
            try
            {
                await GoToAsync($"//{rootRoute}", false);
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine($"Tab navigation reset failed: {exception}");
            }
            finally
            {
                isResettingTabStack = false;
            }
        }

        private string? GetRootRouteToRestore()
        {
            var selectedContent = CurrentItem?.CurrentItem?.CurrentItem;
            if (selectedContent?.Route.Equals("HomePage", StringComparison.OrdinalIgnoreCase) == true)
                return "HomePage";
            if (selectedContent?.Route.Equals("CatalogPage", StringComparison.OrdinalIgnoreCase) == true)
                return "CatalogPage";
            if (selectedContent?.Route.Equals("CartPage", StringComparison.OrdinalIgnoreCase) == true)
                return "CartPage";
            if (selectedContent?.Route.Equals("AccountPage", StringComparison.OrdinalIgnoreCase) == true)
                return "AccountPage";
            return null;
        }
    }
}
