using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace RTS.DOTS
{
    // A single writer owns claims, construction, queues and wallet transactions.
    // This avoids races when several workers deposit or finish the same building on one tick.
    [BurstCompile]
    public struct GameplayEconomyJob : IJob
    {
        [ReadOnly] public NativeArray<Entity> Workers, Buildings, Depots;
        public ComponentLookup<GameplayActor> Actors;
        public ComponentLookup<GameplayWorker> WorkerData;
        public ComponentLookup<GameplayResource> Resources;
        public ComponentLookup<CombatBody> Bodies;
        public ComponentLookup<GameplayConstruction> Construction;
        public ComponentLookup<GameplayProducer> Producers;
        public ComponentLookup<PlayerWallet> Wallets;
        [ReadOnly] public ComponentLookup<GameplayDepot> DepotData;
        [ReadOnly] public BufferLookup<GatheringProfileData> Profiles;
        public BufferLookup<TrainingRequest> Requests;
        public NativeQueue<GameplayEvent> Events;
        public Entity Wallet;
        public float DeltaTime;
        [ReadOnly] public NativeArray<byte> Walkable;
        public float2 Origin;
        public float CellSize;
        public int2 Size;

        public void Execute()
        {
            foreach (var entity in Workers)
            {
                var worker = WorkerData[entity];
                var actor = Actors[entity];
                var body = Bodies[entity];
                if (body.Health <= 0)
                {
                    Release(ref worker, entity); WorkerData[entity] = worker; continue;
                }
                if (worker.Phase == WorkPhase.Sheltered)
                {
                    if (!Alive(worker.Shelter))
                    {
                        worker.Phase = WorkPhase.None; worker.Shelter = Entity.Null; body.Targetable = 1;
                        body.Position = body.PreviousPosition = GameplayMath.NearestWalkable(body.Position, body.Radius, Origin, CellSize, Size, Walkable);
                    }
                }
                else if (worker.Phase == WorkPhase.ReturningToShelter)
                {
                    if (!Alive(worker.Shelter))
                    { worker.Phase = WorkPhase.None; actor.Moving = 0; }
                    else if (Distance(body, Bodies[worker.Shelter]) <= 0.5f)
                    {
                        body.Position = Bodies[worker.Shelter].Position; body.PreviousPosition = body.Position;
                        body.Targetable = 0; actor.Moving = 0; actor.State = GameplayState.Idle;
                        worker.Phase = WorkPhase.Sheltered;
                    }
                }
                else if (worker.Phase == WorkPhase.Constructing)
                {
                    if (!Alive(worker.Structure) || !Construction.HasComponent(worker.Structure) || Construction[worker.Structure].Active == 0)
                    { worker.Structure = Entity.Null; worker.Phase = WorkPhase.None; actor.Task = GameplayTask.None; actor.State = GameplayState.Idle; }
                    else if (Distance(body, Bodies[worker.Structure]) <= math.max(0.7f, actor.Detection))
                    {
                        var build = Construction[worker.Structure];
                        if (build.Worker == Entity.Null || build.Worker == entity || !Alive(build.Worker))
                        {
                            build.Worker = entity; Construction[worker.Structure] = build;
                            actor.Moving = 0; actor.State = GameplayState.Building;
                        }
                    }
                    else Approach(ref actor, Bodies[worker.Structure].Position);
                }
                else if (worker.Phase == WorkPhase.Gathering || worker.Phase == WorkPhase.Depositing)
                {
                    GatheringProfileData profile = default;
                    bool hasProfile = false;
                    foreach (var candidate in Profiles[entity])
                        if (candidate.ResourceType == worker.ResourceType) { profile = candidate; hasProfile = true; break; }
                    if (!hasProfile) { Release(ref worker, entity); actor.Moving = 0; }
                    else if (worker.Phase == WorkPhase.Depositing)
                    {
                        if (!ValidDepot(worker.Depot, worker.ResourceType)) worker.Depot = FindDepot(body.Position, worker.ResourceType);
                        if (worker.Depot != Entity.Null)
                        {
                            var depotBody = Bodies[worker.Depot];
                            if (Distance(body, depotBody) <= 0.55f)
                            {
                                var wallet = Wallets[Wallet]; wallet.Add(worker.ResourceType, worker.Carried); Wallets[Wallet] = wallet;
                                Events.Enqueue(new GameplayEvent { Kind = GameplayEventKind.Deposit, Entity = entity, Amount = worker.Carried, ResourceType = worker.ResourceType });
                                worker.Carried = 0; worker.Timer = 0; worker.Depot = Entity.Null;
                                worker.Phase = Resources.HasComponent(worker.Node) ? WorkPhase.Gathering : WorkPhase.None;
                                if (worker.Phase == WorkPhase.Gathering) worker.ResourceType = Resources[worker.Node].Type;
                                actor.Task = worker.Phase == WorkPhase.None ? GameplayTask.None : profile.Task;
                                actor.State = GameplayState.Idle; actor.Moving = 0;
                            }
                            else { Approach(ref actor, depotBody.Position); actor.Task = GameplayTask.ReturnResource; }
                        }
                        else { actor.Moving = 0; actor.State = GameplayState.Idle; }
                    }
                    else if (!Resources.HasComponent(worker.Node))
                    {
                        worker.Node = Entity.Null;
                        worker.Phase = worker.Carried > 0 ? WorkPhase.Depositing : WorkPhase.None;
                        actor.Target = Entity.Null; actor.Moving = 0; actor.State = GameplayState.Idle; actor.Task = GameplayTask.None;
                    }
                    else
                    {
                        var node = Resources[worker.Node];
                        float2 point = Bodies.HasComponent(worker.Node) ? Bodies[worker.Node].Position : node.Position;
                        if (node.RequiresKill != 0 && Alive(worker.Node))
                        { actor.Target = worker.Node; actor.Task = GameplayTask.Attack; }
                        else
                        {
                            actor.Target = Entity.Null;
                            float distance = math.max(0, math.distance(body.Position, point) - body.Radius - node.Radius);
                            if (distance <= 0.55f)
                            {
                                actor.Moving = 0; actor.State = profile.State; actor.Task = profile.Task;
                                actor.Facing = math.normalizesafe(point - body.Position, actor.Facing);
                                worker.Timer += DeltaTime;
                                if (worker.Timer >= math.max(0.01f, profile.TickTime))
                                {
                                    worker.Timer -= math.max(0.01f, profile.TickTime);
                                    worker.Carried = math.min(profile.Capacity, worker.Carried + profile.Amount);
                                    Events.Enqueue(new GameplayEvent { Kind = GameplayEventKind.ResourceHit, Entity = worker.Node });
                                    if (worker.Carried >= profile.Capacity)
                                    { worker.Phase = WorkPhase.Depositing; actor.Task = GameplayTask.ReturnResource; actor.State = GameplayState.Idle; worker.Timer = 0; }
                                }
                            }
                            else Approach(ref actor, point);
                        }
                    }
                }
                Actors[entity] = actor; Bodies[entity] = body; WorkerData[entity] = worker;
            }

            foreach (var entity in Buildings)
            {
                if (!Alive(entity)) continue;
                var build = Construction[entity];
                if (build.Active != 0)
                {
                    if (Alive(build.Worker) && WorkerData.HasComponent(build.Worker)
                        && WorkerData[build.Worker].Structure == entity && Actors[build.Worker].State == GameplayState.Building)
                    {
                        build.Elapsed += DeltaTime;
                        if (build.Middle == 0 && build.Elapsed >= build.Duration * 0.5f)
                        { build.Middle = 1; Events.Enqueue(new GameplayEvent { Kind = GameplayEventKind.ConstructionMiddle, Entity = entity, RequestId = build.RequestId }); }
                        if (build.Elapsed >= build.Duration)
                        {
                            build.Active = 0; build.Finished = 1;
                            var worker = WorkerData[build.Worker]; worker.Structure = Entity.Null; worker.Phase = WorkPhase.None; WorkerData[build.Worker] = worker;
                            var actor = Actors[build.Worker]; actor.Task = GameplayTask.None; actor.State = GameplayState.Idle; actor.Moving = 0; Actors[build.Worker] = actor;
                            Events.Enqueue(new GameplayEvent { Kind = GameplayEventKind.ConstructionComplete, Entity = entity, RequestId = build.RequestId });
                        }
                    }
                    else build.Worker = Entity.Null;
                    Construction[entity] = build;
                    continue; // Production and tower combat start on the next tick after completion.
                }
                if (!Requests.HasBuffer(entity)) continue;
                var queue = Requests[entity];
                if (queue.Length == 0) continue;
                var producer = Producers[entity];
                producer.CurrentRequest = queue[0].RequestId; producer.Elapsed += DeltaTime;
                if (producer.Elapsed >= math.max(0.01f, queue[0].Duration))
                {
                    Events.Enqueue(new GameplayEvent { Kind = GameplayEventKind.TrainComplete, Entity = entity, RequestId = queue[0].RequestId });
                    queue.RemoveAt(0); producer.Elapsed = 0; producer.CurrentRequest = queue.Length > 0 ? queue[0].RequestId : 0;
                }
                Producers[entity] = producer;
            }
        }

        bool Alive(Entity entity) => Bodies.HasComponent(entity) && Bodies[entity].Health > 0;
        bool ValidDepot(Entity entity, int type) => Alive(entity) && DepotData.HasComponent(entity)
            && (DepotData[entity].ResourceMask & (1 << type)) != 0 && (!Construction.HasComponent(entity) || Construction[entity].Active == 0);
        Entity FindDepot(float2 position, int type)
        {
            Entity best = Entity.Null; float distance = float.MaxValue;
            foreach (var entity in Depots)
                if (ValidDepot(entity, type))
                { float d = math.distancesq(position, Bodies[entity].Position); if (d < distance) { best = entity; distance = d; } }
            return best;
        }
        static float Distance(CombatBody a, CombatBody b) => GameplayMath.DistanceToBody(a.Position, b) - a.Radius;
        public static void Approach(ref GameplayActor actor, float2 destination)
        {
            if (actor.Moving == 0 || math.distancesq(actor.Destination, destination) > 0.25f)
            { actor.Destination = destination; actor.Moving = 1; actor.PathDirty = 1; }
            actor.State = GameplayState.Moving;
        }
        void Release(ref GameplayWorker worker, Entity entity)
        {
            if (Resources.HasComponent(worker.Node))
            { var node = Resources[worker.Node]; node.Claims = math.max(0, node.Claims - 1); Resources[worker.Node] = node; }
            if (Construction.HasComponent(worker.Structure))
            { var build = Construction[worker.Structure]; if (build.Worker == entity) { build.Worker = Entity.Null; Construction[worker.Structure] = build; } }
            worker.Node = worker.Depot = worker.Structure = Entity.Null; worker.Phase = WorkPhase.None;
        }
    }
}
