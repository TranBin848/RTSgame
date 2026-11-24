using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace RTS.SaveLoad
{
    public sealed class SaveCodec
    {
        readonly SaveCatalog m_Catalog;
        static readonly UTF8Encoding Utf8 = new(false, true);
        sealed class RequiredFields : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization serialization)
            {
                var property = base.CreateProperty(member, serialization);
                property.Required = Required.AllowNull;
                return property;
            }
        }
        static JsonSerializer Serializer() => JsonSerializer.Create(new JsonSerializerSettings
        {
            ContractResolver = new RequiredFields(), TypeNameHandling = TypeNameHandling.None,
            MissingMemberHandling = MissingMemberHandling.Error, DateParseHandling = DateParseHandling.None,
            Culture = CultureInfo.InvariantCulture, MaxDepth = 24, FloatParseHandling = FloatParseHandling.Double
        });
        public SaveCodec(SaveCatalog catalog) { m_Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog)); }
        public byte[] Encode(MatchSaveData snapshot)
        {
            SaveValidator.Validate(snapshot, m_Catalog);
            var serializer = Serializer();
            string payload = JObject.FromObject(snapshot, serializer).ToString(Formatting.None);
            byte[] bytes = Utf8.GetBytes(payload);
            var envelope = new JObject
            {
                ["format"] = "RTS.MatchSave", ["schemaVersion"] = SaveValidator.SchemaVersion,
                ["payloadBytes"] = bytes.Length, ["checksum"] = SaveCatalog.Hash(bytes), ["payload"] = payload
            };
            byte[] result = Utf8.GetBytes(envelope.ToString(Formatting.None));
            SaveValidator.Require(result.Length <= SaveValidator.MaxFileBytes, "Save file size limit exceeded.");
            return result;
        }
        public MatchSaveData Decode(byte[] bytes)
        {
            SaveValidator.Require(bytes != null && bytes.Length > 0 && bytes.Length <= SaveValidator.MaxFileBytes, "Invalid save file size.");
            try
            {
                var envelope = Parse(Utf8.GetString(bytes));
                SaveValidator.Require(envelope.Count == 5 && (string)envelope["format"] == "RTS.MatchSave"
                    && envelope["schemaVersion"]?.Type == JTokenType.Integer
                    && (int)envelope["schemaVersion"] == SaveValidator.SchemaVersion
                    && envelope["payloadBytes"]?.Type == JTokenType.Integer
                    && envelope["checksum"]?.Type == JTokenType.String && envelope["payload"]?.Type == JTokenType.String, "Invalid file envelope/schema.");
                string payload = (string)envelope["payload"];
                byte[] payloadBytes = Utf8.GetBytes(payload);
                SaveValidator.Require((long)envelope["payloadBytes"] == payloadBytes.Length
                    && (string)envelope["checksum"] == SaveCatalog.Hash(payloadBytes), "Save checksum/size mismatch.");
                var token = Parse(payload);
                ValidateTokens(token);
                ValidateShape(token, typeof(MatchSaveData));
                var snapshot = token.ToObject<MatchSaveData>(Serializer());
                SaveValidator.Validate(snapshot, m_Catalog);
                return snapshot;
            }
            catch (Exception error) when (error is JsonException || error is DecoderFallbackException
                || error is FormatException || error is OverflowException || error is InvalidCastException || error is ArgumentException)
            { throw new SaveValidationException("Invalid save JSON: " + error.Message); }
        }
        static JObject Parse(string text)
        {
            using var input = new StringReader(text);
            using var reader = new JsonTextReader(input) { MaxDepth = 24, DateParseHandling = DateParseHandling.None, FloatParseHandling = FloatParseHandling.Double };
            var result = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error, CommentHandling = CommentHandling.Load });
            SaveValidator.Require(!reader.Read(), "Trailing JSON content.");
            return result;
        }
        static void ValidateTokens(JToken token)
        {
            if (token is JArray array) SaveValidator.Require(array.Count <= SaveValidator.MaxDimension * SaveValidator.MaxDimension, "JSON array limit exceeded.");
            if (token.Type == JTokenType.Float)
            {
                double number = (double)token;
                SaveValidator.Require(!double.IsNaN(number) && !double.IsInfinity(number), "Non-finite JSON number.");
            }
            SaveValidator.Require(token.Type != JTokenType.Comment && token.Type != JTokenType.Constructor
                && token.Type != JTokenType.Undefined, "Unsupported JSON token.");
            foreach (var child in token.Children()) ValidateTokens(child);
        }
        static void ValidateShape(JToken token, Type type)
        {
            if (token.Type == JTokenType.Null)
            { SaveValidator.Require(!type.IsValueType, "Null scalar in save."); return; }
            if (type == typeof(string))
            { SaveValidator.Require(token.Type == JTokenType.String, "Expected JSON string."); return; }
            if (type == typeof(bool))
            { SaveValidator.Require(token.Type == JTokenType.Boolean, "Expected JSON boolean."); return; }
            if (type == typeof(int) || type == typeof(long))
            {
                SaveValidator.Require(token.Type == JTokenType.Integer, "Expected JSON integer.");
                long value = (long)token;
                SaveValidator.Require(type != typeof(int) || value >= int.MinValue && value <= int.MaxValue, "Integer out of range.");
                return;
            }
            if (type == typeof(float))
            { SaveValidator.Require(token.Type == JTokenType.Float || token.Type == JTokenType.Integer, "Expected JSON number."); return; }
            if (type == typeof(byte[]))
            {
                SaveValidator.Require(token.Type == JTokenType.String && ((string)token).Length <= (SaveValidator.MaxDimension * SaveValidator.MaxDimension + 2) / 3 * 4, "Invalid/oversized binary mask.");
                return;
            }
            if (type.IsArray)
            {
                int maximum = type.GetElementType() == typeof(TrainingSaveData) ? SaveValidator.MaxQueue
                    : type.GetElementType() == typeof(GatheringSaveData) ? 3 : SaveValidator.MaxObjects;
                SaveValidator.Require(token is JArray array && array.Count <= maximum, "Invalid/oversized typed array.");
                foreach (var child in token.Children()) ValidateShape(child, type.GetElementType());
                return;
            }
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            SaveValidator.Require(token is JObject obj && obj.Count == fields.Length, "Missing or unknown snapshot field.");
            foreach (var field in fields)
            {
                var child = token[field.Name];
                SaveValidator.Require(child != null, "Missing snapshot field: " + field.Name);
                ValidateShape(child, field.FieldType);
            }
        }
    }
}
