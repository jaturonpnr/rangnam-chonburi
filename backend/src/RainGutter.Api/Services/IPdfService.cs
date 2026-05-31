using RainGutter.Api.Models;

namespace RainGutter.Api.Services;

public interface IPdfService
{
    byte[] GenerateQuotePdf(QuoteRequest quote, Lead lead, ShopProfile shop);
}
