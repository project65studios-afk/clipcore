namespace ClipCore.API.Interfaces;

public interface IOrderFulfillmentService
{
    Task FulfillOrderAsync(string sessionId);
}
