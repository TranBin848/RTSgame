using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace RTS.SaveLoad
{
    public static class SaveValidator
    {
        public const int SchemaVersion = 1, MaxObjects = 50000, MaxQueue = 256, MaxDimension = 1024;
        public const int MaxFileBytes = 64 * 1024 * 1024;
        internal static void Require(bool condition, string message)
        { if (!condition) throw new SaveValidationException(message); }
        public static void Validate(MatchSaveData save, SaveCatalog catalog)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            Require(save != null, "Missing snapshot.");
            Require(save.SchemaVersion == SchemaVersion, "Unsupported save schema.");
            Require(save.ContentFingerprint == catalog.Fingerprint, "Save content/configuration differs from this build.");
            Require(Guid.TryParseExact(save.SaveId, "N", out _), "Invalid snapshot ID.");
            Require(DateTimeOffset.TryParseExact(save.CapturedUtc, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                && date.Offset == TimeSpan.Zero, "Invalid UTC capture timestamp.");
            Require(save.Generation > 0 && save.Generation < long.MaxValue && save.NextSaveId > 0 && save.NextSaveId < long.MaxValue
                && save.NextRequestId > 0 && save.NextRequestId < int.MaxValue, "Invalid ID/generation counters.");
            Require(save.GameplayTicks >= 0 && save.SwarmTicks >= 0 && save.TotalSpawned >= 0 && save.TotalHits >= 0, "Invalid simulation counters.");
            ValidateNumbers(save);
            Map(save.Map);
            Require(save.Objects != null && save.Objects.Length > 0 && save.Objects.Length <= MaxObjects, "Invalid object count.");
            var objects = new Dictionary<long, ObjectSaveData>();
            foreach (var obj in save.Objects)
            {
                Require(obj != null && obj.Id > 0 && obj.Id < save.NextSaveId && !objects.ContainsKey(obj.Id), "Duplicate or invalid object SaveId.");
                Require(catalog.TryDefinition(obj.DefinitionId, out var definition), "Unknown object definition.");
                SaveCapability flags = 0;
                if (obj.Body != null) flags |= SaveCapability.Body;
                if (obj.Actor != null) flags |= SaveCapability.Actor;
                if (obj.Worker != null) flags |= SaveCapability.Worker;
                if (obj.Resource != null) flags |= SaveCapability.Resource;
                if (obj.Depot != null) flags |= SaveCapability.Depot;
                if (obj.Construction != null) flags |= SaveCapability.Construction;
                if (obj.Producer != null) flags |= SaveCapability.Producer;
                if (obj.Enemy != null) flags |= SaveCapability.Enemy;
                if (obj.Projectile != null) flags |= SaveCapability.Projectile;
                Require(flags == definition.Capabilities, "Object capabilities differ from catalog.");
                Require((obj.Actor == null || obj.Body != null) && (obj.Worker == null || obj.Actor?.Kind == 0)
                    && (obj.Enemy == null || obj.Body != null && obj.Actor == null)
                    && (obj.Projectile == null || flags == SaveCapability.Projectile)
                    && (obj.Depot == null || obj.Actor != null) && (obj.Construction == null || obj.Actor != null)
                    && (obj.Producer == null || obj.Construction != null), "Invalid component combination.");
                objects.Add(obj.Id, obj);
            }
            ObjectSaveData Ref(long id, SaveCapability capability, bool required = false)
            {
                if (id == 0 && !required) return null;
                Require(objects.TryGetValue(id, out var obj), "Dangling object reference.");
                Require(catalog.TryDefinition(obj.DefinitionId, out var def) && (def.Capabilities & capability) == capability, "Wrong reference capability.");
                return obj;
            }
            var requests = new HashSet<int>();
            void Request(int id)
            { Require(id > 0 && id < save.NextRequestId && requests.Add(id), "Duplicate or invalid request ID."); }
            foreach (var obj in save.Objects)
            {
                if (obj.Body is { } body)
                    Require(body.Radius > 0 && body.Radius <= 100 && body.Faction >= 0 && body.Faction <= 2
                        && body.BoxHalfSize.X >= 0 && body.BoxHalfSize.Y >= 0, "Invalid combat body.");
                if (obj.Actor is { } a)
                {
                    Require(a.Kind >= 0 && a.Kind <= 5 && a.State >= 0 && a.State <= 7 && a.Task >= 0 && a.Task <= 6
                        && a.Speed >= 0 && a.Range >= 0 && a.Detection >= 0 && a.Interval > 0 && a.Delay >= 0
                        && a.Windup >= -1 && a.Damage >= 0 && a.RandomSeed >= 0 && obj.MaxHealth > 0 && a.MinIdle >= 0 && a.MaxIdle >= a.MinIdle
                        && a.MinGraze >= 0 && a.MaxGraze >= a.MinGraze && a.FleeDistance >= 0 && a.FleeJitter >= 0, "Invalid actor state.");
                    Ref(a.Target, SaveCapability.Body); Ref(a.LastDamager, SaveCapability.Body);
                    Parameters(a.Projectile);
                }
                if (obj.Worker is { } w)
                {
                    Require(w.Phase >= 0 && w.Phase <= 5 && w.ResourceType >= 0 && w.ResourceType <= 2 && w.Carried >= 0
                        && w.Timer >= 0 && w.Profiles != null && w.Profiles.Length <= 3, "Invalid worker.");
                    var types = new HashSet<int>();
                    foreach (var p in w.Profiles)
                        Require(p.ResourceType >= 0 && p.ResourceType <= 2 && types.Add(p.ResourceType) && p.Amount > 0
                            && p.Capacity > 0 && p.TickTime > 0 && p.State >= 0 && p.State <= 7 && p.Task >= 0 && p.Task <= 6, "Invalid gathering profile.");
                    Ref(w.Node, SaveCapability.Resource); Ref(w.Depot, SaveCapability.Depot);
                    Ref(w.Structure, SaveCapability.Construction); var shelter = Ref(w.Shelter, SaveCapability.Depot, w.Phase == 4 || w.Phase == 5);
                    Require(shelter == null || shelter.Depot.TownHall, "Shelter is not a TownHall.");
                    Require(w.Phase != 5 || !obj.Body.Targetable, "Sheltered worker is targetable.");
                    Ref(w.ResumeResource, SaveCapability.Resource); Ref(w.ResumeConstruction, SaveCapability.Construction);
                }
                if (obj.Resource is { } r)
                    Require(r.Type >= 0 && r.Type <= 2 && r.Radius >= 0 && r.Capacity > 0 && r.Claims >= 0 && r.Claims <= r.Capacity, "Invalid resource/claims.");
                if (obj.Depot is { } depot) Require(depot.ResourceMask >= 0 && depot.ResourceMask <= 7, "Invalid depot mask.");
                if (obj.Construction is { } c)
                {
                    Require(c.Duration >= 0 && c.Elapsed >= 0 && c.Elapsed <= c.Duration + (c.Finished ? .051f : 0) && c.RequiredTechnology >= 0
                        && c.RequiredTechnology <= 2 && !(c.Active && c.Finished), "Invalid construction.");
                    Ref(c.Worker, SaveCapability.Worker);
                    if (c.RequestId != 0)
                    { Request(c.RequestId); Require(catalog.HasAction(c.ActionId) && c.Duration > 0, "Unknown construction action."); }
                    else Require(!c.Active && c.ActionId == null, "Missing construction request.");
                }
                if (obj.Producer is { } producer)
                {
                    Require(producer.Elapsed >= 0 && producer.SpawnSequence >= 0 && producer.Queue != null && producer.Queue.Length <= MaxQueue, "Invalid production queue.");
                    foreach (var item in producer.Queue)
                    {
                        Require(item != null && item.Duration > 0 && catalog.HasAction(item.ActionId)
                            && catalog.TryDefinition(item.PrefabId, out var def) && (def.Capabilities & SaveCapability.Actor) != 0, "Invalid training context.");
                        Request(item.RequestId);
                    }
                    Require(producer.Queue.Length == 0 ? producer.CurrentRequest == 0 && producer.Elapsed == 0
                        : producer.CurrentRequest == 0 && producer.Elapsed == 0 || producer.CurrentRequest == producer.Queue[0].RequestId
                            && producer.Elapsed <= producer.Queue[0].Duration, "Production head/elapsed differs from FIFO.");
                }
                if (obj.Enemy is { } e)
                {
                    Require(e.State >= 0 && e.State <= 3 && e.Speed >= 0 && e.AttackRange > 0 && e.DetectionRange > 0
                        && e.AttackInterval > 0 && e.AttackDelay >= 0 && e.Windup >= -1 && e.Damage >= 0
                        && e.VisualId >= 0 && e.ProjectileVisualId >= 0 && e.RandomSeed >= 0 && e.VisualScale > 0, "Invalid enemy.");
                    Ref(e.Target, SaveCapability.Body); Parameters(e.Projectile);
                }
                if (obj.Projectile is { } shot)
                {
                    Require(shot.Speed > 0 && shot.Radius > 0 && shot.Lifetime > 0 && shot.Age >= 0 && shot.Duration > 0
                        && shot.VisualScale > 0 && shot.Damage >= 0 && shot.Faction >= 0 && shot.Faction <= 2 && shot.VisualId >= 0, "Invalid projectile.");
                    Ref(shot.Owner, SaveCapability.Body); Ref(shot.Target, SaveCapability.Body);
                }
            }
            foreach (var p in save.Map.Placements) Ref(p.ObjectId, SaveCapability.Resource);
            var player = save.Player;
            Require(player != null && player.SurvivalNights > 0 && player.UnlockedTechnologyIds != null
                && player.UnlockedTechnologyIds.Length <= 32 && player.ResearchElapsed >= 0, "Invalid player/research state.");
            Amounts(player.Wallet); Stats(player.Statistics);
            var unlocked = new HashSet<string>(StringComparer.Ordinal);
            foreach (string tech in player.UnlockedTechnologyIds)
                Require(catalog.HasTechnology(tech) && unlocked.Add(tech), "Unknown/duplicate technology.");
            if (player.ActiveTechnologyId != null)
            {
                Require(catalog.HasTechnology(player.ActiveTechnologyId) && !unlocked.Contains(player.ActiveTechnologyId), "Invalid active technology.");
                var hall = Ref(player.ResearchTownHall, SaveCapability.Depot, true);
                Require(hall.Depot.TownHall && hall.Body.Health > 0, "Research Hall is unavailable.");
            }
            else Require(player.ResearchTownHall == 0, "Inactive research retains a Hall.");
            Require(save.Clock != null && save.Clock.Day >= 1 && save.Clock.Day <= player.SurvivalNights && save.Clock.Phase >= 0
                && save.Clock.Phase <= 2 && save.Clock.Elapsed >= 0, "Invalid clock or terminal match.");
            Require(save.Wave != null && save.Wave.CurrentTownHallRange >= 0 && save.Wave.ProceduralStartingRange >= 0
                && save.Wave.WaveIndex >= 0 && save.Wave.SpawnedInWave >= 0 && save.Wave.StartedWaveCount >= 0
                && save.Wave.FailedSpawnAttempts >= 0 && save.Wave.FallbackSpawnCount >= 0, "Invalid wave.");
            Require(save.Reports != null && save.Reports.PreviousPhase >= 0 && save.Reports.PreviousPhase <= 2
                && save.Reports.CompletedNight >= 0 && save.Reports.CompletedNight < save.Clock.Day
                && save.Reports.LastAutosavedNight >= 0 && save.Reports.LastAutosavedNight <= save.Reports.CompletedNight
                && save.Reports.WarningConsumedDay >= 0 && save.Reports.WarningConsumedDay <= save.Clock.Day, "Invalid reports.");
            Stats(save.Reports.PreviousDawn.Statistics); Stats(save.Reports.LatestNight.Statistics);
            int cells = save.Map.Width * save.Map.Height;
            Require(save.Reports.PreviousDawn.ExploredCells >= 0 && save.Reports.PreviousDawn.ExploredCells <= cells
                && save.Reports.LatestNight.ExploredCells >= 0 && save.Reports.LatestNight.ExploredCells <= cells, "Invalid report exploration.");
            Require(save.View != null && save.View.Zoom > 0 && save.View.Selection != null && save.View.Selection.Length <= MaxObjects, "Invalid camera/selection.");
            var selection = new HashSet<long>();
            foreach (long id in save.View.Selection)
                Require(Ref(id, SaveCapability.Actor, true).Body.Faction == 0 && selection.Add(id), "Invalid selection.");
            Require(save.Objects.Any(o => o.Depot?.TownHall == true && o.Body.Health > 0), "A running save requires a living Hall.");
        }
        static void Parameters(ProjectileParametersData p)
        { Require(p != null && p.Speed >= 0 && p.ArcHeight >= 0 && p.VisualScale >= 0 && p.MinDuration >= 0
            && p.MaxDuration >= p.MinDuration && p.DurationFactor >= 0, "Invalid projectile parameters."); }
        static void Amounts(ResourceAmounts value)
        { Require(value.Wood >= 0 && value.Gold >= 0 && value.Meat >= 0, "Negative resource totals."); }
        static void Stats(StatisticsSaveData s)
        { Amounts(s.Deposited); Amounts(s.Spent); Amounts(s.Granted); Require(s.UnitsLost >= 0 && s.ResearchCompleted >= 0 && s.TownHallDamage >= 0, "Invalid statistics."); }
        static void Map(MapSaveData map)
        {
            Require(map != null && map.Width >= 32 && map.Height >= 32 && map.Width <= MaxDimension && map.Height <= MaxDimension
                && map.CellSize > 0 && map.Attempt >= 0 && (map.GeneratorVersion == 1 || map.GeneratorVersion == 3), "Invalid map geometry/version.");
            int count = map.Width * map.Height;
            bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < map.Width && y < map.Height;
            Require(InBounds(map.StartX, map.StartY) && !string.IsNullOrEmpty(map.SettingsJson) && map.SettingsJson.Length <= 16384, "Invalid map start/settings.");
            foreach (var mask in new[] { map.Terrain, map.Reserved, map.Explored })
                Require(mask != null && mask.Length == count && mask.All(x => x <= 1), "Invalid map/fog mask.");
            Require(map.RegionIds != null && map.Routes != null && map.Regions != null && map.Connections != null && map.Gateways != null
                && map.Placements != null && map.Placements.Length <= MaxObjects, "Missing map topology/placements.");
            if (map.GeneratorVersion == 3)
            {
                Require(map.RegionIds.Length == count && map.RegionIds.All(x => x < 3) && map.Routes.Length == count && map.Routes.All(x => x <= 1)
                    && map.Regions.Length == 3 && map.Connections.Length == 2 && map.Gateways.Length == 2, "Invalid strategic topology.");
                foreach (var r in map.Regions) Require(InBounds(r.X, r.Y) && r.Radius > 0 && r.Role >= 0 && r.Role <= 2, "Invalid region.");
                foreach (var c in map.Connections) Require(c.From >= 0 && c.From < 3 && c.To >= 0 && c.To < 3 && c.From != c.To, "Invalid region connection.");
                foreach (var g in map.Gateways) Require(InBounds(g.X, g.Y) && g.Region >= 0 && g.Region < 3, "Invalid gateway.");
                Require(InBounds(map.Landmark.X, map.Landmark.Y) && map.Landmark.Radius > 0 && map.Landmark.Region >= 0 && map.Landmark.Region < 3, "Invalid landmark.");
            }
            else Require(map.RegionIds.Length == 0 && map.Routes.Length == 0 && map.Regions.Length == 0 && map.Connections.Length == 0 && map.Gateways.Length == 0, "Unexpected legacy topology.");
            var ids = new HashSet<long>();
            foreach (var p in map.Placements)
                Require(InBounds(p.X, p.Y) && p.Kind >= 0 && p.Kind <= 2 && p.VariantId >= 0 && p.ClusterId >= 0
                    && p.ObjectId >= 0 && (p.ObjectId == 0 || ids.Add(p.ObjectId)), "Invalid/duplicate resource placement.");
        }
        // Covers every scalar, including newly added DTO fields; no scene reflection or type loading.
        static void ValidateNumbers(object value, int depth = 0)
        {
            Require(depth <= 24, "Snapshot nesting limit exceeded.");
            if (value == null || value is string || value is byte[] || value is long || value is int || value is bool) return;
            if (value is float f)
            { Require(!float.IsNaN(f) && !float.IsInfinity(f) && Math.Abs(f) <= 1e9f, "Invalid/non-finite numeric value."); return; }
            if (value is Array array)
            { Require(array.Length <= MaxDimension * MaxDimension, "Array limit exceeded."); foreach (var item in array) ValidateNumbers(item, depth + 1); return; }
            foreach (var field in value.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance)) ValidateNumbers(field.GetValue(value), depth + 1);
        }
    }
}
