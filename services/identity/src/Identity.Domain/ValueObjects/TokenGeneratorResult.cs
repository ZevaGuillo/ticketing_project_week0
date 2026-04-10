namespace Identity.Domain.ValueObjects;

public record TokenGeneratorResult(
    string Token,
    DateTime ExpiresAt
);
