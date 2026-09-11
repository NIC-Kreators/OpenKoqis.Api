namespace OpenKoqis.Domain.Models.Dto;

public record UserRegistrationDto
{
    // Using as login/unique identifier
    public required string Nickname { get; set; }

    // Passsword that has to hashed
    public required string Password { get; set; }

    // used for FullName in model User
    public required string FullName { get; set; }
}

public record UserLoginDto
{
    //  Used for user search
    public required string Nickname { get; set; }

    // User entered passsword
    public required string Password { get; set; }
}
