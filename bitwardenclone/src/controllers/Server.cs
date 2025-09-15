using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using bitwardenclone.src.models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace bitwardenclone.src.controllers;

[ApiController]
[Route("server")]
[Authorize]
[Produces("application/json")]
public class ServerController(ApplicationDbContext context) : Controller
{
    /// <summary>
    /// Updates the user's encrypted vault.
    /// </summary>
    [HttpPut("vault")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Store([FromBody] VaultRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var vault = await context.Vaults.FirstOrDefaultAsync(v => v.UserId == userId);

        if (vault == null)
        {
            vault = new Vault
            {
                Id = Guid.NewGuid(),
                UserId = userId.Value,
                EncryptedData = request.EncryptedData,
            };
            context.Vaults.Add(vault);
        }
        else
        {
            vault.EncryptedData = request.EncryptedData;
        }

        await context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Retrieves the user's encrypted vault.
    /// </summary>
    [HttpGet("vault")]
    [ProducesResponseType(typeof(VaultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var vault = await context.Vaults.FirstOrDefaultAsync(v => v.UserId == userId);

        if (vault == null)
        {
            return NotFound("No vault found for this user.");
        }

        return Ok(new VaultResponse { EncryptedData = vault.EncryptedData });
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}

// --- DTOs ---

public record VaultRequest
{
    [Required]
    public required string EncryptedData { get; init; }
}

public record VaultResponse
{
    [Required]
    public required string EncryptedData { get; init; }
}
