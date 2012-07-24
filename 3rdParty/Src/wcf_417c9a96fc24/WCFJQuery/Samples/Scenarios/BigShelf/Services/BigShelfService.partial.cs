// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace BigShelf
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.ServiceModel.DomainServices.Server;
    using System.Web;
    using System.Web.Security;
    using BigShelf.Models;

    public enum Sort
    {
        None,
        Title,
        Author,
        Rating,
        MightRead
    }

    public partial class BigShelfService
    {
        private static int cudLatency = 500;
        private static int queryLatency = 500;

        [Invoke]
        public static string SetLatency(int queryDelay, int cudDelay)
        {
            if (queryDelay >= 0 && cudDelay >= 0)
            {
                queryLatency = queryDelay;
                cudLatency = cudDelay;
                return "Latency successfully set.";
            }
            else
            {
                return "Delay must be greater than or equal to 0";
            }
        }

        public IQueryable<Book> GetBooksForSearch(string profileIds, Sort sort, bool sortAscending)
        {
            IQueryable<Book> books;

            var authenticatedProfileId = this.GetUser().Id;

            if (profileIds == null || profileIds == "null")
            {
                books = this.ObjectContext.Books;
            }
            else
            {
                int[] profileIdsAsInts = 
                    String.IsNullOrEmpty(profileIds) ? new int[0] : profileIds.Split(',').Select(id => int.Parse(id, NumberFormatInfo.InvariantInfo)).ToArray();

                books =
                    (from flaggedBook in this.ObjectContext.FlaggedBooks
                    from book in this.ObjectContext.Books
                    where profileIdsAsInts.Contains(flaggedBook.ProfileId)
                    where flaggedBook.Book.Id == book.Id
                    select book).Distinct();
            }

            switch (sort)
            {
                case Sort.Title:  // Title
                    return sortAscending ? books.OrderBy(book => book.Title) : books.OrderByDescending(book => book.Title);

                case Sort.Author:  // Author
                    return sortAscending ? books.OrderBy(book => book.Author) : books.OrderByDescending(book => book.Author);

                case Sort.Rating:  // Rating
                    // Put not-flagged books at the end of our sorted list by giving them a weighting of -1.
                    // Put flagged-to-read books (not yet rated) after all rated books by giving them a weighting of 0.
                    return
                        from book in books
                        let flaggedBook = this.ObjectContext.FlaggedBooks.Where(flaggedBook => flaggedBook.Book == book && flaggedBook.ProfileId == authenticatedProfileId).FirstOrDefault()
                        let weighting = flaggedBook == null ? -1 : (flaggedBook.IsFlaggedToRead != 0 ? 0 : flaggedBook.Rating)
                        let weightingWithSort = sortAscending ? weighting : -weighting
                        orderby weightingWithSort
                        select book;

                case Sort.MightRead:  // Might Read
                    // Put not-flagged books at the end of our sorted list by giving them a weighting of -1.
                    // Put flagged-to-read books at the top of our sorted list by giving them a weighting of 6.
                    return
                        from book in books
                        let flaggedBook = this.ObjectContext.FlaggedBooks.Where(flaggedBook => flaggedBook.Book == book && flaggedBook.ProfileId == authenticatedProfileId).FirstOrDefault()
                        let weighting = flaggedBook == null ? -1 : (flaggedBook.IsFlaggedToRead != 0 ? 6 : flaggedBook.Rating)
                        let weightingWithSort = sortAscending ? weighting : -weighting
                        orderby weightingWithSort
                        select book;

                case Sort.None:
                default:
                    return books;
            }
        }

        public IQueryable<Profile> GetProfileForSearch()
        {
            var authenticatedProfileId = this.GetUser().Id;
            return this.ObjectContext.Profiles.Include("Friends.FriendProfile").Include("FlaggedBooks.Book").Where(p => p.Id == authenticatedProfileId);
        }

        public IQueryable<Profile> GetProfileForProfileUpdate()
        {
            var authenticatedProfileId = this.GetUser().Id;
            return this.ObjectContext.Profiles.Include("Friends.FriendProfile").Where(p => p.Id == authenticatedProfileId);
        }

        public override System.Collections.IEnumerable Query(QueryDescription queryDescription, out IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> validationErrors, out int totalCount)
        {
            System.Threading.Thread.Sleep(queryLatency);
            return base.Query(queryDescription, out validationErrors, out totalCount);
        }

        protected override bool PersistChangeSet()
        {
            System.Threading.Thread.Sleep(cudLatency);
            return base.PersistChangeSet();
        }

        private Profile GetUser()
        {
            if (HttpContext.Current != null
                && HttpContext.Current.User != null
                && HttpContext.Current.User.Identity.IsAuthenticated)
            {
                var profiles = this.ObjectContext.Profiles;
                var aspNetGuid = Membership.GetUser(HttpContext.Current.User.Identity.Name).ProviderUserKey.ToString();
                return profiles.First(p => p.AspNetUserGuid == aspNetGuid);
            }

            return null;
        }
    }
}
