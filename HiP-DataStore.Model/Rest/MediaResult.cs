﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class MediaResult
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool Used { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public MediaType Type { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public ContentStatus Status { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}
