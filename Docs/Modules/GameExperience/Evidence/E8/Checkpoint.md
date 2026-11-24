# Checkpoint E8 — 24/11/2026

**Implementation/kiểm kỹ thuật Editor đã xong; G3manual và nghiệm thu đầy đủE8 vẫn OPEN.** Người dùng trả lời chưa thửG3 và yêu cầu giữ mở. [Report đầy đủ/giới hạn](../../E8-ExpansionCommands.md).

- [89/89 EditMode](editmode-result.json), [integration](integration.txt), [preview text](preview-text.txt), [ảnh HUD](preview-hud.png).
- [Movement rally](rally-arrival.txt), [actual shelter/resume](shelter-arrival.txt), [lifecycle/menu/reload/legacy](lifecycle.txt).
- [Logistics audit/summary](logistics-summary.json), raw ba lượt trong `Logistics/`; Hall/kho cùng seed0wave8001,mỏ,carrier,khôngresearch/train/rally. Window535s,Hall80G/kho240G,cost100W100G,net+60G đổi100W. Empty0income/range14.13,ba lượt thuađêm4. Source `Tools/PlaytestE8Logistics.cs` deriveG3, không ghi đè evidenceG3; `node Tools/SummarizeE8.mjs` PASS.
- [Editor cuối](final-editor-state.txt), [RQL](final-rql-state.txt), [source fingerprint](source-fingerprint.json).

Fixture integration có node/build/reveal/health/position injection để kiểm task và layout, không dùng làm natural scouting/income evidence. Archer gốc ở ô thiếuclearance bị từ chối đúngguard; fixture chuyển sang ô resolve hợp lệ rồi mới kiểm actualmovement. Giới hạn spawn này ghi rõ trong report, không coi mọi actor khởi tạo đã cópath.

Seed0 chỉ đổi memory trong cặp đo; seed1 gốc khôi phục. Không nângEditor/pipeline, không tune giá/balance, không commitGit. Chưa Player/performance/naturalE8 hoặc manualG3; E9 là mốc tiếp nếu được yêu cầu.
