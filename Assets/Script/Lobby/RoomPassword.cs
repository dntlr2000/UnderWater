using System.Security.Cryptography;
using System.Text;

public static class RoomPassword
{
    public const int MinLength = 4;
    public const int MaxLength = 12;

    public static bool IsValid(string password)
    {
        if (string.IsNullOrEmpty(password)) return false;
        return password.Length >= MinLength && password.Length <= MaxLength;
    }

    public static string Hash(string password)
    {
        if (string.IsNullOrEmpty(password)) return string.Empty;

        using (SHA256 sha = SHA256.Create())
        {
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            StringBuilder builder = new StringBuilder(bytes.Length * 2);

            for (int i = 0; i < bytes.Length; i++)
                builder.Append(bytes[i].ToString("x2"));

            return builder.ToString();
        }
    }

    public static bool Matches(string input, string hash)
    {
        if (string.IsNullOrEmpty(hash)) return true;
        if (string.IsNullOrEmpty(input)) return false;

        return Hash(input) == hash;
    }
}