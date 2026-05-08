namespace backend.Services.Auth;

public static class IdentityHashInspector
{
    public static string DetectHashType(string? hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
        {
            return "None";
        }

        if (TryGetIdentityHashVersion(hash, out var version))
        {
            return version switch
            {
                0 => "AspNetIdentityV2",
                1 => "AspNetIdentityV3",
                _ => "AspNetIdentity"
            };
        }

        return "Unknown";
    }

    public static bool IsIdentityHash(string? hash)
    {
        return TryGetIdentityHashVersion(hash, out _);
    }

    public static string GetHashPrefix(string? hash, int maxLength = 16)
    {
        if (string.IsNullOrWhiteSpace(hash))
        {
            return string.Empty;
        }

        return hash[..Math.Min(maxLength, hash.Length)];
    }

    private static bool TryGetIdentityHashVersion(string? hash, out int version)
    {
        version = -1;

        if (string.IsNullOrWhiteSpace(hash))
        {
            return false;
        }

        try
        {
            var bytes = Convert.FromBase64String(hash);
            if (bytes.Length == 0)
            {
                return false;
            }

            version = bytes[0];
            return version == 0 || version == 1;
        }
        catch
        {
            return false;
        }
    }
}