namespace Models.Review;

public class Review
{
    public string? ReviewId { get; set; }
    public string? BookId { get; set; }
    public string? UserId { get; set; }
    public string? Description { get; set; }
    public int Rating { get; set; }
}