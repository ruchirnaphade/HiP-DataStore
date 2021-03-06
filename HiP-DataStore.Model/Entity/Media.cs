﻿using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public class MediaElement : ContentBase
    {
        public string Title { get; set; }

        public string Description { get; set; }

        /// <summary>
        /// The path to the actual file.
        /// </summary>
        public string File { get; set; }

        public MediaType Type { get; set; }

        public MediaElement()
        {
        }

        public MediaElement(MediaArgs args)
        {
            Title = args.Title;
            Description = args.Description;
            Type = args.Type;
            Status = args.Status;
        }

        public MediaArgs CreateMediaArgs()
        {
            var args = new MediaArgs
            {
                Title = Title,
                Description = Description,
                Type = Type,
                Status = Status
            };
            return args;
        }
    }

    public enum MediaType
    {
        Image, Audio
    }
}
