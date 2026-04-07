namespace Identity.Domain.Ports;
using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;

public interface ITokenGenerator
{
    string Generate(User user);
    TokenGeneratorResult GenerateWithExpiration(User user);
}