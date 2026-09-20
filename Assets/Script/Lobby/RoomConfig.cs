public static class RoomConfig
{
    public const byte MaxPlayers = 4;
    public const int EntriesPerPage = 9;
    public const string DateFormat = "yyyy.MM.dd";
    public const int RoomNameMaxLength = 16;
}

public static class RoomKeys
{
    public const string SaveOwner = "SaveOwner";
    public const string CreatedAt = "CreatedAt";
    public const string IsLoadedGame = "IsLoadedGame";
    public const string HasPassword = "HasPwd";
    public const string PasswordHash = "PwdHash";
}