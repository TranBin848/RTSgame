# Checkpoint E4 — 23/11/2026

**E4 DONE phạm vi implementation/Editor**, tiếp E5. [Báo cáo và ảnh](../../E4-DaytimeRegions.md). Người dùng đã [nghe được chuông E3](../E3/AudioAcceptance.md).

- Mở rộng ProceduralMapPresentation bằng partial Daytime; root cấp clock. Seed314159, trần220chi tiết mới gồm12ambient. Crop một frame của bush strip8frame; atlas pixel64×64 dùng chung7sprite; không thêm gameplay Unit/Entity/collider hoặc dependency.
- Seed0/1/1543:194/184/190tổngdecor,179/168/176E4 mới. Cùng config cho cùng visual/ground hash; config khác đổi hình ảnh nhưng full map SHA256 không đổi và khớp Core generator v3,150resourceplacements. Fog/clearance/sorting/RNG/repeat-listener PASS.
- Ba seed native economy/deposit20wood+20gold và xây kho landmark PASS. Đây là simulation-time fixture Editor, không là manual playthrough/FPS.
- Lifecycle component-only destroy/menu→match/legacy PASS; atlas/sprites/views sạch, shared art/material còn, một fog listener, new match cùngvisualhash vàunknown site reset.
- 81/81 EditMode PASS,0fail/skip,6.4467015s. Ảnh cuối A/B ba vùng cùngseed/camera/clock có cropfix ở cảhai; debugfogoff ghi rõ. ẢnhunknownGoldfog-on, sample bướm/ripple0 và0.8 có ngữ cảnh riêng.
- Seed1 gốc đã khôi phục; Unity6000.3.11f1, ProceduralGame sạch, ngoài Play/pause/compile,0Exception. Own TMP font cache đã dọn. Không nâng Editor/pipeline hoặc sửa Core/DOTS runtime/clock/quota.
- Chưa build/đo Player E4 hoặc nghiệm thu thẩm mỹ bởi người dùng. G3/G5 vẫn mở; không làm E5–E10. Giữ mọi thay đổi E0–E3 đang có, không commit.
