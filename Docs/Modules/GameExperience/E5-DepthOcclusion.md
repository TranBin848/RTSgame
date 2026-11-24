# E5 — chiều sâu, bóng tiếp xúc và che khuất

Ngày **24/11/2026 — DONE phạm vi implementation và kiểm Editor seed1**. Bám [E5 build plan](BuildPlan.md), tiếp E6. Chưa build/đo Player E5 hoặc nghiệm thu thẩm mỹ bởi người dùng; G3/G5 vẫn mở.

## Mục tiêu và phạm vi

Quân, cây, tài nguyên và công trình che nhau theo chân; mái/tán mờ khi che quân đang chọn, giữ thân/tường dưới nguyên. Thêm bóng tiếp xúc, bóng dài hơn giữa Dusk và vòng chọn ở chân. Giữ collider, sprite pivot, transform gameplay, giá/tech, AI/pathfinding, dữ liệu map và clock60/30/45.

E5 chỉ được tạo bởi GameSceneCompositionRoot của ProceduralGame. SampleScene và Swarm demo giữ chế độ renderer legacy khi không được cấp material E5. Old Watchpost S7 cũng được tính như cảnh vật cao; chi tiết nhỏ E4 giữ sorting11–13.

## Dữ liệu, dependency và ownership

WorldDepthPresentation nhận game, clock, fog, camera, EnvironmentPresentationDefinition, SwarmInstancedRenderer, GeneratedResourceRoot và danh sách decor từ root. ProceduralMapAuthoring expose GeneratedResourceRoot read-only; consumer không Find/Instance hoặc tra service toàn cục. Root tài nguyên này cần thiết vì cây/cừu procedural không nằm trong ba container legacy của GameManager.

EnvironmentPresentationDefinition giữ hai material asset FootDepth và ContactShadow; ProceduralMapEditorTools.ConfigureDepthMaterials tạo/wire asset khi thiếu, scene generator gọi lại cùng đường cấu hình. Shader được giữ bằng reference asset cho Player. Không sửa YAML scene hoặc material prefab.

Controller sở hữu ba material runtime (body/contact/ring), hai mesh/view batch và một texture explored visibility. Native renderer sở hữu material/mesh/view contact riêng. Trước override, mỗi body lưu material, sortingOrder và MaterialPropertyBlock gốc; unregister/pool return/teardown khôi phục chúng. Không có material mới mỗi frame; probe20refresh xác nhận số Material không tăng.

Registry đọc friendly/enemy views, structures và resource/unit trong generated root mỗi0.25giây; sử dụng list/hash set dùng lại. Cập nhật shader/mesh theo snapshot presentation mỗi frame; không gửi lệnh gameplay. RTS.DOTS vẫn độc lập Unit/GameManager: API runtime mới chỉ nhận Material và ContactStretch, còn nhận diện Unit/Tree/Structure nằm ở Integration.

## Đối chiếu sorting và thuật toán

SpriteRenderer trước E5 dùng order25 cố định; native renderer đã dùng Z gần bằng -1 + actorY*0.001 và ghi depth. Chỉ đổi sortingOrder hoặc renderQueue không đủ thống nhất hai đường. Renderer2D còn xét sorting layer/order trước queue: DrawMeshInstanced contact với order mặc định0 bị terrain10 vẽ đè.

Giải pháp cuối:

| Lớp | Cách vẽ |
|---|---|
| Terrain/decor nhỏ | Giữ đường S7/E4; terrain10, chi tiết11–13 |
| Contact shadows | Mesh batch sortingOrder20, ZWriteOff, ZTestLEqual, alpha ellipse mềm |
| Bodies/occluders | FootDepth, order25, ZWriteOn, alpha clip; vertex Z theo chân |
| Native actors | Giữ instancing sprite; Z theo chân, point/snap bật riêng khi E5 active |
| Glow E2 | Giữ mask visibility, queue+4 và Z gần hơn mặt nhà một epsilon theo cùng chân |
| Vòng chọn | Mesh batch order100, ZTestAlways, mask visibility từng pixel |

Mesh batch chỉ có hai view cho controller và một view native, không tạo GameObject/Light2D cho từng quái. Trần512contact MonoBehaviour và1023contact native trong camera; vòng chọn tối đa1023. Đây là budget implementation, chưa phải khả năng FPS đã đo.

### Điểm neo chân

Đọc offset và nửa chiều cao collider hiện có, chuyển sang world bằng TransformPoint; không đổi collider hoặc pivot. Dùng mép dưới capsule/box/circle làm điểm neo render cho SpriteRenderer. Cảnh vật không collider như Old Watchpost dùng origin authored.

Native Visual có FootOffset mặc định -0.5 trước scale, dành cho canvas Tiny Swords192px/64PPU hiện tại; đây là metadata hình ảnh, có thể author theo visual mới. XY của sprite và dữ liệu CombatBody không đổi; projectile giữ đường depth cũ. Metadata này chỉ tham gia Z/contact khi E5 active.

Cả hai đường dùng Z = -1 + footY*0.001. Frame/pivot sprite của dân không bị dịch để sửa thứ tự; vòng chọn được đặt ở chân thay vì tâm canvas.

### Che khuất

Chỉ xét Unit đang được chọn, còn selectable, sprite đang hiện và trong current vision. Vùng thân/đầu quân dựa collider với padding nhỏ; thu bớt vùng bounds ngang occluder để giảm tác động của canvas trống. Điều kiện còn yêu cầu chân quân nằm phía sau chân occluder và vùng thân/đầu giao phần trên.

FootDepth nhận _DepthFootY, _DepthRoofY, _DepthFade và _DepthMobile qua property block. Chỉ phần trên ngưỡng max(foot+0.2, boundsMinY+0.43*height), blend trong0.15world, mới chuyển sang coverage40%. Thân dưới/tường/trunk vẫn nguyên.

Dùng stipple Bayer4×4, không alpha toàn bộ sprite: pixel bị loại không ghi depth, nên native actor phía sau hiện qua đúng lỗ depth; pixel còn lại tiếp tục che. Đây là mờ có hạt theo pixel art, không phải kính alpha mượt. Không dùng RNG hoặc thay animation để suy ra gameplay.

### Fog

Texture scene-local128×128 lấy từ fog: Visible255, Explored115, Unexplored0, filterPoint. Static body giữ explored memory, mobile chỉ hiện tại current vision; alpha cuối không vượt alpha fog có sẵn. Shader clip từng pixel, không chỉ kiểm ô tâm.

Fader chỉ bật khi occluder và quân đang chọn đang thấy được. Probe zero-current-visibility giữ selection cũ nhưng cho0contact/0ring/0occluder mờ. Contact/ring native và Mono đều dùng texture visibility hiện có của Swarm renderer để không vẽ ngoài vision. Trước fog initialized thì không xuất effect.

### Bóng, material và zoom

Bóng là ellipse mềm thuần hình ảnh, không phải ray-traced/caster shadow. Dusk stretch = 1 + 0.8*sin(pi*progress); giữa Dusk1.8, hai biên về1; Day/Night là contact ngắn1. Không thêm timer pha; pause giữ geometry.

FootDepth và native E5 dùng point-clamp sampler cùng pixel snap của render anchor trong clip space. Không sửa TextureImporter shared hoặc vị trí camera/actor. Glow và contact mềm giữ tính chất gradient riêng. Pixel snap áp dụng cho thân/occluder, không tuyên bố pixel-perfect toàn bộ terrain/UI hoặc mọi tỉ lệ màn hình.

Unit highlight legacy vẫn được gọi bởi selection; E5 áp lại material chung và dùng vòng chọn riêng làm dấu chọn dễ đọc. Không tạo material cho từng lần refresh/selected body.

## Tương thích E2 và lifecycle

NightGlow lấy _NightFootY từ cùng chân collider, đặt mặt phát sáng gần hơn body một epsilon và giữ per-pixel mask. Vì vậy facade còn glow sau khi bodies ghi depth; vật phía trước vẫn có thể che glow. Không thay cường độ, budget8Light2D hoặc flicker của E2.

OnDestroy bỏ fog listener, phục hồi material/order/block gốc cho renderer còn sống, dọn mesh/view/texture/material runtime và tắt native depth/contact mode. Native setter null cũng dọn contact mesh/view của nó; shared assets không bị Destroy.

## Cách chạy và tái kiểm

- Mở ProceduralGame, Play. Chọn dân/quân đi qua trước và sau cây/nhà: thứ tự theo chân, vòng chọn ở mặt đất; phần trên che quân mờ40%coverage, phần dưới còn nguyên.
- Quan sát Dusk: bóng tiếp xúc dài hơn, về ngắn ở Night. Pause bằng menu không làm contact geometry chuyển động.
- Tools/ValidateGameExperienceE5.cs: Run trên fresh Ready, Editor-paused match; GpuProbe tạo fixture layer31 và dọn nó sau đo.
- Tools/CaptureGameExperienceE5.cs: Setup → step → Routes. Fixture bind thêm một cây, điều khiển worker bằng MoveActor và tick ECS0.05. Phases/progress được set để lấy mẫu hình ảnh; **không phải chuyển pha tự nhiên**. Điểm Hall đặt ngoài footprint đi bộ; arrival tolerance0.8world được ghi trong log.
- DenseGroup spawn96native đứng yên, zero damage, có kiểm vị trí hợp lệ; Zooms chụp ortho4/6/8. Đây là fixture tải hình ảnh, không là benchmark.
- SetupPool → step → PoolReturn → step → CheckPool dùng RuntimeObjectPool.Spawn và bridge.Register, kiểm đúng cùng instance return/reuse; không dùng SpawnGameplay (factory này Instantiate).
- Tools/ValidateGameExperienceE5Lifecycle.cs: FogAndPause → step → CheckPause; DestroyComponent → step → CheckDestroyAndMenu → step → MenuAndReload → step → CheckNewMatch. ActiveUnload → step → CheckActiveUnload → step → CheckNewMatch kiểm unload khi E5 còn hoạt động. CheckLegacy ở SampleScene.
- Tools/ValidateGameExperienceE5Glow.cs: E5GlowSceneProbe.Run kiểm pixel glow facade đang sáng, so bình thường/đẩy depth gần/tắt core; restore camera, PP và block sau probe. Dùng Night midpoint, fog-off và refresh lighting trước khi gọi.
- Thoát Play, mở ProceduralGame sạch rồi tests-run EditMode. Không cần nâng Editor/package/pipeline.

## Kết quả đã kiểm

[81/81 EditMode PASS](Evidence/E5/editmode-result.json),0fail/skip,10.2861536giây. [Integration](Evidence/E5/integration.txt):158bodies baseline, native mode bật, repeat init/one fog listener,20refresh không tăng số Material; pose/scale, wallet/actor/tick/navigation và UnityEngine.Random giữ nguyên.

[Fingerprint source/config cuối](Evidence/E5/source-fingerprint.json) và [trạng thái Editor](Evidence/E5/final-rql-state.txt): ProceduralGame sạch, seed1, ngoài Play/pause/compile; root LastError null trong EditMode. Root chưa initialize ngoài Play là trạng thái dự kiến. Không dùng lịch sử lỗi của các vòng fixture trước làm kết quả cuối.

[GPU192×192](Evidence/E5/gpu-probe.txt): native-shader quad sau/trước SpriteRenderer được depth-test đúng; fade upper có76red/100green sample, lower body vẫn opaque. Static visible/explored/unexplored và mobile explored-only rejection PASS; contact fog mask và vòng chọn analytic PASS. Probe dùng real shader với quad MeshRenderer, không phải native gameplay workload.

Texture nguồn bilinear dạng checker vẫn cho pixel sắc nét qua point shader tại ortho2/3/4; camera pan0.001world cho ảnh giống hệt sau anchor snap. Đây là phép thử nhỏ có điều kiện, không chứng nhận mọi zoom/resolution/GPU.

[Route context](Evidence/E5/route-context.txt): cùng bốn đích đi qua cây/Hall ở Day/Dusk/Night, Move native thật. Cả12ảnh kiểm namedTargetFade=1 khi sau,0 khi trước; ring1. Contacts42, stretch1/1.8/1; localLights0/1/1. Các số travel là simulation time fixture, không là hiệu năng hoặc manual playthrough.

| Pha | Cây phía sau / phía trước | Hall phía sau / phía trước |
|---|---|---|
|Day|[Sau cây](Evidence/E5/route-Day-Tree-behind.png) / [Trước cây](Evidence/E5/route-Day-Tree-front.png)|[Sau Hall](Evidence/E5/route-Day-Hall-behind.png) / [Trước Hall](Evidence/E5/route-Day-Hall-front.png)|
|Dusk|[Sau cây](Evidence/E5/route-Dusk-Tree-behind.png) / [Trước cây](Evidence/E5/route-Dusk-Tree-front.png)|[Sau Hall](Evidence/E5/route-Dusk-Hall-behind.png) / [Trước Hall](Evidence/E5/route-Dusk-Hall-front.png)|
|Night|[Sau cây](Evidence/E5/route-Night-Tree-behind.png) / [Trước cây](Evidence/E5/route-Night-Tree-front.png)|[Sau Hall](Evidence/E5/route-Night-Hall-behind.png) / [Trước Hall](Evidence/E5/route-Night-Hall-front.png)|

Ảnh camera1920×1080, ortho6, debug fog-off để đối chiếu hình. Ảnh cuối đã xem trực tiếp; không dùng như bằng chứng người chơi đã khám phá map.

[Nhóm đông](Evidence/E5/dense-group.txt):96enemy fixture cộng2native đang có,98rendered/contact native,4ring và42contact Mono; [ảnh Night](Evidence/E5/dense-native-group-Night-fog-off.png). [Zoom thực](Evidence/E5/zoom.txt):[4](Evidence/E5/zoom-4-Night.png),[6](Evidence/E5/zoom-6-Night.png),[8](Evidence/E5/zoom-8-Night.png).

[Pool](Evidence/E5/pool.txt): return phục hồi material/unbind, cùng worker view reuse/bind lại, không giữ roof fade. [Lifecycle](Evidence/E5/lifecycle.txt): fog probe, pause thực + step, component-only teardown, direct active unload, menu/new match và SampleScene legacy PASS. Mọi ID E5 được ghi đều bị dọn; assets dùng chung còn sống.

[S7 compatibility](Evidence/E5/s7-compatibility.txt) và [E4 regression](Evidence/E5/E4-regression/seed-1-integration.txt) PASS; full map SHA256 vẫn **26102ba0e108e16be640d2d6532189a47a30cf834ca74e04fcd5e160e9d71f5e**, generator v3,150placements. E4 signature chỉ normalize order Old Watchpost về10 để so placement/decor trước E5; render order của landmark trong game vẫn25 theo depth.

[E2 GPU mask/tint với foot block](Evidence/E5/E2-regression.txt) PASS. [Probe facade thật, tắt postprocessing](Evidence/E5/E2-scene-glow-probe.txt): pixel normal=near=(1,1,0.5176471), tắt core=(0.745098054,0.819607854,0.5647059); glow ấm còn được vẽ, không bị body depth chặn. Lấy mẫu HDR ra RGB24 có clamp, không là đo luminance thiết bị.

## Hiệu năng, giới hạn và hướng tiếp

Chưa build/đo Player E5, chưa full G5 matrix hoặc mọi seed/loại quân. Ba mesh batch và budget512/1023 là trần dữ liệu, không khẳng định FPS hoặc GC/frame. Mesh được upload khi refresh; texture fog được cập nhật theo fog event, shader body/property block được cập nhật theo registry. Cần đo lại Player trong E10/G5.

Fade dựa bounds/collider với padding, không phải hit-test alpha chính xác; có thể bật hơi sớm ở asset có nhiều canvas trống. Coverage40% dạng stipple có thể cần người dùng nghiệm thu thẩm mỹ. FootOffset native cần author lại cho art có canvas/pivot khác; không tự đổi collider để khớp hình. Chưa chứng nhận mọi độ phân giải lẻ, camera rotation hoặc platform ngoài D3D11 đã thử.

Không sửa AI/combat/economy, không nâng Unity/UPM/render pipeline và không thay scene/prefab gameplay. Unity MCP tự đổi scripting define UNITY_MCP_DEPS_47→50 trong ProjectSettings khi resolve dependencies; đây là cập nhật tự động của plugin, không là đổi luật game. Không tự đóng G3/G5; tiếp E6 theo plan.

## Nguồn đối chiếu

- [BuildPlan E5](BuildPlan.md), [E2](E2-NightLighting.md), [E4](E4-DaytimeRegions.md).
- [DOTS Architecture](../DOTS/Architecture.md), [DevelopmentGuide](../DOTS/DevelopmentGuide.md), [Manual DI](../DependencyInjection/Architecture.md).
- Source: WorldDepthPresentation, FootDepth/ContactShadow/NightGlow shaders, StructureNightLighting, GameSceneCompositionRoot, ProceduralMapAuthoring; SwarmInstancedRenderer, SwarmVisualLibrary và SwarmSprite.shader trong RTS.DOTS.
- Đối chiếu sampler/SpriteRenderer instancing bằng Core2D.hlsl của URP cài trong Library/PackageCache; thử trực tiếp bằng GPU probes, không dựa vào giả định chỉ đổi renderQueue.
