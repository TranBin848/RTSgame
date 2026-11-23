// Unity MCP script-execute: G4Trace.Run(directory).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditorInternal;

public static class G4Trace
{
    public static string Run(string directory)
    {
        int sampleCount = 0;
        var captures = File.Exists(directory) ? new[] { directory } : Directory.GetFiles(directory, "*.raw");
        foreach (var file in captures)
        {
            if (!ProfilerDriver.LoadProfile(file, false))
                throw new InvalidOperationException("Cannot load " + file);
            using var output = new StreamWriter(Path.ChangeExtension(file, ".tsv"));
            output.WriteLine("frame\tthread\tframeMs\tsampleMs\tselfMs\tgcBytes\tpath\tstack");
            for (int frame = ProfilerDriver.firstFrameIndex; frame >= 0 && frame <= ProfilerDriver.lastFrameIndex; frame++)
            {
                for (int thread = 0; ; thread++)
                {
                    using var view = ProfilerDriver.GetRawFrameDataView(frame, thread);
                    if (!view.valid) break;
                    var ancestors = new List<(int end, string name)>();
                    var callstack = new List<ulong>();
                    for (int i = 0; i < view.sampleCount; i++)
                    {
                        while (ancestors.Count > 0 && ancestors[^1].end < i)
                            ancestors.RemoveAt(ancestors.Count - 1);
                        var name = view.GetSampleName(i) ?? "(unnamed)";
                        int descendants = view.GetSampleChildrenCountRecursive(i);
                        double ms = view.GetSampleTimeMs(i);
                        bool alloc = name == "GC.Alloc";
                        if (alloc || ms >= 0.75 || name.StartsWith("RTS.", StringComparison.Ordinal))
                        {
                            double selfMs = ms;
                            for (int child = i + 1; child <= i + descendants;)
                            {
                                selfMs -= view.GetSampleTimeMs(child);
                                child += 1 + view.GetSampleChildrenCountRecursive(child);
                            }
                            long bytes = alloc ? view.GetSampleMetadataAsLong(i, 0) : 0;
                            callstack.Clear();
                            if (alloc) view.GetSampleCallstack(i, callstack);
                            string stack = string.Join(" > ", callstack.Select(address => view.ResolveMethodInfo(address).methodName));
                            string path = string.Join("/", ancestors.Select(a => a.name).Append(name));
                            output.WriteLine(FormattableString.Invariant(
                                $"{frame}\t{view.threadName}\t{view.frameTimeMs:F4}\t{ms:F4}\t{selfMs:F4}\t{bytes}\t{path}\t{stack.Replace('\n', ' ').Replace('\t', ' ')}"));
                            sampleCount++;
                        }
                        if (descendants > 0) ancestors.Add((i + descendants, name));
                    }
                }
            }
        }
        if (sampleCount == 0) throw new InvalidOperationException("Profiler export has no samples.");
        return $"Exported {sampleCount} profiler samples.";
    }
}
