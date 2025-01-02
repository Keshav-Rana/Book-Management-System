using Models.Book;
using Models.BorrowedBook;
using MySqlConnector;
using Utilities.Utility;

namespace Services.BorrowedBookService;

// wrapper class for borrowed book filter
public class BorrowedBookFilterCriteria
{
    public DateOnly MinBorrowDate { get; set; }
    public DateOnly MaxBorrowDate { get; set; }
    public DateOnly MinReturnDate { get; set; }
    public DateOnly MaxReturnDate { get; set; }
    public string? Status { get; set; }
}

public interface IBorrowedBookService
{
    public Task<List<Book>> GetAvailableBooks();
    public Task<List<Book>> GetBorrowedBooks();
    public Task<List<BorrowedBook>> GetAllBorrowedBookDetails();
    public Task<List<BorrowedBook>> GetAllBorrowedBookDetailsWithFilter(BorrowedBookFilterCriteria filterCriteria);
    public Task<Dictionary<Book, BorrowedBook>> GetBorrowedBookDetails(string borrowId);
    public Task BorrowBook(BorrowedBook borrowBook);
    public Task ReturnBook(string borrowId);
}

public class BorrowedBookService : IBorrowedBookService
{
    private readonly MySqlConnection _connection;
    public BorrowedBookService(MySqlConnection connection)
    {
        _connection = connection;
    }

    // the fine amount is 50 cents per day, this function calculates the fine for a user who has a book overdue
    public Decimal CalculateFineAmountHelper(DateOnly returnDate, DateOnly actualReturnDate)
    {
        int daysBetween = actualReturnDate.DayNumber - returnDate.DayNumber;

        if (daysBetween > 0)
            return (Decimal)0.5 * daysBetween;

        else
            return 0;
    }

    // get all available books not borrowed
    public async Task<List<Book>> GetAvailableBooks()
    {
        await _connection.OpenAsync();

        List<Book> books = new List<Book>();

        string query = "SELECT Book.* FROM Book LEFT JOIN BorrowedBook ON Book.bookId = BorrowedBook.bookId";
        query += " WHERE BorrowedBook.Status IS NULL OR BorrowedBook.Status = 'returned' OR BorrowedBook.Status = 'overdue'";

        using (MySqlCommand cmd = new MySqlCommand(query, _connection))
        {
            using (MySqlDataReader reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    Book book = new()
                    {
                        BookId = reader.GetString("BookId"),
                        Title = reader.GetString("Title"),
                        Author = reader.GetString("Author"),
                        Publisher = reader.GetString("Publisher"),
                        ISBN = reader.GetString("ISBN"),
                        Genre = reader.GetString("Genre"),
                        PublishedDate = reader.GetDateOnly("PublishedDate"),
                        Edition = reader.GetInt16("Edition"),
                        Description = reader.GetString("Description"),
                        Price = reader.GetDecimal("Price"),
                        Rating = reader.GetInt16("Rating"),
                    };

                    books.Add(book);
                }
            }
        }

        return books;
    }

    // get all borrowed books, doesn't give user details
    public async Task<List<Book>> GetBorrowedBooks()
    {
        await _connection.OpenAsync();

        List<Book> books = new List<Book>();

        string query = "SELECT Book.* FROM Book LEFT JOIN BorrowedBook ON Book.bookId = BorrowedBook.bookId";
        query += " WHERE BorrowedBook.status = 'borrowed'";

        using (MySqlCommand cmd = new MySqlCommand(query, _connection))
        {
            using (MySqlDataReader reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    Book book = new()
                    {
                        BookId = reader.GetString("BookId"),
                        Title = reader.GetString("Title"),
                        Author = reader.GetString("Author"),
                        Publisher = reader.GetString("Publisher"),
                        ISBN = reader.GetString("ISBN"),
                        Genre = reader.GetString("Genre"),
                        PublishedDate = reader.GetDateOnly("PublishedDate"),
                        Edition = reader.GetInt16("Edition"),
                        Description = reader.GetString("Description"),
                        Price = reader.GetDecimal("Price"),
                        Rating = reader.GetInt16("Rating"),
                    };

                    books.Add(book);
                }
            }
        }

        return books;
    }

    // get all borrowed book details
    public async Task<List<BorrowedBook>> GetAllBorrowedBookDetails()
    {
        await _connection.OpenAsync();

        List<BorrowedBook> borrowedBooks = new List<BorrowedBook>();

        string query = "SELECT * FROM BorrowedBook";

        using (MySqlCommand cmd = new MySqlCommand(query, _connection))
        {
            using (MySqlDataReader reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    // adding Published data null check because it can be null in some scenarios
                    // get the index of the column ActualReturnDate in the database table
                    int ActualReturnDateIdx = reader.GetOrdinal("ActualReturnDate");

                    DateOnly? ActualReturnDateVal = reader.IsDBNull(ActualReturnDateIdx) ? null : reader.GetDateOnly("ActualReturnDate");

                    BorrowedBook borrowedBook = new BorrowedBook()
                    {
                        BorrowId = reader.GetString("BorrowId"),
                        BookId = reader.GetString("BookId"),
                        UserId = reader.GetString("UserId"),
                        BorrowDate = reader.GetDateOnly("BorrowDate"),
                        ReturnDate = reader.GetDateOnly("ReturnDate"),
                        ActualReturnDate = ActualReturnDateVal,
                        Status = reader.GetString("Status"),
                        FineAmount = reader.GetDecimal("FineAmount")
                    };

                    borrowedBooks.Add(borrowedBook);
                }
            }
        }

        return borrowedBooks;
    }

    // get borrowed book details with borrowed book filter criteria
    public async Task<List<BorrowedBook>> GetAllBorrowedBookDetailsWithFilter(BorrowedBookFilterCriteria filterCriteria)
    {
        await _connection.OpenAsync();

        List<BorrowedBook> borrowedBooks = new List<BorrowedBook>();

        List<String> WhereClauses = new List<String>();
        WhereClauses.Add(Utility.IsValidDate(filterCriteria.MinBorrowDate) ? " BorrowDate >= @minborrowdate" : "");
        WhereClauses.Add(Utility.IsValidDate(filterCriteria.MaxBorrowDate) ? " BorrowDate <= @maxborrowdate" : "");
        WhereClauses.Add(Utility.IsValidDate(filterCriteria.MinReturnDate) ? " ReturnDate >= @minreturndate" : "");
        WhereClauses.Add(Utility.IsValidDate(filterCriteria.MaxReturnDate) ? " ReturnDate <= @maxreturndate" : "");
        WhereClauses.Add(filterCriteria.Status == "returned" || filterCriteria.Status == "borrowed" || filterCriteria.Status == "overdue" ? " Status = @status" : "");

        // remove all empty clauses
        WhereClauses.RemoveAll(x => x == "");

        string query = "SELECT * FROM BorrowedBook WHERE " + string.Join(" AND ", WhereClauses);

        Console.WriteLine(query);

        using (MySqlCommand cmd = new MySqlCommand(query, _connection))
        {
            cmd.Parameters.AddWithValue("@minborrowdate", filterCriteria.MinBorrowDate);
            cmd.Parameters.AddWithValue("@maxborrowdate", filterCriteria.MaxBorrowDate);
            cmd.Parameters.AddWithValue("@minreturndate", filterCriteria.MinReturnDate);
            cmd.Parameters.AddWithValue("@maxreturndate", filterCriteria.MaxReturnDate);
            cmd.Parameters.AddWithValue("@status", filterCriteria.Status);

            using (MySqlDataReader reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    // adding Published data null check because it can be null in some scenarios
                    // get the index of the column ActualReturnDate in the database table
                    int ActualReturnDateIdx = reader.GetOrdinal("ActualReturnDate");

                    DateOnly? ActualReturnDateVal = reader.IsDBNull(ActualReturnDateIdx) ? null : reader.GetDateOnly("ActualReturnDate");

                    BorrowedBook borrowedBook = new BorrowedBook()
                    {
                        BorrowId = reader.GetString("BorrowId"),
                        BookId = reader.GetString("BookId"),
                        UserId = reader.GetString("UserId"),
                        BorrowDate = reader.GetDateOnly("BorrowDate"),
                        ReturnDate = reader.GetDateOnly("ReturnDate"),
                        ActualReturnDate = ActualReturnDateVal,
                        Status = reader.GetString("Status"),
                        FineAmount = reader.GetDecimal("FineAmount")
                    };

                    borrowedBooks.Add(borrowedBook);
                }
            }
        }

        return borrowedBooks;
    }

    // gives full details of a single borrowed book including the book details and the borrow details
    public async Task<Dictionary<Book, BorrowedBook>> GetBorrowedBookDetails(string borrowId)
    {
        await _connection.OpenAsync();

        Dictionary<Book, BorrowedBook> allDetailsOfBook = new Dictionary<Book, BorrowedBook>();

        string query = "SELECT * FROM Book LEFT JOIN BorrowedBook ON Book.bookId = BorrowedBook.bookId";
        query += " WHERE BorrowedBook.BorrowId = @borrowId";

        using (MySqlCommand cmd = new MySqlCommand(query, _connection))
        {
            cmd.Parameters.AddWithValue("@borrowId", borrowId);
            using (MySqlDataReader reader = await cmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    Book bookDetails = new()
                    {
                        BookId = reader.GetString("BookId"),
                        Title = reader.GetString("Title"),
                        Author = reader.GetString("Author"),
                        Publisher = reader.GetString("Publisher"),
                        ISBN = reader.GetString("ISBN"),
                        Genre = reader.GetString("Genre"),
                        PublishedDate = reader.GetDateOnly("PublishedDate"),
                        Edition = reader.GetInt16("Edition"),
                        Description = reader.GetString("Description"),
                        Price = reader.GetDecimal("Price"),
                        Rating = reader.GetInt16("Rating"),
                    };

                    int ActualReturnDateIdx = reader.GetOrdinal("ActualReturnDate");

                    DateOnly? ActualReturnDateVal = reader.IsDBNull(ActualReturnDateIdx) ? null : reader.GetDateOnly("ActualReturnDate");

                    BorrowedBook bookBorrowDetails = new()
                    {
                        BorrowId = reader.GetString("BorrowId"),
                        BookId = reader.GetString("BookId"),
                        UserId = reader.GetString("UserId"),
                        BorrowDate = reader.GetDateOnly("BorrowDate"),
                        ReturnDate = reader.GetDateOnly("ReturnDate"),
                        ActualReturnDate = ActualReturnDateVal,
                        Status = reader.GetString("Status"),
                        FineAmount = reader.GetDecimal("FineAmount")
                    };

                    allDetailsOfBook.Add(bookDetails, bookBorrowDetails);

                    return allDetailsOfBook;
                }
            }
        }

        return null;
    }

    public async Task BorrowBook(BorrowedBook borrowBook)
    {
        await _connection.OpenAsync();

        // if the book Id and User Id combination already exists in the table, then update it's status to borrowed
        string checkQuery = "SELECT BookId, UserId FROM BorrowedBook WHERE BookId = @bookId AND UserId = @userid";
        bool bookExists = false;

        using (MySqlCommand cmd = new MySqlCommand(checkQuery, _connection))
        {
            cmd.Parameters.AddWithValue("@bookid", borrowBook.BookId);
            cmd.Parameters.AddWithValue("@userid", borrowBook.UserId);

            using (MySqlDataReader reader = await cmd.ExecuteReaderAsync())
            {
                // book exists
                if (await reader.ReadAsync()) { bookExists = true; }
            }
        }

        string updateStatus = "UPDATE BorrowedBook SET BorrowId = @borrowid, Status = 'borrowed' WHERE BookId = @bookid AND UserId = @userid";

        if (bookExists)
        {
            using (MySqlCommand cmd = new MySqlCommand(updateStatus, _connection))
            {
                cmd.Parameters.AddWithValue("@borrowid", borrowBook.BorrowId);
                cmd.Parameters.AddWithValue("@bookid", borrowBook.BookId);
                cmd.Parameters.AddWithValue("@userid", borrowBook.UserId);

                await cmd.ExecuteNonQueryAsync();
            }
        }

        else
        {
            if (string.IsNullOrEmpty(borrowBook.BorrowId) || string.IsNullOrEmpty(borrowBook.BookId) || string.IsNullOrEmpty(borrowBook.UserId))
            {
                throw new Exception("Field cannot be empty or null - Borrow Id, Book Id, User Id");
            }

            // // validate Borrow date
            // borrowBook.BorrowDate = Utility.IsValidDate(borrowBook.BorrowDate)
            // ? borrowBook.BorrowDate : throw new Exception("borrow Date is not valid");

            borrowBook.BorrowDate = DateOnly.FromDateTime(DateTime.Today);

            borrowBook.ReturnDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7));

            // // validate actual return date
            // borrowBook.ActualReturnDate = Utility.IsValidDate(borrowBook.ActualReturnDate)
            // ? borrowBook.ActualReturnDate : throw new Exception("Return date is not valid");

            // validate status
            borrowBook.Status = string.IsNullOrEmpty(borrowBook.Status) || Utility.IsValidBorrowStatus(borrowBook.Status.ToLower())
            ? borrowBook.Status : throw new Exception("Status is not valid. It should be either empty or returned in order to borrow the book");

            // set the status to borrowed
            borrowBook.Status = "borrowed";

            borrowBook.FineAmount = 0;

            string query = "INSERT INTO BorrowedBook(BorrowId, BookId, UserId, BorrowDate, ReturnDate, ";
            query += "Status, FineAmount) ";
            query += "VALUES(@borrowid, @bookid, @userid, @borrowdate, @returndate, @status, @fineamount)";

            using (MySqlCommand cmd = new MySqlCommand(query, _connection))
            {
                cmd.Parameters.AddWithValue("@borrowid", borrowBook.BorrowId);
                cmd.Parameters.AddWithValue("@bookid", borrowBook.BookId);
                cmd.Parameters.AddWithValue("@userid", borrowBook.UserId);
                cmd.Parameters.AddWithValue("@borrowdate", borrowBook.BorrowDate);
                cmd.Parameters.AddWithValue("@returndate", borrowBook.ReturnDate);
                //cmd.Parameters.AddWithValue("@actualreturndate", borrowBook.ActualReturnDate);
                cmd.Parameters.AddWithValue("@status", borrowBook.Status);
                cmd.Parameters.AddWithValue("@fineamount", borrowBook.FineAmount);

                await cmd.ExecuteNonQueryAsync();
            }
        }
    }

    public async Task ReturnBook(string borrowId)
    {
        await _connection.OpenAsync();

        // retrieve - returnDate, actualReturnDate of the book
        string borrowedBookDetails = "SELECT ReturnDate, Status FROM BorrowedBook WHERE BorrowId = @borrowId";
        DateOnly returnDate = DateOnly.MinValue, actualReturnDate = DateOnly.FromDateTime(DateTime.Today);
        string status = "";
        bool borrowedBookFound = false;

        using (MySqlCommand cmd = new MySqlCommand(borrowedBookDetails, _connection))
        {
            cmd.Parameters.AddWithValue("@borrowId", borrowId);
            using (MySqlDataReader reader = await cmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    returnDate = reader.GetDateOnly("ReturnDate");
                    status = reader.GetString("Status");
                    borrowedBookFound = true;
                }
            }
        }

        if (!borrowedBookFound || status == "returned" || status == "overdue")
        {
            throw new Exception("No borrowed book found with borrow Id: " + borrowId);
        }

        // calculate FineAmount if applicate and update status of book
        Decimal fineAmount = CalculateFineAmountHelper(returnDate, actualReturnDate);

        // set the correct status
        status = fineAmount != 0 ? "overdue" : "returned";

        string whereClause = " WHERE BorrowId = @borrowId";

        string query =
        "UPDATE BorrowedBook SET Status = @status, ActualReturnDate = @actualreturndate, FineAmount = @fineamount" + whereClause;

        using (MySqlCommand cmd = new MySqlCommand(query, _connection))
        {
            cmd.Parameters.AddWithValue("@borrowId", borrowId);
            cmd.Parameters.AddWithValue("@actualreturndate", actualReturnDate);
            cmd.Parameters.AddWithValue("@fineamount", fineAmount);
            cmd.Parameters.AddWithValue("@status", status);

            int rowsAffected = await cmd.ExecuteNonQueryAsync();
        }
    }
}