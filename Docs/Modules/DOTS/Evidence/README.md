# Bằng chứng kiểm chứng DOTS

Các file được sinh từ Player Windows của đợt Swarm trước khi mở rộng toàn bộ gameplay. Thời gian trong tên/header capture là UTC; lần kiểm chứng diễn ra ngày 15/10/2026 theo múi giờ Asia/Saigon. Bằng chứng ngày 16/10 cho gameplay ECS nằm tại [DOTS Gameplay](../../DOTSGameplay/Evidence/README.md).

| Bài chạy | Số liệu gốc | Tổng hợp | Ảnh |
|---|---|---|---|
| Swarm 5.000 | [CSV](swarm-5000.csv) | [JSON](swarm-5000.json) | [PNG](swarm-5000.png) |
| Swarm 10.000 | [CSV](swarm-10000.csv) | [JSON](swarm-10000.json) | [PNG](swarm-10000.png) |
| Tích hợp RTS | [CSV](rts-integration.csv) | [JSON](rts-integration.json) | [PNG](rts-integration.png) |
| EditMode tests | [NUnit XML: 12/12 pass](editmode-tests.xml) | — | — |

Đọc điều kiện và giới hạn trong [Validation.md](../Validation.md) trước khi trích dẫn. Mỗi bài hiện chỉ là một capture offscreen 10 giây sau warmup 5 giây. Không phải kết quả trung bình của ba lần chạy 60 giây, không phải benchmark FPS interactive và không có baseline MonoBehaviour tương đương.

`Tools/Collect-DotsEvidence.ps1` tạo JSON tổng hợp từ CSV và kiểm tra `result.json`/log Player. Ví dụ chạy ở thư mục gốc repo:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Collect-DotsEvidence.ps1 -CaptureDirectory .utmp/verified-swarm-5000 -Name swarm-5000 -PlayerLogPath Logs/dots-verified-5000.log
```

Tool sao chép CSV/PNG và tạo JSON vào thư mục này; có thể chỉ định `-OutputDirectory` khác cho thí nghiệm mới. Percentile lấy mẫu đã sắp xếp tại chỉ số `floor(N*p)`, giới hạn tối đa `N-1`. Thời gian tick được thống kê sau khi loại `ecs_tick_id` lặp. `unity_allocated_bytes` giữ giá trị lấy mẫu 1 Hz; `gc_bytes=-1` là dữ liệu không khả dụng.
