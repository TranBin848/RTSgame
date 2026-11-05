# Kiến trúc Game Flow

## Mục tiêu và phạm vi

Một bản build bắt đầu ở `MainMenu`; người chơi bấm **Chơi** để vào `ProceduralGame`, dùng nút góc trên phải hoặc `Escape` để tạm dừng, rồi tiếp tục, chỉnh âm lượng, về menu hoặc thoát. Module này chỉ điều phối scene và UI; các hệ thống RTS, DOTS và bản đồ vẫn thuộc scene gameplay hiện có.

## Dữ liệu và luồng xử lý

`Assets/Game/Scripts/UI/GameFlowMenu.cs` là MonoBehaviour gắn trên `MenuCanvas` và `GameFlowCanvas`. Các bảng UI và `Slider` được nối qua serialized fields. Button dùng `UnityEvent` trong scene. `Play()` và `ReturnToMenu()` gọi `SceneManager.LoadScene` theo tên scene. `Pause()` đặt `Time.timeScale = 0`; `Resume()`, chuyển scene và `Awake()` đặt lại `1`. `Escape` đóng bảng Options trước, sau đó đổi trạng thái Pause. Canvas gameplay có sorting order 100 để nút và bảng hiện phía trên HUD.

Options hiện có một điều khiển âm lượng chung. `AudioListener.volume` nhận giá trị từ slider; `PlayerPrefs` lưu khóa `MasterVolume` để dùng ở scene sau hoặc lần chạy sau. Không có cấu hình đồ họa hoặc remap phím.

UI dùng UGUI như HUD hiện có. Nút dùng sprite `TinySquareBlueButton` và `TinySquareRedButton`, bảng dùng `WoodTable`, chữ dùng `PatrickHand-Regular`. Hai sprite nút được import dạng Sprite (Single) với border 9-slice để kéo dài mà không méo viền.

## Tích hợp và giới hạn

`ProjectSettings/EditorBuildSettings.asset` đặt `MainMenu` ở index 0 và `ProceduralGame` ở index 1; `SampleScene` và `SwarmSimulation` vẫn được giữ để phát triển. `Time.timeScale = 0` dừng thời gian mô phỏng theo Unity; cần kiểm tra thêm từng hệ thống fixed-step ECS và tác vụ chạy theo thời gian thực trong bản Player. Chưa có save/load; vào game từ menu bắt đầu phiên chơi mới.

## Thuật toán và nguồn tham khảo

Luồng chỉ có chuyển scene đồng bộ và bật/tắt bảng UI, không có thuật toán gameplay mới. Nguồn kiểm chứng chính: `GameFlowMenu.cs`, `MainMenu.unity`, `ProceduralGame.unity`, `EditorBuildSettings.asset`; tham chiếu API Unity: `SceneManager.LoadScene`, `Time.timeScale`, `PlayerPrefs` và `AudioListener.volume`.
