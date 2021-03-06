﻿using System;
using System.Collections.Generic;
using PaderbornUniversity.SILab.Hip.DataStore.Controllers;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using System.Linq;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using PaderbornUniversity.SILab.Hip.DataStore.Utility;
using System.Security.Principal;
using PaderbornUniversity.SILab.Hip.EventSourcing.Events;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;

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
        /// Get UserId of an entity owner
        /// </summary>
        public string Owner(ResourceType entityType, int id)
        {
            var info = GetOrCreateEntityTypeInfo(entityType);

            if (info.Entities.TryGetValue(id, out var entity))
                return entity.UserId;

            return null;
        }

        /// <summary>
        /// Gets the IDs of all entities of the given type and status.
        /// </summary>
        public IReadOnlyCollection<int> AllIds(ResourceType entityType, ContentStatus status, IIdentity user)
        {
            lock (_lockObject)
            {
                bool isAllowedGetAll = UserPermissions.IsAllowedToGetAll(user, status);
                string userId = user.GetUserIdentity();
                var info = GetOrCreateEntityTypeInfo(entityType);
                return info.Entities
                    .AsQueryable()
                    .FilterIf(!isAllowedGetAll, x =>
                        ((status == ContentStatus.All) && (x.Value.Status == ContentStatus.Published)) || (x.Value.UserId == userId))
                    .FilterIf(status == ContentStatus.All, x => x.Value.Status != ContentStatus.Deleted)
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
                return info.Entities.TryGetValue(id, out var entity) && entity.Status != ContentStatus.Deleted;
            }
        }

        public void ApplyEvent(IEvent e)
        {
            switch (e)
            {
                case DeletedEvent ev:
                    lock (_lockObject)
                    {
                        var info3 = GetOrCreateEntityTypeInfo(ev.GetEntityType());
                        if (info3.Entities.TryGetValue(ev.Id, out var entity))
                            entity.Status = ContentStatus.Deleted;
                    }
                    break;
                case CreatedEvent ev:
                    lock (_lockObject)
                    {
                        var owner = ev.UserId;
                        var info = GetOrCreateEntityTypeInfo(ev.GetEntityType());
                        info.MaximumId = Math.Max(info.MaximumId, ev.Id);
                        info.Entities.Add(ev.Id, new EntityInfo { UserId = owner });
                    }
                    break;
                case PropertyChangedEvent ev:
                    lock (_lockObject)
                    {
                        var info2 = GetOrCreateEntityTypeInfo(ev.GetEntityType());
                        if (info2.Entities.TryGetValue(ev.Id, out var entity) && ev.PropertyName == nameof(IContentArgs.Status) && ev.Value is ContentStatus status)
                        {
                            entity.Status = status;
                        }
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

            /// <summary>
            /// Owner of the entity
            /// </summary>
            public string UserId { get; set; }
        }
    }
}
