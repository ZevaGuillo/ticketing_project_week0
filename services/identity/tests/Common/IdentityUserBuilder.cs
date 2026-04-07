using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;

namespace Identity.IntegrationTests.Common;

public class IdentityUserBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _email = "test@example.com";
    private string _passwordHash = "hashed_password";
    private Role _role = Role.User;

    public IdentityUserBuilder() { }

    public IdentityUserBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public IdentityUserBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public IdentityUserBuilder WithPasswordHash(string passwordHash)
    {
        _passwordHash = passwordHash;
        return this;
    }

    public IdentityUserBuilder WithRole(Role role)
    {
        _role = role;
        return this;
    }

    public IdentityUserBuilder AsAdmin()
    {
        _role = Role.Admin;
        return this;
    }

    public IdentityUserBuilder AsUser()
    {
        _role = Role.User;
        return this;
    }

    public static IdentityUserBuilder Default() 
    {
        return new IdentityUserBuilder();
    }

    public static IdentityUserBuilder Admin() 
    {
        var builder = new IdentityUserBuilder();
        return builder.AsAdmin();
    }

    public static IdentityUserBuilder Guest() 
    {
        var builder = new IdentityUserBuilder();
        return builder.WithEmail("guest@example.com");
    }

    public User Build()
    {
        var user = new User(_email, _passwordHash, _role);
        
        var idProperty = typeof(User).GetProperty("Id", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (idProperty != null && idProperty.CanWrite)
        {
            idProperty.SetValue(user, _id);
        }
        
        return user;
    }
}
