using RainGutter.Api.Models;

namespace RainGutter.Api.Services;

public interface ILineNotificationService
{
    Task SendNewLeadNotificationAsync(QuoteRequest quote, Lead lead);
    Task SendServiceRequestNotificationAsync(ServiceRequest sr, Job job);
}
