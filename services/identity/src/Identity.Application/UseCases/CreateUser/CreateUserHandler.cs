namespace Identity.Application.UseCases.CreateUser;

using Identity.Domain.Ports;
using Identity.Domain.Entities;

public class CreateUserHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private const int MinPasswordLength = 8;

    public CreateUserHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Guid> Handle(CreateUserCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Email))
            throw new ArgumentException("Email is required", nameof(command));

        if (!IsValidEmail(command.Email))
            throw new ArgumentException("Invalid email format", nameof(command));

        if (string.IsNullOrWhiteSpace(command.Password))
            throw new ArgumentException("Password is required", nameof(command));

        if (command.Password.Length < MinPasswordLength)
            throw new ArgumentException($"Password must be at least {MinPasswordLength} characters", nameof(command));

        if (!IsValidPassword(command.Password))
            throw new ArgumentException("Password must contain at least one uppercase letter, one lowercase letter, and one number", nameof(command));

        var existingUser = await _userRepository.GetByEmailAsync(command.Email);
        if (existingUser is not null)
            throw new InvalidOperationException($"User with email {command.Email} already exists");

        var passwordHash = _passwordHasher.HashPassword(command.Password);

        var user = new User(command.Email, passwordHash, command.Role);

        await _userRepository.SaveAsync(user);

        return user.Id;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password)) return false;
        
        bool hasUpper = false, hasLower = false, hasDigit = false;
        
        foreach (char c in password)
        {
            if (char.IsUpper(c)) hasUpper = true;
            else if (char.IsLower(c)) hasLower = true;
            else if (char.IsDigit(c)) hasDigit = true;
        }
        
        return hasUpper && hasLower && hasDigit;
    }
}
