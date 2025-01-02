using Models.Book;
using MySqlConnector;
using Utilities.Utility;

// wrapper class to define the filter criteria
public class FilterCriteria
{
    public string? Genre { get; set; }
    public string? Author { get; set; }
    public int? MinRating { get; set; }
    public int? MaxRating { get; set; }

    public int? MinPrice { get; set; }
    public int? MaxPrice { get; set; }
}

public interface IBookService
{
    public Task<List<Book>> GetBooks();
    public Task<Book> GetBookById(string bookId);
    public Task<List<Book>> GetBookByFilterCriteria(FilterCriteria filterCriteria);
    public Task AddBook(Book book);
    public Task ModifyBook(Book book);
    public Task DeleteBook(string bookId);
}

public class BookService : IBookService
{
    private readonly MySqlConnection _connection;

    // inject MySQL connection via constructor
    public BookService(MySqlConnection connection)
    {
        _connection = connection;
    }

    // calculate the rating by calculating the mean of all ratings associated with a book
    public async Task<Decimal> CalculateRatingHelper(string bookId)
    {
        //await _connection.OpenAsync();

        List<int> Ratings = new List<int>();

        string query = "SELECT Review.Rating FROM Review LEFT JOIN Book ON Review.BookId = Book.BookId";

        using (MySqlCommand cmd = new MySqlCommand(query, _connection))
        {
            using (MySqlDataReader reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    int Rating = reader.GetInt16("Rating");
                    Ratings.Add(Rating);
                }
            }
        }

        Console.WriteLine(Ratings);
        Console.WriteLine(Ratings.Sum());
        Console.WriteLine(Ratings.Count);

        // no ratings
        if (Ratings.Count == 0)
            return 0;

        Decimal avgRating = Ratings.Sum() / Ratings.Count;
        return avgRating;
    }

    // get all books in the system
    public async Task<List<Book>> GetBooks()
    {
        List<Book> books = new List<Book>();

        string query = "SELECT * FROM Book";

        await _connection.OpenAsync();

        // prepare the sql query to be executed
        using (MySqlCommand cmd = new MySqlCommand(query, _connection))
        {
            // execute the query
            using (MySqlDataReader reader = await cmd.ExecuteReaderAsync())
            {
                // use the results
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

    // get a single book by book Id
    public async Task<Book> GetBookById(string bookId)
    {
        await _connection.OpenAsync();

        string query = "SELECT * FROM Book WHERE BookId = @bookId";

        using (MySqlCommand cmd = new MySqlCommand(query, _connection))
        {
            cmd.Parameters.AddWithValue("@bookId", bookId);
            using (MySqlDataReader reader = await cmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    return new Book
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
                        Price = reader.GetInt16("Price"),
                        Rating = reader.GetInt16("Rating"),
                    };
                }
            }
        }

        return null;
    }

    // get all books filtered by defined filter criteria
    public async Task<List<Book>> GetBookByFilterCriteria(FilterCriteria filterCriteria)
    {
        await _connection.OpenAsync();

        List<Book> books = new List<Book>();

        List<String> WhereClauses = new List<String>();
        WhereClauses.Add(!string.IsNullOrEmpty(filterCriteria.Genre) ? " Genre = @genre" : "");
        WhereClauses.Add(!string.IsNullOrEmpty(filterCriteria.Author) ? " Author = @author" : "");
        WhereClauses.Add(Utility.IsValidRating(filterCriteria.MinRating) ? " Rating >= @minrating" : "");
        WhereClauses.Add(Utility.IsValidRating(filterCriteria.MaxRating) ? " Rating <= @maxrating" : "");
        WhereClauses.Add(Utility.IsValidPrice(filterCriteria.MinPrice) ? " Price >= @minprice" : "");
        WhereClauses.Add(Utility.IsValidPrice(filterCriteria.MaxPrice) ? " Price <= @maxprice" : "");

        // remove all empty where clauses
        WhereClauses.RemoveAll(x => x == "");

        // make sql query
        string query = "SELECT * FROM Book WHERE" + string.Join(" AND ", WhereClauses);

        using (MySqlCommand cmd = new MySqlCommand(query, _connection))
        {
            cmd.Parameters.AddWithValue("@genre", filterCriteria.Genre);
            cmd.Parameters.AddWithValue("@author", filterCriteria.Author);
            cmd.Parameters.AddWithValue("@minrating", filterCriteria.MinRating);
            cmd.Parameters.AddWithValue("@maxrating", filterCriteria.MaxRating);
            cmd.Parameters.AddWithValue("@minprice", filterCriteria.MinPrice);
            cmd.Parameters.AddWithValue("@maxprice", filterCriteria.MaxPrice);

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

        //Console.WriteLine(query);

        return books;
    }

    public async Task AddBook(Book book)
    {
        await _connection.OpenAsync();
        if (
            string.IsNullOrEmpty(book.BookId)
            || string.IsNullOrEmpty(book.Title)
            || string.IsNullOrEmpty(book.Author)
            || string.IsNullOrEmpty(book.Publisher)
            || string.IsNullOrEmpty(book.ISBN)
            || string.IsNullOrEmpty(book.Genre)
            || book.PublishedDate == null
        )
        {
            throw new Exception("Fields - Title, Author, Publisher, ISBN, Genre, Rating, Published Date cannot be null or empty");
        }

        book.Edition = !Utility.IsValidEdition(book.Edition)
        ? throw new Exception("Edition must be greater than 0 and cannot be empty") : book.Edition;

        book.Price = !Utility.IsValidPrice(book.Price)
        ? throw new Exception("Price cannot be negative or empty") : book.Price;

        book.Description = string.IsNullOrEmpty(book.Description) ? "No Description Available" : book.Description;

        // Rating - set rating for a book as the average of all reviews
        book.Rating = await CalculateRatingHelper(book.BookId);

        string query =
            "INSERT INTO Book(BookId, Title, Author, Publisher, ISBN, Genre, PublishedDate, Edition, Description, Price, Rating) ";
        query +=
            "VALUES(@bookId, @title, @author, @publisher, @isbn, @genre, @publishedDate, @edition, @description, @price, @rating)";

        using (MySqlCommand cmd = new MySqlCommand(query, _connection))
        {
            cmd.Parameters.AddWithValue("@bookId", book.BookId);
            cmd.Parameters.AddWithValue("@title", book.Title);
            cmd.Parameters.AddWithValue("@author", book.Author);
            cmd.Parameters.AddWithValue("@publisher", book.Publisher);
            cmd.Parameters.AddWithValue("@isbn", book.ISBN);
            cmd.Parameters.AddWithValue("@genre", book.Genre);
            cmd.Parameters.AddWithValue("@publishedDate", book.PublishedDate);
            cmd.Parameters.AddWithValue("@edition", book.Edition);
            cmd.Parameters.AddWithValue("@description", book.Description);
            cmd.Parameters.AddWithValue("@price", book.Price);
            cmd.Parameters.AddWithValue("@rating", book.Rating);

            // execute the query
            await cmd.ExecuteNonQueryAsync();
        }
    }

    // modify details of a book
    public async Task ModifyBook(Book book)
    {
        await _connection.OpenAsync();

        List<String> SetClauses = new List<String>();
        SetClauses.Add(string.IsNullOrEmpty(book.Title) ? "" : "Title = @title");
        SetClauses.Add(string.IsNullOrEmpty(book.Author) ? "" : "Author = @author");
        SetClauses.Add(string.IsNullOrEmpty(book.Publisher) ? "" : "Publisher = @publisher");
        SetClauses.Add(string.IsNullOrEmpty(book.ISBN) ? "" : "ISBN = @isbn");
        SetClauses.Add(string.IsNullOrEmpty(book.Genre) ? "" : "Genre = @genre");
        SetClauses.Add(book.PublishedDate == null ? "" : "PublishedDate = @publisheddate");
        SetClauses.Add(book.Edition == null || book.Edition <= 0 ? "" : "Edition = @edition");
        SetClauses.Add(book.Description == null ? "Description = No description available" : "Description = @description");
        SetClauses.Add(book.Price == null || book.Price < 0 ? "" : "Price = @price");
        //SetClauses.Add(book.Rating == null ? "" : "Rating = @rating"); don't need this as this is calculated from Reviews table

        string bookIdFilter = book.BookId == null
        ? throw new Exception("book Id is required to update the book") : " WHERE BookId = @bookId";

        // get the latest rating of the book
        book.Rating = await CalculateRatingHelper(book.BookId);
        SetClauses.Add("Rating = @rating");

        // remove all the empty "" set clauses from the list
        SetClauses.RemoveAll(i => i == "");

        string query = "UPDATE Book SET " + string.Join(", ", SetClauses) + bookIdFilter;

        using (MySqlCommand cmd = new MySqlCommand(query, _connection))
        {
            cmd.Parameters.AddWithValue("@bookId", book.BookId);
            cmd.Parameters.AddWithValue("@title", book.Title);
            cmd.Parameters.AddWithValue("@author", book.Author);
            cmd.Parameters.AddWithValue("@publisher", book.Publisher);
            cmd.Parameters.AddWithValue("@isbn", book.ISBN);
            cmd.Parameters.AddWithValue("@genre", book.Genre);
            cmd.Parameters.AddWithValue("@publisheddate", book.PublishedDate);
            cmd.Parameters.AddWithValue("@edition", book.Edition);
            cmd.Parameters.AddWithValue("@description", book.Description);
            cmd.Parameters.AddWithValue("@price", book.Price);
            cmd.Parameters.AddWithValue("@rating", book.Rating);

            int rowsAffected = await cmd.ExecuteNonQueryAsync();
            if (rowsAffected == 0)
            {
                throw new Exception("No book found with Book Id: " + book.BookId);
            }
        }
    }

    // delete a book - admin only operation
    public async Task DeleteBook(string bookId)
    {
        // null/empty validation check for bookId
        if (string.IsNullOrEmpty(bookId))
        {
            throw new Exception("bookId cannot be null or empty");
        }
        await _connection.OpenAsync();
        string query = "DELETE FROM Book WHERE BookId = @bookId";
        using (MySqlCommand cmd = new MySqlCommand(query, _connection))
        {
            cmd.Parameters.AddWithValue("bookId", bookId);

            int rowsAffected = await cmd.ExecuteNonQueryAsync();

            if (rowsAffected == 0)
            {
                throw new Exception("No Book found with bookId: " + bookId);
            }
        }
    }
}
