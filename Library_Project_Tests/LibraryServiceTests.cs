using Library_Project.Model;
using Library_Project.Services;
using Library_Project.Services.Interfaces;
using Library_Project.Services;
using Moq;

namespace Library_Project_Tests
{
    public class LibraryServiceTests
    {
        private readonly Mock<IBookRepository> _repoMock;
        private readonly Mock<IMemberService> _memberMock;
        private readonly Mock<INotificationService> _notifMock;
        private readonly LibraryService _service;

        public LibraryServiceTests()
        {
            _repoMock = new Mock<IBookRepository>();
            _memberMock = new Mock<IMemberService>();
            _notifMock = new Mock<INotificationService>();

            _service = new LibraryService(_repoMock.Object, _memberMock.Object, _notifMock.Object);
        }

        /// <summary>
        /// Verifies that when an existing book is added, its copies are summed up.
        /// </summary>
        [Fact]
        public void AddBook_ExistingBook_CopiesCountIncreaseValid()
        {
            // Arrange
            const string title = "Harry Potter";
            const int startCopiesCount = 2;
            const int addedCopies = 3;
            const int expectedCopiesCount = startCopiesCount + addedCopies;
            var testBook = new Book { Title = title, Copies = startCopiesCount };

            _repoMock.Setup(r => r.FindBook(title)).Returns(testBook);

            // Act
            _service.AddBook(title, addedCopies);

            // Assert
            Assert.Equal(expectedCopiesCount, testBook.Copies);
        }

        /// <summary>
        /// Verifies that the repository's save method is called exactly once when adding an existing book.
        /// </summary>
        [Fact]
        public void AddBook_ExistingBook_CallsRepositorySaveOnce()
        {
            // Arrange
            const string title = "Harry Potter";
            var testBook = new Book { Title = title, Copies = 2 };

            _repoMock.Setup(r => r.FindBook(title)).Returns(testBook);

            // Act
            _service.AddBook(title, 3);

            // Assert
            _repoMock.Verify(r => r.SaveBook(testBook), Times.Once);
        }

        /// <summary>
        /// Verifies that passing invalid title parameters throws an ArgumentException.
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void AddBook_InvalidTitle_ThrowsArgumentException(string invalidTitle)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _service.AddBook(invalidTitle, 5));
        }

        /// <summary>
        /// Verifies that passing zero or negative copies throws an ArgumentException.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void AddBook_InvalidCopies_ThrowsArgumentException(int invalidCopies)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _service.AddBook("Valid Title", invalidCopies));
        }

        /// <summary>
        /// Verifies that successful book borrowing triggers notification.
        /// </summary>
        [Fact]
        public void BorrowBook_ValidConditions_NotifiesBorrowIsCalled()
        {
            // Arrange
            const int memberId = 1;
            const string title = "C# Programming";
            var book = new Book { Title = title, Copies = 3 };

            _memberMock.Setup(m => m.IsValidMember(memberId)).Returns(true);
            _repoMock.Setup(r => r.FindBook(title)).Returns(book);

            // Act
            _service.BorrowBook(memberId, title);

            // Assert
            _notifMock.Verify(n => n.NotifyBorrow(memberId, title));
        }

        /// <summary>
        /// Verifies that borrowing a book with zero copies not decrese book copies count.
        /// </summary>
        [Fact]
        public void BorrowBook_ZeroCopies_CopiesCountNotDecresed()
        {
            // Arrange
            const int memberId = 1;
            const string title = "The Bullfighters from Vasyukivka";
            var book = new Book { Title = title, Copies = 0 };

            _memberMock.Setup(m => m.IsValidMember(memberId)).Returns(true);
            _repoMock.Setup(r => r.FindBook(title)).Returns(book);

            // Act
            bool sucsesfull = _service.BorrowBook(memberId, title);

            // Assert
            Assert.NotEqual(-1, book.Copies);

        }

        /// <summary>
        /// Verifies that borrowing a book with zero copies doesn`t trigger notification for anyone about anything.
        /// </summary>
        [Fact]
        public void BorrowBook_ZeroCopies_NeverNotifies()
        {
            // Arrange
            const int memberId = 1;
            const string title = "The man who mistook his wife for a hat";
            var book = new Book { Title = title, Copies = 0 };

            _memberMock.Setup(m => m.IsValidMember(memberId)).Returns(true);
            _repoMock.Setup(r => r.FindBook(title)).Returns(book);

            // Act
            bool sucsesfull = _service.BorrowBook(memberId, title);

            // Assert
            _notifMock.Verify(n => n.NotifyBorrow(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Checks that, when a nonexistent book is returned, the method signals this by returning "false" as the result
        /// </summary>
        [Fact]
        public void ReturnBook_BookNotExist_ReturnFalse()
        {
            // Arrange
            const string title = "Harry Proper";
            const int memberId = 1;

            _repoMock.Setup(r => r.FindBook(title)).Returns((Book)null!);

            // Act
            bool sucsesfull = _service.ReturnBook(memberId, title);

            // Assert
            Assert.False(sucsesfull);
        }

        /// <summary>
        /// Cheks that successful book return triggers return notification one time to correct member with correct title.
        /// </summary>
        [Fact]
        public void ReturnBook_ValidBook_NotifiesCorrectMemberAboutCorrectTitle()
        {
            // Arrange
            const int memberId = 2;
            const string title = "Clean Code";
            var book = new Book { Title = title, Copies = 1 };

            _repoMock.Setup(r => r.FindBook(title)).Returns(book);

            // Act
            _service.ReturnBook(memberId, title);

            // Assert
            _notifMock.Verify(n => n.NotifyReturn(It.Is<int>(id => id == memberId), It.Is<string>(name => name.Equals(title))), Times.Once);
        }

        /// <summary>
        /// Cheks that GetAvailableBooks returns only books with copies greater than zero.
        /// </summary>
        [Fact]
        public void GetAvailableBooks_ListOfBooksWithZeroAndMoreCount_ReturnListOfBooksWithMoreThanZeroCount()
        {
            // Arrange
            var list = new List<Book>
            {
                new Book { Title = "A", Copies = 2 },
                new Book { Title = "B", Copies = 0 }
            };
            _repoMock.Setup(r => r.GetAllBooks()).Returns(list);

            // Act
            var outputedList = _service.GetAvailableBooks();

            // Assert
            Assert.True(outputedList.All(x => x.Copies > 0));
        }

        /// <summary>
        /// Verifies that GetAvailableBooks returns a non-empty collection when exists books with field Copies > 0.
        /// </summary>
        [Fact]
        public void GetAvailableBooks_WhenBooksExist_ReturnsNonEmptyCollection()
        {
            // Arrange
            var books = new List<Book> { new Book { Title = "Algorithm", Copies = 4 } };
            _repoMock.Setup(r => r.GetAllBooks()).Returns(books);

            // Act
            var result = _service.GetAvailableBooks();

            // Assert
            Assert.NotEmpty(result);
        }

    }
}
