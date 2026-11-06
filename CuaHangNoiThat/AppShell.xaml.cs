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
            // Modal filter navigation does not change Shell routes.
            var previousPage = args.Previous?.Location.OriginalString.Split('?', 2)[0]
                .Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            var currentPage = args.Current?.Location.OriginalString.Split('?', 2)[0]
                .Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            if (string.Equals(previousPage, "CatalogPage", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(currentPage, "CatalogPage", StringComparison.OrdinalIgnoreCase))
                (App.Services.GetService(typeof(Controllers.CatalogController)) as Controllers.CatalogController)?
                    .ClearFiltersCommand.Execute(null);

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
