using ErrorOr;

namespace OpenKoqis.Humans.Domain.Errors;

public static class LoginErrors
{
    public static Error Empty => Error.Validation(
        code: "Login.Empty",
        description: "Login cannot be empty.");

    public static Error InvalidUsername => Error.Validation(
        code: "Login.InvalidUsername",
        description: "Username should be between 1 and 32 symbols, start with a lowercase latin letter, "
                     + "consist only of lowercase latin letters, digits and single hyphens, and not end with a hyphen.");

    public static Error InvalidEmail => Error.Validation(
        code: "Login.InvalidEmail",
        description: "Email should be a valid address in the form 'name@domain.tld'.");

    public static Error InvalidPhoneNumber => Error.Validation(
        code: "Login.InvalidPhoneNumber",
        description: "Phone number should be in international format: '+' followed by 8-15 digits, including the country code.");
}
