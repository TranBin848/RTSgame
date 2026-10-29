# Bằng chứng thực thi DOTS Gameplay

Ngày kiểm chứng: **16/10/2026**. Unity **6000.3.11f1**, Windows x64 Release, managed backend Mono, Burst AOT. Kiểm chứng dùng bản sao trong `.utmp/dots-validation` để không đóng hoặc thay scene của Editor người dùng.

- [editmode-tests.xml](editmode-tests.xml): NUnit XML, 32/32 Passed; gồm 19 case gameplay và 13 case Swarm.
- [gameplay-result.json](gameplay-result.json): 18/18 check tích hợp SampleScene, `errors=[]`; registry ban đầu có 25 actor/view, JSON hiện lưu ghi cuối bài có 29 actor và 5 quái ECS. Số cuối bài là snapshot của lần chạy, không phải một hằng số population hoặc kết quả benchmark.

Runner `GameplaySmokeAutomation` chỉ được bật bằng `--gameplay-smoke`. Nó dùng prefab và command adapter thật, rút ngắn timer trong dữ liệu runtime để kiểm tra nhanh, teleport đối tượng kiểm thử tới điểm tiếp cận và tắt lịch ngày/đêm/wave tự động. Các thay đổi này không ghi lại vào prefab hoặc ScriptableObject của game.

Log build và Player được giữ tại `Logs/gameplay-build.log`, `Logs/gameplay-player.log` trong workspace. Dùng `Tools/Collect-GameplayEvidence.ps1` sau khi Player thoát để kiểm tra report và log rồi cập nhật hai artifact ở đây. Các log workspace không được dùng làm tài liệu công bố nguyên trạng.

Bằng chứng này kiểm tra chức năng và tích hợp, không đo FPS/VRAM hoặc chứng nhận tải hàng nghìn worker/Animator. Các phép đo Swarm trước đây nằm trong [module Swarm](../../DOTS/Evidence/README.md), với điều kiện đo riêng. Xem [giới hạn kiểm thử](../Validation.md) trước khi trích vào khóa luận.
