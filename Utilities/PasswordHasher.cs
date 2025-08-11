using System;
using System.Security.Cryptography;
using System.Text;

public static class PasswordHasher
{
    public static string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty");

        string salt = GenerateSalt();
        return HashPasswordWithSalt(password, salt);
    }

    private static string HashPasswordWithSalt(string password, string salt)
    {
        string saltedPassword = password + salt;
        using SHA256 sha256Hash = SHA256.Create();
        byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
        StringBuilder builder = new();
        for (int i = 0; i < bytes.Length; i++)
        {
            builder.Append(bytes[i].ToString("x2"));
        }
        return builder.ToString() + ":" + salt;
    }

    public static bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
            return false;

        try
        {
            string[] parts = storedHash.Split(':');
            if (parts.Length != 2)
                return false;

            string storedHashPart = parts[0];
            string salt = parts[1];

            string hashedInput = HashPasswordWithSalt(password, salt);
            string[] inputParts = hashedInput.Split(':');

            if (inputParts.Length != 2)
                return false;

            string inputHashPart = inputParts[0];

            return string.Equals(inputHashPart, storedHashPart, StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    private static string GenerateSalt()
    {
        byte[] saltBytes = new byte[16];
        RandomNumberGenerator.Fill(saltBytes);
        return Convert.ToBase64String(saltBytes);
    }
}