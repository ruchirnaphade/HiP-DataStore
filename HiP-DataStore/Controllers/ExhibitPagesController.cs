﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.DataStore.Utility;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using PaderbornUniversity.SILab.Hip.EventSourcing.EventStoreLlp;
using PaderbornUniversity.SILab.Hip.EventSourcing.Mongo;
using PaderbornUniversity.SILab.Hip.UserStore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ContentStatus = PaderbornUniversity.SILab.Hip.DataStore.Model.ContentStatus;

namespace PaderbornUniversity.SILab.Hip.DataStore.Controllers
{
    [Authorize]
    [Route("api/Exhibits")]
    public class ExhibitPagesController : Controller
    {
        private readonly IOptions<ExhibitPagesConfig> _exhibitPagesConfig;
        private readonly EventStoreService _eventStore;
        private readonly IMongoDbContext _db;
        private readonly MediaIndex _mediaIndex;
        private readonly EntityIndex _entityIndex;
        private readonly ReferencesIndex _referencesIndex;
        private readonly ExhibitPageIndex _exhibitPageIndex;
        private readonly ReviewIndex _reviewIndex;
        private readonly UserStoreService _userStoreService;
        private readonly ILogger<ExhibitPagesController> _logger;

        public ExhibitPagesController(
            IOptions<ExhibitPagesConfig> exhibitPagesConfig,
            EventStoreService eventStore,
            IMongoDbContext db,
            InMemoryCache cache,
            UserStoreService userStoreService,
            ILogger<ExhibitPagesController> logger)
        {
            _exhibitPagesConfig = exhibitPagesConfig;
            _eventStore = eventStore;
            _db = db;
            _mediaIndex = cache.Index<MediaIndex>();
            _entityIndex = cache.Index<EntityIndex>();
            _referencesIndex = cache.Index<ReferencesIndex>();
            _exhibitPageIndex = cache.Index<ExhibitPageIndex>();
            _reviewIndex = cache.Index<ReviewIndex>();
            _userStoreService = userStoreService;
            _logger = logger;
        }

        [HttpGet("Pages/ids")]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(typeof(IReadOnlyCollection<int>), 200)]
        public IActionResult GetAllIds(ContentStatus status = ContentStatus.Published)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (status == ContentStatus.Deleted && !UserPermissions.IsAllowedToGetDeleted(User.Identity))
                return Forbid();

            return Ok(_entityIndex.AllIds(ResourceTypes.ExhibitPage, status, User.Identity));
        }

        /// <summary>
        /// Gets all pages in no particular order, unless otherwise specified in the query arguments.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        [HttpGet("Pages")]
        [ProducesResponseType(typeof(AllItemsResult<ExhibitPageResult>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(422)]
        public IActionResult GetAllPages([FromQuery]ExhibitPageQueryArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            args = args ?? new ExhibitPageQueryArgs();

            if (args.Status == ContentStatus.Deleted && !UserPermissions.IsAllowedToGetDeleted(User.Identity))
                return Forbid();

            var query = _db.GetCollection<ExhibitPage>(ResourceTypes.ExhibitPage);
            return QueryExhibitPages(query, args);
        }

        [HttpGet("{exhibitId:int}/Pages/ids")]
        [ProducesResponseType(typeof(IReadOnlyCollection<int>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public IActionResult GetIdsForExhibit(int exhibitId, ContentStatus status = ContentStatus.Published)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (status == ContentStatus.Deleted && !UserPermissions.IsAllowedToGetDeleted(User.Identity))
                return Forbid();

            var exhibit = _db.Get<Exhibit>((ResourceTypes.Exhibit, exhibitId));

            if (exhibit == null)
                return NotFound();

            var pageIds = _db.GetMany<ExhibitPage>(ResourceTypes.ExhibitPage, exhibit.Pages)
                .AsQueryable()
                .Where(p => status == ContentStatus.All || p.Status == status)
                .FilterIf(status == ContentStatus.All, x => x.Status != ContentStatus.Deleted)
                .Select(p => p.Id)
                .ToList();

            return Ok(pageIds);
        }

        /// <summary>
        /// Gets the pages of an exhibit in the correct order (as specified in the exhibit),
        /// unless otherwise specified in the query arguments.
        /// </summary>
        [HttpGet("{exhibitId}/Pages")]
        [ProducesResponseType(typeof(AllItemsResult<ExhibitPageResult>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(422)]
        public IActionResult GetPagesForExhibit(int exhibitId, ExhibitPageQueryArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            args = args ?? new ExhibitPageQueryArgs();

            if (args.Status == ContentStatus.Deleted && !UserPermissions.IsAllowedToGetDeleted(User.Identity))
                return Forbid();

            var exhibit = _db.Get<Exhibit>((ResourceTypes.Exhibit, exhibitId));

            if (exhibit == null)
                return NotFound();

            var query = _db.GetMany<ExhibitPage>(ResourceTypes.ExhibitPage, exhibit.Pages).AsQueryable();

            return QueryExhibitPages(query, args);
        }

        [HttpGet("Pages/{id}")]
        [ProducesResponseType(typeof(ExhibitPageResult), 200)]
        [ProducesResponseType(304)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public IActionResult GetById(int id, DateTimeOffset? timestamp = null)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var status = _entityIndex.Status(ResourceTypes.ExhibitPage, id) ?? ContentStatus.Published;
            if (!UserPermissions.IsAllowedToGet(User.Identity, status, _entityIndex.Owner(ResourceTypes.ExhibitPage, id)))
                return Forbid();

            var page = _db.Get<ExhibitPage>((ResourceTypes.ExhibitPage, id));

            if (page == null)
                return NotFound();

            if (timestamp != null && page.Timestamp <= timestamp.Value)
                return StatusCode(304);

            var result = new ExhibitPageResult(page)
            {
                Timestamp = _referencesIndex.LastModificationCascading(ResourceTypes.ExhibitPage, id)
            };

            return Ok(result);
        }

        [HttpPost("Pages")]
        [ProducesResponseType(typeof(int), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> PostAsync([FromBody]ExhibitPageArgs2 args)
        {
            // if font family is not specified, fallback to the configured default font family
            if (args != null && args.FontFamily == null)
                args.FontFamily = _exhibitPagesConfig.Value.DefaultFontFamily;

            ValidateExhibitPageArgs(args);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // ReSharper disable once PossibleNullReferenceException (args == null is handled through ModelState.IsValid)
            if (!UserPermissions.IsAllowedToCreate(User.Identity, args.Status))
                return Forbid();

            // validation passed, emit event
            var newPageId = _entityIndex.NextId(ResourceTypes.ExhibitPage);

            await EntityManager.CreateEntityAsync(_eventStore, args, ResourceTypes.ExhibitPage, newPageId, User.Identity.GetUserIdentity());
            return Created($"{Request.Scheme}://{Request.Host}/api/Exhibits/Pages/{newPageId}", newPageId);
        }

        [HttpPut("Pages/{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(422)]
        public async Task<IActionResult> PutAsync(int id, [FromBody]ExhibitPageArgs2 args)
        {
            // if font family is not specified, fallback to the configured default font family
            if (args != null && args.FontFamily == null)
                args.FontFamily = _exhibitPagesConfig.Value.DefaultFontFamily;

            ValidateExhibitPageArgs(args);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceTypes.ExhibitPage, id))
                return NotFound();

            // ReSharper disable once PossibleNullReferenceException (args == null is handled through ModelState.IsValid)
            if (!UserPermissions.IsAllowedToEdit(User.Identity, args.Status, _entityIndex.Owner(ResourceTypes.ExhibitPage, id)))
                return Forbid();

            var oldStatus = _entityIndex.Status(ResourceTypes.ExhibitPage, id).GetValueOrDefault();
            if (args.Status == ContentStatus.Unpublished && oldStatus != ContentStatus.Published)
                return BadRequest(ErrorMessages.CannotBeUnpublished(ResourceTypes.ExhibitPage));

            // ReSharper disable once PossibleInvalidOperationException (.Value is safe here since we know the entity exists)
            var currentPageType = _exhibitPageIndex.PageType(id).Value;
            // ReSharper disable once PossibleNullReferenceException (args == null is handled through ModelState.IsValid)
            if (currentPageType != args.Type)
                return StatusCode(422, ErrorMessages.CannotChangeExhibitPageType(currentPageType, args.Type));

            // validation passed, emit event
            var currentArgs = await EventStreamExtensions.GetCurrentEntityAsync<ExhibitPageArgs2>(_eventStore.EventStream, ResourceTypes.ExhibitPage, id);
            await EntityManager.UpdateEntityAsync(_eventStore, currentArgs, args, ResourceTypes.ExhibitPage, id, User.Identity.GetUserIdentity());
            return StatusCode(204);
        }

        [HttpDelete("Pages/{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceTypes.ExhibitPage, id))
                return NotFound();

            var status = _entityIndex.Status(ResourceTypes.ExhibitPage, id).GetValueOrDefault();
            if (!UserPermissions.IsAllowedToDelete(User.Identity, status, _entityIndex.Owner(ResourceTypes.ExhibitPage, id)))
                return Forbid();

            if (status == ContentStatus.Published)
                return BadRequest(ErrorMessages.CannotBeDeleted(ResourceTypes.ExhibitPage, id));

            if (_referencesIndex.IsUsed(ResourceTypes.ExhibitPage, id))
                return BadRequest(ErrorMessages.ResourceInUse);

            await EntityManager.DeleteEntityAsync(_eventStore, ResourceTypes.ExhibitPage, id, User.Identity.GetUserIdentity());
            return NoContent();
        }

        [HttpGet("Pages/{id}/Refs")]
        [ProducesResponseType(typeof(ReferenceInfoResult), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public IActionResult GetReferenceInfo(int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!UserPermissions.IsAllowedToGet(User.Identity, _entityIndex.Owner(ResourceTypes.ExhibitPage, id)))
                return Forbid();

            return ReferenceInfoHelper.GetReferenceInfo(ResourceTypes.ExhibitPage, id, _entityIndex, _referencesIndex);
        }

        /// <summary>
        /// Returns the review to the exhibit page with the given ID
        /// </summary>
        /// <param name="id">ID of the exhibit page the review belongs to</param>
        [HttpGet("Pages/Review/{id}")]
        [ProducesResponseType(typeof(ReviewResult), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public IActionResult GetReview(int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (ReviewHelper.CheckNotFoundGet(id, ResourceTypes.ExhibitPage, _entityIndex, _reviewIndex) is string errorMessage)
                return NotFound(errorMessage);

            var reviewId = _reviewIndex.GetReviewId(ResourceTypes.ExhibitPage.Name, id);
            var review = _db.Get<Review>((ResourceTypes.Review, reviewId));

            if (!review.ReviewableByStudents && !UserPermissions.IsSupervisorOrAdmin(User.Identity))
                return Forbid();

            var result = new ReviewResult(review);

            return Ok(result);
        }

        /// <summary>
        /// Creates a review for the exhibit page with the given ID
        /// </summary>
        /// <param name="id">ID of the exhibit page the review belongs to</param>
        /// <param name="args">Arguments for the review</param>
        [HttpPost("Pages/Review/{id}")]
        [ProducesResponseType(typeof(int), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> PostReviewAsync(int id, ReviewArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_entityIndex.Exists(ResourceTypes.ExhibitPage, id))
                return NotFound(ErrorMessages.ContentNotFound(ResourceTypes.ExhibitPage, id));

            if (ReviewHelper.CheckBadRequestPost(id, ResourceTypes.ExhibitPage, _entityIndex, _reviewIndex) is string errorMessage)
                return BadRequest(errorMessage);

            if (!UserPermissions.IsAllowedToCreateReview(User.Identity, _entityIndex.Owner(ResourceTypes.ExhibitPage, id)))
                return Forbid();

            var reviewId = _reviewIndex.NextId(ResourceTypes.ExhibitPage);

            args.EntityId = id;
            args.EntityType = ResourceTypes.ExhibitPage.Name;

            await ReviewHelper.SendReviewRequestNotificationsAsync(_userStoreService, _db, _logger, id, ReviewEntityType.ExhibitPage, args.Reviewers);

            await EntityManager.CreateEntityAsync(_eventStore, args, ResourceTypes.Review, reviewId, User.Identity.GetUserIdentity());

            return Created($"{Request.Scheme}://{Request.Host}/api/Exhibits/Review/{reviewId}", reviewId);
        }

        /// <summary>
        /// Changes the review that belongs to the exhibit page with the given ID
        /// </summary>
        /// <param name="id">ID of the exhibit page the review belongs to</param>
        /// <param name="args">Arguments for the review</param>
        [HttpPut("Pages/Review/{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> PutReviewAsync(int id, ReviewArgs args)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (ReviewHelper.CheckNotFoundPut(id, ResourceTypes.ExhibitPage, _entityIndex, _reviewIndex) is string errorMessage)
                return NotFound(errorMessage);

            var reviewId = _reviewIndex.GetReviewId(ResourceTypes.ExhibitPage.Name, id);
            var oldReviewArgs = await _eventStore.EventStream.GetCurrentEntityAsync<ReviewArgs>(ResourceTypes.Review, reviewId);

            if (ReviewHelper.CheckForbidPut(oldReviewArgs, User.Identity, _reviewIndex, reviewId))
                return Forbid();

            //only take the new reviewers
            var newReviewers = args.Reviewers?.Except(oldReviewArgs.Reviewers ?? new List<string>());
            await ReviewHelper.SendReviewRequestNotificationsAsync(_userStoreService, _db, _logger, id, ReviewEntityType.ExhibitPage, newReviewers);

            args = ReviewHelper.UpdateReviewArgs(args, oldReviewArgs, User.Identity);

            await EntityManager.UpdateEntityAsync(_eventStore, oldReviewArgs, args, ResourceTypes.Review, reviewId, User.Identity.GetUserIdentity());
            return StatusCode(204);
        }

        /// <summary>
        /// Deletes the review of the exhibit page with the given ID
        /// </summary>
        /// <param name="id">ID of the exhibit page the review belongs to</param>
        /// <returns></returns>
        [HttpDelete("Pages/Review/{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteReviewAsync(int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // only supervisors or admins are allowed to delete reviews
            if (!UserPermissions.IsSupervisorOrAdmin(User.Identity))
                return Forbid();

            if (ReviewHelper.CheckNotFoundGet(id, ResourceTypes.ExhibitPage, _entityIndex, _reviewIndex) is string errorMessage)
                return NotFound(errorMessage);

            var reviewId = _reviewIndex.GetReviewId(ResourceTypes.ExhibitPage.Name, id);

            await EntityManager.DeleteEntityAsync(_eventStore, ResourceTypes.Review, reviewId, User.Identity.GetUserIdentity());
            return NoContent();
        }

        private IActionResult QueryExhibitPages(IQueryable<ExhibitPage> allPages, ExhibitPageQueryArgs args)
        {
            try
            {
                var pages = allPages
                    .FilterByIds(args.Exclude, args.IncludeOnly)
                    .FilterByUser(args.Status, User.Identity)
                    .FilterByStatus(args.Status, User.Identity)
                    .FilterByTimestamp(args.Timestamp)
                    .FilterIf(!string.IsNullOrEmpty(args.Query), x =>
                        x.Title.ToLower().Contains(args.Query.ToLower()) ||
                        x.Text.ToLower().Contains(args.Query.ToLower()) ||
                        x.Description.ToLower().Contains(args.Query.ToLower()))
                    .FilterIf(args.Type != null, x => x.Type == args.Type)
                    .Sort(args.OrderBy,
                        ("id", x => x.Id),
                        ("title", x => x.Title),
                        ("timestamp", x => x.Timestamp))
                    .PaginateAndSelect(args.Page, args.PageSize, x => new ExhibitPageResult(x)
                    {
                        Timestamp = _referencesIndex.LastModificationCascading(ResourceTypes.ExhibitPage, x.Id)
                    });

                return Ok(pages);
            }
            catch (InvalidSortKeyException e)
            {
                return StatusCode(422, e.Message);
            }
        }

        private void ValidateExhibitPageArgs(ExhibitPageArgs2 args)
        {
            if (args == null)
                return;

            // constrain properties Image, Images and HideYearNumbers to their respective page types
            if (args.Image != null && args.Type != PageType.Image_Page)
                ModelState.AddModelError(nameof(args.Image),
                    ErrorMessages.FieldNotAllowedForPageType(nameof(args.Image), args.Type));

            if (args.Images != null && args.Type != PageType.Slider_Page)
                ModelState.AddModelError(nameof(args.Images),
                    ErrorMessages.FieldNotAllowedForPageType(nameof(args.Images), args.Type));

            if (args.HideYearNumbers != null && args.Type != PageType.Slider_Page)
                ModelState.AddModelError(nameof(args.HideYearNumbers),
                    ErrorMessages.FieldNotAllowedForPageType(nameof(args.HideYearNumbers), args.Type));

            // validate font family
            if (!_exhibitPagesConfig.Value.IsFontFamilyValid(args.FontFamily))
                ModelState.AddModelError(nameof(args.FontFamily), $"Font family must be null/unspecified (which defaults to {_exhibitPagesConfig.Value.DefaultFontFamily}) or one of the following: {string.Join(", ", _exhibitPagesConfig.Value.FontFamilies)}");

            // ensure referenced image exists
            if (args.Image != null && !_mediaIndex.IsImage(args.Image.Value))
                ModelState.AddModelError(nameof(args.Image),
                    ErrorMessages.ImageNotFound(args.Image.Value));

            // ensure referenced audio exists
            if (args.Audio != null && !_mediaIndex.IsAudio(args.Audio.Value))
                ModelState.AddModelError(nameof(args.Audio),
                    ErrorMessages.AudioNotFound(args.Audio.Value));

            // ensure referenced slider page images exist
            if (args.Images != null)
            {
                var invalidIds = args.Images
                    .Select(img => img.Image)
                    .Where(id => !_mediaIndex.IsImage(id))
                    .ToList();

                foreach (var id in invalidIds)
                    ModelState.AddModelError(nameof(args.Images),
                        ErrorMessages.ImageNotFound(id));
            }

            // ensure referenced additional info pages exist
            if (args.AdditionalInformationPages != null)
            {
                var invalidIds = args.AdditionalInformationPages
                    .Where(id => !_entityIndex.Exists(ResourceTypes.ExhibitPage, id))
                    .ToList();

                foreach (var id in invalidIds)
                    ModelState.AddModelError(nameof(args.AdditionalInformationPages),
                        ErrorMessages.ExhibitPageNotFound(id));
            }
        }
    }
}
