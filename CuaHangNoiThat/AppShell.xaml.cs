namespace CuaHangNoiThat
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("OrdersPage", typeof(Views.OrdersPage));
            Routing.RegisterRoute("CheckoutPage", typeof(Views.CheckoutPage));
            Routing.RegisterRoute("AddressPage", typeof(Views.AddressPage));
            Routing.RegisterRoute("PromotionPage", typeof(Views.PromotionPage));
            Routing.RegisterRoute("ProductDetailPage", typeof(Views.ProductDetailPage));
        }
    }
}
