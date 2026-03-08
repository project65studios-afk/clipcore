using System.Text.RegularExpressions;
using ClipCore.Core;
using ClipCore.Core.Entities;
using ClipCore.Core.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace ClipCore.Web.Services;

public class SellerService
{
    private readonly ISellerRepository _sellerRepository;
    private readonly IStorefrontRepository _storefrontRepository;
    private readonly UserManager<ApplicationUser> _userManager;

    private static readonly Regex SlugRegex = new(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled);

    public SellerService(
        ISellerRepository sellerRepository,
        IStorefrontRepository storefrontRepository,
        UserManager<ApplicationUser> userManager)
    {
        _sellerRepository = sellerRepository;
        _storefrontRepository = storefrontRepository;
        _userManager = userManager;
    }

    public static (bool valid, string? error) ValidateSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return (false, "Slug is required.");

        if (slug.Length < 3 || slug.Length > 30)
            return (false, "Slug must be between 3 and 30 characters.");

        if (!SlugRegex.IsMatch(slug))
            return (false, "Slug may only contain lowercase letters, numbers, and hyphens, and cannot start or end with a hyphen.");

        if (ReservedSlugs.IsReserved(slug))
            return (false, $"'{slug}' is a reserved name and cannot be used.");

        return (true, null);
    }

    public async Task<(bool success, string? error)> RegisterSellerAsync(
        string userId,
        string slug,
        string displayName,
        string? bio)
    {
        // Guard: already a seller
        var existing = await _sellerRepository.GetByUserIdAsync(userId);
        if (existing != null)
            return (false, "You are already registered as a seller.");

        // Validate slug
        var (validSlug, slugError) = ValidateSlug(slug);
        if (!validSlug)
            return (false, slugError);

        // Slug uniqueness
        if (await _storefrontRepository.SlugExistsAsync(slug))
            return (false, "That slug is already taken. Please choose another.");

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return (false, "User not found.");

        // Create Seller
        var seller = new Seller
        {
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        await _sellerRepository.AddAsync(seller);

        // Reload to get generated Id
        seller = await _sellerRepository.GetByUserIdAsync(userId);
        if (seller == null)
            return (false, "Failed to create seller record.");

        // Create Storefront
        var storefront = new Storefront
        {
            SellerId = seller.Id,
            Slug = slug.ToLowerInvariant(),
            DisplayName = displayName.Trim(),
            Bio = bio?.Trim(),
            IsPublished = false,
            CreatedAt = DateTime.UtcNow
        };
        await _storefrontRepository.AddAsync(storefront);

        // Assign Seller role
        if (!await _userManager.IsInRoleAsync(user, "Seller"))
            await _userManager.AddToRoleAsync(user, "Seller");

        return (true, null);
    }
}
