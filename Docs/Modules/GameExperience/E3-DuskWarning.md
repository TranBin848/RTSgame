# E3 — hoàng hôn và báo nguy hiểm

Ngày **23/11/2026**. **E3 DONE phạm vi implementation và kiểm Editor**. Bằng chứng kiểm Editor và capture tự nhiên ở [Evidence/E3](Evidence/E3/). Người dùng đã [xác nhận nghe được chuông](Evidence/E3/AudioAcceptance.md) và yêu cầu tiếp E4. Không đổi lịch Day60/Dusk30/Night45, luật wave, kinh tế, fog hoặc map. Chưa đo Player E3.

## Mục tiêu và phạm vi

Cho người chơi biết sắp hết thời gian chuẩn bị bằng chuông, chữ `! DUSK`, countdown và các dấu hướng lối tiếp cận **đã khám phá**. Chữ vẫn rõ khi MasterVolume bằng0. Sang Night, banner nhắc bảo vệ Town Hall. Day ẩn banner và marker. Hình ảnh thế giới tiếp tục dùng chuyển palette liên tục của [E1](E1-ArtDirection.md) và đèn tăng dần trong Dusk của [E2](E2-NightLighting.md); không tạo thêm clock hoặc một hệ thống ánh sáng trùng lặp.

## Dữ liệu và tích hợp

`GameSceneCompositionRoot` tạo một `DuskWarningPresentation` trên object clock HUD, truyền trực tiếp `DayNightCycleManager`, `EnemyWaveManager`, `ProceduralMapAuthoring`, `FogOfWarManager`, world camera và overlay Canvas. Consumer không tìm manager, không dùng singleton; chỉ ProceduralGame root tạo nên SampleScene giữ HUD cũ.

Nguồn dữ liệu:

| Nội dung | Nguồn và ý nghĩa |
|---|---|
| Pha, countdown | `CurrentPhase`, `CurrentPhaseProgress`, `Definition.DuskDuration` từ clock gameplay |
| Số wave đêm nay | `EnemyWaveManager.CurrentWaveCount`, theo ngày và definition hiện có |
| Vị trí gateway | `GeneratedMap.Layout.Gateways` cộng `OriginCell`, chuyển ô bằng FogTilemap |
| Được phép biết gateway | Fog đã initialize và `TryGetCellState` tại **chính ô gateway** trả Explored hoặc Visible |
| Góc/điểm marker | Chiếu vị trí gateway qua world camera sang overlay Canvas, kẹp vào vùng HUD |

Wave chọn gateway ngẫu nhiên lúc spawn; lịch hiện tại chưa chốt một hướng hoặc loại quái cho Dusk. Vì vậy marker ghi `POSSIBLE APPROACH`, banner ghi `Routes chosen on spawn`; không vẽ đường di chuyển, không hứa quái sẽ xuất hiện ở vị trí marker. Khi chưa biết gateway nào, chỉ hiện cảnh báo chung `Enemy approaches unknown - scout the border`. Khám phá một đoạn đường chưa đủ mở marker gateway; không dùng nhánh fog helper trả true trước initialization.

## Luồng xử lý và thuật toán

1. Initialize kiểm dependency và overlay Canvas. Gọi lại cùng context không nhân UI/audio/listener; context khác bị từ chối.
2. Đăng ký một callback `OnPhaseChanged`. So sánh pha trước để bỏ sự kiện Dusk trùng; chỉ một lần gọi `AudioSource.Play` khi thực sự vào Dusk. Không phát lại lúc UI enable hoặc resume.
3. Countdown = `Ceil(Max(0,(1-progress)*DuskDuration))`. Không cộng trừ bằng deltaTime riêng. Clock đứng khi pause hoặc kết thúc trận thì countdown cũng đứng.
4. LateUpdate cập nhật marker theo camera/fog hiện có, chỉ format lại text khi pha, số giây, số wave hoặc số gateway biết thay đổi. Tối đa một marker cho mỗi gateway; không có Entity/collider/command gameplay.
5. Khi destroy, bỏ listener và dọn banner, marker, AudioSource, AudioClip do controller tạo. Tất cả thuộc scene hiện tại; không DontDestroyOnLoad.

Chuông là một strike tổng hợp xác định dài2giây, mono22050Hz, ba sin440/880/1193Hz tắt dần bằng exponential envelope, attack8ms chống click. Tạo clip một lần mỗi scene; AudioSource2D, volume0.28, không loop/playOnAwake, tuân theo AudioListener MasterVolume. Không có audio asset phù hợp trong inventory hiện tại, không cài dependency. Hàm tạo clip, marker, E1 và E2 không tiêu thụ UnityEngine.Random.

## Cách chạy và tái kiểm

- Mở ProceduralGame và Play; mặc định Day60giây, tiếp Dusk30giây. Cửa sổ/đuốc tăng sáng theo E2; vào Dusk có một chuông và countdown30→1, sang Night đổi chữ.
- Pause bằng menu trong Dusk rồi resume: countdown giữ nguyên lúc pause và chuông không lặp. Tắt volume vẫn đọc được banner/marker.
- Scout tới ô gateway: chỉ gateway đã khám phá có dấu hướng, marker kẹp vào màn hình khi camera đi xa. Đây là vị trí lối tiếp cận có thể dùng, không phải vị trí spawn kế tiếp.
- Menu → Play lại: Day đầu trận, chưa báo Dusk, không còn marker/clip/listener của trận trước.
- Fixtures chạy bằng Unity MCP `script-execute`, full class/method. `Tools/ValidateGameExperienceE3.cs:Run` yêu cầu trận mới Ready và Editor-paused; dùng pha/progress/fog dựng để kiểm branches, thoát Play sau đó. Các method ReturnToMenu → step → MenuAndReload → step → CheckReload kiểm teardown/reset.
- `Tools/CaptureGameExperienceE3.cs:StartNatural` yêu cầu trận mới Ready, Day progress<1%, Editor-paused. Callback Editor có giới hạn125s, chạy timeScale1 và clock thật, pause menu2giây trong Dusk rồi resume; lưu bốn PNG GameView và log, tự dừng Editor-paused lúc Night bắt đầu. Không set phase/progress/timing trong capture.
- Chạy `tests-run` EditMode ngoài Play khi scene đã lưu để kiểm regression.

## Kiểm thử và bằng chứng

[Integration fixture](Evidence/E3/integration-result.txt): PASS một listener/repeat-init; một signal mỗi Dusk/duplicate event/disable-enable; text tắt tiếng và dữ liệu chuông hữu hạn/không clip/không im lặng; pause/resume; countdown tại các mốc30/23/15/1; gateway chưa khám phá ẩn, một gateway khám phá có một marker, reset fog ẩn lại; chu kỳ kế tiếp báo được. Map terrain/navigation/wallet/actor/tick và gameplay RNG giữ nguyên trong fixture không tick simulation.

[Lifecycle fixture](Evidence/E3/lifecycle-result.txt): PASS destroy bỏ listener, menu dọn hết ID sở hữu (controller, AudioSource, clip, banner, marker), trận mới chỉ một controller/một listener, counter0 và Day; vào Dusk counter1.

[Legacy](Evidence/E3/legacy-result.txt): SampleScene không có controller E3 và giữ palette legacy. [Trạng thái Editor cuối](Evidence/E3/final-editor-state.txt): ProceduralGame đã lưu sạch, ngoài Play/pause/compile, không Exception; root chưa initialize ngoài Play là trạng thái dự kiến.

[Regression](Evidence/E3/editmode-result.json): **81/81 EditMode PASS**,0fail/skip,6.5659028giây. Sau regression chỉ chỉnh vị trí banner từ mép trên68→140đơn vị Canvas để tránh thanh wave; capture được chạy lại trên bố cục cuối.

[Capture tự nhiên](Evidence/E3/natural-capture.txt): clock60/30/45 không sửa, fog bật; một lần menu Pause thật2giây giữ progress/countdown/cue, Resume không phát lại. Day trước Dusk → Dusk entry (30giây,1signal) → giữa Dusk (15giây, đèn~0.5) → Night entry (0giây,1signal, đèn1). Dừng ở đầu Night; không claim đã capture trọn45giây Night.

| Bối cảnh | Ảnh GameView1280×720 |
|---|---|
| Cuối Day tự nhiên | [Day](Evidence/E3/natural-Day-before-Dusk.png) |
| Vào Dusk tự nhiên | [Dusk entry](Evidence/E3/natural-Dusk-entry.png) |
| Giữa Dusk tự nhiên | [Dusk mid](Evidence/E3/natural-Dusk-mid.png) |
| Vào Night tự nhiên | [Night entry](Evidence/E3/natural-Night-entry.png) |
| Một gateway đã biết, fog memory dựng bằng fixture | [Marker fixture](Evidence/E3/fixture-one-known-gateway.png), [ngữ cảnh](Evidence/E3/known-marker-fixture.txt) |

Ảnh marker dùng fog memory dựng, không làm bằng chứng khám phá bằng thao tác người chơi. Ảnh cuối đã kiểm banner tránh thanh wave hiện có, countdown/chữ đủ đọc ở GameView1280×720, một marker đúng hướng gateway và gateway khác vẫn ẩn.

## Hiệu năng và giới hạn

Chưa đo Player/performance mới cho E3; kết quả E2 không phải phép đo E3. Việc cập nhật marker là O(số gateway), text chỉ đổi tối đa mỗi giây trong Dusk hoặc khi state thay đổi; clip sinh một lần với44100float (~176KB mẫu managed trước khi clip copy). Đây là mô tả implementation, không là số đo GC/frame hoặc FPS. G5 tiếp tục mở.

Người dùng đã xác nhận nghe được chuông; fixture xác minh mẫu âm thanh và số lần yêu cầu phát. Marker chỉ chỉ hướng gateway đã biết, chưa phải dự báo path/từng wave; không dùng dữ liệu ẩn để cho thêm lợi thế. HUD hiện dùng tiếng Anh theo UI hiện có, không thêm hệ thống localization. Chưa đo/QA mọi tỉ lệ màn hình hoặc mobile. E4–E10, G3 và G5 không được tự đóng trong E3.

## Nguồn đối chiếu

- [Build plan E3](BuildPlan.md), [E1](E1-ArtDirection.md), [E2](E2-NightLighting.md).
- [Manual DI](../DependencyInjection/Architecture.md), [DOTS development](../DOTS/DevelopmentGuide.md).
- Source: `DuskWarningPresentation`, `GameSceneCompositionRoot`, `DayNightCycleManager`, `EnemyWaveManager`, `ProceduralMapAuthoring`, `FogOfWarManager` trong Assets/Game. Giữ gameplay authority và nguồn clock/wave/fog hiện có.
