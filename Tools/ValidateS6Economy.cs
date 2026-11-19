// Unity MCP script-execute: S6EconomyValidation.Run in a fresh paused, disposable Play session.
using System;
using System.Linq;
using System.Reflection;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEditor;
using RTS.DOTS;

public static class S6EconomyValidation
{
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    public static string RunConstruction()
    {
        var bridge=UnityEngine.Object.FindFirstObjectByType<SwarmGameBridge>();
        var generator=UnityEngine.Object.FindFirstObjectByType<ProceduralMapAuthoring>();
        var map=UnityEngine.Object.FindFirstObjectByType<TilemapManager>();
        var game=UnityEngine.Object.FindFirstObjectByType<GameManager>();
        var hall=UnityEngine.Object.FindFirstObjectByType<TownHall>();
        var worker=UnityEngine.Object.FindObjectsByType<WorkerUnit>(FindObjectsSortMode.None).OrderBy(w=>w.GetInstanceID()).Last();
        var data=generator.GeneratedMap;
        var origin=map.WalkableTilemap.WorldToCell(hall.transform.position)-new Vector3Int(data.StartX,data.StartY,0);
        var landmark=data.Layout.Landmark;
        var cell=origin+new Vector3Int(landmark.X,landmark.Y,0);
        var position=map.WalkableTilemap.CellToWorld(cell);
        var action=AssetDatabase.LoadAssetAtPath<BuildActionSo>("Assets/Game/ScriptableObjects/Units/BuildStoragePitAction.asset");
        var placement=new PlacementProcess(action,map,position);
        placement.ShowPlacementOutline();
        typeof(PlacementProcess).GetMethod("HighLightTiles",Private).Invoke(placement,new object[]{position});
        Check(placement.TryFinalizePlacement(out var accepted),"Landmark placement rejected.");
        int wood=game.Wood,gold=game.Gold;
        Check((bool)typeof(GameManager).GetMethod("TryDeductResources",Private).Invoke(game,new object[]{action.GoldCost,action.WoodCost}),"Construction debit rejected.");
        Check(game.Wood==wood-action.WoodCost&&game.Gold==gold-action.GoldCost,"Construction debit mismatch.");
        int actors=bridge.Gameplay.ActorCount;
        var process=new BuildingProcess(action,accepted,worker,game,bridge);
        Physics2D.SyncTransforms();
        var refresh=typeof(SwarmGameBridge).GetMethod("RefreshNavigation",Private);
        var sync=typeof(SwarmGameBridge).GetMethod("SynchronizeGameplay",Private);
        refresh.Invoke(bridge,null);
        var gameplay=bridge.Gameplay;var originalTime=gameplay.World.Time;
        float finishedAt=0;
        try
        {
            for(int tick=1;tick<=6000;tick++)
            {
                gameplay.World.SetTime(new TimeData(originalTime.ElapsedTime+tick*.05,.05f));
                gameplay.Update();
                if(tick%10==0){sync.Invoke(bridge,null);Physics2D.SyncTransforms();}
                if(process.m_isFinished){finishedAt=tick*.05f;break;}
            }
            Check(process.m_isFinished&&bridge.Gameplay.ActorCount==actors+1,"Worker did not finish exactly one building.");
            var structure=UnityEngine.Object.FindObjectsByType<StructureUnit>(FindObjectsSortMode.None)
                .First(s=>Vector2.Distance(s.transform.position,accepted)<.1f);
            Check(structure.IsDotsControlled&&structure.SpriteRenderer.sprite==action.CompleteSprite,"Completed building presentation mismatch.");
            var build=gameplay.EntityManager.GetComponentData<GameplayConstruction>(structure.DotsEntity);
            Check(build.Finished!=0&&build.Active==0,"Construction state not completed.");
            return $"PASS seed={data.Seed}; StoragePit built at landmark cell={cell}; completedAt={finishedAt:F2}s; debit={action.WoodCost}wood/{action.GoldCost}gold; actors={actors}->{gameplay.ActorCount}; run CheckFogCamera after one Editor Step so the new view executes Start";
        }
        finally{gameplay.World.SetTime(originalTime);placement.CleanUp();}
    }

    public static string CheckFogCamera()
    {
        var generator=UnityEngine.Object.FindFirstObjectByType<ProceduralMapAuthoring>();
        var map=UnityEngine.Object.FindFirstObjectByType<TilemapManager>();
        var hall=UnityEngine.Object.FindFirstObjectByType<TownHall>();
        var game=UnityEngine.Object.FindFirstObjectByType<GameManager>();
        var fog=UnityEngine.Object.FindFirstObjectByType<FogOfWarManager>();
        var minimap=UnityEngine.Object.FindFirstObjectByType<MinimapFogUI>();
        var camera=UnityEngine.Object.FindFirstObjectByType<CameraController>();
        var data=generator.GeneratedMap;
        var origin=map.WalkableTilemap.WorldToCell(hall.transform.position)-new Vector3Int(data.StartX,data.StartY,0);
        var cell=origin+new Vector3Int(data.Layout.Landmark.X,data.Layout.Landmark.Y,0);
        var position=map.WalkableTilemap.CellToWorld(cell);
        Check(game.GetPlayerStructures().Any(s=>s!=null&&Vector2.Distance(s.transform.position,position)<.1f),"New view has not run Start/registered with GameManager; advance one Editor Step first.");
        typeof(FogOfWarManager).GetMethod("RefreshFog",Private).Invoke(fog,null);
        Check(fog.IsInitialized&&fog.Bounds==map.GetWorldMapBounds()&&fog.IsWorldPositionExplored(position),"Fog did not reveal region building.");
        typeof(MinimapFogUI).GetMethod("RefreshMinimap",Private).Invoke(minimap,null);
        var texture=(Texture2D)typeof(MinimapFogUI).GetField("m_MinimapTexture",Private).GetValue(minimap);
        Check(texture!=null&&texture.width==fog.Bounds.size.x&&texture.height==fog.Bounds.size.y,"Minimap bounds mismatch.");
        var cameraPosition=Camera.main.transform.position;
        try
        {
            camera.FocusWorldPosition(position);
            Check(Vector2.Distance(Camera.main.transform.position,position)<.01f&&Camera.main.transform.position.z==cameraPosition.z,"Camera focus mismatch.");
            return $"PASS seed={data.Seed}; completed building registered via Start; fog revealed; minimap={texture.width}x{texture.height}; camera focus";
        }
        finally { Camera.main.transform.position=cameraPosition; }
    }

    public static string CheckResourceBinding()
    {
        var bridge = UnityEngine.Object.FindFirstObjectByType<SwarmGameBridge>();
        var map = UnityEngine.Object.FindFirstObjectByType<TilemapManager>();
        var existing = UnityEngine.Object.FindObjectsByType<ResourceNodeBase>(FindObjectsSortMode.None);
        Check(existing.Length > 0 && existing.All(n => n.IsInitialized), "Scene resources were not injected.");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/TerrainObjects/Tree.prefab");
        var view = bridge.SpawnGameplay(prefab, new Vector3(1000,1000), Quaternion.identity);
        var node = view.GetComponent<ResourceNodeBase>();
        var em = bridge.Gameplay.EntityManager;
        using var query = em.CreateEntityQuery(typeof(GameplayResource));
        try
        {
            var first = bridge.ResourceEntity(node);
            int count = query.CalculateEntityCount();
            node.Initialize(bridge, map);
            Check(bridge.RegisterResource(node) == first && query.CalculateEntityCount() == count, "Repeat registration duplicated resource.");
            bool rejected = false;
            try { node.Initialize(null, map); } catch (ArgumentException) { rejected = true; }
            Check(rejected, "Missing dependency accepted.");
            var collider = node.GetComponent<CapsuleCollider2D>();
            int version = map.NavigationVersion;
            for (int i=0;i<node.MaxWorkers;i++) Check(node.TryClaim(), "Valid claim rejected.");
            Check(node.IsClaimed && !node.TryClaim() && !collider.enabled && map.NavigationVersion > version, "Claims/collider/navigation mismatch.");
            for (int i=0;i<node.MaxWorkers;i++) node.Release();
            Check(!node.IsClaimed && collider.enabled, "Release did not restore collider.");
            bridge.UnregisterResource(node);
            Check(!node.IsInitialized && !em.Exists(first), "Unregister retained dependency/entity.");
            var second = bridge.RegisterResource(node);
            Check(node.IsInitialized && second != first && query.CalculateEntityCount() == count, "Rebind failed.");
            UnityEngine.Object.DestroyImmediate(view);
            Check(!em.Exists(second) && query.CalculateEntityCount() == count-1, "Destroy leaked resource Entity.");
            return $"PASS injectedResources={existing.Length}; repeat/missing/claim-capacity/collider/navigation/unregister/rebind/destroy";
        }
        finally { if (view != null) UnityEngine.Object.DestroyImmediate(view); }
    }

    public static string Run()
    {
        var bridge = UnityEngine.Object.FindFirstObjectByType<SwarmGameBridge>();
        var generator = UnityEngine.Object.FindFirstObjectByType<ProceduralMapAuthoring>();
        var map = UnityEngine.Object.FindFirstObjectByType<TilemapManager>();
        var hall = UnityEngine.Object.FindFirstObjectByType<TownHall>();
        var workers = UnityEngine.Object.FindObjectsByType<WorkerUnit>(FindObjectsSortMode.None).OrderBy(w => w.GetInstanceID()).Take(2).ToArray();
        Check(workers.Length == 2 && workers.All(w => w.IsDotsControlled), "Need two bound workers.");
        var data = generator.GeneratedMap;
        var origin = map.WalkableTilemap.WorldToCell(hall.transform.position) - new Vector3Int(data.StartX,data.StartY,0);
        var nodes = UnityEngine.Object.FindObjectsByType<ResourceNodeBase>(FindObjectsSortMode.None);
        var targets = new ResourceNodeBase[2];
        for (int i=0;i<2;i++)
        {
            int region=i+1;
            var center = map.WalkableTilemap.GetCellCenterWorld(origin + new Vector3Int(data.Layout.Regions[region].X,data.Layout.Regions[region].Y,0));
            targets[i]=nodes.Where(n => n.ResourceType == (i==0?ResourceType.Wood:ResourceType.Gold)).Where(n =>
            {
                var cell=map.WalkableTilemap.WorldToCell(n.transform.position)-origin;
                return data.InBounds(cell.x,cell.y)&&data.Layout.RegionIds[data.Index(cell.x,cell.y)]==region;
            }).OrderBy(n => Vector3.SqrMagnitude(n.transform.position-center)).First();
        }
        var gameplay=bridge.Gameplay;
        var em=gameplay.EntityManager;
        var originalTime=gameplay.World.Time;
        var sync=typeof(SwarmGameBridge).GetMethod("SynchronizeGameplay",Private);
        var refresh=typeof(SwarmGameBridge).GetMethod("RefreshNavigation",Private);
        var deposited=new int[2]; var arrival=new float[2]; var delivery=new float[2];
        var oldBodies=workers.Select(w => em.GetComponentData<CombatBody>(w.DotsEntity)).ToArray();
        int navVersion=map.NavigationVersion;
        try
        {
            for(int i=0;i<2;i++)
            {
                var body=oldBodies[i]; body.Health=1000000; em.SetComponentData(workers[i].DotsEntity,body);
                Check(bridge.AssignResource(workers[i],targets[i]),"Assign region resource failed.");
            }
            while(gameplay.TryTakeEvent(out _)) { }
            for(int tick=1;tick<=12000;tick++)
            {
                gameplay.World.SetTime(new TimeData(originalTime.ElapsedTime+tick*.05,.05f));
                gameplay.Update();
                while(gameplay.TryTakeEvent(out var evt))
                    if(evt.Kind==GameplayEventKind.Deposit)
                        for(int i=0;i<2;i++) if(evt.Entity==workers[i].DotsEntity)
                        { deposited[i]+=evt.Amount;if(delivery[i]==0)delivery[i]=tick*.05f; }
                for(int i=0;i<2;i++)
                    if(arrival[i]==0 && gameplay.TryGetWorker(workers[i].DotsEntity,out var w) && w.Carried>0) arrival[i]=tick*.05f;
                if(tick%10==0)
                {
                    sync.Invoke(bridge,null); Physics2D.SyncTransforms();
                    if(navVersion!=map.NavigationVersion){refresh.Invoke(bridge,null);navVersion=map.NavigationVersion;}
                }
                if(deposited.All(n => n>0))break;
            }
            Check(deposited.All(n=>n>0), $"Region gather/deposit failed seed={data.Seed}; amounts={string.Join(",",deposited)}; arrival={string.Join(",",arrival)}; phases="
                +string.Join(";",workers.Select(w=>gameplay.TryGetWorker(w.DotsEntity,out var state)?state.ToString()+$" phase={state.Phase} cargo={state.Carried}":"missing")));
            var wallet=gameplay.Wallet;
            Check(wallet.Wood>=100+deposited[0] && wallet.Gold>=100+deposited[1],"Deposit event/wallet mismatch.");
            return $"PASS seed={data.Seed}; Economy wood={deposited[0]} firstCargoAt={arrival[0]:F2}s depositAt={delivery[0]:F2}s; GoldFrontier gold={deposited[1]} firstCargoAt={arrival[1]:F2}s depositAt={delivery[1]:F2}s; wallet={wallet.Wood}/{wallet.Gold}/{wallet.Meat}";
        }
        finally
        {
            gameplay.World.SetTime(originalTime);
            for(int i=0;i<2;i++) if(em.Exists(workers[i].DotsEntity))
            { var body=em.GetComponentData<CombatBody>(workers[i].DotsEntity);body.Health=oldBodies[i].Health;em.SetComponentData(workers[i].DotsEntity,body); }
        }
    }
}
