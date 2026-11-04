using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.ObjectModel;
using BCrypt.Net;
using CuaHangNoiThat.Models;

namespace CuaHangNoiThat.Services;

public sealed class SupabaseService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    private readonly HttpClient httpClient;
    private readonly Task<SupabaseConfiguration> configuration;
    private string? accessToken;
    private AuthenticatedUser? currentUser;

    public SupabaseService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
        configuration = SupabaseConfiguration.LoadAsync();
    }

    public AuthenticatedUser? CurrentUser => currentUser;

    public async Task<AuthenticatedUser?> RestoreSessionAsync()
    {
        try
        {
            var storedUser = await SecureStorage.Default.GetAsync("supabase_user");

            if (string.IsNullOrWhiteSpace(storedUser))
            {
                accessToken = null;
                currentUser = null;
                return null;
            }

            accessToken = await SecureStorage.Default.GetAsync("supabase_access_token");
            currentUser = JsonSerializer.Deserialize<AuthenticatedUser>(storedUser, JsonOptions);
            return currentUser;
        }
        catch (Exception exception)
        {
            accessToken = null;
            currentUser = null;
            throw new InvalidOperationException($"Không thể đọc phiên lưu trữ: {exception.Message}");
        }
    }

    public async Task<AuthenticatedUser> LoginAsync(string usernameOrEmail, string password, CancellationToken cancellationToken = default)
    {
        var config = await configuration;
        var loginValue = usernameOrEmail.Trim();
        var account = await FindLoginAccountAsync(config, loginValue, cancellationToken);

        if (account?.IdKhachhang is null || string.IsNullOrWhiteSpace(account.MatKhau) ||
            !VerifyPassword(password, account.MatKhau))
            throw new InvalidOperationException("Tên đăng nhập/email hoặc mật khẩu không đúng.");

        var customer = await FindCustomerByIdAsync(config, account.IdKhachhang.Value, cancellationToken);
        if (customer?.IdKhachhang is null)
            throw new InvalidOperationException("Tài khoản chưa được liên kết với khách hàng hợp lệ.");

        currentUser = new AuthenticatedUser(
            customer.IdKhachhang.Value.ToString(),
            customer.Email ?? loginValue,
            customer.HoTen ?? loginValue,
            customer.Sdt ?? string.Empty);
        await SecureStorage.Default.SetAsync("supabase_user", JsonSerializer.Serialize(currentUser, JsonOptions));
        return currentUser;
    }

    public async Task<AuthenticatedUser> RegisterAsync(string email, string password, string fullName, CancellationToken cancellationToken = default)
    {
        var config = await configuration;
        var normalizedEmail = email.Trim();
        // The RPC inserts both records in one database transaction. Never fall back to
        // separate requests: a failed account insert must roll back the customer too.
        var createdCustomers = await SendJsonReturningAsync<CustomerDto>(config, HttpMethod.Post, "/rest/v1/rpc/register_customer", new
        {
            p_full_name = fullName.Trim(),
            p_email = normalizedEmail,
            p_password_hash = HashPassword(password)
        }, cancellationToken);
        var newId = createdCustomers.FirstOrDefault()?.IdKhachhang
            ?? throw new HttpRequestException("Supabase không trả về mã khách hàng vừa tạo.");

        currentUser = new AuthenticatedUser(newId.ToString(), normalizedEmail, fullName.Trim(), string.Empty);
        await SecureStorage.Default.SetAsync("supabase_user", JsonSerializer.Serialize(currentUser, JsonOptions));
        return currentUser;
    }

    public async Task SignOutAsync()
    {
        accessToken = null;
        currentUser = null;
        SecureStorage.Default.Remove("supabase_access_token");
        SecureStorage.Default.Remove("supabase_user");
        await Task.CompletedTask;
    }

    private async Task<LoginAccountDto?> FindLoginAccountAsync(
        SupabaseConfiguration config,
        string loginValue,
        CancellationToken cancellationToken)
    {
        var encodedLogin = Uri.EscapeDataString(loginValue);
        var accounts = new List<LoginAccountDto>();

        if (loginValue.Contains('@'))
        {
            var customers = await FetchListAsync<CustomerDto>(
                config,
                $"/rest/v1/khachhang?select=id_khachhang&email=eq.{encodedLogin}&limit=1",
                cancellationToken);
            var customer = customers.FirstOrDefault();
            if (customer?.IdKhachhang is null)
                return null;

            accounts.AddRange(await FetchListAsync<LoginAccountDto>(
                config,
                $"/rest/v1/dangnhap?select=tendangnhap,matkhau,id_khachhang&loaitaikhoan=eq.KHACHHANG&trangthai=eq.1&id_khachhang=eq.{customer.IdKhachhang}&limit=1",
                cancellationToken));
        }
        else
        {
            accounts.AddRange(await FetchListAsync<LoginAccountDto>(
                config,
                $"/rest/v1/dangnhap?select=tendangnhap,matkhau,id_khachhang&loaitaikhoan=eq.KHACHHANG&trangthai=eq.1&tendangnhap=eq.{encodedLogin}&limit=1",
                cancellationToken));
        }

        return accounts.FirstOrDefault();
    }

    private async Task<CustomerDto?> FindCustomerByIdAsync(
        SupabaseConfiguration config,
        int customerId,
        CancellationToken cancellationToken)
    {
        var customers = await FetchListAsync<CustomerDto>(
            config,
            $"/rest/v1/khachhang?select=id_khachhang,hoten,email,sdt&id_khachhang=eq.{customerId}&limit=1",
            cancellationToken);
        return customers.FirstOrDefault();
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash))
                return false;

            // Hỗ trợ mật khẩu băm theo PBKDF2 (dạng pbkdf2-sha256$iterations$salt$hash)
            // do DB admin seed sẵn (salt/hash mã hoá base64url, không padding).
            if (storedHash.StartsWith("pbkdf2", StringComparison.OrdinalIgnoreCase))
            {
                var parts = storedHash.Split('$');
                if (parts.Length != 4)
                    return false;

                if (!parts[0].Contains("sha256", StringComparison.OrdinalIgnoreCase))
                    return false;

                if (!int.TryParse(parts[1], out var iterations) || iterations <= 0)
                    return false;

                var salt = DecodeBase64Url(parts[2]);
                var expectedHash = DecodeBase64Url(parts[3]);
                if (salt is null || expectedHash is null || expectedHash.Length == 0)
                    return false;

                var derived = Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    salt,
                    iterations,
                    HashAlgorithmName.SHA256,
                    expectedHash.Length);

                return CryptographicOperations.FixedTimeEquals(derived, expectedHash);
            }

            // Tài khoản đăng ký qua app dùng BCrypt.
            return BCrypt.Net.BCrypt.Verify(password, storedHash);
        }
        catch
        {
            return false;
        }
    }

    private static string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new InvalidOperationException("Mật khẩu không được để trống.");

        const int iterations = 120_000;
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            32);

        return $"pbkdf2-sha256${iterations}${EncodeBase64Url(salt)}${EncodeBase64Url(hash)}";
    }

    private static string EncodeBase64Url(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[]? DecodeBase64Url(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var padded = value.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2:
                padded += "==";
                break;
            case 3:
                padded += "=";
                break;
            case 0:
                break;
            default:
                return null;
        }

        try
        {
            return Convert.FromBase64String(padded);
        }
        catch
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<FurnitureItem>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        var config = await configuration;
        var products = await FetchProductsAsync(config, cancellationToken);
        var lookups = await FetchLookupsAsync(config, cancellationToken);

        var woodMap = lookups.Loaigo
            .Where(x => x.IdMago.HasValue)
            .GroupBy(x => x.IdMago!.Value)
            .ToDictionary(x => x.Key, x => x.First().Tengo ?? string.Empty);
        var styleMap = lookups.Kieudang
            .Where(x => x.IdKieudang.HasValue)
            .GroupBy(x => x.IdKieudang!.Value)
            .ToDictionary(x => x.Key, x => x.First());
        var productTypeMap = lookups.Loaisp
            .Where(x => x.IdLoaiSp.HasValue)
            .GroupBy(x => x.IdLoaiSp!.Value)
            .ToDictionary(x => x.Key, x => x.First().TenLoai ?? string.Empty);

        return products.Select(p => MapProduct(p, woodMap, styleMap, productTypeMap, config)).ToList();
    }

    public async Task<ProductDto?> GetProductByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var config = await configuration;
        var products = await FetchListAsync<ProductDto>(
            config,
            $"/rest/v1/sanpham?select=*,kmtheosanpham(phantram,chuongtrinhkhuyenmai(ngaybd,ngaykt,trangthai))&id_sanpham=eq.{id}&isdeleted=eq.0&trangthai=eq.1&limit=1",
            cancellationToken);
        return products.FirstOrDefault();
    }

    public async Task<FurnitureItem?> GetFurnitureItemByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var config = await configuration;
        var product = await GetProductByIdAsync(id, cancellationToken);
        if (product is null)
            return null;

        var lookups = await FetchLookupsAsync(config, cancellationToken);
        var woodMap = lookups.Loaigo
            .Where(x => x.IdMago.HasValue)
            .GroupBy(x => x.IdMago!.Value)
            .ToDictionary(x => x.Key, x => x.First().Tengo ?? string.Empty);
        var styleMap = lookups.Kieudang
            .Where(x => x.IdKieudang.HasValue)
            .GroupBy(x => x.IdKieudang!.Value)
            .ToDictionary(x => x.Key, x => x.First());
        var productTypeMap = lookups.Loaisp
            .Where(x => x.IdLoaiSp.HasValue)
            .GroupBy(x => x.IdLoaiSp!.Value)
            .ToDictionary(x => x.Key, x => x.First().TenLoai ?? string.Empty);

        return MapProduct(product, woodMap, styleMap, productTypeMap, config);
    }

    private static FurnitureItem MapProduct(
        ProductDto product,
        IReadOnlyDictionary<int, string> woodMap,
        IReadOnlyDictionary<int, KieudangDto> styleMap,
        IReadOnlyDictionary<int, string> productTypeMap,
        SupabaseConfiguration config)
    {
        var style = styleMap.GetValueOrDefault(product.IdKieuDang.GetValueOrDefault());
        var styleName = style is null ? null : style.Tenkieudang ?? string.Empty;
        return new()
        {
            IdSanPham = product.IdSanPham,
            Name = product.TenSanPham ?? string.Empty,
            Category = styleName
                ?? woodMap.GetValueOrDefault(product.IdMaGo.GetValueOrDefault())
                ?? "Nội thất",
            ProductTypeId = style?.IdLoaiSp,
            ProductType = productTypeMap.GetValueOrDefault(style?.IdLoaiSp ?? 0) ?? string.Empty,
            WoodTypeId = product.IdMaGo,
            WoodType = woodMap.GetValueOrDefault(product.IdMaGo.GetValueOrDefault()) ?? string.Empty,
            Description = product.MoTa ?? string.Empty,
            Price = product.DonGia.GetValueOrDefault() * (1 - GetProductDiscount(product) / 100m),
            OriginalPrice = product.DonGia.GetValueOrDefault(),
            DiscountPercent = GetProductDiscount(product),
            StockAvailable = Math.Max(0, product.SoLuong.GetValueOrDefault()),
            ImageName = GetStorageImageUrl(config, product.HinhAnh),
        };
    }

    private static decimal GetProductDiscount(ProductDto product)
    {
        var today = DateTime.UtcNow.Date;
        return product.Promotions
            .Where(p => p.Promotion?.TrangThai == 1 &&
                        p.Promotion.NgayBd.HasValue && p.Promotion.NgayBd.Value.Date <= today &&
                        p.Promotion.NgayKt.HasValue && p.Promotion.NgayKt.Value.Date >= today)
            .Select(p => p.PhanTram.GetValueOrDefault())
            .Where(percent => percent > 0 && percent <= 100)
            .DefaultIfEmpty(0)
            .Max();
    }

    private static string? GetStorageImageUrl(SupabaseConfiguration config, string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            return null;

        var trimmedPath = imagePath.Trim();
        if (Uri.TryCreate(trimmedPath, UriKind.Absolute, out var absoluteUri))
            return absoluteUri.ToString();

        const string bucketName = "NoiThatGo_hinhAnh";
        var encodedPath = string.Join(
            "/",
            trimmedPath.TrimStart('/').Split('/').Select(Uri.EscapeDataString));
        var baseUrl = config.Url.TrimEnd('/');
        if (baseUrl.EndsWith("/rest/v1", StringComparison.OrdinalIgnoreCase))
            baseUrl = baseUrl[..^"/rest/v1".Length];

        return $"{baseUrl}/storage/v1/object/public/{Uri.EscapeDataString(bucketName)}/{encodedPath}";
    }

    public async Task<IReadOnlyList<AddressDto>> GetAddressesAsync(CancellationToken cancellationToken = default)
    {
        var config = await configuration;
        var customerId = GetCurrentCustomerId();
        return await FetchListAsync<AddressDto>(
            config,
            $"/rest/v1/diachigiaohang?select=id_diachi,id_khachhang,tinhthanh,phuongxa,duong,macdinh&id_khachhang=eq.{customerId}&order=macdinh.desc,id_diachi.desc",
            cancellationToken);
    }

    public async Task AddAddressAsync(int provinceCode, int wardCode, string street, bool makeDefault, CancellationToken cancellationToken = default)
    {
        var config = await configuration;
        var customerId = GetCurrentCustomerId();
        if (makeDefault)
            await UpdateAsync(config, $"/rest/v1/diachigiaohang?macdinh=eq.1&id_khachhang=eq.{customerId}", new { macdinh = 0 }, cancellationToken);
        await SendJsonAsync(config, HttpMethod.Post, "/rest/v1/diachigiaohang", new
        {
            id_khachhang = customerId,
            tinhthanh = provinceCode,
            phuongxa = wardCode,
            duong = street.Trim(),
            macdinh = makeDefault ? 1 : 0
        }, cancellationToken);
    }

    public async Task DeleteAddressAsync(int addressId, CancellationToken cancellationToken = default)
    {
        var config = await configuration;
        var customerId = GetCurrentCustomerId();
        var oldAddress = (await GetAddressesAsync(cancellationToken)).FirstOrDefault(a => a.IdDiaChi == addressId);
        using var request = CreateRequest(
            config,
            HttpMethod.Delete,
            $"/rest/v1/diachigiaohang?id_diachi=eq.{addressId}&id_khachhang=eq.{customerId}",
            useAccessToken: false);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
        if (oldAddress?.MacDinh == 1)
        {
            var nextAddress = (await GetAddressesAsync(cancellationToken)).FirstOrDefault();
            if (nextAddress is not null)
                await SetDefaultAddressAsync(nextAddress.IdDiaChi, cancellationToken);
        }
    }

    public async Task SetDefaultAddressAsync(int addressId, CancellationToken cancellationToken = default)
    {
        var config = await configuration;
        var customerId = GetCurrentCustomerId();
        await SendJsonAsync(config, HttpMethod.Post, "/rest/v1/rpc/set_default_address", new
        {
            p_customer_id = customerId,
            p_address_id = addressId
        }, cancellationToken);
    }

    public async Task ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var config = await configuration;
        var customerId = GetCurrentCustomerId();
        if (newPassword.Trim().Length < 6)
            throw new InvalidOperationException("Mật khẩu mới phải có ít nhất 6 ký tự.");
        var accounts = await FetchListAsync<LoginAccountDto>(config,
            $"/rest/v1/dangnhap?select=matkhau&id_khachhang=eq.{customerId}&loaitaikhoan=eq.KHACHHANG&trangthai=eq.1&limit=1", cancellationToken);
        var account = accounts.FirstOrDefault();
        if (account?.MatKhau is null || !VerifyPassword(currentPassword, account.MatKhau))
            throw new InvalidOperationException("Mật khẩu hiện tại không đúng.");
        await UpdateAsync(config, $"/rest/v1/dangnhap?id_khachhang=eq.{customerId}&loaitaikhoan=eq.KHACHHANG",
            new { matkhau = HashPassword(newPassword) }, cancellationToken);
    }

    public async Task UpdateProfileAsync(string fullName, string email, string phone, CancellationToken cancellationToken = default)
    {
        var config = await configuration;
        var customerId = GetCurrentCustomerId();
        var normalizedFullName = fullName.Trim();
        var normalizedEmail = email.Trim();
        var normalizedPhone = phone.Trim();

        await SendJsonAsync(config, HttpMethod.Post, "/rest/v1/rpc/update_customer_profile", new
        {
            p_customer_id = customerId,
            p_full_name = normalizedFullName,
            p_email = normalizedEmail,
            p_phone = normalizedPhone
        }, cancellationToken);

        currentUser = currentUser! with { FullName = normalizedFullName, Email = normalizedEmail, Phone = normalizedPhone };
        await SecureStorage.Default.SetAsync("supabase_user", JsonSerializer.Serialize(currentUser, JsonOptions));
    }

    public async Task<int> CreateOrderAsync(
        OrderCreateDto order,
        IReadOnlyList<OrderItemCreateDto> items,
        CancellationToken cancellationToken = default)
    {
        var config = await configuration;
        var customerId = GetCurrentCustomerId();
        if (order.IdKhachHang != customerId)
            throw new InvalidOperationException("Thông tin khách hàng không hợp lệ.");
        if (items.Count == 0)
            throw new InvalidOperationException("Giỏ hàng đang trống.");

        var created = await SendJsonReturningAsync<OrderDto>(config, HttpMethod.Post, "/rest/v1/hoadon?select=id_hoadon", new
        {
            ngaydat = DateTime.UtcNow.Date.ToString("yyyy-MM-dd"),
            id_diachigiaohang = order.IdDiaChiGiaoHang,
            tongtien = order.TongTien,
            trangthai = 1,
            id_khachhang = customerId,
            id_mactkm = order.IdMaCtkM,
            id_voucher = order.IdVoucher
        }, cancellationToken);

        if (created.Count == 0)
            throw new HttpRequestException("Supabase không trả về mã đơn hàng vừa tạo.");

        var orderId = created[0].IdHoadon;
        foreach (var item in items)
        {
            await SendJsonAsync(config, HttpMethod.Post, "/rest/v1/chitiethoadon", new
            {
                id_sanpham = item.IdSanPham,
                id_hoadon = orderId,
                soluong = item.SoLuong,
                dongia = item.DonGia,
                thanhtien = item.ThanhTien,
                id_mactkm = item.IdMaCtkM
            }, cancellationToken);
        }

        return orderId;
    }

    public async Task<IReadOnlyList<PromotionDto>> GetPromotionsAsync(CancellationToken cancellationToken = default)
    {
        var config = await configuration;
        var today = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
        return await FetchListAsync<PromotionDto>(
            config,
            $"/rest/v1/chuongtrinhkhuyenmai?select=*&trangthai=eq.1&ngaybd=lte.{today}&ngaykt=gte.{today}",
            cancellationToken);
    }

    public async Task<IReadOnlyList<VoucherDto>> GetVouchersAsync(CancellationToken cancellationToken = default)
    {
        var config = await configuration;
        var today = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
        return await FetchListAsync<VoucherDto>(
            config,
            $"/rest/v1/kmtheovoucher?select=id_voucher,id_mactkm,phantram,soluong,chuongtrinhkhuyenmai!inner(tenctkm,ngaybd,ngaykt,trangthai)&soluong=gt.0&chuongtrinhkhuyenmai.trangthai=eq.1&chuongtrinhkhuyenmai.ngaybd=lte.{today}&chuongtrinhkhuyenmai.ngaykt=gte.{today}",
            cancellationToken);
    }

    public async Task<decimal> ApplyVoucherAsync(string voucherCode, decimal orderTotal, CancellationToken cancellationToken = default)
    {
        var config = await configuration;
        var today = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
        var vouchers = await FetchListAsync<VoucherDto>(
            config,
            $"/rest/v1/kmtheovoucher?select=id_voucher,id_mactkm,phantram,soluong,chuongtrinhkhuyenmai!inner(ngaybd,ngaykt,trangthai)&id_voucher=eq.{Uri.EscapeDataString(voucherCode.Trim())}&soluong=gt.0&chuongtrinhkhuyenmai.trangthai=eq.1&chuongtrinhkhuyenmai.ngaybd=lte.{today}&chuongtrinhkhuyenmai.ngaykt=gte.{today}&limit=1",
            cancellationToken);
        var voucher = vouchers.FirstOrDefault();
        if (voucher?.PhanTram is null || voucher.SoLuong <= 0 || voucher.Promotion is null ||
            voucher.Promotion.TrangThai != 1 || voucher.Promotion.NgayBd?.Date > DateTime.UtcNow.Date ||
            voucher.Promotion.NgayKt?.Date < DateTime.UtcNow.Date)
            return 0;
        return Math.Min(orderTotal, orderTotal * voucher.PhanTram.Value / 100m);
    }

    public async Task<IReadOnlyList<OrderModel>> GetOrdersAsync(CancellationToken cancellationToken = default)
    {
        var config = await configuration;
        if (currentUser is null)
            throw new InvalidOperationException("Vui lòng đăng nhập để xem đơn hàng.");

        var orders = await FetchListAsync<OrderDto>(
            config,
            $"/rest/v1/hoadon?select=id_hoadon,ngaydat,ngaygiao,tongtien,trangthai&id_khachhang=eq.{Uri.EscapeDataString(currentUser.Id)}&order=ngaydat.desc",
            cancellationToken);

        var result = new List<OrderModel>();
        foreach (var o in orders)
        {
            var items = await FetchListAsync<OrderItemDto>(
                config,
                $"/rest/v1/chitiethoadon?select=id_sanpham,soluong,dongia,thanhtien,sanpham(tensanpham)&id_hoadon=eq.{o.IdHoadon}",
                cancellationToken);

            result.Add(new OrderModel
            {
                Code = $"MK-{o.IdHoadon:D5}",
                OrderId = o.IdHoadon.ToString(),
                Date = o.Ngaydat?.ToString("dd/MM/yyyy") ?? string.Empty,
                TotalAmount = o.Tongtien ?? 0,
                Status = o.TrangThai,
                Items = new ObservableCollection<OrderItemModel>(items.Select(i => new OrderItemModel
                {
                    ProductName = i.TenSanPham ?? "Sản phẩm",
                    Quantity = i.SoLuong ?? 0,
                    UnitPrice = i.DonGia ?? 0,
                    TotalPrice = i.ThanhTien ?? 0,
                })),
            });
        }

        return result;
    }

    private async Task<IReadOnlyList<ProductDto>> FetchProductsAsync(SupabaseConfiguration config, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(config, HttpMethod.Get, "/rest/v1/sanpham?select=*,kmtheosanpham(phantram,chuongtrinhkhuyenmai(ngaybd,ngaykt,trangthai))&isdeleted=eq.0&trangthai=eq.1&order=id_sanpham");
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<IReadOnlyList<ProductDto>>(JsonOptions, cancellationToken) ?? [];
    }

    private async Task<(IReadOnlyList<LoaigoDto> Loaigo, IReadOnlyList<KieudangDto> Kieudang, IReadOnlyList<LoaispDto> Loaisp)> FetchLookupsAsync(SupabaseConfiguration config, CancellationToken cancellationToken)
    {
        var loaigo = await FetchListAsync<LoaigoDto>(config, "/rest/v1/loaigo?select=id_mago,tengo&isdeleted=eq.0&trangthai=eq.1", cancellationToken);
        var kieudang = await FetchListAsync<KieudangDto>(config, "/rest/v1/kieudang?select=id_kieudang,id_loaisp,tenkieudang&isdeleted=eq.0&trangthai=eq.1", cancellationToken);
        var loaisp = await FetchListAsync<LoaispDto>(config, "/rest/v1/loaisp?select=id_loaisp,tenloai&isdeleted=eq.0&trangthai=eq.1", cancellationToken);
        return (loaigo, kieudang, loaisp);
    }

    private async Task<IReadOnlyList<T>> FetchListAsync<T>(SupabaseConfiguration config, string path, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(config, HttpMethod.Get, path);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<IReadOnlyList<T>>(JsonOptions, cancellationToken) ?? [];
    }

    private int GetCurrentCustomerId()
    {
        if (currentUser is null || !int.TryParse(currentUser.Id, out var customerId))
            throw new InvalidOperationException("Vui lòng đăng nhập để tiếp tục.");
        return customerId;
    }

    private async Task SendJsonAsync(
        SupabaseConfiguration config,
        HttpMethod method,
        string path,
        object payload,
        CancellationToken cancellationToken)
    {
        using var request = CreateRequest(config, method, path, useAccessToken: false);
        request.Content = JsonContent.Create(payload, options: JsonOptions);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
    }

    private async Task UpdateAsync(SupabaseConfiguration config, string path, object payload, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(config, HttpMethod.Patch, path, useAccessToken: false);
        request.Content = JsonContent.Create(payload, options: JsonOptions);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
    }

    private async Task<IReadOnlyList<T>> SendJsonReturningAsync<T>(
        SupabaseConfiguration config,
        HttpMethod method,
        string path,
        object payload,
        CancellationToken cancellationToken)
    {
        using var request = CreateRequest(config, method, path, useAccessToken: false);
        request.Headers.Add("Prefer", "return=representation");
        request.Content = JsonContent.Create(payload, options: JsonOptions);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<IReadOnlyList<T>>(JsonOptions, cancellationToken) ?? [];
    }

    private HttpRequestMessage CreateRequest(SupabaseConfiguration config, HttpMethod method, string path, bool useAccessToken = true)
    {
        var baseUrl = config.Url.TrimEnd('/');
        const string restApiPath = "/rest/v1";
        if (baseUrl.EndsWith(restApiPath, StringComparison.OrdinalIgnoreCase))
            baseUrl = baseUrl[..^restApiPath.Length];

        var request = new HttpRequestMessage(method, $"{baseUrl}{(path.StartsWith('/') ? path : $"/{path}")}");
        request.Headers.Add("apikey", config.AnonKey);
        var bearerToken = useAccessToken && !string.IsNullOrWhiteSpace(accessToken)
            ? accessToken
            : config.AnonKey;
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        return request;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var details = await response.Content.ReadAsStringAsync();
        var errorCode = string.Empty;
        var serverMessage = string.Empty;
        try
        {
            using var document = JsonDocument.Parse(details);
            errorCode = document.RootElement.TryGetProperty("error_code", out var code)
                ? code.GetString() ?? string.Empty
                : string.Empty;
            serverMessage = document.RootElement.TryGetProperty("message", out var messageElement)
                ? messageElement.GetString() ?? string.Empty
                : string.Empty;
        }
        catch (JsonException)
        {
            serverMessage = details.Trim();
        }

        var message = errorCode switch
        {
            "invalid_credentials" => "Email hoặc mật khẩu không đúng. Nếu bạn chưa có tài khoản, hãy chọn Đăng ký.",
            "email_exists" => "Email này đã được đăng ký. Hãy đăng nhập hoặc dùng email khác.",
            "user_already_exists" => "Email này đã được đăng ký. Hãy đăng nhập hoặc dùng email khác.",
            "weak_password" => "Mật khẩu chưa đủ mạnh. Hãy dùng ít nhất 6 ký tự.",
            _ => string.IsNullOrWhiteSpace(serverMessage)
                ? $"Supabase trả về {(int)response.StatusCode} ({response.ReasonPhrase})."
                : $"Supabase trả về {(int)response.StatusCode}: {serverMessage}"
        };

        throw new HttpRequestException(message);
    }

    private sealed class OrderItemDto
    {
        [JsonPropertyName("id_sanpham")] public int IdSanPham { get; set; }
        [JsonPropertyName("tensanpham")] public string? TenSanPham { get; set; }
        [JsonPropertyName("soluong")] public int? SoLuong { get; set; }
        [JsonPropertyName("dongia")] public decimal? DonGia { get; set; }
        [JsonPropertyName("thanhtien")] public decimal? ThanhTien { get; set; }
    }

    private sealed class LoaigoDto
    {
        [JsonPropertyName("id_mago")] public int? IdMago { get; set; }
        [JsonPropertyName("tengo")] public string? Tengo { get; set; }
    }

    private sealed class KieudangDto
    {
        [JsonPropertyName("id_kieudang")] public int? IdKieudang { get; set; }
        [JsonPropertyName("id_loaisp")] public int? IdLoaiSp { get; set; }
        [JsonPropertyName("tenkieudang")] public string? Tenkieudang { get; set; }
    }

    private sealed class LoaispDto
    {
        [JsonPropertyName("id_loaisp")] public int? IdLoaiSp { get; set; }
        [JsonPropertyName("tenloai")] public string? TenLoai { get; set; }
    }

    private sealed class OrderDto
    {
        [JsonPropertyName("id_hoadon")] public int IdHoadon { get; set; }
        [JsonPropertyName("ngaydat")] public DateTime? Ngaydat { get; set; }
        [JsonPropertyName("ngaygiao")] public DateTime? Ngaygiao { get; set; }
        [JsonPropertyName("tongtien")] public decimal? Tongtien { get; set; }
        [JsonPropertyName("trangthai")] public int TrangThai { get; set; }
    }

    private sealed class CustomerDto
    {
        [JsonPropertyName("id_khachhang")] public int? IdKhachhang { get; set; }
        [JsonPropertyName("hoten")] public string? HoTen { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("sdt")] public string? Sdt { get; set; }
    }

    private sealed class LoginAccountDto
    {
        [JsonPropertyName("tendangnhap")] public string? TenDangNhap { get; set; }
        [JsonPropertyName("matkhau")] public string? MatKhau { get; set; }
        [JsonPropertyName("id_khachhang")] public int? IdKhachhang { get; set; }
    }

    private sealed class AuthenticatedCustomerDto
    {
        [JsonPropertyName("id_khachhang")] public int? IdKhachhang { get; set; }
        [JsonPropertyName("hoten")] public string? HoTen { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
    }
}

public sealed record AuthenticatedUser(string Id, string Email, string FullName, string Phone = "");
