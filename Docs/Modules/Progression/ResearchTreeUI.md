# Giao diện cây công nghệ toàn màn hình

Ngày 23/11/2026. Phạm vi: thay giao diện nghiên cứu sáu panel trên HUD bằng cây công nghệ có kéo/zoom. Giữ nguyên sáu ID, prerequisite, giá, thời gian, modifiers và command ECS. Các hub lĩnh vực là phân nhóm trực quan, không phải công nghệ mua thêm.

## Mục tiêu và hình thức

Tham khảo cách trình bày graph trên nền tối, đường nối phát sáng và điều hướng drag/zoom trong [Crown Siege trên Steam](https://store.steampowered.com/app/4310440/Crown_Siege/) cùng hai ảnh người dùng cung cấp. Dùng icon Tiny Swords hiện có của dự án, không sao chép asset của game tham khảo.

Node TownHall ở giữa nối ba hub: Kinh tế màu xanh lục, Quân sự màu xanh lam và Mở rộng màu tím. Ba chuỗi công nghệ hiện có là Tools/Cargo, Archery/Training và Outposts/Fortify. Tên tiếng Việt chỉ là presentation, ID runtime giữ nguyên. Chọn node mở thông tin bên phải và bấm Nghiên cứu để gửi lệnh.

## Dữ liệu, owner và tích hợp

- `ResearchTreePanel` nhận `IResearchService`, `GameManager`, `CameraController` qua `Initialize` từ `GameSceneCompositionRoot`. Catalog definition được đọc khi tạo sáu node. UI không giữ ví, unlock hoặc timer authoritative riêng.
- `ResearchTreeViewport` giữ vị trí và tỷ lệ hiển thị của graph. `ResearchTreeGraphic` dựng mesh UGUI cho khung, vòng sáng, đường nối và nền. Glow dùng mesh alpha nhiều lớp, không cần đổi URP hoặc bật bloom trên camera.
- `ResearchTreeEditorTools.Configure` cấu hình icon/reference trong scene và vô hiệu panel cũ. Công cụ tạo lại scene procedural gọi cùng hàm để không quay về bố cục cũ. Panel cũ được giữ trong scene ở trạng thái inactive.
- `GameManager` được cấp reference cây để chặn selection/placement khi overlay mở và trong frame vừa đóng. Camera giữ khóa cũ rồi khôi phục khi đóng. `GameFlowMenu` không nhận tiếp Escape đã dùng để đóng cây. Không có global/static service theo scene mới.

Trận vẫn chạy khi mở cây, không đổi `Time.timeScale`. Điều này cho phép nhìn tiến độ nghiên cứu ngay trong panel, đồng thời người chơi vẫn chịu áp lực wave. Nhãn trong bảng chi tiết nói rõ hành vi này. Nếu đang pause, không mở cây mới; khi trận kết thúc cây đóng và result UI hiện ra. Close/disable/unload khôi phục khóa camera.

Lệnh nghiên cứu ưu tiên TownHall đang chọn nếu hợp lệ, nếu không dùng TownHall đã hoàn thành, còn sống, đã bind ECS trong registry của GameManager. UI không tự chọn một Unit hoặc Find manager. `ResearchService.Start` và command ECS vẫn kiểm tra tính hợp lệ cuối cùng, prerequisite, tiền và slot trước debit.

## Thuật toán điều hướng

Zoom giới hạn 0,45–1,6. Với pivot con trỏ `p`, pan mục tiêu `t`, scale cũ `z` và scale mới `z'`, pan mới là `p - (p - t) * z' / z`. Nhờ đó điểm graph dưới con trỏ giữ vị trí mục tiêu. Pan và scale hiển thị dùng cùng hệ số `1 - exp(-16 * unscaledDeltaTime)` để chuyển mượt, không phụ thuộc tốc độ mô phỏng.

Drag dùng tọa độ local của viewport để tôn trọng CanvasScaler. Sau khi vượt drag threshold, bỏ `eligibleForClick` để kéo trên node không đồng thời chọn hoặc mua tech. Pan có giới hạn để có thể tìm lại cây. Nút Về trung tâm fit bố cục hiện tại vào viewport.

## Cách chạy

1. Mở `ProceduralGame` hoặc vào từ MainMenu. Nếu cần cấu hình lại reference, dùng `RTS/Progression/Configure Research Tree UI` ngoài Play Mode. Không cần tạo lại toàn scene.
2. Bấm **Công nghệ [T]** hoặc phím **T** để mở/đóng.
3. Kéo chuột trái hoặc giữa trên vùng cây. Cuộn chuột hoặc bấm +/− để zoom. Home hoặc Về trung tâm reset view.
4. Chọn một trong sáu node, đọc prerequisite/giá/hiệu ứng rồi bấm Nghiên cứu. Esc đóng cây, không đồng thời bật menu pause.

## Kiểm thử và bằng chứng

`Tools/ValidateResearchTree.cs` là fixture tích hợp chạy một lần trong Play Mode trận mới. Nó kiểm node/reference, modal/camera, init lặp, prerequisite không debit, zoom pivot/clamp/NaN, drag không click, chọn Hall tự động và giao dịch nghiên cứu không debit hai lần. Fixture có bắt đầu Tools và thay selection, vì vậy thoát Play sau khi chạy. `Tools/ValidateS3.cs` chấp nhận cây mới hoặc sáu panel legacy và vẫn kiểm toàn bộ unlock/modifier.

Kết quả thực chạy, ảnh và giới hạn nghiệm thu sẽ được bổ sung sau khi kiểm Unity. Chưa có số đo hiệu năng mới của overlay. Kết quả allocation của `ResearchPanel` cũ trong S9 không áp dụng cho panel graph này.

## Giới hạn và hướng phát triển

Sáu công nghệ chỉ tạo ba chuỗi hai cấp, không phải cây hàng trăm nâng cấp. Hub và root không có cost/level hoặc hiệu ứng mới. Chưa có nâng cấp nhiều level, refund, save/load hoặc thay đổi cân bằng. Desktop mouse drag/scroll là phạm vi điều hướng chính, chưa bổ sung pinch gesture riêng.

UI tạo khi composition, không dựng lại mỗi frame. Trạng thái refresh tối đa 10 Hz khi mở; mesh không tham gia simulation DOTS. Chưa benchmark lượng node lớn hoặc mọi aspect ratio. Nếu mở rộng catalog, cần chuyển layout/metadata hiện cố định sang cấu hình và đánh giá lại graph, mask và tooltip.

Nguồn implementation: `ResearchTreePanel.cs`, `ResearchTreeViewport.cs`, `ResearchTreeGraphic.cs`, `ResearchTreeEditorTools.cs`, `GameplayResearch.cs`, `ResearchService.cs`, `EconomyTools.asset`. Tài liệu liên quan: [kiến trúc progression](Architecture.md), [cách chạy](Usage.md), [DI](../DependencyInjection/Architecture.md).
