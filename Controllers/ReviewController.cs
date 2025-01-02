using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models.Review;
using Services.ReviewService;

namespace Controllers.ReviewController;

[ApiController]
[Route("/[controller]")]
public class ReviewController : ControllerBase
{
    private readonly IReviewService _review;
    public ReviewController(IReviewService review)
    {
        _review = review;
    }

    // default policy - anyone can see reviews of a book
    [Authorize]
    [HttpGet("{bookId}")]
    public async Task<IActionResult> GetAllReviewOfBook(string bookId)
    {
        try
        {
            return Ok(await _review.GetAllReviewsOfBook(bookId));
        }
        catch (System.Exception ex)
        {

            return StatusCode(500, new { Message = "Cannot retrieve reviews of the book with book Id: " + bookId, Details = ex.Message });
        }
    }

    // default policy - anyone can add a review
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> AddReview([FromBody] Review review)
    {
        try
        {
            await _review.AddReview(review);
            return Created("/reviews", "Added review for the book with book Id: " + review.BookId);
        }
        catch (System.Exception ex)
        {

            return BadRequest(new { Message = "Cannot add review", Details = ex.Message });
        }
    }
}