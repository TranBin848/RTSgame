# E2 checkpoint — 23/11/2026

**DONE phạm vi chức năng/ảnh và hai cặp Release A/B seed1 đã đo.** [Báo cáo đầy đủ](../../E2-NightLighting.md). Không đóng G3/G5 hoặc chứng nhận tải8đèn Player.

- E0 được người dùng xác nhận “tôi đã test và thấy oke”; [Acceptance](../E0/Acceptance.md) giữ rõ giới hạn artifact.
- Unity MCP có live RTSgame2026; vì tool thao tác Editor chưa hiện trong chat, dùng CLI chính thức có sẵn trong cache qua config server. Instance thay đổi cuối phiên; đã chọn lại và xác minh Ready trước lifecycle cuối. Không nâng Editor/pipeline/package.
- E2: root inject game/clock/fog/map/camera/palette/visibility renderer; Hall/Tower/depot có điểm sáng; shader mask từng pixel; budget8local/camera, glow fallback; clock vẫn60/30/45, không thêm RNG gameplay.
- Fixture phase/pause/culling/registry/8-light budget/pool/death/destroy/fog PASS. GPU mask và SpriteRenderer tint PASS; raw trong integration-result.txt và gpu-mask-tint-result.txt.
- Regression mới81/81 PASS,0fail/skip,19.8600791s; raw editmode-tool-result.txt.
- Night fog-on A/B cùng seed/camera/state: before0/0, after3/3 local lights/glow groups. PNG gốc trước/sau; fixture Tower/Depot không phải giao dịch xây dựng thật.
- Windows Release Mono build Succeeded228.6971s,0errors/0warnings;137776076bytes theo report. CLI ngừng chờ khi gặp progress modal compile shader; build vẫn chạy, kết luận dựa BuildReport trên disk và việc Player chạy thành công, không dựa lỗi modal của CLI.
- Hai cặp hợp lệ ở thư mục .utmp/e2-lighting-onscreen-2026-11-23-retry2, warm30/capture90 mỗi lượt,1080p/Ultra/D3D11/vSync1,invalid0. P95baseline13.8878/13.8879ms; candidate13.8881/13.8879ms; tăng0.00216%/0%, đạt gate đề xuất10%. Có đầy đủ p99/max/frames>16.67ms. GC Release unavailable.
- Player đo1local/1glow group;8local chỉ kiểm Editor. VSync lượng tử hóa frame time: không tuyên bố chi phí đèn bằng0, không dùng làm GPU benchmark.
- Giữ cả đợt đầu và retry1 có920/30frame invalid; run-history.json ghi7lượt, chỉ4lượt retry2 được dùng so sánh. Không loại từng frame hoặc xóa lượt chưa hợp lệ để cải thiện thống kê.
- Cleanup qua MainMenu→game mới và SampleScene PASS trên instance đã Ready.7owned IDs bị hủy khi unload, đúng1controller khi reload, research reset; lifecycle-results.json. Source probes lưu trong Tools/ValidateGameExperienceE2Lifecycle.cs.
- Final Editor: ProceduralGame đã lưu, ngoài Play/pause/compile; xem final-editor-state.txt. Shared scene/timing SampleScene và DayNightCycleDefinition không có diff. Font TMP auto-cache tự đổi khi kiểm thử đã được hoàn tác riêng, không đưa vào implementation.
- Fingerprint lưu source/config, executable, Assembly-CSharp.dll và asset bundles liên quan. Build/cache nặng và CSV gốc giữ ở .utmp, không đưa cả build vào Docs.

**Tiếp E3**: cảnh báo/chuông Dusk, countdown clock thật, hướng gateway chỉ khi đã khám phá, không hứa hướng/loại quái chưa chốt. Tái sử dụng ramp đèn E2, không tạo đồng hồ presentation mới. E4–E10 chưa triển khai trong lượt này.
