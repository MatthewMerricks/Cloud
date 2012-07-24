// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace BigShelf.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Data.Objects.DataClasses;
    using System.ServiceModel.DomainServices.Server;

    // The MetadataTypeAttribute identifies BookMetadata as the class
    // that carries additional metadata for the Book class.
    [MetadataTypeAttribute(typeof(Book.BookMetadata))]
    public partial class Book
    {
        // This class allows you to attach custom attributes to properties
        // of the Book class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class BookMetadata
        {
            // Metadata classes are not meant to be instantiated.
            private BookMetadata()
            {
            }

            public DateTime AddedDate { get; set; }

            public string ASIN { get; set; }

            public string Author { get; set; }

            public int CategoryId { get; set; }

            public CategoryName CategoryName { get; set; }

            public string Description { get; set; }

            public EntityCollection<FlaggedBook> FlaggedBooks { get; set; }

            public int Id { get; set; }

            public DateTime PublishDate { get; set; }

            public string Title { get; set; }
        }
    }

    // The MetadataTypeAttribute identifies CategoryMetadata as the class
    // that carries additional metadata for the Category class.
    [MetadataTypeAttribute(typeof(Category.CategoryMetadata))]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Standard RIA Services project format")] 
    public partial class Category
    {
        // This class allows you to attach custom attributes to properties
        // of the Category class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class CategoryMetadata
        {
            // Metadata classes are not meant to be instantiated.
            private CategoryMetadata()
            {
            }

            public int CategoryId { get; set; }

            [Include]
            public CategoryName CategoryName { get; set; }

            public int Id { get; set; }

            public Profile Profile { get; set; }

            public int ProfileId { get; set; }
        }
    }

    // The MetadataTypeAttribute identifies CategoryNameMetadata as the class
    // that carries additional metadata for the CategoryName class.
    [MetadataTypeAttribute(typeof(CategoryName.CategoryNameMetadata))]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Standard RIA Services project format")] 
    public partial class CategoryName
    {
        // This class allows you to attach custom attributes to properties
        // of the CategoryName class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class CategoryNameMetadata
        {
            // Metadata classes are not meant to be instantiated.
            private CategoryNameMetadata()
            {
            }

            public EntityCollection<Book> Books { get; set; }

            public EntityCollection<Category> Categories { get; set; }

            public int Id { get; set; }

            public string Name { get; set; }
        }
    }

    // The MetadataTypeAttribute identifies FlaggedBookMetadata as the class
    // that carries additional metadata for the FlaggedBook class.
    [MetadataTypeAttribute(typeof(FlaggedBook.FlaggedBookMetadata))]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Standard RIA Services project format")] 
    public partial class FlaggedBook
    {
        // This class allows you to attach custom attributes to properties
        // of the FlaggedBook class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class FlaggedBookMetadata
        {
            // Metadata classes are not meant to be instantiated.
            private FlaggedBookMetadata()
            {
            }

            [Include]
            public Book Book { get; set; }

            public int BookId { get; set; }

            public int Id { get; set; }

            public int IsFlaggedToRead { get; set; }

            public Profile Profile { get; set; }

            public int ProfileId { get; set; }

            public int Rating { get; set; }
        }
    }

    // The MetadataTypeAttribute identifies FriendMetadata as the class
    // that carries additional metadata for the Friend class.
    [MetadataTypeAttribute(typeof(Friend.FriendMetadata))]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Standard RIA Services project format")] 
    public partial class Friend
    {
        // This class allows you to attach custom attributes to properties
        // of the Friend class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class FriendMetadata
        {
            // Metadata classes are not meant to be instantiated.
            private FriendMetadata()
            {
            }

            public int FriendId { get; set; }

            public int Id { get; set; }

            [Include]
            public Profile FriendProfile { get; set; }

            public Profile Profile { get; set; }

            public int ProfileId { get; set; }
        }
    }

    // The MetadataTypeAttribute identifies ProfileMetadata as the class
    // that carries additional metadata for the Profile class.
    [MetadataTypeAttribute(typeof(Profile.ProfileMetadata))]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Standard RIA Services project format")] 
    public partial class Profile
    {
        // This class allows you to attach custom attributes to properties
        // of the Profile class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class ProfileMetadata
        {
            // Metadata classes are not meant to be instantiated.
            private ProfileMetadata()
            {
            }

            [Include]
            public EntityCollection<Category> Categories { get; set; }

            [Include]
            public EntityCollection<FlaggedBook> FlaggedBooks { get; set; }

            [Include]
            public EntityCollection<Friend> Friends { get; set; }

            public int Id { get; set; }

            [Required]
            public string Name { get; set; }

            [Required, DataType(DataType.EmailAddress)]
            public string EmailAddress { get; set; }
        }
    }
}
