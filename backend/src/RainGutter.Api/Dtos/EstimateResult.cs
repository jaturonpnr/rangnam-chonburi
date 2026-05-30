namespace RainGutter.Api.Dtos;

public record BreakdownItem(string Label, decimal Amount);

public record EstimateResult(
    IReadOnlyList<BreakdownItem> Breakdown,
    decimal Total,
    string Disclaimer
);
