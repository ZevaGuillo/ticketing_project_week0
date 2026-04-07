using FluentAssertions;
using Identity.Application.UseCases.CreateUser;
using Identity.Domain.Entities;
using Identity.Domain.Ports;
using Identity.Domain.ValueObjects;
using Moq;

namespace Identity.UnitTests.UseCases;

public class CreateUserHandlerValidationTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly CreateUserHandler _handler;

    public CreateUserHandlerValidationTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _handler = new CreateUserHandler(_userRepositoryMock.Object, _passwordHasherMock.Object);
    }

    [Fact]
    public async Task Handle_WithExistingEmail_ShouldThrowException()
    {
        var command = new CreateUserCommand("existing@example.com", "Password123!");
        var existingUser = new User("existing@example.com", "hashed");
        
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(command.Email))
            .ReturnsAsync(existingUser);

        var act = () => _handler.Handle(command);

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task Handle_WithPasswordTooShort_ShouldThrowException()
    {
        var command = new CreateUserCommand("new@example.com", "short");
        
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(command.Email))
            .ReturnsAsync((User?)null);

        var act = () => _handler.Handle(command);

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*password*");
    }

    [Fact]
    public async Task Handle_WithNullPassword_ShouldThrowException()
    {
        var command = new CreateUserCommand("new@example.com", "");
        
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(command.Email))
            .ReturnsAsync((User?)null);

        var act = () => _handler.Handle(command);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*password*");
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateUserWithDefaultRole()
    {
        var command = new CreateUserCommand("new@example.com", "ValidPassword123!");
        
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(command.Email))
            .ReturnsAsync((User?)null);
        
        _passwordHasherMock.Setup(x => x.HashPassword(command.Password))
            .Returns("hashed_password");

        var result = await _handler.Handle(command);

        result.Should().NotBeEmpty();
        
        _userRepositoryMock.Verify(x => x.SaveAsync(It.Is<User>(u => 
            u.Email == command.Email && 
            u.Role == Role.User)), Times.Once);
    }

    [Fact]
    public async Task Handle_WithExplicitRole_ShouldCreateUserWithSpecifiedRole()
    {
        var command = new CreateUserCommand("admin@example.com", "AdminPassword123!", Role.Admin);
        
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(command.Email))
            .ReturnsAsync((User?)null);
        
        _passwordHasherMock.Setup(x => x.HashPassword(command.Password))
            .Returns("hashed_password");

        var result = await _handler.Handle(command);

        result.Should().NotBeEmpty();
        
        _userRepositoryMock.Verify(x => x.SaveAsync(It.Is<User>(u => 
            u.Email == command.Email && 
            u.Role == Role.Admin)), Times.Once);
    }

    [Theory]
    [InlineData("Abc123!")]
    [InlineData("Pass1!")]
    [InlineData("12345678")]
    public async Task Handle_WithWeakPassword_ShouldThrowException(string weakPassword)
    {
        var command = new CreateUserCommand("user@example.com", weakPassword);
        
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(command.Email))
            .ReturnsAsync((User?)null);

        var act = () => _handler.Handle(command);

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*password*");
    }

    [Theory]
    [InlineData("ValidPass1!")]
    [InlineData("StrongPassword123!")]
    [InlineData("MyP@ssw0rd")]
    public async Task Handle_WithValidPassword_ShouldSucceed(string validPassword)
    {
        var command = new CreateUserCommand("user@example.com", validPassword);
        
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(command.Email))
            .ReturnsAsync((User?)null);
        
        _passwordHasherMock.Setup(x => x.HashPassword(command.Password))
            .Returns("hashed_password");

        var result = await _handler.Handle(command);

        result.Should().NotBeEmpty();
    }
}
