using System.Text.RegularExpressions;

public static class OutgameMessages
{
    public const int MinPasswordLength = 6;

    public const string TabLogin = "·Î±×ÀÎ";
    public const string TabRegister = "È¸¿ø°¡ÀÔ";
    public const string TabNickname = "´Ğ³×ÀÓ ¼³Á¤";

    public const string InvalidEmail = "ÀÌ¸ŞÀÏ Çü½ÄÀÌ ¿Ã¹Ù¸£Áö ¾Ê½À´Ï´Ù.";
    public const string InvalidPassword = "ºñ¹Ğ¹øÈ£´Â 6ÀÚ ÀÌ»ó ÀÔ·ÂÇØÁÖ¼¼¿ä.";
    public const string EmptyField = "ÀÌ¸ŞÀÏ°ú ºñ¹Ğ¹øÈ£¸¦ ÀÔ·ÂÇØÁÖ¼¼¿ä.";
    public const string LoginProcessing = "·Î±×ÀÎ Áß...";
    public const string AuthNotReady = "ÀÎÁõ ¸ğµâÀ» ÃÊ±âÈ­ÇÏ´Â ÁßÀÔ´Ï´Ù.";
    public const string DuplicateLogin = "´Ù¸¥ ±â±â¿¡¼­ Á¢¼ÓÁßÀÎ ¾ÆÀÌµğÀÔ´Ï´Ù.";
    public const string LoginFailedDefault = "·Î±×ÀÎ¿¡ ½ÇÆĞÇÏ¿´½À´Ï´Ù.";
    public const string InvalidNickname = "´Ğ³×ÀÓÀº 2~12ÀÚÀÇ ÇÑ±Û, ¿µ¹®, ¼ıÀÚ¸¸ »ç¿ëÇÒ ¼ö ÀÖ½À´Ï´Ù.";

    private static readonly Regex EmailRegex =
        new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$", RegexOptions.Compiled);


    private static readonly Regex NicknameRegex =
        new Regex(@"^[°¡-ÆRa-zA-Z0-9]{2,12}$", RegexOptions.Compiled);

    public static bool IsValidEmail(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        return EmailRegex.IsMatch(value.Trim());
    }

    public static bool IsValidPassword(string value)
    {
        return !string.IsNullOrEmpty(value) && value.Length >= MinPasswordLength;
    }

    public static bool IsValidNickname(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        return NicknameRegex.IsMatch(value.Trim());
    }

    public static string ToLoginMessage(Firebase.Auth.AuthError error)
    {
        switch (error)
        {
            case Firebase.Auth.AuthError.InvalidEmail:
                return InvalidEmail;
            case Firebase.Auth.AuthError.WrongPassword:
            case Firebase.Auth.AuthError.UserNotFound:
                return "ÀÌ¸ŞÀÏ ¶Ç´Â ºñ¹Ğ¹øÈ£°¡ ÀÏÄ¡ÇÏÁö ¾Ê½À´Ï´Ù.";
            case Firebase.Auth.AuthError.TooManyRequests:
                return "¿äÃ»ÀÌ ¸¹½À´Ï´Ù. Àá½Ã ÈÄ ´Ù½Ã ½ÃµµÇØÁÖ¼¼¿ä.";
            case Firebase.Auth.AuthError.NetworkRequestFailed:
                return "³×Æ®¿öÅ© ¿¬°áÀ» È®ÀÎÇØÁÖ¼¼¿ä.";
            case Firebase.Auth.AuthError.UserDisabled:
                return "»ç¿ëÀÌ ÁßÁöµÈ °èÁ¤ÀÔ´Ï´Ù.";
            default:
                return LoginFailedDefault;
        }
    }

    public static string ToRegisterMessage(Firebase.Auth.AuthError error)
    {
        switch (error)
        {
            case Firebase.Auth.AuthError.EmailAlreadyInUse:
                return "ÀÌ¹Ì »ç¿ë ÁßÀÎ ÀÌ¸ŞÀÏÀÔ´Ï´Ù.";
            case Firebase.Auth.AuthError.InvalidEmail:
                return InvalidEmail;
            case Firebase.Auth.AuthError.WeakPassword:
                return InvalidPassword;
            case Firebase.Auth.AuthError.NetworkRequestFailed:
                return "³×Æ®¿öÅ© ¿¬°áÀ» È®ÀÎÇØÁÖ¼¼¿ä.";
            default:
                return "È¸¿ø°¡ÀÔ¿¡ ½ÇÆĞÇÏ¿´½À´Ï´Ù.";
        }
    }
}