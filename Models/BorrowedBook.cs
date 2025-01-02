namespace Models.BorrowedBook;

public enum BorrowStatus
{
    Borrowed,
    Returned,
    Overdue
}

public class BorrowedBook
{
    public string? BorrowId { get; set; }
    public string? BookId { get; set; }
    public string? UserId { get; set; }
    public DateOnly BorrowDate { get; set; }
    public DateOnly ReturnDate { get; set; }
    public DateOnly? ActualReturnDate { get; set; }
    public string? Status { get; set; }
    public decimal FineAmount { get; set; }
}