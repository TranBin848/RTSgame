# Kiểm chứng Save/Load

Ngày **24/11/2026**, Unity Editor **6000.3.11f1**, Windows. [Progress](Progress.md) phân biệt chức năng có sẵn và gate chưa đạt. SL4 có hydrate và cold-process smoke first-day/idle-work; chưa có restore toàn trận hoặc Continue UI.

Kết quả SL4: **148/148 PASS** (47SaveLoad+83DOTS+18procedural), Windows build0errors/0warnings và cold-process capture→load0/1/1543 canonicalPASS. Preflight10ca, apply-failure hold và Resume PASS. Raw và giới hạn tại [SL4 report](SL4-Report.md). [145/145SL3](SL3-Report.md), [139/139SL0–SL2](SL0-SL2-Report.md) là lịch sử.

## Coverage tự động

- DTO encode→decode→so toàn bộ serialized fields; ID và ledger lớn hơn 2^53 giữ long chính xác; queue/cargo/research/đạn giữa quỹ đạo trong fixture.
- Duplicate/unknown/dangling/wrong-capability ID, overflow, NaN/Infinity, dimensions/masks/count/queue bounds, request ID, technology, negative cargo, shelter và construction sai bị từ chối.
- JSON thiếu/thừa field (kể cả field0), duplicate property, metadata type, numeric string, fractional integer, UTF-8 lỗi, truncated/checksum sai và oversized file bị từ chối.
- Ghi lần đầu rồi replace nhiều thế hệ; hai slot riêng, backup báo đúng generation; read không sửa primary hỏng/backup; write sau recovery không xoay corruption đè backup tốt.
- Inject IOException sau tạo temp/write/flush/verify, trước commit và sau commit; đọc lại thế hệ tốt, ghi lại sau lỗi. Double request một instance và instance thứ hai bị chặn; generation không lùi; validation lỗi không đổi primary.
- Factory actor/resource/enemy/projectile/proxy dùng cùng allocator; cừu Actor+Resource giữ một ID; reset không tái dùng ID. Cừu hai Entity khác nhau nhận cùng saved seed/counter có cùng graze timer.
- SL3 native tests: nested fence/command bị từ chối, cả hai tick dừng, dispose sau lỗi nhả fence, pending gameplay events không bị Clear, pending damage apply một lần, detached buffers và geometry collider native không lệ thuộc view.
- SL3 integration: catalog/resolver/hash thực, capture ở frame thật lúc pause/chạy, cargo/build/FIFO/research/shelter/đạn/native quái, double request, identity/claims/mapping150placements, repeat/error release và flush completion1lần. SampleScene legacy vẫn bind/25actors, không bị inject capture service.

## Gate còn mở

Fresh scene/process restore first-day/idle-work đã kiểm SL4; native collider/path cache và allocator nextSaveId đã import. Chưa kiểm completion exactly-once **sau load active queue**, shelter/daybreak, đạn/research đang chạy, clock/wave giữa đêm/reports, autosave, fullmenuclick, rollback hoặc repeated-load leak. Các state ngoài SL4 bị từ chối trước rời menu. Chưa Release median/p95/GC hoặc mất điện cưỡng bức; không gọi lát cắt này là Save/Load hoàn chỉnh.
