using System;
using System.Collections;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RTS.DOTS;
using RTS.SaveLoad;
using UnityEngine;
using UnityEngine.SceneManagement;

// Explicit CLI test path only. Normal player/menu behavior never creates this component.
public sealed class SL4PlayerSmoke : MonoBehaviour
{
    string m_Mode, m_Folder;
    int m_Seed;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        var args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "--sl4-smoke");
        if (index < 0) return;
        if (index + 3 >= args.Length) { Application.Quit(2); return; }
        var runner = new GameObject("SL4 Player smoke").AddComponent<SL4PlayerSmoke>();
        DontDestroyOnLoad(runner.gameObject);
        runner.m_Mode = args[index + 1]; runner.m_Folder = Path.GetFullPath(args[index + 2]); runner.m_Seed = int.Parse(args[index + 3]);
        runner.StartCoroutine(runner.Run());
    }
    IEnumerator Run()
    {
        double deadline = Time.realtimeSinceStartupAsDouble + 45;
        Directory.CreateDirectory(m_Folder); Application.runInBackground = true;
        try
        {
            if (m_Mode == "capture")
            { ProceduralMapAuthoring.BenchmarkSeedOverride = m_Seed; SceneManager.LoadScene("ProceduralGame"); }
            else if (m_Mode == "load")
                MatchLoadBootstrap.LoadFromMenu(File.ReadAllBytes(Path.Combine(m_Folder, "Source.json")), Resources.Load<SaveContentCatalog>("ProceduralSaveCatalog"));
            else throw new ArgumentException("Unknown SL4 smoke mode.");
        }
        catch (Exception error) { Fail(error); yield break; }
        GameSceneCompositionRoot root = null;
        while (root == null || !root.IsReady || MatchLoadBootstrap.IsLoading)
        {
            root = UnityEngine.Object.FindFirstObjectByType<GameSceneCompositionRoot>();
            if (Time.realtimeSinceStartupAsDouble > deadline || root != null && root.LastError != null || MatchLoadBootstrap.LastError != null)
            { Fail(new Exception(root != null ? root.LastError ?? MatchLoadBootstrap.LastError ?? "Ready timeout" : "No scene root")); yield break; }
            yield return null;
        }
        try
        {
            Time.timeScale = 0;
            if (m_Mode == "capture")
            {
                var bridge = root.GetComponent<SwarmGameBridge>(); var content = Resources.Load<SaveContentCatalog>("ProceduralSaveCatalog");
                bridge.Gameplay.AddResources(37, -11, 23);
                var resource = UnityEngine.Object.FindObjectsByType<ResourceNodeBase>(FindObjectsSortMode.None).First();
                bridge.UnregisterResource(resource); Destroy(resource.gameObject);
                bridge.SpawnGameplay(content.ResolvePrefab("storagepit"), new Vector3(8, 8), Quaternion.identity);
                var hall = UnityEngine.Object.FindFirstObjectByType<TownHall>();
                var body = bridge.Gameplay.EntityManager.GetComponentData<CombatBody>(hall.DotsEntity);
                if (!bridge.Simulation.TryResolveSpawnPosition(body.Position + new Unity.Mathematics.float2(7, 0), .25f, out var position)) throw new Exception("Spawn fixture failed.");
                bridge.Simulation.SpawnEnemy(position, EnemySpawnSettings.Default);
            }
            if (!root.SaveCapture.RequestCapture(m_Mode == "capture" ? 1 : 2)) throw new Exception("Capture refused.");
        }
        catch (Exception error) { Fail(error); yield break; }
        while (root.SaveCapture.IsPending || root.SaveCapture.IsCapturing)
        {
            if (Time.realtimeSinceStartupAsDouble > deadline) { Fail(new Exception("Capture timeout.")); yield break; }
            yield return null;
        }
        try
        {
            var service = root.SaveCapture;
            if (service.LastSnapshot == null) throw new Exception(service.LastError);
            var codec = new SaveCodec(service.Catalog); var saved = service.LastSnapshot;
            if (m_Mode == "capture")
            {
                // Make final terrain differ from seed output; a seed-only loader must fail comparison.
                saved.Map.Terrain[0] = (byte)(saved.Map.Terrain[0] == 0 ? 1 : 0);
                File.WriteAllBytes(Path.Combine(m_Folder, "Source.json"), codec.Encode(saved));
            }
            else
            {
                var original = codec.Decode(File.ReadAllBytes(Path.Combine(m_Folder, "Source.json")));
                var a = JObject.FromObject(original); var b = JObject.FromObject(saved);
                foreach (string key in new[] { "SaveId", "CapturedUtc", "Generation" }) { a.Remove(key); b.Remove(key); }
                if (!JToken.DeepEquals(a, b))
                { File.WriteAllText(Path.Combine(m_Folder, "Mismatch.json"), JsonConvert.SerializeObject(new { expected = a, actual = b })); throw new Exception("Cold-process canonical state mismatch."); }
                if (!MatchLoadBootstrap.LastLoadSucceeded || !root.GetComponent<SwarmGameBridge>().Simulation.Paused) throw new Exception("Loaded state/pause mismatch.");
            }
            File.WriteAllText(Path.Combine(m_Folder, m_Mode + ".json"), JsonConvert.SerializeObject(new { passed = true, mode = m_Mode,
                seed = saved.Map.Seed, objects = saved.Objects.Length, resources = saved.Objects.Count(x => x.Resource != null),
                missingResourcePlacements = saved.Map.Placements.Count(x => x.ObjectId == 0), wallet = saved.Player.Wallet,
                paused = Time.timeScale == 0, processId = System.Diagnostics.Process.GetCurrentProcess().Id }, Formatting.Indented));
            Application.Quit(0);
        }
        catch (Exception error) { Fail(error); }
    }
    void Fail(Exception error)
    {
        File.WriteAllText(Path.Combine(m_Folder, m_Mode + "-error.json"), JsonConvert.SerializeObject(new { passed = false, error = error.ToString() }, Formatting.Indented));
        Application.Quit(1);
    }
}
