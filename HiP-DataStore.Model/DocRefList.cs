﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model
{
    /// <summary>
    /// A strongly-typed reference to multiple other documents in a Mongo database.
    /// </summary>
    public class DocRefList<T> : DocRefBase, ICollection<BsonValue>
    {
        [BsonElement("ids")]
        private HashSet<BsonValue> _ids = new HashSet<BsonValue>();

        public int Count => _ids.Count;

        public DocRefList()
        {
        }

        public DocRefList(string collection = null, string database = null) : base(collection, database)
        {
        }

        public DocRefList(IEnumerable<BsonValue> ids, string collection = null, string database = null) : base(collection, database)
        {
            if (ids == null)
                throw new ArgumentNullException(nameof(ids));

            _ids = new HashSet<BsonValue>(ids);
        }

        public bool Add(BsonValue id) => _ids.Add(id);

        public void Add(IEnumerable<BsonValue> ids)
        {
            foreach (var id in ids ?? Enumerable.Empty<BsonValue>())
                _ids.Add(id);
        }

        public bool Remove(BsonValue id) => _ids.Remove(id);

        public void Clear() => _ids.Clear();

        public bool Contains(BsonValue id) => _ids.Contains(id);

        public IEnumerator<BsonValue> GetEnumerator() => _ids.GetEnumerator();


        bool ICollection<BsonValue>.IsReadOnly => false;

        void ICollection<BsonValue>.Add(BsonValue id) => Add(id);

        void ICollection<BsonValue>.CopyTo(BsonValue[] array, int arrayIndex) => _ids.CopyTo(array, arrayIndex);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}