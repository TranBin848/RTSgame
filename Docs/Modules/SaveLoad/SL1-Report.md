# SL1 — Identity, DTO và codec

Ngày24/11/2026, **DONE phạm vi implementation/Editor sau SL3**. Đã triển khai SaveIdentity/allocator chung, seed runtime không phụ thuộc Entity sau remap, schema DTO, SaveCatalog whitelist/fingerprint, validator và UTF-8 JSON codec. Catalog authoring21prefab/8action/6tech, resolver prefab/action và fingerprint toàn content/config hiện có đã nối. [Architecture](Architecture.md) mô tả schema/API/thuật toán/giới hạn; [Usage](Usage.md) cách chạy tests; [báo cáo nền tảng](SL0-SL2-Report.md) là lịch sử trước SL3.

Xem [SL3 report](SL3-Report.md) cho kiểm catalog/fingerprint thực. Capture round-trip không chứng nhận restore hoặc cold-process Continue; SL4–SL9 vẫn mở.
