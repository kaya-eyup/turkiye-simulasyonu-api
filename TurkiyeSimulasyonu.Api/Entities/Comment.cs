namespace TurkiyeSimulasyonu.Api.Entities
{
    public class Comment
    {
        public int Id { get; set; }
        public required string Author { get; set; }
        public required string Body { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public int ItemId { get; set; }
        public Item Item { get; set; } = null!;

    }
}
 