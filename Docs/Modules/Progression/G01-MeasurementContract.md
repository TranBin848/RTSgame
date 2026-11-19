# G0–G1 — hợp đồng đo, 18/11/2026

Người dùng đã duyệt triển khai G0–G1. Chỉ đo baseline và sửa phương pháp policy; G2–G7 chưa được thực hiện. Raw S8 giữ nguyên. Nguồn: [kế hoạch](NextPhasePlan.md), `Tools/PlaytestS8.cs`, `Tools/PlaytestG01.cs`, source wave/clock/native gameplay trong project.

## Mục tiêu và phạm vi

Khóa map `0,1,1543`, wave RNG `8001`, dt `0.05`, goal5đêm. Không grant tiền, sửa HP, giá, multiplier, phase hoặc wave. Matrix cuối12run C/E/M/X dùng **schemaG01-v3**. Hai pilotv1, hai runv2 đã xong và một runv2 lỗi diagnostic được giữ riêng, không gộp vào matrixv3. V2 dừng khi gặp resource không có CombatBody; v3 đọc fallback GameplayResource và cố định vai trò worker. Không đo FPS bằng harness. Ngân sách G2 vẫn tối đa2candidate và chưa mở.

G0 đạt khi metadata/config resolve và ledger nhất quán. G1 đạt khi cả3seed có quân chủ động phát attack sequence, không spam move, worker bị kẹt được ghi lý do và policy thích nghi; không dùng kết quả này tuyên bố cân bằng đã đạt. Giữ tiêu chí đích G2/G3 trong mục3 của plan; không điều chỉnh ngưỡng theo kết quả baseline.

## Dữ liệu và luồng

Mỗi run dùng scene mới, composition Ready, paused Play, ví100W/100G/100M, ngày1. Snapshot `authoring.json` giữ path/serialized fields và nguồn override HP; `fingerprint-v3.json` ghi SHA256 source/asset/prefab/scene/packages/harness, `config-id.txt` là SHA256 fingerprintv3. Giữ `fingerprint.json`/`fingerprint-v2.json` cho pilot; bản sourcev2/v3 được lưu cạnh raw. Run ID là UTC timestamp+policy+mapseed. Timestamp tên file UTC; ngày tài liệu theo Asia/Saigon.

Clock/wave Tick → Gameplay.Update → Swarm.Update → bridge SynchronizeGameplay, nav refresh khi version đổi; World.Time và Unity Random state được phục hồi trong finally. Wave có state RNG riêng quanh Tick; map seed không quyết định wave seed. Đây là cùng luật native, không phải replay realtime tuyệt đối: frame callbacks/Animator/UI không chạy đủ như người chơi.

Ledger thứ tự **W/G/M**: `100 + deposit - researchSpend - trainSpend - buildSpend = final`, assert từng loại và đối chiếu tổngspend. `GameManager.AddResources` nhận **G/W/M**; native `Gameplay.AddResources` nhận **W/G/M**. Deposit đo delta ví trong native tick không có command chi; gather là tổng delta cargo dương cùng loại, không tính lại hàng khi đổi nhãn. Gather không phải event accounting đầy đủ; mất cargo do chết, cargo cuối và thay đổi nhiệm vụ đọc từ worker snapshot.

## Policy matrixv3 (hoàn thiện từ pilotv1/v2)

- C: không lệnh; giữ AI và quân/công trình author sẵn. Peak vẫn đo mỗi tick; expansion ghi not attempted.
- E: Tools→Cargo→Archery→Training→Outposts→Fortify, tối đa6worker/8warrior/6archer.
- M: Archery→Training→Tools→Cargo→Outposts→Fortify, tối đa4worker/8warrior/6archer.
- X: cùng thứ tựM, giữ100W/100G cho kho landmark; thử từ30s, trước đó chỉ chi khi vượt ngưỡng dự trữ. Sau đặt kho dùng quy tắcM. Đây là policy thử logistics, chưa là tối ưu mở rộng.
- E/M/X: một lệnh `CodeTriggered` cho mỗi Entity quân mới tới hai điểm quanh hall (±2,-2); acquisition vẫn bật. Không gửi move lặp5s. Ba worker đầu được gán thứ tự tên ordinal, worker mới nối theo lượt xuất hiện; role wood,wood,gold,meat lặp lại và giữ cho cả đời Entity. Nếu15s không đổi vị trí (>0.2), cargo hoặc attack sequence khi Gathering và cargo0, ghi node/path/distance rồi CancelWork và thử node khác. Đây là phản ứng policy, không tăng range hoặc teleport để che vị trí không tiếp cận được.

Policy biết tọa độ resource/landmark ngoài fog; không đại diện kỹ năng khám phá người chơi. Không dùng hai policy khác số dân/quân để kết luận nhân quả riêng của tech.

Map và stream wave được tái lập bằng seed; không cam kết bitwise replay toàn bộ gameplay. AI cừu hiện tạo RNG từ Entity.Index/sequence, các handle có thể đổi khi reload scene. Cố định role worker tránh phụ thuộc thứ tự query; vẫn lưu Entity và vị trí để nhận biết biến thiên đường đi/cừu, không coi một run là tỷ lệ thắng của người chơi.

## Counter và giới hạn

Snapshot actor mỗi30giây: vị trí/target/state/AttackSequence/Moving/SuppressAcquire/path/detection/range, worker cargo/node/depot/phase. Counter army firstSeen/atHallLine/firstAttack ghi vị trí, target và thời gian từ rally; atHallLine là lần đầu trong vòng4unit quanh bodyHall, có thể là0nếu quân sinh sẵn ở gần. AttackSequence là bắt đầu hoạt ảnh native, không bằng damage thực; damage attribution ghi unavailable. Hall HP là damage ròng quan sát.

Diagnostic stalled hỗ trợ cả node chỉ có GameplayResource: khoảng cách khi đó dùng position/radius của resource, không là phép đo collider Physics chính xác; `targetHP=0` của fallback cây/mỏ là **unavailable**, không phải cây đã chết. Chỉ cừu có CombatBody mới có HP target được đo. Snapshot worker/loại tài nguyên và Entity dùng để đối chiếu. Không diễn giải một lần stalled là bug pathfinding đã xác nhận: mục tiêu không tiếp cận được có thể là bố trí node/vật cản hoặc policy chọn điểm chưa phù hợp.

Quota riêng từng đêm; eligibleSlots mô phỏng chính Tick scheduler với mọi spawn thành công trước bình minh. Spawn thực đo TotalSpawned delta; firstSeen ghi cả enemy author sẵn với initial=true. LineArrivals là từng Entity đi vào vòng6worldunit quanh hall, hallAttackers là Target=Hall và State=Attacking, không tuyên bố đó là hit đã gây damage. RetreatBeforeLine là Entity chuyển Retreating trước khi vào vòng6; không đồng nghĩa chưa đánh bất cứ công trình nào. Arrival timestamp lưu riêng để nối firstSeen→arrival. Không cộng counter theo phase vì chúng là cumulative.

## Cách chạy, kiểm thử và phục hồi

Qua Unity MCP `script-execute`: đọc full `Tools/PlaytestG01.cs`, className `G01Playtest`, method `Control`, `Economy`, `Military` hoặc `Expansion`, timeout290. Trước mỗi run: paused Play, đặt `StrategicProceduralMapSettings.Seed` chỉ trong bộ nhớ, `SceneManager.LoadScene("ProceduralGame")`, call riêng `EditorApplication.Step()`, rồi kiểm Ready. Không SaveAssets/SaveScene trong Play. Lỗi run lưu file FAILED trước khi throw; timeout của host có thể chỉ còn checkpoint bên ngoài, không tự chạy lại khi chưa kiểm hiệu ứng.

Sau run hoặc khi phục hồi phiên dừng: đặt lại Seed=1 trong bộ nhớ, thoát Play, xác nhận scene sạch/ngoài pause-compile và disk seed1. Không reset scene/prefab/git. G0–G1 không author candidate nên không cần phục hồi HP/wave/tech. Nếu sau này thử candidate, dùng path/serialized property trong authoring snapshot để khôi phục đúng từng trường qua SerializedObject/Unity API; không overwrite cả asset từ JSON và không hand-edit YAML.

Kiểm tích hợp là matrix với assert ledger/HP/terminal; không thêm test production cho harness nằm ngoài Assets. Regression native không cần chạy lại nếu không đổi runtime; nếu xác nhận và sửa bug runtime phải thêm test rồi chạy suite liên quan. Hiệu năng, realtime candidate, Release/onscreen và manual feedback thuộc các gate sau; chưa xác nhận ở G0–G1.

## Kết quả và thay đổi sau matrix

[Báo cáo G01](G01-Validation.md) chốt12/12run và giới hạn. Matrixv3 đo trước guard presentation trong Unit.cs; source trước/sau, fingerprint và self-check worker/archer lưu riêng. Guard loại worker khỏi trigger Attack chung vì worker dùng bool isAttacking. Không thay native simulation và không chạy lại matrix sau guard. Script kiểm offline: `node Tools/SummarizeG01.mjs --complete --compact`.
