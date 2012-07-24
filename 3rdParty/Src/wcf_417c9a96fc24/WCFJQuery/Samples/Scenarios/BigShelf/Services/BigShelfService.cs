// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace BigShelf
{
    using System.Data;
    using System.Linq;
    using System.ServiceModel.DomainServices.EntityFramework;
    using System.ServiceModel.DomainServices.Hosting;
    using System.ServiceModel.DomainServices.Server;
    using BigShelf.Models;

    // Implements application logic using the BigShelfEntities context.
    // TODO: Add your application logic to these methods or in additional methods.
    // TODO: Wire up authentication (Windows/ASP.NET Forms) and uncomment the following to disable anonymous access
    // Also consider adding roles to restrict access as appropriate.
    [RequiresAuthentication]
    [EnableClientAccess()]
    public partial class BigShelfService : LinqToEntitiesDomainService<BigShelfEntities>
    {
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'Books' query.
        [Query(IsDefault = true)]
        public IQueryable<Book> GetBooks()
        {
            return this.ObjectContext.Books;
        }

        public void InsertBook(Book book)
        {
            if (book != null)
            {
                if (book.EntityState != EntityState.Detached)
                {
                    this.ObjectContext.ObjectStateManager.ChangeObjectState(book, EntityState.Added);
                }
                else
                {
                    this.ObjectContext.Books.AddObject(book);
                }
            }
        }

        public void UpdateBook(Book currentBook)
        {
            this.ObjectContext.Books.AttachAsModified(currentBook, this.ChangeSet.GetOriginal(currentBook));
        }

        public void DeleteBook(Book book)
        {
            if (book != null)
            {
                if (book.EntityState != EntityState.Detached)
                {
                    this.ObjectContext.ObjectStateManager.ChangeObjectState(book, EntityState.Deleted);
                }
                else
                {
                    this.ObjectContext.Books.Attach(book);
                    this.ObjectContext.Books.DeleteObject(book);
                }
            }
        }

        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'Categories' query.
        [Query(IsDefault = true)]
        public IQueryable<Category> GetCategories()
        {
            return this.ObjectContext.Categories;
        }

        public void InsertCategory(Category category)
        {
            if (category != null)
            {
                if (category.EntityState != EntityState.Detached)
                {
                    this.ObjectContext.ObjectStateManager.ChangeObjectState(category, EntityState.Added);
                }
                else
                {
                    this.ObjectContext.Categories.AddObject(category);
                }
            }
        }

        public void UpdateCategory(Category currentCategory)
        {
            this.ObjectContext.Categories.AttachAsModified(currentCategory, this.ChangeSet.GetOriginal(currentCategory));
        }

        public void DeleteCategory(Category category)
        {
            if (category != null)
            {
                if (category.EntityState != EntityState.Detached)
                {
                    this.ObjectContext.ObjectStateManager.ChangeObjectState(category, EntityState.Deleted);
                }
                else
                {
                    this.ObjectContext.Categories.Attach(category);
                    this.ObjectContext.Categories.DeleteObject(category);
                }
            }
        }

        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'CategoryNames' query.
        [Query(IsDefault = true)]
        public IQueryable<CategoryName> GetCategoryNames()
        {
            return this.ObjectContext.CategoryNames;
        }

        public void InsertCategoryName(CategoryName categoryName)
        {
            if (categoryName != null)
            {
                if (categoryName.EntityState != EntityState.Detached)
                {
                    this.ObjectContext.ObjectStateManager.ChangeObjectState(categoryName, EntityState.Added);
                }
                else
                {
                    this.ObjectContext.CategoryNames.AddObject(categoryName);
                }
            }
        }

        public void UpdateCategoryName(CategoryName currentCategoryName)
        {
            this.ObjectContext.CategoryNames.AttachAsModified(currentCategoryName, this.ChangeSet.GetOriginal(currentCategoryName));
        }

        public void DeleteCategoryName(CategoryName categoryName)
        {
            if (categoryName != null)
            {
                if (categoryName.EntityState != EntityState.Detached)
                {
                    this.ObjectContext.ObjectStateManager.ChangeObjectState(categoryName, EntityState.Deleted);
                }
                else
                {
                    this.ObjectContext.CategoryNames.Attach(categoryName);
                    this.ObjectContext.CategoryNames.DeleteObject(categoryName);
                }
            }
        }

        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'FlaggedBooks' query.
        [Query(IsDefault = true)]
        public IQueryable<FlaggedBook> GetFlaggedBooks()
        {
            return this.ObjectContext.FlaggedBooks;
        }

        public void InsertFlaggedBook(FlaggedBook flaggedBook)
        {
            if (flaggedBook != null)
            {
                if (flaggedBook.EntityState != EntityState.Detached)
                {
                    this.ObjectContext.ObjectStateManager.ChangeObjectState(flaggedBook, EntityState.Added);
                }
                else
                {
                    this.ObjectContext.FlaggedBooks.AddObject(flaggedBook);
                }
            }
        }

        public void UpdateFlaggedBook(FlaggedBook currentFlaggedBook)
        {
            this.ObjectContext.FlaggedBooks.AttachAsModified(currentFlaggedBook, this.ChangeSet.GetOriginal(currentFlaggedBook));
        }

        public void DeleteFlaggedBook(FlaggedBook flaggedBook)
        {
            if (flaggedBook != null)
            {
                if (flaggedBook.EntityState != EntityState.Detached)
                {
                    this.ObjectContext.ObjectStateManager.ChangeObjectState(flaggedBook, EntityState.Deleted);
                }
                else
                {
                    this.ObjectContext.FlaggedBooks.Attach(flaggedBook);
                    this.ObjectContext.FlaggedBooks.DeleteObject(flaggedBook);
                }
            }
        }

        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'Friends' query.
        [Query(IsDefault = true)]
        public IQueryable<Friend> GetFriends()
        {
            return this.ObjectContext.Friends;
        }

        public void InsertFriend(Friend friend)
        {
            if (friend != null)
            {
                if (friend.EntityState != EntityState.Detached)
                {
                    this.ObjectContext.ObjectStateManager.ChangeObjectState(friend, EntityState.Added);
                }
                else
                {
                    this.ObjectContext.Friends.AddObject(friend);
                }
            }
        }

        public void UpdateFriend(Friend currentFriend)
        {
            this.ObjectContext.Friends.AttachAsModified(currentFriend, this.ChangeSet.GetOriginal(currentFriend));
        }

        public void DeleteFriend(Friend friend)
        {
            if (friend != null)
            {
                if (friend.EntityState != EntityState.Detached)
                {
                    this.ObjectContext.ObjectStateManager.ChangeObjectState(friend, EntityState.Deleted);
                }
                else
                {
                    this.ObjectContext.Friends.Attach(friend);
                    this.ObjectContext.Friends.DeleteObject(friend);
                }
            }
        }

        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'Profiles' query.
        [Query(IsDefault = true)]
        public IQueryable<Profile> GetProfiles()
        {
            return this.ObjectContext.Profiles;
        }

        public void InsertProfile(Profile profile)
        {
            if (profile != null)
            {
                if (profile.EntityState != EntityState.Detached)
                {
                    this.ObjectContext.ObjectStateManager.ChangeObjectState(profile, EntityState.Added);
                }
                else
                {
                    this.ObjectContext.Profiles.AddObject(profile);
                }
            }
        }

        public void UpdateProfile(Profile currentProfile)
        {
            this.ObjectContext.Profiles.AttachAsModified(currentProfile, this.ChangeSet.GetOriginal(currentProfile));
        }

        public void DeleteProfile(Profile profile)
        {
            if (profile != null)
            {
                if (profile.EntityState != EntityState.Detached)
                {
                    this.ObjectContext.ObjectStateManager.ChangeObjectState(profile, EntityState.Deleted);
                }
                else
                {
                    this.ObjectContext.Profiles.Attach(profile);
                    this.ObjectContext.Profiles.DeleteObject(profile);
                }
            }
        }
    }
}