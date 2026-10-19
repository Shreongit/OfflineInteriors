
using System;
using System.Collections.Generic;
using GTA.Math;

namespace OfflineInteriors.Interiors
{
    public sealed class InteriorDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public Vector3 EntrancePosition { get; }
        public Vector3 ExitPosition { get; }
        public float EntranceHeading { get; }
        public int InteriorId { get; }
        public bool AllowAmbientPeds { get; }
        public int MaxAmbientPeds { get; }
        public IReadOnlyList<string> RequiredIpls { get; }

        public InteriorDefinition(
            string id,
            string name,
            Vector3 entrancePosition,
            Vector3 exitPosition,
            float entranceHeading,
            int interiorId,
            bool allowAmbientPeds,
            int maxAmbientPeds,
            IEnumerable<string> requiredIpls)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Interior ID is required.", nameof(id));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Interior name is required.", nameof(name));

            if (maxAmbientPeds < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(maxAmbientPeds),
                    "The pedestrian limit cannot be negative.");

            Id = id;
            Name = name;
            EntrancePosition = entrancePosition;
            ExitPosition = exitPosition;
            EntranceHeading = entranceHeading;
            InteriorId = interiorId;
            AllowAmbientPeds = allowAmbientPeds;
            MaxAmbientPeds = allowAmbientPeds ? maxAmbientPeds : 0;

            var iplList = new List<string>();

            if (requiredIpls != null)
            {
                foreach (string ipl in requiredIpls)
                {
                    if (!string.IsNullOrWhiteSpace(ipl))
                        iplList.Add(ipl);
                }
            }

            RequiredIpls = iplList.AsReadOnly();
        }
    }
}
