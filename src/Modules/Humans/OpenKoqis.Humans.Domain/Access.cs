using ErrorOr;
using OpenKoqis.Shared.Kernel;

namespace OpenKoqis.Humans.Domain;

public class Access : ValueObject
{
    public required Action Actions { get; init; }
    public required string Resource { get; init; }

    private Access() { }

    public static ErrorOr<Access> Parse(byte actions, string resource)
    {
        if (!Enum.IsDefined(typeof(Action), actions))
            return Error.Validation(
                code: "Access.InvalidActionValue",
                description: "Action cannot exceed 4 bits, as it has only 4 values");

        if (resource.Length is < 1 or > 32)
            return Error.Validation(
                code: "Access.InvalidResourceLength",
                description: "Resource length can be between 1 and 32 symbols");

        return new Access
        {
            Actions = (Action)actions,
            Resource = resource
        };
    }

    protected override IEnumerable<object> GetEqualityComponents() => [Actions, Resource];
}
