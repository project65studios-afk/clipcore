namespace ClipCore.Core;

public static class ReservedSlugs
{
    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin", "api", "store", "auth", "help", "about", "terms", "privacy",
        "faq", "search", "collections", "clips", "checkout", "cart", "delivery",
        "orders", "account", "login", "logout", "register", "health", "debug",
        "static", "assets", "cdn", "www", "mail", "support", "blog", "status",
        "clipcore", "dashboard", "seller", "buyer", "upload", "fulfillment"
    };

    public static bool IsReserved(string slug) => All.Contains(slug);
}
