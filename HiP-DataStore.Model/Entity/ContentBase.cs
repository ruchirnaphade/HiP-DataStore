﻿using PaderbornUniversity.SILab.Hip.EventSourcing;
using System;
using System.Collections.Generic;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    /// <summary>
    /// Base class for routes, exhibits, pages, tags, media etc.
    /// </summary>
    public abstract class ContentBase : IEntity<int>
    {
        /// <summary>
        /// Owner of the content
        /// </summary>
        public string UserId { get; set; }

        public ContentStatus Status { get; set; }

        /// <summary>
        /// Other entities that are referenced by this entity.
        /// </summary>
        public List<EntityId> References { get; private set; } = new List<EntityId>();

        /// <summary>
        /// Other entities referencing this entity.
        /// </summary>
        /// <remarks>
        /// Based on this list, it can be determined whether an entity is in use.
        /// 
        /// Example references:
        /// - An exhibit is in use if it is contained in a route.
        /// - A media file is in use if it is referenced by a route, an exhibit, an exhibit page or a tag.
        /// - A tag is is use if it is referenced by a route or an exhibit.
        /// </remarks>
        public List<EntityId> Referencers { get; private set; } = new List<EntityId>();

        /// <summary>
        /// The date and time of the last modification.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

        public int Id { get; set; }
    }
}
