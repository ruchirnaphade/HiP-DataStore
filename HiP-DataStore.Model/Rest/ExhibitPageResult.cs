﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using System.Linq;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class ExhibitPageResult
    {
        public int Id { get; set; }

        public int? ExhibitId { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public PageType Type { get; set; }

        public string Title { get; set; }

        public string Text { get; set; }

        public string Description { get; set; }

        public string FontFamily { get; set; }

        public int? Audio { get; set; }

        public int? Image { get; set; }

        public IReadOnlyCollection<SliderPageImageResult> Images { get; set; }

        public bool HideYearNumbers { get; set; }

        public IReadOnlyCollection<int> AdditionalInformationPages { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ContentStatus Status { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public ExhibitPageResult()
        {
        }

        public ExhibitPageResult(ExhibitPage page)
        {
            Id = page.Id;
            ExhibitId = (int)page.Exhibit.Id;
            Type = page.Type;
            Title = page.Title;
            Text = page.Text;
            Description = page.Description;
            FontFamily = page.FontFamily;
            Audio = (int?)page.Audio.Id;
            Image = (int?)page.Image.Id;
            Images = page.Images?.Select(img => new SliderPageImageResult(img)).ToArray() ?? Array.Empty<SliderPageImageResult>();
            HideYearNumbers = page.HideYearNumbers;
            AdditionalInformationPages = page.AdditionalInformationPages.Select(id => (int)id).ToList();
            Status = page.Status;
            Timestamp = page.Timestamp;
        }
    }
}
