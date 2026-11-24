# Báo cáo nền tảng SL0–SL2

Ngày **24/11/2026**. Mục tiêu: làm inventory/schema/identity/codec và storage theo [BuildPlan](BuildPlan.md), trước capture/restore. Trạng thái: **SL0 DONE, SL1 PARTIAL, SL2 Core DONE**. Toàn hệ thống Save/Load chưa hoàn thành.

**Checkpoint lịch sử trước SL3.** Lượt tiếp đã hoàn tất catalog thực và capture, xem [SL3 report](SL3-Report.md); không thay các số đo/test đã ghi dưới đây.

## Implementation

Thêm assembly Core `RTS.SaveLoad` không tham chiếu engine và test assembly riêng. Schema field tường minh có whitelist/config fingerprint, UTF-8 envelope SHA-256, validation trước decode/apply, hai slot generation/temp/backup, OS writer lock và fault probe. Không cài dependency mới. Phạm vi dữ liệu/thuật toán nằm ở [Architecture](Architecture.md), coverage source ở [Inventory](Inventory.md), API/cách chạy ở [Usage](Usage.md).

Runtime DOTS thêm SaveIdentity dùng chung factories và seed lưu được cho GameplayActor/SwarmAgent. Các công thức RNG/cadence giữ seed bằng Entity.Index ban đầu; không đổi balance. Lưu MaxHealth authoring override và producer SpawnSequence trong schema. Chưa inject service vào root/menu; startup scene vẫn NewGame.

## Kết quả kiểm

Kết quả sau source cuối, chạy trực tiếp qua Unity Test Runner:

| Bộ kiểm | Kết quả | Duration Test Runner | Evidence |
|---|---|---|---|
| RTS.SaveLoad.Tests | 47/47 PASS,0skip |58.0970868s|[JSON](Evidence/SL0-SL2-2026-11-24/CoreTests.json), [XML](Evidence/SL0-SL2-2026-11-24/CoreTests.xml)|
| RTS.DOTS.Tests |75/75 PASS,0skip|3.9729792s|[JSON](Evidence/SL0-SL2-2026-11-24/DotsTests.json), [XML](Evidence/SL0-SL2-2026-11-24/DotsTests.xml)|
| RTS.ProceduralGeneration.Tests |17/17 PASS,0skip|6.9887462s|[JSON](Evidence/SL0-SL2-2026-11-24/ProceduralTests.json), [XML](Evidence/SL0-SL2-2026-11-24/ProceduralTests.xml)|

Tổng **139/139 PASS**. Bao gồm phép thử filesystem Windows thật: File.Replace giữ thế hệ cũ, lỗi trước commit giữ primary/backup, lỗi sau commit đọc được thế hệ mới, read fallback không sửa file, write sau fallback giữ backup tốt.

[Startup smoke ProceduralGame](Evidence/SL0-SL2-2026-11-24/StartupSmoke.json) PASS: seed1/generator3, Ready,25actors/150resource components,18cừu actor+resource cùng Entity;846identity còn sống đều dương/unique ở thời điểm lấy mẫu, nextSaveId1534. Tổng identity gồm obstacle proxies dẫn xuất và có khoảng trống do nav rebuild, không phải846đơn vị gameplay. Đây chỉ là kiểm startup, chưa phải capture/restore.

[Sample nhân tạo](Evidence/SL0-SL2-2026-11-24/SyntheticSample.json):10.587bytes,4objects, timestamp/UUID cố định, không chứa dữ liệu cá nhân. Sample có research/cargo/FIFO/đạn giữa quỹ đạo để minh họa DTO; không lấy từ trận thực. [Source fingerprints](Evidence/SL0-SL2-2026-11-24/SourceFingerprints.json) dùng SHA-256 trên code thực tế, gồm cả thay đổi chưa commit của workspace.

Các lượt trung gian Core42/42 và DOTS74/74 là lịch sử development; bảng trên mới là lượt sau source cuối.

## Đo lường và giới hạn

Chưa có số đo save/load gameplay. Duration của Test Runner bao gồm fixture/fault I/O và Editor overhead, không được dùng làm latency/fps. Các phép thử ghi File.Replace/Flush(true) chạy trong Unity Editor trên filesystem Windows hiện tại; chưa build/cold-process Player, chưa cưỡng bức tắt máy giữa syscall.

SL1 vẫn còn actual catalog authoring/resolver/fingerprint scene; SL3 capture chưa có; SL4–SL9 giữ TODO. Không ghi báo cáo KLTN là đã hoàn thành Save/Load. Hướng tiếp theo theo [Progress](Progress.md).
