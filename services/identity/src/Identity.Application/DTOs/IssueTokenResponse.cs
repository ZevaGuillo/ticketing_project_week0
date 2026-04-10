namespace Identity.Application.DTOs;

public record IssueTokenResponse(
    string Token,
    DateTime ExpiresAt,
    string UserRole,
    string UserEmail
);
