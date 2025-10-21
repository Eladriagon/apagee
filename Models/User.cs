namespace Apagee.Models;

[Table("User")]
public class User
{
    [ExplicitKey]
    public required string Uid { get; set; }

    public required string Username { get; set; }

    public required string PassHash { get; set; }

    public string? Email { get; set; }

    public DateTime? LastLogin { get; set; }
}