# Checkpoint E5 — 24/11/2026

**E5 DONE phạm vi implementation/Editor seed1**, tiếp E6. [Báo cáo](../../E5-DepthOcclusion.md). Chưa Player/performance/manual thẩm mỹ, G3/G5 vẫn mở.

- Root cấp WorldDepthPresentation, generated resource root và scenery. Two retained material assets/shaders, one scene visibility texture;158bodies baseline, shadow/contact/ring mesh batches order20/100 tương thích Renderer2D. Native sprite instancing giữ nguyên, thêm contact batch cap1023 và render metadata FootOffset.
- Foot anchor dưới collider cho Mono, FootOffset native; không đổi collider/pivot/transform gameplay. Upper-only stipple40%, lower body nguyên; point/anchor snap. Dusk midpoint1.8, Day/Night1.
- GPU192x192 PASS depth native-shader/SpriteRenderer, roof holes vsopaque trunk, static/mobile fog, contact mask, ring, bilinear source quaPoint và subpixel pan;12ảnh native Move qua cây/Hall với namedTargetFade1sau/0trước ởDay/Dusk/Night. Ảnh route1920x1080 fog-off/phases probe, không phải natural clock/manual discovery.
-96stationary zero-damage enemy fixture,98native rendered/contact (gồm2đang có),4ring,42Mono contacts; không là benchmark. Zoom actual4/6/8; pool đúng RuntimeObjectPool +bridge.Register same-instance PASS.
- Fog0visibility/pause thật+step, component-only cleanup restorematerial/order/block, direct active unload/newmatches/legacy PASS. IDs owned E5/native contact đều gone, shared materials sống.
-81/81 EditMode PASS,0fail/skip,10.2861536s. S7/E4 compatibility PASS, full map hash26102ba0e108e16be640d2d6532189a47a30cf834ca74e04fcd5e160e9d71f5e/version3/150placements giữ nguyên. Existing E4 evidence không bị ghi đè; bản kiểm mới ởE5/E4-regression.
- E2 mask/tint với foot block PASS; actual facade raw pixel normal=near=(1,1,.5176471), tắt core=(.745098054,.819607854,.5647059), kiểm glow không bị depth E5 chặn. Shader compile/source ownership checked; không thay budget8lights.
- Editor cuối Unity6000.3.11f1/ProceduralGame/seed1 sạch ngoàiPlay-pause-compile. Không nâng Editor/package/pipeline hoặc sửa AI/jobs/economy. Plugin tự đổi MCP dependency define47→50; giữ ghi nhận trong report. Không commit hoặc triển khai E6–E10.
