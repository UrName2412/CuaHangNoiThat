using System.Net;
using System.Text.Json;
using CuaHangNoiThat.Controllers;
using CuaHangNoiThat.Services;

internal static class CatalogChecks
{
    public static async Task RunAsync()
    {
        var handler = new CatalogHandler();
        var service = new SupabaseService(new HttpClient(handler));
        var catalog = new CatalogController(service, new CartController());
        await catalog.LoadAsync();
        Check(!catalog.HasError && catalog.Items.Count == 8, "Catalog fixture must load through the real service.");
        var table = catalog.Items.Single(item => item.IdSanPham == 101);
        Check(table.ProductTypeId == 10 && table.ProductType == "Bàn" &&
              table.WoodTypeId == 20 && table.WoodType == "Thông" && table.Category == "Hiện đại",
            "Product type must resolve through style while wood and existing category map independently.");
        Check(catalog.Items.Single(item => item.IdSanPham == 102) is
              { ProductTypeId: 11, ProductType: "Ghế", Category: "Hiện đại" },
            "Styles with the same display name must still resolve their separate product types by ID.");
        Check(catalog.Items.Single(item => item.IdSanPham == 106) is
              { ProductTypeId: null, ProductType: "", WoodTypeId: null, WoodType: "" },
            "Missing lookup relations must remain unclassified instead of inferring attributes from names.");
        var detail = await service.GetFurnitureItemByIdAsync(101);
        Check(detail is { ProductTypeId: 10, ProductType: "Bàn", WoodTypeId: 20, WoodType: "Thông", Category: "Hiện đại" },
            "Product details must use the same type and wood mapping as catalog products.");
        Check(catalog.ProductTypeOptions.Count == 3 && catalog.WoodTypeOptions.Count == 3 &&
              catalog.ProductTypeOptions.Count(option => option.Id is null) == 1 &&
              catalog.WoodTypeOptions.Count(option => option.Id is null) == 1,
            "Attribute options must be deduplicated, with one All option and no unknown-value option.");

        await ApplyAsync(catalog, () => catalog.DraftProductType = catalog.ProductTypeOptions.Single(option => option.Id == 10));
        ExpectIds(catalog, 101, 103, 104, 105, 107, 108);
        await ApplyAsync(catalog, () =>
        {
            catalog.DraftProductType = catalog.ProductTypeOptions[0];
            catalog.DraftWoodType = catalog.WoodTypeOptions.Single(option => option.Id == 20);
        });
        ExpectIds(catalog, 101, 102, 105);
        await ApplyAsync(catalog, () =>
        {
            catalog.DraftProductType = catalog.ProductTypeOptions.Single(option => option.Id == 10);
            catalog.DraftWoodType = catalog.WoodTypeOptions.Single(option => option.Id == 21);
        });
        ExpectIds(catalog, 103, 104, 107, 108);
        Check(catalog.ActiveFilterCount == 2 && catalog.FilterSummary.Contains("Bàn") && catalog.FilterSummary.Contains("Sồi"),
            "Both attribute conditions must appear in the active filter count and summary.");

        await catalog.OpenFiltersCommand.ExecuteAsync(null);
        catalog.DraftProductType = catalog.ProductTypeOptions.Single(option => option.Id == 11);
        catalog.DraftWoodType = catalog.WoodTypeOptions.Single(option => option.Id == 20);
        await catalog.CloseFiltersCommand.ExecuteAsync(null);
        ExpectIds(catalog, 103, 104, 107, 108);
        await catalog.OpenFiltersCommand.ExecuteAsync(null);
        Check(catalog.DraftProductType?.Id == 10 && catalog.DraftWoodType?.Id == 21,
            "Closing without Apply must discard attribute changes when filters reopen.");
        catalog.DraftProductType = catalog.ProductTypeOptions.Single(option => option.Id == 11);
        catalog.MinimumPriceInput = "6.000.000";
        catalog.MaximumPriceInput = "4.000.000";
        await catalog.ApplyFiltersCommand.ExecuteAsync(null);
        Check(!string.IsNullOrWhiteSpace(catalog.FilterMessage) && Shell.Current.Navigation.ModalDepth == 1,
            "An invalid price range must keep the filter modal open.");
        ExpectIds(catalog, 103, 104, 107, 108);
        catalog.ResetFilterDraftCommand.Execute(null);
        Check(catalog.DraftProductType?.Id is null && catalog.DraftWoodType?.Id is null,
            "Reset must reset both attribute drafts.");
        ExpectIds(catalog, 103, 104, 107, 108);
        await catalog.ApplyFiltersCommand.ExecuteAsync(null);
        ExpectIds(catalog, 101, 102, 103, 104, 105, 106, 107, 108);

        catalog.SelectedCategory = "Hiện đại";
        catalog.SearchText = "bàn";
        await ApplyAsync(catalog, () =>
        {
            catalog.DraftProductType = catalog.ProductTypeOptions.Single(option => option.Id == 10);
            catalog.DraftWoodType = catalog.WoodTypeOptions.Single(option => option.Id == 21);
            catalog.MinimumPriceInput = "3.500.000";
            catalog.MaximumPriceInput = "4.500.000";
            catalog.DraftInStockOnly = true;
            catalog.DraftDiscountedOnly = true;
            catalog.DraftSortOrder = "Giá giảm dần";
        });
        ExpectIds(catalog, 104);
        Check(catalog.ActiveFilterCount == 6 && catalog.ProductTypeOptions.Count == 3 && catalog.WoodTypeOptions.Count == 3,
            "Attributes must combine with price/stock/sale without shrinking available options.");
        catalog.ClearFiltersCommand.Execute(null);
        ExpectIds(catalog, 101, 104, 105, 107, 108);
        Check(catalog.ActiveFilterCount == 0 && catalog.SelectedCategory == "Hiện đại" && catalog.SearchText == "bàn",
            "Clear advanced filters must preserve the current category and search.");

        catalog.SelectedCategory = "Tất cả";
        catalog.SearchText = string.Empty;
        await ApplyAsync(catalog, () => catalog.DraftProductType = catalog.ProductTypeOptions.Single(option => option.Id == 10));
        handler.RemoveTables = true;
        await catalog.LoadAsync();
        ExpectIds(catalog);
        Check(!catalog.HasError && catalog.ActiveFilterCount == 1 && catalog.ProductTypeOptions.Any(option => option.Id == 10),
            "A reload must retain an applied attribute filter even when all matching products disappear.");
        await catalog.OpenFiltersCommand.ExecuteAsync(null);
        Check(catalog.DraftProductType?.Id == 10, "An applied filter must remain available to remove after a reload.");
        await catalog.CloseFiltersCommand.ExecuteAsync(null);
        catalog.ClearFiltersCommand.Execute(null);
        ExpectIds(catalog, 102, 106);

        Console.WriteLine("PASS: catalog product-type/wood mapping, combined filters, drafts, validation, reset/clear and reloads (offline).");
    }

    private static async Task ApplyAsync(CatalogController catalog, Action changeDraft)
    {
        await catalog.OpenFiltersCommand.ExecuteAsync(null);
        changeDraft();
        await catalog.ApplyFiltersCommand.ExecuteAsync(null);
        Check(Shell.Current.Navigation.ModalDepth == 0, "Valid filters must close the modal.");
    }

    private static void ExpectIds(CatalogController catalog, params int[] expected) =>
        Check(catalog.Items.Select(item => item.IdSanPham).Order().SequenceEqual(expected.Order()),
            "Unexpected filtered products: " + string.Join(",", catalog.Items.Select(item => item.IdSanPham)));

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private sealed class CatalogHandler : HttpMessageHandler
    {
        public bool RemoveTables { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Method != HttpMethod.Get) throw new Exception("Catalog checks must only read data.");
            object payload;
            switch (request.RequestUri!.AbsolutePath)
            {
                case "/rest/v1/sanpham":
                    var products = new[]
                    {
                        Product(101, "Bàn thông", 20, 30, 5_000_000, 3, true),
                        Product(102, "Ghế thông", 20, 32, 2_000_000, 0, false),
                        Product(103, "Bàn sồi cổ điển", 21, 31, 6_000_000, 3, false),
                        Product(104, "Bàn sồi giảm giá", 21, 30, 5_000_000, 5, true),
                        Product(105, "Bàn thông lớn", 20, 30, 7_000_000, 1, false),
                        Product(106, "Sản phẩm chưa phân loại", null, 33, 1_000_000, 2, false),
                        Product(107, "Bàn sồi hết hàng", 21, 30, 5_000_000, 0, true),
                        Product(108, "Bàn sồi thường", 21, 30, 4_000_000, 3, false)
                    }.AsEnumerable();
                    if (RemoveTables)
                        products = products.Where(product => (int)product["id_sanpham"]! is 102 or 106);
                    if (Uri.UnescapeDataString(request.RequestUri.Query).Contains("id_sanpham=eq.101"))
                        products = products.Where(product => (int)product["id_sanpham"]! == 101);
                    payload = products.ToArray();
                    break;
                case "/rest/v1/loaigo":
                    payload = new[] { new { id_mago = 20, tengo = "Thông" }, new { id_mago = 21, tengo = "Sồi" } };
                    break;
                case "/rest/v1/kieudang":
                    payload = new[]
                    {
                        new { id_kieudang = 30, id_loaisp = (int?)10, tenkieudang = "Hiện đại" },
                        new { id_kieudang = 31, id_loaisp = (int?)10, tenkieudang = "Cổ điển" },
                        new { id_kieudang = 32, id_loaisp = (int?)11, tenkieudang = "Hiện đại" },
                        new { id_kieudang = 33, id_loaisp = (int?)null, tenkieudang = "Hiện đại" }
                    };
                    break;
                case "/rest/v1/loaisp":
                    payload = new[] { new { id_loaisp = 10, tenloai = "Bàn" }, new { id_loaisp = 11, tenloai = "Ghế" } };
                    break;
                default:
                    throw new Exception("Unexpected catalog endpoint: " + request.RequestUri.AbsolutePath);
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload))
            });
        }

        private static Dictionary<string, object?> Product(int id, string name, int? wood, int style,
            decimal price, int stock, bool discounted) => new()
        {
            ["id_sanpham"] = id, ["tensanpham"] = name, ["id_mago"] = wood, ["id_kieudang"] = style,
            ["dongia"] = price, ["soluong"] = stock,
            ["kmtheosanpham"] = discounted ? new[]
            {
                new
                {
                    phantram = 20,
                    chuongtrinhkhuyenmai = new
                    {
                        ngaybd = DateTime.UtcNow.Date.AddDays(-1),
                        ngaykt = DateTime.UtcNow.Date.AddDays(1), trangthai = 1
                    }
                }
            } : Array.Empty<object>()
        };
    }
}
