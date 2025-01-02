using Models.Review;
using MySqlConnector;
using Utilities.Utility;

namespace Services.ReviewService;

public interface IReviewService
{
    public Task<List<Review>> GetAllReviewsOfBook(string bookId);
    public Task AddReview(Review review);
}

public class ReviewService : IReviewService
{
    private readonly MySqlConnection _connection;
    public ReviewService(MySqlConnection connection)
    {
        _connection = connection;
    }

    // get all reviews of a single book
    public async Task<List<Review>> GetAllReviewsOfBook(string bookId)
    {
        await _connection.OpenAsync();

        List<Review> reviews = new List<Review>();

        string query = "SELECT Review.* FROM Review LEFT JOIN Book ON Review.bookId = Book.bookId WHERE Book.BookId = @bookId";

        using (MySqlCommand cmd = new MySqlCommand(query, _connection))
        {
            cmd.Parameters.AddWithValue("@bookId", bookId);
            using (MySqlDataReader reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    Review review = new Review()
                    {
                        ReviewId = reader.GetString("ReviewId"),
                        BookId = reader.GetString("BookId"),
                        UserId = reader.GetString("UserId"),
                        Description = reader.GetString("Description"),
                        Rating = reader.GetInt16("Rating")
                    };

                    reviews.Add(review);
                }
            }
        }

        return reviews;
    }

    // add a review for a single book
    public async Task AddReview(Review review)
    {
        await _connection.OpenAsync();

        if (string.IsNullOrEmpty(review.ReviewId)
        || string.IsNullOrEmpty(review.BookId)
        || string.IsNullOrEmpty(review.UserId))
        {
            throw new Exception("Fields cannot be blank or null - Review Id, Book Id, User Id");
        }

        review.Rating = !Utility.IsValidRating(review.Rating)
        ? throw new Exception("Rating needs to between 1 and 5") : review.Rating;

        // check if review already exists - same book Id and user Id
        string reviewExists = "SELECT COUNT(*) FROM Review WHERE BookId = @bookId AND UserId = @userid";

        using (MySqlCommand cmd = new MySqlCommand(reviewExists, _connection))
        {
            cmd.Parameters.AddWithValue("@bookId", review.BookId);
            cmd.Parameters.AddWithValue("@userid", review.UserId);

            int numOfReviews = Convert.ToInt16(await cmd.ExecuteScalarAsync());

            // if review exists update the existing review
            if (numOfReviews > 0)
            {
                List<String> SetClauses = new List<String>();
                SetClauses.Add("ReviewId = @reviewid");
                SetClauses.Add("BookId = @bookid");
                SetClauses.Add("UserId = @userid");
                SetClauses.Add("Description = @description");
                SetClauses.Add("Rating = @rating");

                string filter = " WHERE BookId = @bookid AND UserId = @userid";

                string updateReview = "UPDATE Review SET " + string.Join(", ", SetClauses) + filter;

                using (MySqlCommand updatecmd = new MySqlCommand(updateReview, _connection))
                {
                    updatecmd.Parameters.AddWithValue("@reviewid", review.ReviewId);
                    updatecmd.Parameters.AddWithValue("@bookid", review.BookId);
                    updatecmd.Parameters.AddWithValue("@userid", review.UserId);
                    updatecmd.Parameters.AddWithValue("@description", review.Description);
                    updatecmd.Parameters.AddWithValue("@rating", review.Rating);

                    await updatecmd.ExecuteNonQueryAsync();
                }
            }

            // if review doesn't exist add a review based on user input
            else
            {
                string insertReview = "INSERT INTO Review(ReviewId, BookId, UserId, Description, Rating) ";
                insertReview += "VALUES(@reviewId, @bookId, @userid, @description, @rating)";

                using (MySqlCommand insertcmd = new MySqlCommand(insertReview, _connection))
                {
                    insertcmd.Parameters.AddWithValue("@reviewid", review.ReviewId);
                    insertcmd.Parameters.AddWithValue("@bookid", review.BookId);
                    insertcmd.Parameters.AddWithValue("@userid", review.UserId);
                    insertcmd.Parameters.AddWithValue("@description", review.Description);
                    insertcmd.Parameters.AddWithValue("@rating", review.Rating);

                    await insertcmd.ExecuteNonQueryAsync();
                }
            }
        }
    }
}