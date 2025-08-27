namespace api.Models;

public class SquareEventRaw
{
    public long Id { get; set; }
    public int StoreId { get; set; }
    public string EventType { get; set; } = "";
    public string Signature { get; set; } = "";
    public string Payload { get; set; } = "";
    public DateTime ReceivedAt { get; set; }
    public bool Processed { get; set; }
}
