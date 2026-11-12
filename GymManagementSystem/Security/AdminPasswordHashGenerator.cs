using System.Security;
using System.Text;
using Microsoft.AspNetCore.Identity;

namespace GymManagementSystem.Security;

public static class AdminPasswordHashGenerator
{
    public static Task<int> RunAsync()
    {
        if (Console.IsInputRedirected || Console.IsOutputRedirected)
        {
            Console.Error.WriteLine("Run this command in an interactive terminal so the password can be entered securely.");
            return Task.FromResult(1);
        }

        try
        {
            Console.Write("Enter a new administrator password (at least 12 characters): ");
            var password = ReadHiddenPassword();
            Console.WriteLine();
            Console.Write("Confirm the password: ");
            var confirmation = ReadHiddenPassword();
            Console.WriteLine();

            if (password.Length < 12)
            {
                Console.Error.WriteLine("The administrator password must contain at least 12 characters.");
                return Task.FromResult(1);
            }

            if (!string.Equals(password, confirmation, StringComparison.Ordinal))
            {
                Console.Error.WriteLine("The passwords did not match.");
                return Task.FromResult(1);
            }

            var hasher = new PasswordHasher<AdminCredential>();
            var hash = hasher.HashPassword(new AdminCredential(string.Empty), password);
            Console.WriteLine("AdminAuthentication:PasswordHash");
            Console.WriteLine(hash);
            return Task.FromResult(0);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine();
            Console.Error.WriteLine("Password generation was cancelled.");
            return Task.FromResult(1);
        }
    }

    private static string ReadHiddenPassword()
    {
        var password = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                return password.ToString();
            }

            if (key.Key == ConsoleKey.Escape)
            {
                password.Clear();
                throw new OperationCanceledException();
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (password.Length > 0)
                {
                    password.Length--;
                }

                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                password.Append(key.KeyChar);
            }
        }
    }
}
