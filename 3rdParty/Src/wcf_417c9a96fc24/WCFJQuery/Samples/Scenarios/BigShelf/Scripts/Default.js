/// <reference path="References.js" />

$(document).ready(function () {

    // Constants
    var serviceUrl = "BigShelf-BigShelfService.svc",
        pageSize = 6;

    // Load our "profile" record, including those flagged books belonging to this profile.
    var profile,
        profileDataSource = $.dataSource({
            serviceUrl: serviceUrl,
            queryName: "GetProfileForSearch",
            refresh: function (profiles) {
                profile = profiles[0];
                render();
            }
        }).refresh();

    function render() {
        var remoteBooks, books = [], remoteBooksQueryParameters = {};
        var View = {
            All: 1,
            MyBooks: 2,
            JustFriends: 3
        }, currentView;

        // A list UI control over our "books" array.
        var booksList = new ListControl(books, {
            template: "#bookTemplate",
            container: "#books",
            itemAdded: function (book, bookElement) {
                // Bind edit controls on each book element to a FlaggedBook entity for "profile".
                enableFlaggingForBook(book, bookElement);
            }
        });

        // Our "Show Me:" nav bar.
        $(".filterButton").click(function () {
            var $this = $(this);

            // Only clicked filter button gets the "selected" class.
            $(".filterButton").not($this).removeClass("selected");
            $this.addClass("selected");

            // Change the view based on what's clicked in "Show Me:".
            switchView($this.data("book-view"));
        });

        // A friends filter used only by "Show Me: Just friends".
        $.each(profile.get_Friends(), function () {
            $("#friendsListTemplate").tmpl(this).appendTo($("#friendsList"))
            .filter(".friendButton")
            .data("friendId", this.FriendId)
            .click(function () {
                // Clicked friend is (un)selected.
                var wasChecked = $(this).hasClass("selected");
                $(this).toggleClass("selected", !wasChecked);

                // Refresh those books displayed, based on changes to the friends filter.
                refreshBooksList();
            });
        });

        // Our "Sort By:" sort control.
        $(".sortButton").click(function () {
            var newSort = $(this).data("book-sort"),
                $currentSortElement = $(".sortButton.selected"),
                currentSort = $currentSortElement.data("book-sort");
            if (newSort !== currentSort) {
                // Only clicked sort button gets the "selected" class.
                $currentSortElement.removeClass("selected");
                $(this).addClass("selected");

                // Changing our sort should move us back to page 0.  Don't refresh here.  Do so below.
                $("#pager").pager("setPage", { page: 0, refresh: false });

                // Refresh those books displayed, based on the new sort.
                refreshBooksList();
            }
        })
        .eq(0).addClass("selected");

        // A "search" text box to do substring searches on book title.
        $("#searchBox").autocomplete({
            source: function () {
                // A pause in typing in our search control should refresh the search results.
                refreshBooksList();
            },
            minLength: 0
        }).watermark();

        // A pager control over books.  We'll configure the pager with a data source as we "switchView" below.
        $("#pager").pager({
            template: "#pageNumberTemplate",
            currentClass: "selected",
            pageSize: pageSize
        });

        // Flagged books should be disabled in the UI while they're being sync'd with the server.
        $([profile.get_FlaggedBooks()]).dataSource().option("entityStateChange", function (flaggedBook, entityState) {
            var savingFlaggedBook = entityState === "ServerAdding" || entityState === "ServerUpdating",
                bookElement = booksList.getElement(flaggedBook.get_Book());
            $(bookElement).toggleClass("disabled", savingFlaggedBook);
        });

        // Trigger a click on "Show Me: All" to fetch our initial page of book data.
        $(".filterButton").eq(0).click();


        //
        // Helper functions
        //

        function switchView(newView) {
            if (currentView) {
                // Destroy the data sources that supported previous view.
                $([books]).dataSource().destroy();
                if (currentView === View.MyBooks) {
                    $([remoteBooks]).dataSource().destroy();
                }

                // Empty our books array, so we can reload it to for the newly selected view.
                $.splice(books, 0, books.length);
            }

            // The friends filter is only used for the "Show Me: Just friends" view.
            $("#friendsList")[newView === View.JustFriends ? "show" : "hide"]();

            // Create a data source to support the newly selected view.
            if (newView === View.MyBooks) {
                // Here, using a remote data source, we load all books flagged for the current profile 
                // into "remoteBooks".  Then, we populate our "books" array (rendered into our list control)
                // using a local data source.  This local data source does (fast) paging/sorting/filtering 
                // in memory, in JavaScript against "remoteBooks".
                remoteBooks = [];
                createRemoteDataSource(remoteBooks);
                $([books]).dataSource({
                    inputData: remoteBooks,
                    entityCollection: books
                });
            } else {
                createRemoteDataSource(books);
            }

            var newDataSource = $([books]).dataSource();

            // Configure our pager with the new data source.
            $("#pager").pager("option", "dataSource", newDataSource);

            // Configure our new data source so that, while a refresh is in progress, we make the UI appear disabled.
            newDataSource
                .option("refreshing", function () { $("#content").addClass("disabled"); })
                .option("refresh", function () { $("#content").removeClass("disabled"); });

            currentView = newView;

            // Have our new data source load the contents of our "books" array.
            refreshBooksList(true);
        };

        function createRemoteDataSource(entityCollection) {
            $([entityCollection]).dataSource({
                serviceUrl: serviceUrl,
                queryName: "GetBooksForSearch",
                queryParameters: remoteBooksQueryParameters,
                dataContext: profileDataSource.dataContext()
                // With "dataContext" here, books from "profile.get_FlaggedBooks().get_Book()" and "books" will be the same objects.
                // See the use of "===" in getFlaggedBook below.
            });
        };

        function refreshBooksList(refreshAll) {
            var dataSource = $([books]).dataSource();

            // Filter books by title, based on the substring from our search box.
            var titleSubstring = $("#searchBox").val() || "";
            dataSource.option("filter", {
                property: "Title", operator: "Contains", value: titleSubstring
            });

            // Determine the profile id's for which we're fetching books.
            switch (currentView) {
                case View.All:
                    remoteBooksQueryParameters.profileIds = null;
                    break;

                case View.MyBooks:
                    remoteBooksQueryParameters.profileIds = [profile.Id].toString();
                    break;

                case View.JustFriends:
                    // Determine the profile id's based on friends selected in our friends filter.
                    remoteBooksQueryParameters.profileIds = $(".friendButton.selected").map(function () {
                        return $(this).data("friendId");
                    }).toArray().toString();
                    break;
            }

            // Determine sort.  Unfortunately, the sort we use for "Rating" and "Might Read" is more complex than
            // a simple {property, direction}-pair, so we have to treat the local and remote querying cases separately.
            // In many apps, database views are designed to turn a complex sort like this into a simpler one over 
            // a single entity type.  We've kept the more complex sort below to better illustrate RIA/JS use.
            var Sort = {
                None: 0,
                Title: 1,
                Author: 2,
                Rating: 3,
                MightRead: 4
            }, currentSort = $(".sortButton.selected").data("book-sort");
            if (currentView === View.MyBooks) {
                remoteBooksQueryParameters.sort = Sort.None;
                $([books]).dataSource().option("sort", getLocalSortFunction(currentSort));

            } else {
                remoteBooksQueryParameters.sort = currentSort;
                remoteBooksQueryParameters.sortAscending = currentSort === Sort.Title || currentSort === Sort.Author;  // TODO -- Make direction selectable throughout.
            }

            // Refresh our books data source.
            dataSource.refresh({ all: refreshAll });

            function getLocalSortFunction(sort) {
                var sortFunction;
                switch (sort) {
                    case Sort.Title:
                    case Sort.Author:
                        sortFunction = { property: sort === Sort.Title ? "Title" : "Author", direction: "ascending" };  // TODO -- Make direction selectable throughout.
                        break;

                    case Sort.Rating:
                        sortFunction = getSortFunction(function getRating(book) {
                            var flaggedBook = getFlaggedBook(book);
                            return !flaggedBook ? -1 : (flaggedBook.IsFlaggedToRead ? 0 : flaggedBook.Rating);
                            // Put not-flagged books at the end of our sorted list by giving them a weighting of -1.
                            // Put flagged-to-read books (not yet rated) after all rated books by giving them a weighting of 0.
                        });
                        break;

                    case Sort.MightRead:
                        sortFunction = getSortFunction(function (book) {
                            var flaggedBook = getFlaggedBook(book);
                            return !flaggedBook ? -1 : (flaggedBook.IsFlaggedToRead ? 6 : flaggedBook.Rating);
                            // Put not-flagged books at the end of our sorted list by giving them a weighting of -1.
                            // Put flagged-to-read books at the top of our sorted list by giving them a weighting of 6.
                        });
                        break;
                }

                return sortFunction;

                function getSortFunction(getWeighting) {
                    return function (book1, book2) {
                        var weighting1 = getWeighting(book1), weighting2 = getWeighting(book2),
                            sortAscending = false,  // TODO -- Make direction selectable throughout.
                            result = weighting1 === weighting2 ? 0 : (weighting1 > weighting2 ? 1 : -1);
                        return sortAscending ? result : -result;
                    };
                };
            };
        };

        function getFlaggedBook(book) {
            return $.grep(profile.get_FlaggedBooks(), function (myFlaggedBook) {
                return myFlaggedBook.get_Book() === book;
                // We can use === here, since our profile data source and books data source both share the same
                // data context (grep for "dataContext:" in this file).
            })[0];
        };

        function enableFlaggingForBook(book, bookElement) {
            var flaggedBook = getFlaggedBook(book),  // Will be null if current profile hasn't yet saved/rated this book.
                $button = $("input:button[name='status']", bookElement),
                ratingChanged;

            if (flaggedBook) {
                // Style the Save button based on initial flaggedBook.Rating value.
                styleSaveBookButton();

                // Clicks on the star rating control are translated onto "flaggedBook.Rating".
                ratingChanged = function (event, value) {
                    $(flaggedBook).setField("Rating", value.rating);
                    styleSaveBookButton();
                };
            } else {
                // If this book has not yet been flagged by the user create a new flagged book 
                flaggedBook = { BookId: book.Id, Rating: 0 };

                // Clicking on the Save button will add the new flagged book entity to "profile.get_FlaggedBooks()".
                $button.click(function () {
                    $.push(profile.get_FlaggedBooks(), flaggedBook);
                    styleSaveBookButton();
                });

                // Clicks on the star rating control are translated onto "flaggedBook.Rating". Also, since the book
                // was not previously flagged, this will also add a new flagged book entity to "profile.get_FlaggedBooks()".
                ratingChanged = function (event, value) {
                    $(flaggedBook).setField("Rating", value.rating);
                    $.push(profile.get_FlaggedBooks(), flaggedBook);
                    styleSaveBookButton();
                };
            }

            // Bind our ratingChanged method to the appropriate event from the starRating control
            $(".star-rating", bookElement)
                .starRating(flaggedBook.Rating)
                .bind("ratingChanged", ratingChanged);

            function styleSaveBookButton() {
                $button
                    .val(flaggedBook.Rating > 0 ? "Done reading" : "Might read it")
                    .removeClass("book-notadded book-saved book-read")
                    .addClass(flaggedBook.Rating > 0 ? "book-read" : "book-saved")
                    .disable();
            };
        };
    };
});
