using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models.Book;

namespace Controllers.BookController;

[Route("/[controller]")]
[ApiController]
[Authorize]
public class BookController : ControllerBase
{
    private readonly IBookService _bs;

    // inject Book Service into the controller
    public BookController(IBookService bs)
    {
        _bs = bs;
    }

    // all authenticated users can see books in the system
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetBooks()
    {
        try
        {
            var books = await _bs.GetBooks();
            if (books == null)
            {
                return NotFound("No books found");
            }
            return Ok(books);
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, new { Message = "cannot retrieve all books", Details = ex.Message });
        }
    }

    // all authenticated users can see filtered books in the system
    [Authorize]
    [HttpGet("filter")]
    public async Task<IActionResult> GetBooksWithFilter([FromQuery] FilterCriteria filterCriteria)
    {
        try
        {
            var books = await _bs.GetBookByFilterCriteria(filterCriteria);
            return Ok(books);
        }
        catch (System.Exception ex)
        {
            return BadRequest(new { Message = "Filter criter is invalid", Details = ex.Message });
        }
    }

    // allowed for all authenticated users
    [Authorize]
    [HttpGet("{bookId}")]
    public async Task<IActionResult> GetBookById(string bookId)
    {
        try
        {
            var book = await _bs.GetBookById(bookId);
            if (book == null)
            {
                return NotFound("No book found for the Id: " + bookId);
            }
            return Ok(book);
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, new { Message = "cannot retrieve book with the Id: " + bookId, Details = ex.Message });
        }
    }

    // add a book to the system
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> AddBook([FromBody] Book book)
    {
        try
        {
            await _bs.AddBook(book);
            return Created("/Book/{book}", $"Book with bookId: {book.BookId} and Title: {book.Title} added successfully");
        }
        catch (System.Exception ex)
        {
            return BadRequest(new { Message = "Book Data is invalid", Details = ex.Message });
        }
    }

    // modify book details
    [Authorize(Policy = "AdminOnly")]
    [HttpPatch]
    public async Task<IActionResult> ModifyBook([FromBody] Book book)
    {
        try
        {
            await _bs.ModifyBook(book);
            return StatusCode(201, "Book with bookId: " + book.BookId + " updated successfully.");
        }
        catch (System.Exception ex)
        {
            return BadRequest(new { Message = "Book Update failed", Details = ex.Message });
        }
    }

    // delete a book - admin only so that users cannot delete books created by other users
    [Authorize(Policy = "AdminOnly")]
    [HttpDelete("{bookId}")]
    public async Task<IActionResult> DeleteBook(string bookId)
    {
        try
        {
            await _bs.DeleteBook(bookId);
            return Ok("Deleted book with Id: " + bookId);
        }
        catch (System.Exception ex)
        {
            return BadRequest(new { Message = "Failed to delete the book", Details = ex.Message });
        }
    }
}
