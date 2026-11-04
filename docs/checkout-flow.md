# Luồng mua hàng

1. Giỏ hàng: chọn từng sản phẩm hoặc chọn tất cả; chỉ hàng được chọn được tính
   vào tổng tiền, ngưỡng miễn phí vận chuyển và bước thanh toán.
2. Thanh toán: kiểm tra địa chỉ đang chọn, thông tin liên hệ, danh sách sản phẩm,
   giao hàng, voucher, phương thức và tổng tiền. Nút Đặt hàng cố định phía dưới.
3. COD: tạo đơn trạng thái đang duyệt bằng luồng hiện có. Màn hình kết quả nêu rõ
   thanh toán khi nhận hàng; không coi đặt hàng thành công là đã thu được tiền.
   Chỉ các dòng đã đặt được xóa khỏi giỏ. Người dùng có thể mở Đơn mua hoặc mua tiếp.
4. MoMo: hiện hiển thị đang thiết lập, không cho gửi đơn bằng lựa chọn này.
   `MoMo.Api` có phần giao tiếp MoMo và xác minh IPN, nhưng chưa có adapter lưu
   giao dịch/hóa đơn, xác thực backend hoặc nối vào app. Xem README của backend.
   Cần tài khoản merchant/sandbox, cấu hình HTTPS và schema/trigger đầy đủ trước
   khi hoàn thiện và kiểm thử với MoMo.

Thêm, chọn và sửa số lượng trong giỏ chỉ tác động dữ liệu giỏ cục bộ, không trừ kho
trong database. Việc trừ kho thuộc thời điểm thanh toán và hình thành hóa đơn theo
nghiệp vụ. Không thêm lệnh trừ kho vào bước chọn hàng.

Schema HOADON hiện có chỉ chứa trạng thái xử lý đơn (hủy/đang duyệt/hoàn thành),
chưa có trạng thái thanh toán. Không dùng `trangthai=1` để khẳng định MoMo đã thu tiền.
Adapter giao dịch online cần lưu trạng thái thanh toán riêng, tạo hóa đơn và cập nhật
kho đúng một lần sau IPN thành công, có xử lý khi thanh toán xong nhưng không đủ hàng.

Phần chưa được cung cấp để nối database: DDL của SANPHAM, các bảng khuyến mãi,
policy RLS và trigger liên quan đến HOADON/CHITIETHOADON/SANPHAM.
