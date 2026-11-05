# Chạy Game Flow

1. Mở `Assets/Scenes/MainMenu.unity` trong Unity rồi nhấn Play. Khi build, scene này tự chạy đầu tiên theo Build Settings.
2. Bấm **Chơi** để mở `ProceduralGame`. Dùng nút **II** ở góc trên phải hoặc `Escape` để mở bảng tạm dừng.
3. **Tiếp tục** hoặc `Escape` để chạy tiếp. **Tùy chọn** mở thanh âm lượng; **Quay lại** trở về bảng trước. **Về Menu** tải lại `MainMenu`; **Thoát Game** dừng Play Mode trong Editor và đóng Player trong bản build.

Các scene `SampleScene` và `SwarmSimulation` vẫn có thể mở trực tiếp để kiểm thử riêng. Nếu Play từ một scene khác, menu không xuất hiện trước; đây là hành vi bình thường của Unity Editor.

Thẩm tra nhanh: Play → Pause → Options → đổi âm lượng → Quay lại → Tiếp tục → Pause → Về Menu. Kiểm tra bảng Pause che thao tác vào game; trong lúc Pause, chuyển động mô phỏng dừng. Khi sửa UI, kiểm tra lại tỉ lệ Game View hẹp và rộng. Chưa đo hiệu năng UI vì thao tác này không có yêu cầu benchmark; xem [Validation](Validation.md) để biết những bước đã xác nhận.
