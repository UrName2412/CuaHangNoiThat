using CuaHangNoiThat.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace CuaHangNoiThat.Views;

[QueryProperty(nameof(ProductId), "id")]
public partial class ProductDetailPage : ContentPage
{
    private readonly ProductDetailController controller;
    private int productId;

    public string ProductId
    {
        get => productId.ToString();
        set
        {
            var decodedValue = Uri.UnescapeDataString(value);
            if (int.TryParse(decodedValue, out var id))
                productId = id;
        }
    }

    public ProductDetailPage()
    {
        InitializeComponent();
        controller = App.Services.GetRequiredService<ProductDetailController>();
        BindingContext = controller;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (productId > 0)
            await controller.LoadAsync(productId);
    }
}
