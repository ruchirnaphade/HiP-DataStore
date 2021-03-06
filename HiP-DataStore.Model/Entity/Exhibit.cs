﻿using PaderbornUniversity.SILab.Hip.DataStore.Model.EventArgs;
using System.Collections.Generic;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public class Exhibit : ContentBase
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public float Latitude { get; set; }

        public float Longitude { get; set; }

        public float AccessRadius { get; set; }

        public int? Image { get; set; }

        public List<int> Tags { get; set; } = new List<int>();

        public List<int> Pages { get; private set; } = new List<int>();

        public List<int> Questions { get; private set; } = new List<int>();

        public Exhibit()
        {
        }

        public Exhibit(ExhibitTransferArgs args)
        {
            Name = args.Name;
            Description = args.Description;
            Image = args.Image;
            Latitude = args.Latitude;
            Longitude = args.Longitude;
            Status = args.Status;
            Tags = args.Tags ?? new List<int>();
            Pages = args.Pages ?? new List<int>();
            AccessRadius = args.AccessRadius;
            Questions = args.Questions ?? new List<int>();
        }

        public ExhibitTransferArgs CreateExhibitArgs()
        {
            var args = new ExhibitTransferArgs
            {
                Name = Name,
                Description = Description,
                Image = Image,
                Latitude = Latitude,
                Longitude = Longitude,
                Status = Status,
                Tags = Tags,
                Pages = Pages,
                AccessRadius = AccessRadius,
                Questions = Questions
            };
            return args;
        }
    }
}
