
using Microsoft.AspNetCore.Identity;
using System.Text;

var studentId = "20152691234567894";

Console.WriteLine("ADMIN PASSWORD RESET");
Console.WriteLine("-------------------");

Console.Write("Enter your new password: ");
var password = ReadPassword();

if (password.Length < 8 || password.Length > 128)
{
    Console.WriteLine("Password must be 8-128 characters.");
    return;
}

var hasher = new PasswordHasher<string>();

// Generate a fresh password hash
var newHash = hasher.HashPassword(studentId, password);

// Immediately verify the generated hash
var result = hasher.VerifyHashedPassword(
    studentId,
    newHash,
    password
);

if (result == PasswordVerificationResult.Failed)
{
    Console.WriteLine("Verification failed. Try again.");
    return;
}

Console.WriteLine("\nPassword verification successful!");
Console.WriteLine("\nNEW PASSWORD HASH:");
Console.WriteLine(newHash);

static string ReadPassword()
{
    var password = new StringBuilder();

    while (true)
    {
        var key = Console.ReadKey(true);

        if (key.Key == ConsoleKey.Enter)
            break;

        if (key.Key == ConsoleKey.Backspace)
        {
            if (password.Length > 0)
                password.Length--;

            continue;
        }

        if (!char.IsControl(key.KeyChar))
            password.Append(key.KeyChar);
    }

    Console.WriteLine();
    return password.ToString();
}
