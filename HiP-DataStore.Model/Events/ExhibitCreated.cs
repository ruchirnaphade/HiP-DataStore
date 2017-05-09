﻿using System;
using Newtonsoft.Json;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class ExhibitCreated : ICreateEvent
    {
        public int Id { get; set; }

        public ExhibitArgs Properties { get; set; }

        [JsonIgnore]
        public Type EntityType => typeof(Exhibit);

        [JsonIgnore]
        public ContentStatus Status => Properties.Status;
    }
}