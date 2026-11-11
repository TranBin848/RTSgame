# S3a — catalog và prerequisite runtime

Ngày 11/11/2026. S3a triển khai phần dữ liệu/command; **S3 tổng thể chưa hoàn tất**. Scene và authoring hiện vẫn dùng một economy_tools, chưa cho người chơi mua node chưa có hiệu ứng.

## Dữ liệu và luồng

ResearchDefinition thêm Id/Prerequisite. ConfigureResearchCatalog nhận 1–6 node, bắt buộc có economy_tools, copy cấu hình để caller không sửa dữ liệu của trận sau validation. ConfigureResearch đơn cũ là adapter một node và tiếp tục phục vụ EconomyTools.asset.

ID được hỗ trợ theo BuildPlan: economy_tools, economy_cargo, military_archery, military_training, expansion_tower, expansion_fortify. Chưa có framework effect tùy ý. Validator từ chối ID khác, duplicate, giá/duration/multiplier sai, prerequisite không tồn tại và chu kỳ bằng cách đi chuỗi parent hữu hạn. Mỗi node có tối đa một prerequisite ở phiên bản này, phù hợp cây ba nhánh hai cấp đang đề xuất.

PlayerResearch có UnlockMask và index Technology của slot đang chạy. Bit tương ứng thứ tự catalog đã chốt cho trận, không phải ID bền vững để save/load. Không đổi thứ tự catalog trong một trận; muốn persistence sau này phải lưu stable IDs. Trường Unlocked cũ giữ vai trò snapshot riêng economy_tools để panel và multiplier hiện tại tương thích; query công nghệ khác dùng IsTechnologyUnlocked(id).

StartResearch kiểm ID/prerequisite trước debit; Busy/AlreadyUnlocked vẫn bị từ chối. Tick hoàn tất OR bit của node, không xóa các bit khác. Mất hall chỉ hủy slot hiện tại và không hoàn tiền; các unlock trước được giữ. ResearchDuration trả duration của node đang chạy; GetResearchDefinition(id) query cấu hình theo ID.

## Kiểm thử

Đã thêm case: prerequisite thiếu không debit; catalog đảo thứ tự vẫn unlock đúng; caller sửa mảng sau configure không sửa giá runtime; hủy node con giữ node cha; duplicate/missing/cycle/unsupported bị từ chối và không làm system cấu hình dở; cả sáu ID và ba nhánh có thể unlock đủ mask 63 trong fixture.

Unity EditMode `RTS.DOTS.Tests` trên code S3a đạt **50/50 pass, 0 fail/skip**, 2,423 giây theo Test Runner ngày 11/11/2026. Trong ProceduralGame, root Ready, service cũ Start economy_tools trả Started và ví còn Wood/Gold 60/85. Đã thoát Play Mode. Chưa xuất XML hoặc build Player mới; đây là compatibility/API check, không phải playtest sáu tech.

## Điểm tiếp S3b

- Thêm hiệu ứng cargo, training và tower damage với dữ liệu config có kiểu rõ; validator phải phân biệt multiplier tăng/giảm, vì training ×0,8 không thể dùng nguyên quy tắc WoodMultiplier≥1.
- Khóa xây ArcheryRange/Tower và train Archer tại command/bridge, trước debit; kiểm các callers ActionSO, ProductionBuilding, BuildingProcess và đường trực tiếp. Scene legacy không bật progression phải giữ nội dung sẵn có.
- Mở authoring catalog đủ sáu cấu hình thử trong plan, service query theo node, UI ba nhánh. Chỉ publish node cho người chơi khi hiệu ứng/gate và test tương ứng hoàn tất.
- Chạy lại scene API và checks D2/UI sau khi mở catalog. Không kết luận đã có sáu hiệu ứng chỉ từ unlock mask.
