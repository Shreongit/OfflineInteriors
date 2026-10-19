
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using OfflineInteriors.Logging;
using GTA.Math;

namespace OfflineInteriors.Interiors
{
    public sealed class InteriorCatalog
    {
        private readonly Dictionary<string, InteriorDefinition> _interiors =
            new Dictionary<string, InteriorDefinition>(
                StringComparer.OrdinalIgnoreCase);

        public int Count => _interiors.Count;

        public bool Register(InteriorDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            if (_interiors.ContainsKey(definition.Id))
                return false;

            _interiors.Add(definition.Id, definition);
            return true;
        }

        public bool TryGet(
            string id,
            out InteriorDefinition definition)
        {
            definition = null;

            if (string.IsNullOrWhiteSpace(id))
                return false;

            return _interiors.TryGetValue(id, out definition);
        }

        public IReadOnlyCollection<InteriorDefinition> GetAll()
        {
            return _interiors.Values
                .OrderBy(
                    interior => interior.Name,
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public bool Contains(string id)
        {
            return !string.IsNullOrWhiteSpace(id) &&
                   _interiors.ContainsKey(id);
        }

        public bool Remove(string id)
        {
            return !string.IsNullOrWhiteSpace(id) &&
                   _interiors.Remove(id);
        }

        public void Clear()
        {
            _interiors.Clear();
        }

        /// <summary>
        /// Loads interior definitions from a JSON catalogue.
        /// Existing entries are preserved if loading fails.
        /// Duplicate IDs within the file are rejected.
        /// </summary>
        public bool LoadFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) ||
                !File.Exists(filePath))
            {
                ModLogger.Warning(
                    "Interior catalogue file was not found: " + filePath);

                return false;
            }

            try
            {
                InteriorCatalogueDocument document;

                var serializer = new DataContractJsonSerializer(
                    typeof(InteriorCatalogueDocument));

                using (FileStream stream = File.OpenRead(filePath))
                {
                    document = serializer.ReadObject(stream)
                        as InteriorCatalogueDocument;
                }

                if (document == null ||
                    document.SchemaVersion != 1 ||
                    !string.Equals(
                        document.GameMode,
                        "StoryModeOnly",
                        StringComparison.Ordinal))
                {
                    ModLogger.Warning(
                        "Interior catalogue has an unsupported schema " +
                        "or game mode.");

                    return false;
                }

                if (document.Interiors == null)
                    return false;

                var pending = new Dictionary<string, InteriorDefinition>(
                    StringComparer.OrdinalIgnoreCase);

                foreach (InteriorRecord record in document.Interiors)
                {
                    if (!IsValidRecord(record))
                    {
                        ModLogger.Warning(
                            "Skipped an invalid interior catalogue entry.");

                        return false;
                    }

                    var definition = new InteriorDefinition(
                        record.Id,
                        record.Name,
                        new Vector3(
                            record.Entrance.X,
                            record.Entrance.Y,
                            record.Entrance.Z),
                        new Vector3(
                            record.Exit.X,
                            record.Exit.Y,
                            record.Exit.Z),
                        record.EntranceHeading,
                        record.InteriorId,
                        record.AllowAmbientPeds,
                        record.MaxAmbientPeds,
                        record.RequiredIpls);

                    if (pending.ContainsKey(definition.Id))
                    {
                        ModLogger.Warning(
                            "Duplicate interior ID in catalogue: " +
                            definition.Id);

                        return false;
                    }

                    pending.Add(definition.Id, definition);
                }

                foreach (string id in pending.Keys)
                {
                    if (_interiors.ContainsKey(id))
                    {
                        ModLogger.Warning(
                            "Interior ID already registered: " + id);

                        return false;
                    }
                }

                foreach (InteriorDefinition definition in pending.Values)
                    _interiors.Add(definition.Id, definition);

                ModLogger.Info(
                    "Loaded " + pending.Count +
                    " interior definitions from the JSON catalogue.");

                return true;
            }
            catch (Exception ex)
            {
                ModLogger.Error(
                    "Failed to load the interior catalogue.",
                    ex);

                return false;
            }
        }

        private static bool IsValidRecord(InteriorRecord record)
        {
            if (record == null ||
                string.IsNullOrWhiteSpace(record.Id) ||
                string.IsNullOrWhiteSpace(record.Name) ||
                record.Id.Length > 100 ||
                record.Name.Length > 150 ||
                record.Entrance == null ||
                record.Exit == null ||
                record.RequiredIpls == null ||
                record.InteriorId < 0 ||
                record.MaxAmbientPeds < 0 ||
                record.MaxAmbientPeds > 20)
            {
                return false;
            }

            if (record.AllowAmbientPeds == false &&
                record.MaxAmbientPeds != 0)
            {
                return false;
            }

            if (!IsFinite(record.Entrance.X) ||
                !IsFinite(record.Entrance.Y) ||
                !IsFinite(record.Entrance.Z) ||
                !IsFinite(record.Exit.X) ||
                !IsFinite(record.Exit.Y) ||
                !IsFinite(record.Exit.Z) ||
                !IsFinite(record.EntranceHeading))
            {
                return false;
            }

            foreach (string ipl in record.RequiredIpls)
            {
                if (string.IsNullOrWhiteSpace(ipl) ||
                    ipl.Length > 128)
                {
                    return false;
                }

                foreach (char character in ipl)
                {
                    if (!(char.IsLetterOrDigit(character) ||
                          character == '_' ||
                          character == '-'))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) &&
                   !float.IsInfinity(value);
        }

        [DataContract]
        private sealed class InteriorCatalogueDocument
        {
            [DataMember(Name = "schemaVersion", IsRequired = true)]
            public int SchemaVersion { get; set; }

            [DataMember(Name = "gameMode", IsRequired = true)]
            public string GameMode { get; set; }

            [DataMember(Name = "interiors", IsRequired = true)]
            public List<InteriorRecord> Interiors { get; set; }
        }

        [DataContract]
        private sealed class InteriorRecord
        {
            [DataMember(Name = "id", IsRequired = true)]
            public string Id { get; set; }

            [DataMember(Name = "name", IsRequired = true)]
            public string Name { get; set; }

            [DataMember(Name = "entrance", IsRequired = true)]
            public PositionRecord Entrance { get; set; }

            [DataMember(Name = "exit", IsRequired = true)]
            public PositionRecord Exit { get; set; }

            [DataMember(Name = "entranceHeading", IsRequired = true)]
            public float EntranceHeading { get; set; }

            [DataMember(Name = "interiorId", IsRequired = true)]
            public int InteriorId { get; set; }

            [DataMember(Name = "allowAmbientPeds", IsRequired = true)]
            public bool AllowAmbientPeds { get; set; }

            [DataMember(Name = "maxAmbientPeds", IsRequired = true)]
            public int MaxAmbientPeds { get; set; }

            [DataMember(Name = "requiredIpls", IsRequired = true)]
            public List<string> RequiredIpls { get; set; }
        }

        [DataContract]
        private sealed class PositionRecord
        {
            [DataMember(Name = "x", IsRequired = true)]
            public float X { get; set; }

            [DataMember(Name = "y", IsRequired = true)]
            public float Y { get; set; }

            [DataMember(Name = "z", IsRequired = true)]
            public float Z { get; set; }
        }
    }
}
