using FluentAssertions;
using Identity.Application.UseCases.IssueToken;
using Identity.Domain.Entities;
using Identity.Domain.Ports;
using Identity.Domain.ValueObjects;
using Moq;
using Xunit;

namespace Identity.UnitTests.Application;

public class IssueTokenHandlerValidationTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ITokenGenerator> _tokenGeneratorMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly IssueTokenHandler _handler;

    public IssueTokenHandlerValidationTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _tokenGeneratorMock = new Mock<ITokenGenerator>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        
        _handler = new IssueTokenHandler(
            _userRepositoryMock.Object,
            _tokenGeneratorMock.Object,
            _passwordHasherMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ShouldReturnTokenWithCorrectRole()
    {
        var command = new IssueTokenCommand("user@example.com", "password123");
        var user = new User("user@example.com", "hashed_password", Role.User);
        
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email))
            .ReturnsAsync(user);
        
        _passwordHasherMock.Setup(h => h.VerifyPassword(command.Password, user.PasswordHash))
            .Returns(true);
            
        _tokenGeneratorMock.Setup(g => g.GenerateWithExpiration(user))
            .Returns(new TokenGeneratorResult("valid_token", DateTime.UtcNow.AddHours(2)));

        var result = await _handler.Handle(command);

        result.Should().NotBeNull();
        result.UserRole.Should().Be(Role.User);
        result.UserEmail.Should().Be(user.Email);
    }

    [Fact]
    public async Task Handle_WithAdminRole_ShouldReturnTokenWithAdminRole()
    {
        var command = new IssueTokenCommand("admin@example.com", "adminpass123");
        var adminUser = new User("admin@example.com", "hashed_password", Role.Admin);
        
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email))
            .ReturnsAsync(adminUser);
        
        _passwordHasherMock.Setup(h => h.VerifyPassword(command.Password, adminUser.PasswordHash))
            .Returns(true);
            
        _tokenGeneratorMock.Setup(g => g.GenerateWithExpiration(adminUser))
            .Returns(new TokenGeneratorResult("admin_token", DateTime.UtcNow.AddHours(2)));

        var result = await _handler.Handle(command);

        result.Should().NotBeNull();
        result.UserRole.Should().Be(Role.Admin);
    }

    [Fact]
    public async Task Handle_WithNullEmail_ShouldThrowException()
    {
        var command = new IssueTokenCommand("", "password123");
        
        var act = () => _handler.Handle(command);

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task Handle_WithNullPassword_ShouldThrowException()
    {
        var command = new IssueTokenCommand("user@example.com", "");
        
        var act = () => _handler.Handle(command);

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*credentials*");
    }

    [Fact]
    public async Task Handle_WithTokenExpiration_ShouldReturnFutureExpirationTime()
    {
        var command = new IssueTokenCommand("user@example.com", "password123");
        var user = new User("user@example.com", "hashed_password");
        
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email))
            .ReturnsAsync(user);
        
        _passwordHasherMock.Setup(h => h.VerifyPassword(command.Password, user.PasswordHash))
            .Returns(true);
            
        _tokenGeneratorMock.Setup(g => g.GenerateWithExpiration(It.IsAny<User>()))
            .Returns(new TokenGeneratorResult("valid_token", DateTime.UtcNow.AddHours(2)));

        var result = await _handler.Handle(command);

        result.Should().NotBeNull();
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Handle_ConcurrentRequests_ShouldReturnDifferentTokens()
    {
        var command = new IssueTokenCommand("user@example.com", "password123");
        var user = new User("user@example.com", "hashed_password");
        
        int callCount = 0;
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email))
            .ReturnsAsync(user);
        
        _passwordHasherMock.Setup(h => h.VerifyPassword(command.Password, user.PasswordHash))
            .Returns(true);
            
        _tokenGeneratorMock.Setup(g => g.GenerateWithExpiration(user))
            .Returns(() => new TokenGeneratorResult($"token_{++callCount}", DateTime.UtcNow.AddHours(2)));

        var result1 = await _handler.Handle(command);
        var result2 = await _handler.Handle(command);

        result1.AccessToken.Should().NotBe(result2.AccessToken);
    }
}
