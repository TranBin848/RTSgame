using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace RTS.SaveLoad
{
    [Flags] public enum SaveCapability { Body = 1, Actor = 2, Worker = 4, Resource = 8, Depot = 16, Construction = 32, Producer = 64, Enemy = 128, Projectile = 256 }
    public sealed class SaveDefinition
    {
        public readonly string Id;
        public readonly SaveCapability Capabilities;
        public SaveDefinition(string id, SaveCapability capabilities) { Id = id; Capabilities = capabilities; }
    }
    // Built by the composition adapter from allowed authoring assets, never from a save file.
    public sealed class SaveCatalog
    {
        readonly Dictionary<string, SaveDefinition> m_Definitions;
        readonly HashSet<string> m_Actions, m_Technologies;
        public string Fingerprint { get; }
        public SaveCatalog(IEnumerable<SaveDefinition> definitions, IEnumerable<string> actions,
            IEnumerable<string> technologies, string configurationJson)
        {
            m_Definitions = new Dictionary<string, SaveDefinition>(StringComparer.Ordinal);
            foreach (var definition in definitions ?? throw new ArgumentNullException(nameof(definitions)))
            {
                if (definition == null || !ValidId(definition.Id) || definition.Capabilities == 0
                    || ((int)definition.Capabilities & ~511) != 0 || m_Definitions.ContainsKey(definition.Id))
                    throw new ArgumentException("Invalid or duplicate save definition.");
                m_Definitions.Add(definition.Id, definition);
            }
            if (m_Definitions.Count == 0) throw new ArgumentException("Empty save catalog.");
            m_Actions = IdSet(actions); m_Technologies = IdSet(technologies);
            if (string.IsNullOrWhiteSpace(configurationJson)) throw new ArgumentException("Content configuration is required.");
            var manifest = new StringBuilder("RTS.SaveLoad/1\n");
            foreach (var item in m_Definitions.OrderBy(x => x.Key, StringComparer.Ordinal))
                manifest.Append(item.Key).Append(':').Append((int)item.Value.Capabilities).Append('\n');
            foreach (string id in m_Actions.OrderBy(x => x, StringComparer.Ordinal)) manifest.Append("action:").Append(id).Append('\n');
            foreach (string id in m_Technologies.OrderBy(x => x, StringComparer.Ordinal)) manifest.Append("tech:").Append(id).Append('\n');
            manifest.Append(configurationJson);
            Fingerprint = Hash(Encoding.UTF8.GetBytes(manifest.ToString()));
        }
        static HashSet<string> IdSet(IEnumerable<string> ids)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in ids ?? throw new ArgumentNullException(nameof(ids)))
                if (!ValidId(id) || !result.Add(id)) throw new ArgumentException("Invalid or duplicate catalog ID.");
            return result;
        }
        public static bool ValidId(string id) => !string.IsNullOrEmpty(id) && id.Length <= 96
            && id.All(c => c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '_' || c == '-' || c == '.');
        public bool TryDefinition(string id, out SaveDefinition definition) => m_Definitions.TryGetValue(id ?? "", out definition);
        public bool HasAction(string id) => id != null && m_Actions.Contains(id);
        public bool HasTechnology(string id) => id != null && m_Technologies.Contains(id);
        public static string Hash(byte[] data)
        { using var hash = SHA256.Create(); return BitConverter.ToString(hash.ComputeHash(data)).Replace("-", "").ToLowerInvariant(); }
    }
    public sealed class SaveValidationException : Exception
    { public SaveValidationException(string message) : base(message) { } }
}
