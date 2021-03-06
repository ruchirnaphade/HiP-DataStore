﻿using PaderbornUniversity.SILab.Hip.DataStore.Model.Utility;
using System.ComponentModel.DataAnnotations;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class TagArgs : IContentArgs
    {
        [Required]
        public string Title { get; set; }

        public string Description { get; set; }

        [Reference(nameof(ResourceTypes.Media))]
        public int? Image { get; set; }

        [AllowedStatuses]
        public ContentStatus Status { get; set; }        
    }
}
