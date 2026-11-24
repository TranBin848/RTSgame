using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace RTS.SaveLoad.Tests
{
    public class SaveLoadTests
    {
        SaveCatalog m_Catalog;
        SaveCodec m_Codec;
        string m_Directory;
        string Primary => Path.Combine(m_Directory, "manual.json");
        [SetUp] public void Setup()
        {
            m_Catalog = Catalog("{\"clock\":[60,30,45],\"research\":\"fixture\"}");
            m_Codec = new SaveCodec(m_Catalog);
            m_Directory = Path.Combine(Path.GetTempPath(), "RTS-SaveLoadTests-" + Guid.NewGuid().ToString("N"));
        }
        [TearDown] public void Cleanup() { if (Directory.Exists(m_Directory)) Directory.Delete(m_Directory, true); }
        static SaveCatalog Catalog(string config) => new(new[]
        {
            new SaveDefinition("hall", SaveCapability.Body | SaveCapability.Actor | SaveCapability.Depot | SaveCapability.Construction | SaveCapability.Producer),
            new SaveDefinition("worker", SaveCapability.Body | SaveCapability.Actor | SaveCapability.Worker),
            new SaveDefinition("tree", SaveCapability.Resource),
            new SaveDefinition("arrow", SaveCapability.Projectile)
        }, new[] { "train_worker", "build_hall" }, new[] { "economy_tools" }, config);
        public MatchSaveData Fixture(long generation = 1)
        {
            return new MatchSaveData
            {
                SchemaVersion = 1, SaveId = Guid.NewGuid().ToString("N"), CapturedUtc = DateTimeOffset.UtcNow.ToString("O"),
                ContentFingerprint = m_Catalog.Fingerprint, Generation = generation, NextSaveId = 100, NextRequestId = 20,
                Map = new MapSaveData
                {
                    Width = 32, Height = 32, GeneratorVersion = 1, StartX = 16, StartY = 16, CellSize = 1,
                    SettingsJson = "{\"Seed\":1}", Terrain = Enumerable.Repeat((byte)1, 1024).ToArray(), Reserved = new byte[1024],
                    Explored = new byte[1024], RegionIds = Array.Empty<byte>(), Routes = Array.Empty<byte>(), Regions = Array.Empty<RegionSaveData>(),
                    Connections = Array.Empty<ConnectionSaveData>(), Gateways = Array.Empty<GatewaySaveData>(),
                    Placements = new[] { new PlacementSaveData { ObjectId = 3, X = 20, Y = 20 } }
                },
                Objects = new[]
                {
                    new ObjectSaveData { Id = 1, DefinitionId = "hall", MaxHealth = 1200, Body = Body(), Actor = Actor(4),
                        Depot = new DepotSaveData { ResourceMask = 7, TownHall = true }, Construction = new ConstructionSaveData(),
                        Producer = new ProducerSaveData { Queue = new[] { new TrainingSaveData { RequestId = 10, Duration = 4, ActionId = "train_worker", PrefabId = "worker" } }, CurrentRequest = 10, Elapsed = 1.5f } },
                    new ObjectSaveData { Id = 2, DefinitionId = "worker", MaxHealth = 100, Body = Body(), Actor = Actor(0),
                        Worker = new WorkerSaveData { Node = 3, Depot = 1, Phase = 2, ResourceType = 0, Carried = 7,
                            ResumeResource = 3, Timer = .75f, Profiles = new[] { new GatheringSaveData { ResourceType = 0, Amount = 2, Capacity = 10, TickTime = 1, State = 3, Task = 2 } } } },
                    new ObjectSaveData { Id = 3, DefinitionId = "tree", Resource = new ResourceSaveData { Type = 0, Radius = 1, Capacity = 4, Claims = 1 } },
                    new ObjectSaveData { Id = 4, DefinitionId = "arrow", Projectile = new ProjectileSaveData { Owner = 2, Target = 1,
                        Position = new SaveVector { X = 2, Y = 3 }, Age = .5f, Duration = 2, Speed = 5, Lifetime = 12, Radius = .08f,
                        Damage = 10, VisualScale = 1 } }
                },
                Player = new PlayerSaveData { Wallet = new ResourceAmounts { Wood = 73, Gold = 41, Meat = 19 },
                    UnlockedTechnologyIds = Array.Empty<string>(), ActiveTechnologyId = "economy_tools", ResearchTownHall = 1, ResearchElapsed = 4.2f, SurvivalNights = 5 },
                Clock = new ClockSaveData { Day = 2, Phase = 2, Elapsed = 12.25f },
                Wave = new WaveSaveData { WaveIndex = 1, SpawnedInWave = 3, StartedWaveCount = 2, SpawnTimer = .3f, DelayTimer = -.01f, WaveSeed = 8001, ProceduralStartingRange = 12 },
                Reports = new ReportSaveData { PreviousPhase = 2, CompletedNight = 1, LastAutosavedNight = 1, WarningConsumedDay = 2 },
                View = new ViewSaveData { Zoom = 9, Selection = new long[] { 2 } }
            };
        }
        static BodySaveData Body() => new() { Radius = .25f, Health = 100, Targetable = true };
        static ActorSaveData Actor(int kind) => new() { Kind = kind, Interval = 1, Windup = -1, RandomSeed = 123,
            Projectile = new ProjectileParametersData { Speed = 10, VisualScale = 1 } };
        [Test] public void DtoRoundTripPreservesEveryFieldAndLargeIds()
        {
            var value = Fixture();
            value.Objects[3].Id = 9007199254740993L; value.NextSaveId = long.MaxValue - 1;
            value.Player.Statistics.Deposited.Gold = 9007199254740993L;
            var decoded = m_Codec.Decode(m_Codec.Encode(value));
            Assert.That(JsonConvert.SerializeObject(decoded), Is.EqualTo(JsonConvert.SerializeObject(value)));
        }
        [TestCase("duplicate")] [TestCase("unknown")] [TestCase("dangling")] [TestCase("negativeId")]
        [TestCase("wrongReference")] [TestCase("nan")] [TestCase("infinity")] [TestCase("overflow")]
        [TestCase("mapSize")] [TestCase("mask")] [TestCase("schema")] [TestCase("config")]
        [TestCase("queue")] [TestCase("request")] [TestCase("technology")] [TestCase("capabilities")]
        [TestCase("cargo")] [TestCase("shelter")] [TestCase("claims")] [TestCase("construction")]
        public void InvalidStateIsRejected(string fault)
        {
            var value = Fixture();
            switch (fault)
            {
                case "duplicate": value.Objects[1].Id = 1; break;
                case "unknown": value.Objects[1].DefinitionId = "../../arbitrary"; break;
                case "dangling": value.Objects[1].Worker.Node = 99; break;
                case "negativeId": value.Objects[0].Id = -1; break;
                case "wrongReference": value.Objects[1].Worker.Node = 1; break;
                case "nan": value.Objects[0].Actor.Windup = float.NaN; break;
                case "infinity": value.View.Zoom = float.PositiveInfinity; break;
                case "overflow": value.NextSaveId = long.MaxValue; break;
                case "mapSize": value.Map.Width = int.MaxValue; break;
                case "mask": value.Map.Explored[0] = 2; break;
                case "schema": value.SchemaVersion++; break;
                case "config": value.ContentFingerprint = "changed"; break;
                case "queue": value.Objects[0].Producer.Queue = new TrainingSaveData[SaveValidator.MaxQueue + 1]; break;
                case "request": value.NextRequestId = 10; break;
                case "technology": value.Player.ActiveTechnologyId = "unknown"; break;
                case "capabilities": value.Objects[1].Resource = new ResourceSaveData(); break;
                case "cargo": value.Objects[1].Worker.Carried = -1; break;
                case "shelter": value.Objects[1].Worker.Phase = 5; value.Objects[1].Worker.Shelter = 1; break;
                case "claims": value.Objects[2].Resource.Claims = 5; break;
                case "construction": value.Objects[0].Construction.Active = true; break;
            }
            Assert.Throws<SaveValidationException>(() => m_Codec.Encode(value));
        }
        byte[] Repack(Action<JObject> mutate)
        {
            var envelope = JObject.Parse(Encoding.UTF8.GetString(m_Codec.Encode(Fixture())));
            var payload = JObject.Parse((string)envelope["payload"]); mutate(payload);
            string text = payload.ToString(Formatting.None); var bytes = Encoding.UTF8.GetBytes(text);
            envelope["payload"] = text; envelope["payloadBytes"] = bytes.Length; envelope["checksum"] = SaveCatalog.Hash(bytes);
            return Encoding.UTF8.GetBytes(envelope.ToString(Formatting.None));
        }
        [Test] public void MissingFieldDoesNotSilentlyDefault() => Assert.Throws<SaveValidationException>(() => m_Codec.Decode(Repack(x => x.Remove("Clock"))));
        [Test] public void UnknownAndTypeMetadataRejected() => Assert.Throws<SaveValidationException>(() => m_Codec.Decode(Repack(x => x["$type"] = "System.IO.FileInfo")));
        [Test] public void ChecksumDoesNotReplaceSemanticValidation() => Assert.Throws<SaveValidationException>(() => m_Codec.Decode(Repack(x => x["Objects"][1]["Id"] = 1)));
        [Test] public void FractionalIntegerIsNotRounded() => Assert.Throws<SaveValidationException>(() => m_Codec.Decode(Repack(x => x["Objects"][1]["Id"] = 2.4)));
        [Test] public void NumericStringIsNotCoerced() => Assert.Throws<SaveValidationException>(() => m_Codec.Decode(Repack(x => x["NextSaveId"] = "100")));
        [Test] public void MissingZeroFieldIsRejected() => Assert.Throws<SaveValidationException>(() => m_Codec.Decode(Repack(x => ((JObject)x["Wave"]).Remove("FailedSpawnAttempts"))));
        [Test] public void ObjectCountIsBounded()
        {
            var value = Fixture(); value.Objects = new ObjectSaveData[SaveValidator.MaxObjects + 1];
            Assert.Throws<SaveValidationException>(() => m_Codec.Encode(value));
        }
        [Test] public void FinishedConstructionKeepsOneTickOvershoot()
        {
            var value = Fixture(); value.Objects[0].Construction = new ConstructionSaveData { RequestId = 12, ActionId = "build_hall", Duration = 2,
                Elapsed = 2.016f, Middle = true, Finished = true };
            Assert.That(m_Codec.Decode(m_Codec.Encode(value)).Objects[0].Construction.Elapsed, Is.EqualTo(2.016f));
        }
        [TestCase("")] [TestCase("{}")] [TestCase("{\"format\":\"a\",\"format\":\"b\"}")]
        [TestCase("{}{}")]
        public void BrokenJsonRejected(string json) => Assert.Throws<SaveValidationException>(() => m_Codec.Decode(Encoding.UTF8.GetBytes(json)));
        [Test] public void CorruptionRejected()
        {
            var bytes = m_Codec.Encode(Fixture()); bytes[bytes.Length / 2] ^= 1;
            Assert.Throws<SaveValidationException>(() => m_Codec.Decode(bytes));
            Assert.Throws<SaveValidationException>(() => m_Codec.Decode(new byte[] { 255, 254 }));
        }
        [Test] public void CatalogFingerprintIsOrderStableButConfigSensitive()
        {
            var catalog = new SaveCatalog(new[] { new SaveDefinition("b", SaveCapability.Resource), new SaveDefinition("a", SaveCapability.Body) }, new[] { "b", "a" }, Array.Empty<string>(), "config");
            var reordered = new SaveCatalog(new[] { new SaveDefinition("a", SaveCapability.Body), new SaveDefinition("b", SaveCapability.Resource) }, new[] { "a", "b" }, Array.Empty<string>(), "config");
            Assert.That(catalog.Fingerprint, Is.EqualTo(reordered.Fingerprint));
            Assert.That(Catalog("changed").Fingerprint, Is.Not.EqualTo(m_Catalog.Fingerprint));
            Assert.Throws<ArgumentException>(() => new SaveCatalog(new[] { new SaveDefinition("a", SaveCapability.Body), new SaveDefinition("a", SaveCapability.Body) }, Array.Empty<string>(), Array.Empty<string>(), "config"));
        }
        [Test] public void FirstWriteThenReplacementKeepsPreviousGeneration()
        {
            var storage = new SaveSlotStorage(m_Directory, m_Codec);
            storage.Write(SaveSlot.Manual, Fixture());
            Assert.That(storage.Read(SaveSlot.Manual).Snapshot.Generation, Is.EqualTo(1));
            Assert.That(File.Exists(Primary + ".bak"), Is.False);
            storage.Write(SaveSlot.Manual, Fixture(2)); storage.Write(SaveSlot.Manual, Fixture(3));
            Assert.That(storage.Read(SaveSlot.Manual).Snapshot.Generation, Is.EqualTo(3));
            Assert.That(m_Codec.Decode(File.ReadAllBytes(Primary + ".bak")).Generation, Is.EqualTo(2));
            Assert.That(File.Exists(Primary + ".tmp"), Is.False);
        }
        [TestCase(SaveCommitStage.TempCreated)] [TestCase(SaveCommitStage.TempWritten)] [TestCase(SaveCommitStage.TempFlushed)]
        [TestCase(SaveCommitStage.TempVerified)] [TestCase(SaveCommitStage.BeforeCommit)] [TestCase(SaveCommitStage.Committed)]
        public void CommitFaultKeepsAtLeastOneVerifiedGeneration(SaveCommitStage fault)
        {
            var initial = new SaveSlotStorage(m_Directory, m_Codec);
            initial.Write(SaveSlot.Manual, Fixture()); initial.Write(SaveSlot.Manual, Fixture(2));
            byte[] before = File.ReadAllBytes(Primary), backup = File.ReadAllBytes(Primary + ".bak");
            var storage = new SaveSlotStorage(m_Directory, m_Codec, stage => { if (stage == fault) throw new IOException("Injected " + stage); });
            Assert.Throws<IOException>(() => storage.Write(SaveSlot.Manual, Fixture(3)));
            Assert.That(storage.Phase, Is.EqualTo(SaveOperationPhase.Failed));
            if (fault == SaveCommitStage.Committed)
            {
                Assert.That(initial.Read(SaveSlot.Manual).Snapshot.Generation, Is.EqualTo(3));
                Assert.That(File.ReadAllBytes(Primary + ".bak"), Is.EqualTo(before));
            }
            else
            {
                Assert.That(File.ReadAllBytes(Primary), Is.EqualTo(before));
                Assert.That(File.ReadAllBytes(Primary + ".bak"), Is.EqualTo(backup));
            }
            initial.Write(SaveSlot.Manual, Fixture(4));
            Assert.That(initial.Read(SaveSlot.Manual).Snapshot.Generation, Is.EqualTo(4));
        }
        [Test] public void CorruptPrimaryUsesBackupWithoutModifyingEitherFile()
        {
            var storage = new SaveSlotStorage(m_Directory, m_Codec);
            storage.Write(SaveSlot.Manual, Fixture()); storage.Write(SaveSlot.Manual, Fixture(2));
            File.WriteAllText(Primary, "truncated"); byte[] backup = File.ReadAllBytes(Primary + ".bak");
            var result = storage.Read(SaveSlot.Manual);
            Assert.That(result.UsedBackup, Is.True); Assert.That(result.Snapshot.Generation, Is.EqualTo(1)); Assert.That(result.PrimaryError, Is.Not.Empty);
            Assert.That(File.ReadAllText(Primary), Is.EqualTo("truncated")); Assert.That(File.ReadAllBytes(Primary + ".bak"), Is.EqualTo(backup));
            storage.Write(SaveSlot.Manual, Fixture(3));
            Assert.That(storage.Read(SaveSlot.Manual).Snapshot.Generation, Is.EqualTo(3));
            Assert.That(File.ReadAllBytes(Primary + ".bak"), Is.EqualTo(backup));
            File.WriteAllText(Primary, "broken");
            File.WriteAllText(Primary + ".bak", "broken");
            Assert.Throws<SaveValidationException>(() => storage.Read(SaveSlot.Manual));
        }
        [Test] public void SizeLimitIsCheckedBeforeReadingWholeFile()
        {
            Directory.CreateDirectory(m_Directory);
            using (var stream = File.Create(Primary)) stream.SetLength(SaveValidator.MaxFileBytes + 1L);
            Assert.Throws<SaveValidationException>(() => new SaveSlotStorage(m_Directory, m_Codec).Read(SaveSlot.Manual));
        }
        [Test] public void DoubleRequestAndSecondStorageInstanceAreBlocked()
        {
            var other = new SaveSlotStorage(m_Directory, m_Codec);
            SaveSlotStorage storage = null;
            storage = new SaveSlotStorage(m_Directory, m_Codec, stage =>
            {
                if (stage != SaveCommitStage.BeforeCommit) return;
                Assert.Throws<InvalidOperationException>(() => storage.Read(SaveSlot.Manual));
                Assert.Throws<IOException>(() => other.Write(SaveSlot.Dawn, Fixture()));
            });
            storage.Write(SaveSlot.Manual, Fixture());
            Assert.That(storage.Phase, Is.EqualTo(SaveOperationPhase.Idle));
            other.Write(SaveSlot.Dawn, Fixture());
            Assert.That(other.Read(SaveSlot.Dawn).Snapshot.Generation, Is.EqualTo(1));
        }
        [Test] public void NoSaveAndInvalidSlotDoNotCreateFiles()
        {
            var storage = new SaveSlotStorage(m_Directory, m_Codec);
            Assert.Throws<FileNotFoundException>(() => storage.Read(SaveSlot.Manual));
            Assert.Throws<ArgumentOutOfRangeException>(() => storage.Write((SaveSlot)99, Fixture()));
            Assert.That(Directory.Exists(m_Directory), Is.False);
        }
        [Test] public void GenerationCannotRegressOrReplaceWithInvalidData()
        {
            var storage = new SaveSlotStorage(m_Directory, m_Codec);
            storage.Write(SaveSlot.Manual, Fixture(3)); byte[] before = File.ReadAllBytes(Primary);
            Assert.Throws<SaveValidationException>(() => storage.Write(SaveSlot.Manual, Fixture(2)));
            var invalid = Fixture(4); invalid.Player.Wallet.Wood = -1;
            Assert.Throws<SaveValidationException>(() => storage.Write(SaveSlot.Manual, invalid));
            Assert.That(File.ReadAllBytes(Primary), Is.EqualTo(before));
        }
    }
}
