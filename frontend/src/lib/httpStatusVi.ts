/** Diễn giải mã HTTP cho người dùng (tiếng Việt). */
export function describeHttpStatus(code: number): string {
  const map: Record<number, string> = {
    0: "Không kết nối được tới máy chủ",
    200: "Thành công",
    201: "Đã tạo mới thành công",
    202: "Đã tiếp nhận xử lý",
    204: "Không có nội dung trả về",
    304: "Dữ liệu không thay đổi",
    400: "Yêu cầu không hợp lệ",
    401: "Phiên đăng nhập hết hạn hoặc thông tin đăng nhập sai",
    403: "Bạn không có quyền thực hiện thao tác này",
    404: "Không tìm thấy tài nguyên",
    405: "Phương thức không được hỗ trợ",
    408: "Hết thời gian chờ yêu cầu",
    409: "Dữ liệu xung đột hoặc trạng thái không cho phép",
    413: "Tệp gửi lên quá lớn",
    415: "Định dạng dữ liệu không được chấp nhận",
    422: "Dữ liệu không thể xử lý",
    429: "Quá nhiều yêu cầu, vui lòng thử lại sau",
    500: "Lỗi nội bộ trên máy chủ",
    502: "Cổng phụ trợ không phản hồi",
    503: "Dịch vụ tạm thời không sẵn sàng",
    504: "Hết thời gian chờ phản hồi từ máy chủ",
  };
  return map[code] ?? `Mã phản hồi HTTP ${code}`;
}
