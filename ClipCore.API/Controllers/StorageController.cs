using System.Security.Claims;
using ClipCore.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClipCore.API.Controllers;

[ApiController]
public class StorageController : ControllerBase
{
    private readonly IR2StorageService _r2;
    public StorageController(IR2StorageService r2) => _r2 = r2;

    [Authorize(Roles = "Seller")]
    [HttpGet("GetUploadUrl")]
    public IActionResult GetUploadUrl(string fileName, string contentType)
    {
        int sellerId = int.Parse(User.FindFirstValue("seller_id")!);
        var key      = $"sellers/{sellerId}/{Guid.NewGuid()}-{fileName}";
        var url      = _r2.GetPresignedUploadUrl(key, contentType);
        return Ok(new { uploadUrl = url, key });
    }
}
