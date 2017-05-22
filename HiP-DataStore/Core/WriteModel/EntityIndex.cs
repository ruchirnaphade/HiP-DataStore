﻿using System;
using System.Collections.Generic;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using System.Linq;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel
{
    /// <summary>
    /// Caches the IDs and the publication statuses of all entities.
    /// </summary>
    public class EntityIndex : IDomainIndex
    {
        private readonly Dictionary<ResourceType, EntityTypeInfo> _types = new Dictionary<ResourceType, EntityTypeInfo>();
        private readonly object _lockObject = new object();

        /// <summary>
        /// Gets a new, never-used-before ID for a new entity of the specified type.
        /// </summary>
        public int NextId(ResourceType entityType)
        {
            lock (_lockObject)
            {
                var info = GetOrCreateEntityTypeInfo(entityType);
                return ++info.MaximumId;
            }
        }

        /// <summary>
        /// Gets the current status of an entity given its type and ID.
        /// </summary>
        public ContentStatus? Status(ResourceType entityType, int id)
        {
            lock (_lockObject)
            {
                var info = GetOrCreateEntityTypeInfo(entityType);

                if (info.Entities.TryGetValue(id, out var entity))
                    return entity.Status;

                return null;
            }
        }

        /// <summary>
        /// Gets the IDs of all entities of the given type and status.
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public IReadOnlyCollection<int> AllIds(ResourceType entityType, ContentStatus status)
        {
            lock (_lockObject)
            {
                var info = GetOrCreateEntityTypeInfo(entityType);
                return info.Entities
                    .Where(x => status == ContentStatus.All || x.Value.Status == status)
                    .Select(x => x.Key)
                    .ToList();
            }
        }

        /// <summary>
        /// Determines whether an entity with the specified type and ID exists.
        /// </summary>
        public bool Exists(ResourceType entityType, int id)
        {
            lock (_lockObject)
            {
                var info = GetOrCreateEntityTypeInfo(entityType);
                return info.Entities.ContainsKey(id);
            }
        }
        
        public void ApplyEvent(IEvent e)
        {
            switch (e)
            {
                case ICreateEvent ev:
                    lock (_lockObject)
                    {
                        var info = GetOrCreateEntityTypeInfo(ev.GetEntityType());
                        info.MaximumId = Math.Max(info.MaximumId, ev.Id);
                        info.Entities.Add(ev.Id, new EntityInfo { Status = ev.GetStatus() });
                    }
                    break;

                case IUpdateEvent ev:
                    lock (_lockObject)
                    {
                        var info2 = GetOrCreateEntityTypeInfo(ev.GetEntityType());
                        if (info2.Entities.TryGetValue(ev.Id, out var entity))
                            entity.Status = ev.GetStatus();
                    }
                    break;

                case IDeleteEvent ev:
                    lock (_lockObject)
                    {
                        var info3 = GetOrCreateEntityTypeInfo(ev.GetEntityType());
                        info3.Entities.Remove(ev.Id);
                    }
                    break;
            }
        }

        private EntityTypeInfo GetOrCreateEntityTypeInfo(ResourceType entityType)
        {
            if (_types.TryGetValue(entityType, out var info))
                return info;

            return _types[entityType] = new EntityTypeInfo();
        }

        class EntityTypeInfo
        {
            /// <summary>
            /// The largest ID ever assigned to an entity of the type.
            /// </summary>
            public int MaximumId { get; set; } = -1;

            /// <summary>
            /// Stores only the most basic information about all entities of the type.
            /// It is assumed that this easily fits in RAM.
            /// </summary>
            public Dictionary<int, EntityInfo> Entities { get; } = new Dictionary<int, EntityInfo>();
        }

        class EntityInfo
        {
            public ContentStatus Status { get; set; }
        }
    }
}
