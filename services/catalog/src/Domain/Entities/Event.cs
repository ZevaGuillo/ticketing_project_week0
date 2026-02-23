namespace Catalog.Domain.Entities;

public class Event
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public decimal BasePrice { get; set; }
    
    public ICollection<Seat> Seats { get; set; } = new List<Seat>();
}
