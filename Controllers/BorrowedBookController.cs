using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Models.BorrowedBook;
using Services.BorrowedBookService;

namespace Controllers.BorrowedBookController;

[ApiController]
[Route("/")]
public class BorrowedBookController : ControllerBase
{
    private readonly IBorrowedBookService _borrowedBook;
    public BorrowedBookController(IBorrowedBookService borrowedBook)
    {
        _borrowedBook = borrowedBook;
    }

    // get all available books to borrow
    [Authorize]
    [HttpGet("/availableBooks")]
    public async Task<IActionResult> GetAvailableBooks()
    {
        try
        {
            return Ok(await _borrowedBook.GetAvailableBooks());
        }
        catch (System.Exception ex)
        {

            return StatusCode(500, new { Message = "Available books cannot be retrieved.", Details = ex.Message });
        }
    }

    // get all borrowed books with book details
    [Authorize]
    [HttpGet("/borrowedBooks")]
    public async Task<IActionResult> GetBorrowedBooks()
    {
        try
        {
            return Ok(await _borrowedBook.GetBorrowedBooks());
        }
        catch (System.Exception ex)
        {

            return StatusCode(500, new { Message = "Borrowed books cannot be retrieved.", Details = ex.Message });
        }
    }

    // get borrow details for all borrowed books
    [Authorize(Policy = "AdminOnly")]
    [HttpGet("/borrowDetails")]
    public async Task<IActionResult> GetAllBorrowedBooksDetail()
    {
        try
        {
            return Ok(await _borrowedBook.GetAllBorrowedBookDetails());
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, new { Message = "Borrowed Books Details cannot be retrieved.", Details = ex.Message });
        }
    }

    // get borrow details for books filtered by criteria defined in borrowed book service
    [Authorize(Policy = "AdminOnly")]
    [HttpGet("/borrowDetails/filter")]
    public async Task<IActionResult> GetAllBorrowedBookDetailsWithFilter([FromQuery] BorrowedBookFilterCriteria filterCriteria)
    {
        try
        {
            var borrowedBooksWithFilter = await _borrowedBook.GetAllBorrowedBookDetailsWithFilter(filterCriteria);
            return Ok(borrowedBooksWithFilter);
        }
        catch (System.Exception ex)
        {

            return BadRequest(new { Message = "Borrowed books with filter cannot be retrieved.", Details = ex.Message });
        }
    }

    // get full details of a single book - book details, borrow details
    [Authorize(Policy = "AdminOnly")]
    [HttpGet("/borrowDetails/{id}")]
    public async Task<IActionResult> GetBorrowedBookDetails(string id)
    {
        try
        {
            var borrowedBookDetails = await _borrowedBook.GetBorrowedBookDetails(id);
            if (borrowedBookDetails == null)
                return NotFound("No borrowed book found with borrow Id: " + id);

            var result = borrowedBookDetails.Select(kv => new
            {
                Book = kv.Key,
                BorrowedBook = kv.Value
            }).ToList();

            return Ok(result);
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, new { Message = "Borrowed book details with borrow Id: " + id + " cannot be retrieved", Details = ex.Message });
        }
    }

    // borrow book - allowed for every authenticated user
    [Authorize]
    [HttpPost("/[controller]/borrow")]
    public async Task<IActionResult> BorrowBook([FromBody] BorrowedBook bookToBorrow)
    {
        try
        {
            await _borrowedBook.BorrowBook(bookToBorrow);
            return Ok("Book borrowed successfully with bookId: " + bookToBorrow.BookId);
        }
        catch (System.Exception ex)
        {
            return BadRequest(new { Message = "Borrow Details payload is invalid", Details = ex.Message });
        }
    }

    // return book - allowed for every authenticated user
    [Authorize]
    [HttpPost("/[controller]/return/{id}")]
    public async Task<IActionResult> ReturnBook(string id)
    {
        try
        {
            await _borrowedBook.ReturnBook(id);
            return Ok("Book Returned Successfully with borrow Id: " + id);
        }
        catch (System.Exception ex)
        {
            return BadRequest(new { Message = "No borrowed book exists with borrow Id: " + id, Details = ex.Message });
        }
    }
}