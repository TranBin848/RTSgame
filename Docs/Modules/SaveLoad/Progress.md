# Tiến độ Save/Load

Cập nhật **24/11/2026 — SL4**. Theo [BuildPlan](BuildPlan.md). Đã có capture và bootstrap/hydrate hai lượt cho lát cắt SL4; **chưa có restore toàn trận hoặc Continue UI**.

| Mốc | Trạng thái | Bằng chứng / phần còn thiếu |
|---|---|---|
| SL0 | DONE về inventory và hợp đồng source | [Coverage owner/field/RNG/fence/rollback](Inventory.md), [Architecture](Architecture.md). Capture thực tế chưa được chứng nhận |
| SL1 | DONE phạm vi implementation/Editor | SaveIdentity/seed, DTO/validator/codec; catalog authoring21prefab/8action/6tech, resolver whitelist, fingerprint file/config thực có Resources dependency. [Báo cáo SL3](SL3-Report.md) |
| SL2 | DONE trong phạm vi Core/storage Editor | Hai slot, generation/temp/backup/checksum, khóa writer, fault injection từng ranh giới commit, file thật. Owner mutation capture/load/newgame xuyên scene còn SL3/SL4/SL7 |
| SL3 | DONE phạm vi implementation/Editor | Request cuối frame, fence simulation/managed/input, flush damage/completion đúng1lần, DTO detached từ ECS+owners+registry. Fixture cargo/shelter/build/FIFO/research/native quái/đạn, repeat/error, seed0/1/1543 và running capture PASS. [Evidence/giới hạn](SL3-Report.md) |
| SL4 | DONE phạm vi lát cắt kỹ thuật | MainMenu→LoadGame trước Awake, terrain snapshot, shells/view và remap2lượt; không grant100/nhân resource; pause/camera giữ tới Resume. Editor canonical PASS, Windows process mới0/1/1543 PASS. [SL4 report](SL4-Report.md) |
| SL5–SL6 | TODO | SL4 preflight từ chối active cargo/work/FIFO/research/projectile/dead actors và ngoài Day1. Tiếp khôi phục gameplay sâu, clock/wave/fog/report, caches/rollback |
| SL7 | TODO | Menu Save/Load/Continue, thông báo backup/errors, dawn autosave và quit/lifecycle |
| SL8–SL9 | TODO | Ma trận toàn trận, measurements, Windows Release cold-process và nghiệm thu người chơi |

Checkpoint tiếp theo là **SL5: worker/cargo → construction/FIFO → research → shelter/resume → combat/projectile**. Giữ SL6–SL9 mở; chưa dựng UI Continue trước khi restore và rollback đạt gate. Cold-process smoke đã kiểm đúng lát cắt SL4, không chứng nhận save/load toàn trận hoặc nghiệm thu SL9.

Các file đang thay đổi từ E7–E10 trước phiên này được giữ nguyên; không commit hoặc clean workspace. Không nâng Editor/package/render pipeline.
