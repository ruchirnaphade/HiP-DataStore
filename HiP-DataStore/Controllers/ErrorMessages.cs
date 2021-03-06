﻿using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.EventSourcing;

namespace PaderbornUniversity.SILab.Hip.DataStore.Controllers
{
    public static class ErrorMessages
    {
        public static string ImageNotFound(int id) =>
            $"No image with ID '{id}' exists";

        public static string AudioNotFound(int id) =>
            $"No audio with ID '{id}' exists";

        public static string ContentNotFound(ResourceType type, int id) =>
            $"No {type.Name.ToLower()} with ID '{id}' exists";

        public static string ExhibitPageNotFound(int id) =>
            $"No page with ID '{id}' exists";

        public static string FieldNotAllowedForPageType(string fieldName, PageType pageType) =>
            $"Field '{fieldName}' not allowed for page type '{pageType}'";

        public static string ResourceInUse =>
            "The resource cannot be deleted because it is referenced by other resources";

        public static string TagNameAlreadyUsed =>
            "A tag with the same name already exists";

        public static string CannotChangeExhibitPageType(PageType currentType, PageType newType) =>
            $"The type of an exhibit page cannot be changed (current type is '{currentType}', attempted to change to '{newType}')";

        public static string CannotBeUnpublished(ResourceType type) => $"Only published {type.Name.ToLower()} can be set to unpublished";

        public static string CannotBeDeleted(ResourceType res, int id) =>
            $"{res.Name.ToLower()} with id {id} has status Published. It can not be deleted";

        public static string QuestionCannotBeCreated(int exhibitId) =>
            $"Exhibit {exhibitId} already has the maximum amount of questions";

        public static string ExhibitHasNoQuestions(int exhibitId) =>
            $"The exhibit {exhibitId} doesn't have any questions";

        public static string CannotAddReviewToContentWithWrongStatus() =>
            $"Reviews can only be added to content, that has status '{ContentStatus.In_Review}'";

        public static string ReviewNotFound(ResourceType type, int id) =>
            $"No review exists for the {type.Name.ToLower()} with id '{id}'";

        public static string ContentAlreadyHasReview(ResourceType type, int id) =>
            $"A review for the {type.Name.ToLower()} with id '{id}' already exists";

        public static string NoReviewWithIdExists(int id) =>
            $"A review with id '{id}' does not exist";

        public static string ReviewCommentNotFound(int id) =>
            $"A review comment with id '{id}' does not exist";

        public static string HighScoreNotFound(int exhibitId) =>
            $"A highscore for the exhibit with id '{exhibitId}' does not exist";
    }
}
