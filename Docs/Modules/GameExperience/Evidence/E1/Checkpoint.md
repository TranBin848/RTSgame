# Checkpoint E1 — 23/11/2026

**DONE phạm vi palette/ownership và kiểm chứng Editor.** Báo cáo đầy đủ: [E1-ArtDirection](../../E1-ArtDirection.md). User yêu cầu E1 riêng sau khi hẹn kiểm drag E0 sau; không đóng E0, G3 hoặc G5.

- Đã compile Unity6000.3.11f1; ProceduralGame dùng asset EnvironmentPresentation riêng, timing60/30/45.
- Integration/rebind/clone/shared profile, phase interpolation và invariants gameplay PASS; `integration-result.json`.
- SampleScene legacy PASS; `sample-scene-result.json`. Scene và asset timing dùng chung không thay đổi trên disk.
- MainMenu→game→menu→game bằng API + Editor.Step PASS; profile và từng override component được dọn; `lifecycle-result.json`. Không gán phép kiểm này là thao tác chuột thật.
- Regression mới81/81 EditMode PASS, fail0/skip0,6.4597257s; `editmode-tool-result.json` lưu raw response.
- Ảnh A/B gốc7PNG1920×1080, seed1/v3, cùng camera/state đóng băng; 6ảnh fog tắt để so màu và1ảnh Night fog bật. Có quái và đạn instanced; chỉ thêm mẫu minh họa trước capture, không đổi asset/policy.
- Final Editor đã mở lại **ProceduralGame**, scene sạch, ngoài Play/pause/compile. Không giữ fixture runtime qua bàn giao. `fingerprint.json` lưu SHA256 của source/config/scene/harness liên quan.
- Không chỉnh shader/render pipeline/material prefab, không tạo light/VFX E2. Unity tự serialize lại một số giá trị tile animation `10`↔`10.000001` khi lưu scene; diff chức năng của scene là reference palette mới. Cache font TMP tự cập nhật trong kiểm thử đã được hoàn tác riêng.
- Chưa Player build/benchmark E1, chưa kiểm người dùng trên màn hình thật hoặc chuyển pha tự nhiên cả trận. Màu/budget có thể tiếp tục được điều chỉnh từ playtest, không phải số đo FPS.

Tiếp **E2: ánh sáng đêm**, đặc biệt đối chiếu shader unlit và chi phí glow/Light2D trước tăng budget. E0/manual drag vẫn mở.
