using Microsoft.Extensions.Configuration;

public static class ConfigurationValidation
{
    public static void ValidateRequiredKeys(IConfiguration config, params string[] requiredKeys)
    {
        var missing = requiredKeys
            .Where(k => string.IsNullOrWhiteSpace(config[k]))
            .ToList();

        if (missing.Count > 0)
            throw new InvalidOperationException("Missing required configuration keys: " + string.Join(", ", missing));
    }
}
