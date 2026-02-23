namespace Catalog.Domain.Entities;

public class Seat
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string SectionCode { get; set; } = string.Empty;
    public int RowNumber { get; set; }
    public int SeatNumber { get; set; }
    public decimal Price { get; set; }
    public string Status { get; set; } = "available"; // available, reserved, sold
    
    public Event? Event { get; set; }
}
