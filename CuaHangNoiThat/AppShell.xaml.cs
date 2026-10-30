namespace CuaHangNoiThat
{
    public partial class AppShell : Shell
    {
        private bool isResettingAccountStack;

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

        private void OnNavigated(object? sender, ShellNavigatedEventArgs args)
        {
            if (isResettingAccountStack ||
                CurrentPage is not Views.OrdersPage ||
                !IsAccountTabSelected() ||
                IsOpeningOrdersPage(args))
                return;

            isResettingAccountStack = true;
            _ = ResetAccountStackAsync();
        }

        private async Task ResetAccountStackAsync()
        {
            try
            {
                await Navigation.PopToRootAsync(false);
            }
            finally
            {
                isResettingAccountStack = false;
            }
        }

        private bool IsAccountTabSelected()
        {
            var selectedContent = CurrentItem?.CurrentItem?.CurrentItem;
            return selectedContent?.Route.Equals("AccountPage", StringComparison.OrdinalIgnoreCase) == true;
        }

        private static bool IsOpeningOrdersPage(ShellNavigatedEventArgs args) =>
            args.Previous?.Location.OriginalString.Contains("AccountPage", StringComparison.OrdinalIgnoreCase) == true &&
            args.Current.Location.OriginalString.Contains("OrdersPage", StringComparison.OrdinalIgnoreCase);
    }
}
