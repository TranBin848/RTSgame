# Baseline rerun — 05/11/2026

Môi trường: Unity **6000.3.11f1**, Windows. Dự án RTSgame2026 tại thời điểm chạy; không có thay đổi gameplay do lần kiểm tra này tạo ra. Đây là kiểm tra tính đúng, không đo FPS hoặc cân bằng.

| Bài chạy | Kết quả | Bằng chứng |
|---|---:|---|
| Unity EditMode, assembly `RTS.DOTS.Tests` | 34/34 pass, 0 fail, 0 skip; Test Runner báo 2,197 s | Kết quả công cụ Unity trong phiên; chưa xuất XML |
| Unity EditMode, assembly `RTS.ProceduralGeneration.Tests` | 3/3 pass, 0 fail, 0 skip; Test Runner báo 5,258 s | Kết quả công cụ Unity trong phiên; chưa xuất XML |
| Windows Player `--gameplay-smoke` trên build mới | 18/18 checks pass, `errors=[]`, 29 actors, 5 enemies, 272 ticks | [JSON gốc](gameplay-smoke-current-2026-11-05.json) |

Player build mới đặt ở `.utmp/swarm-build-s0-2026-11-05/SwarmDemo.exe`. `Editor.log` ghi `Build Finished, Result: Success.` Lời gọi tool build bị timeout vì Unity mất khoảng 501 giây trong lời gọi đồng bộ; kết luận build thành công dựa trên log, file mới và việc chạy Player thành công, không dựa trên response của tool build. SHA-256 của EXE: `5BFB02293C482170C4D5035081245E45D69FE0F617FCC92A81E98ADEE13B945F`.

Lệnh smoke:

```powershell
& '.utmp/swarm-build-s0-2026-11-05/SwarmDemo.exe' --gameplay-smoke --gameplay-output '.utmp/gameplay-smoke-current-2026-11-05'
```

Smoke dùng **SampleScene**, rút ngắn timer khai thác/huấn luyện/xây và tắt lịch ngày/đêm/wave tự nhiên trong phần lớn bài chạy. Nó xác nhận đường tích hợp ECS (bao gồm một check gọi API spawn wave), không thay thế kiểm tra chu kỳ ngày/đêm, UI click thật hoặc độ khó trên ProceduralGame. Các thao tác này do người dùng xác nhận thủ công trong S0 nhưng chưa có artifact độc lập.
