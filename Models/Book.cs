using System.ComponentModel.DataAnnotations;

namespace Models.Book;

public class Book
{
    public string? BookId { get; set; }
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? Publisher { get; set; }
    public string? ISBN { get; set; }
    public string? Genre { get; set; }
    public DateOnly? PublishedDate { get; set; }
    public int? Edition { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public decimal? Rating { get; set; }
}
