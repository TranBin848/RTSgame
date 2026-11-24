# Cách chạy Save/Load trong giai đoạn nền tảng

Ngày **24/11/2026 — SL4**. Có capture service và API restore giới hạn; chưa có nút Save/Load/Continue hoặc autosave. Mở MainMenu/ProceduralGame bình thường vẫn chạy NewGame như trước. Không dùng file fixture để tiếp tục trận thật.

**SL4:** đã có API kỹ thuật `MatchLoadBootstrap.LoadFromMenu(encodedBytes, contentCatalog)` ở MainMenu, preflight trước chuyển scene. Chỉ hỗ trợ first Day/day1, workers idle/cargo0, không queue/research/projectile/active construction; state ngoài lát cắt bị từ chối rõ. Sau load hiển thị pause, Resume nhả native pause và camera. Chưa có Continue/Save/Load UI hoặc autosave; không dùng API này lên trận cần giữ vì rollback live-match chưa có. [Report SL4](SL4-Report.md).

Editor test: đọc `Tools/ValidateSaveLoadSL4.cs`, gọi full-source class `ValidateSaveLoadSL4` các stage `Prepare`, `SaveAndMenu`, `Load`, `RequestRecapture`, `Verify`, có một player frame giữa request và check. Chỉ dùng disposable Play. Windows functional test: build bằng `Tools/BuildSaveLoadSL4.cs`, chạy `Tools/RunSaveLoadSL4.ps1`; flag `--sl4-smoke capture|load <output-dir> <seed>` chỉ dùng test, không tự bật khi chơi. Test ghi vào Outputs, không chạm player slots.

## Kiểm thử

Trong Unity Test Runner, chạy EditMode:

1. `RTS.SaveLoad.Tests`: DTO, schema/catalog/ID/numeric/reference validation, JSON lỗi, hai slot file thật, backup, generation, size limit và commit faults.
2. `RTS.DOTS.Tests`: regression simulation, identity chung ở factories và cừu dùng saved seed sau remap Entity.
3. `RTS.ProceduralGeneration.Tests`: regression generator/topology.

Tests storage tự tạo directory tên `RTS-SaveLoadTests-<UUID>` trong OS temp và chỉ dọn directory của test đó. Không sửa slot người chơi. `SaveCommitStage` probe chỉ inject lỗi trong tests; production không cấp probe.

## API Core đã có

`SaveCatalog(definitions, actionIds, technologyIds, configurationJson)` nhận whitelist/config từ composition. `SaveCodec.Encode` validate rồi trả byte[]; `Decode` kiểm envelope/checksum/shape/semantics. `SaveSlotStorage(directory, codec)` chỉ nhận directory tin cậy do ứng dụng chọn; gọi `Write(SaveSlot.Manual|Dawn, snapshot)` và `Read(slot)`. Caller phải cấp generation tăng, timestamp capture UTC và snapshot detached. `Read` trả snapshot cùng `UsedBackup`/`PrimaryError`; sau backup recovery UI tương lai phải nói rõ giờ checkpoint cũ.

Đường dẫn production dự kiến: `Path.Combine(Application.persistentDataPath, "Saves")`. Chưa có caller ghi vào directory này. File test/sample chỉ chứng minh codec/storage. Không tự chuyển chúng thành save thật bằng cách sửa checksum/content fingerprint.

Storage đồng bộ, chỉ lock thao tác file. Không gọi từ nút UI để giả định đã fence ECS. Root có `SaveCapture.RequestCapture(generation)`; chờ `IsPending/IsCapturing` false ở frame sau, kiểm `LastError`, rồi đọc `LastSnapshot` detached. API từ chối terminal/scene chưa Ready/scene ngoài ProceduralGame và duplicate request. Root truyền dependencies, UI tương lai không Find bridge. Capture chưa tự ghi đĩa và chưa có restore API. Chi tiết thiết kế tại [Architecture](Architecture.md).

## Catalog và integration checks

`RTS/SaveLoad/Rebuild content catalog` tạo/cập nhật `Assets/Game/SaveLoad/Resources/ProceduralSaveCatalog.asset`, dùng prefab tags và action assets đã cho phép. Editor hook cập nhật fingerprint trước Play/build. Không sửa ID bằng tên object runtime; unknown definition bị từ chối.

Script kiểm soát nằm ở `Tools/ValidateSaveCapture.cs`. Trong disposable Play ProceduralGame, dùng script-execute full-source với class `ValidateSaveCapture`, lần lượt method `Setup`, `CheckFixture`, `CheckRepeatAndInjectFailure`, `CheckFailureAndRequestCompletion`, `CheckCompletions`, `CheckFinalRepeat`. Mỗi Request cần một player-loop frame thật trước Check, Editor không được Pause. Fixture dùng timeScale0 và runInBackground tạm để frame UI vẫn chạy; native tick completion được cấp dt.02 riêng, không giả là chơi tự nhiên. Thoát Play sẽ bỏ mọi fixture; trả runInBackground/timeScale/Editor pause về cũ. Không chạy Setup trên trận người dùng muốn giữ.

Output đầy đủ của fixture ở `Outputs/SaveLoad/SL3/` (không phải slot player). Evidence nhỏ ở Docs không chứa sample trận thật. Matrix seed0/1543 dùng BenchmarkSeedOverride chỉ trong disposable Play và reload scene, rồi reset override/null; asset seed1 không đổi.
