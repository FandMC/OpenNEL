namespace OpenNEL.GameLauncher.Utils;

public static class HashUtil
{
    public static string GenerateGameRuntimeId(string gameId, string roleName)
    {
        return gameId + "-" + roleName;
    }
}
