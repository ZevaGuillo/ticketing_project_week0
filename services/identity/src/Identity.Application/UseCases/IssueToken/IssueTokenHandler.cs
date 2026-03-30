namespace Identity.Application.UseCases.IssueToken;

using Identity.Domain.Ports;
using Identity.Domain.Entities;

public class IssueTokenHandler
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IPasswordHasher _passwordHasher;

    public IssueTokenHandler(
        IUserRepository userRepository,
        ITokenGenerator tokenGenerator,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _tokenGenerator = tokenGenerator;
        _passwordHasher = passwordHasher;
    }

    public async Task<TokenResult> Handle(IssueTokenCommand command)
    {
        var user = await _userRepository.GetByEmailAsync(command.Email);

        if (user is null)
            throw new Exception("Invalid credentials");

        if (!_passwordHasher.VerifyPassword(command.Password, user.PasswordHash))
            throw new Exception("Invalid credentials");

        var tokenResult = _tokenGenerator.GenerateWithExpiration(user);

        return new TokenResult(tokenResult.Token, tokenResult.ExpiresAt, user.Role, user.Email);
    }
}