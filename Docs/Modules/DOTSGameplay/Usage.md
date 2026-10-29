# Chạy và kiểm tra gameplay ECS

## Chạy game

Giữ Unity Editor và render pipeline hiện tại. Đợi Unity hoàn tất compile, mở `Assets/Scenes/SampleScene.unity` và Play. Bridge được cài tự động; không cần thay hàng loạt prefab trong Inspector.

Chọn dân/quân/công trình bằng UI cũ. Di chuyển, giao cây/mỏ/cừu, xây công trình và xếp hàng huấn luyện như trước. Trong Entities Hierarchy có thể xem `GameplayActor`, `GameplayWorker`, `GameplayConstruction`, `TrainingRequest`, `PlayerWallet`, `SwarmAgent` và `SwarmProjectile`.

Một GameObject Worker/Warrior/Sheep còn trong Hierarchy là presentation hợp lệ. Kiểm tra `Unit.DotsEntity` khác `Entity.Null` và `AIPawn.enabled=false`; không dùng số GameObject còn lại để kết luận chúng đang chạy AI cũ.

## Tích hợp spawner hoặc tính năng mới

Trong scene gameplay đã cấu hình, dùng:

```csharp
SwarmGameBridge.Instance.SpawnEnemy(enemyPrefab, position); // Entity quái, không tạo view từng con
SwarmGameBridge.Instance.SpawnGameplay(unitPrefab, position, Quaternion.identity); // Entity + view
```

`SpawnGameplay` trả view với dân/quân/công trình/tài nguyên và trả null với enemy (quái được render bằng instancing). Không dựa vào GameObject trả về để giữ mục tiêu quái: dùng Entity và API simulation. Direct `Instantiate` của `EnemyUnit` được chuyển đổi khi đăng ký lifecycle, nhưng API sinh trực tiếp ECS tránh tạo GameObject tạm.

Gửi command qua `Unit.MoveTo`, `SetDotsTarget`, `WorkerUnit.TryAssignResourceNode`, `SendToBuild`, `ReturnToTownHall`, `LeaveTownHallAtDaybreak` hoặc API `GameplaySimulationSystem`. Không ghi `transform.position`, `CurrentState`, máu hay timer mỗi frame như logic gameplay; ECS sẽ ghi đè snapshot. Nếu cần teleport, thêm command cập nhật cả `CombatBody.Position` và `PreviousPosition` như một thao tác mô phỏng có chủ đích.

Prefab loại mới cần ánh xạ vai trò/cấu hình trong `RegisterGameplayUnit`. Giữ các giá trị state/task giữa runtime và presentation tương ứng hoặc cập nhật adapter rõ ràng.

## Kiểm thử

Chạy EditMode trong Unity Test Runner, assembly `RTS.DOTS.Tests`. `GameplaySimulationTests` kiểm tra vòng gameplay; `SwarmSimulationTests` kiểm tra quái/đạn và hồi quy tải.

Để chạy kiểm tra tích hợp bằng Player:

```powershell
& '.utmp/swarm-build/SwarmDemo.exe' --gameplay-smoke --gameplay-output '.utmp/gameplay-smoke'
```

Build bằng menu `RTS/DOTS/Build swarm demo (Windows)`. Runner sẽ mở SampleScene, gửi lệnh qua API thật, kiểm tra Entity/view và ghi `gameplay-result.json`. Cờ này chỉ bật ở bài kiểm thử; runner không chạy trong phiên game thường.

Runner rút ngắn timer khai thác/huấn luyện/xây **trong dữ liệu runtime của lần chạy kiểm thử**, teleport các đối tượng kiểm thử tới vị trí tiếp cận và tắt lịch ngày/đêm/wave để giảm tính ngẫu nhiên. Nó kiểm tra wiring và vòng xử lý, không đo cân bằng game hay FPS thực tế. Kiểm thử ngày/đêm tự nhiên và UI bằng thao tác chuột vẫn cần checklist trong [Validation](Validation.md).

Sau khi Player thoát, thu bằng chứng từ thư mục gốc repo:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Collect-GameplayEvidence.ps1
```

Tham số execution policy chỉ áp dụng cho tiến trình PowerShell này; không đổi thiết lập máy. Script kiểm tra kết quả và log rồi chép XML/JSON sang `Docs/Modules/DOTSGameplay/Evidence`.

## Đo hiệu năng

Đo trong Windows Player, ghi rõ phần cứng, độ phân giải, trạng thái rendering, số từng loại actor và node, số yêu cầu path/tick, số projectile, warmup và thời gian capture. Dùng Unity Profiler/Burst Inspector để phân biệt simulation với presentation. Giữ cùng seed và scenario khi so sánh.

Benchmark Swarm cũ đo nhóm quái/đạn riêng. Muốn đánh giá nền kinh tế RTS phải thêm kịch bản nhiều worker cùng khai thác, nộp kho, xây và tìm đường riêng; không suy diễn số FPS từ test correctness hoặc từ benchmark quái.
