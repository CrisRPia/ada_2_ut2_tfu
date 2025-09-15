using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using bitwardenclone.src.models;
using bitwardenclone.src.services;
using Isopoh.Cryptography.Argon2;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace bitwardenclone.src.controllers;

[ApiController]
[Route("client")]
[Authorize]
[Produces("application/json")]
public class ClientController(
    CryptoService cryptoService,
    ServerController serverController,
    ApplicationDbContext context
) : Controller
{
    /// <summary>
    /// Verifies password, encrypts vault data, and calls the server to store it.
    /// </summary>
    [HttpPost("encrypt-and-update-vault")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EncryptAndUpdateVault([FromBody] VaultUpdateRequest request)
    {
        var userId = GetCurrentUserId();
        var user = await context.Users.FindAsync(userId);
        if (user is null)
            return NotFound("Authenticated user not found.");

        if (!Argon2.Verify(user.MasterPasswordHash, request.MasterPassword))
            return Unauthorized("Invalid master password.");

        var salt = Encoding.UTF8.GetBytes(user.Email);
        var key = cryptoService.DeriveKeyFromPassword(request.MasterPassword, salt);
        var plaintextJson = JsonSerializer.Serialize(request.VaultData);
        var encryptedData = cryptoService.Encrypt(plaintextJson, key);

        serverController.ControllerContext = new ControllerContext { HttpContext = HttpContext };

        var serverRequest = new VaultRequest { EncryptedData = encryptedData };
        return await serverController.Store(serverRequest);
    }

    /// <summary>
    /// Retrieves the encrypted vault from the server and decrypts it.
    /// </summary>
    [HttpPost("decrypt-vault")]
    [ProducesResponseType(typeof(VaultDecryptResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DecryptVault([FromBody] VaultDecryptRequest request)
    {
        var userId = GetCurrentUserId();
        var user = await context.Users.FindAsync(userId);
        if (user is null)
            return NotFound("Authenticated user not found.");

        serverController.ControllerContext = new ControllerContext { HttpContext = HttpContext };

        var serverResult = await serverController.Get();

        if (serverResult is not OkObjectResult { Value: VaultResponse vaultResponse })
            return serverResult;

        try
        {
            var salt = Encoding.UTF8.GetBytes(user.Email);
            var key = cryptoService.DeriveKeyFromPassword(request.MasterPassword, salt);
            var decryptedJson = cryptoService.Decrypt(vaultResponse.EncryptedData, key);
            var vaultData = JsonSerializer.Deserialize<Dictionary<string, LoginInfo>>(
                decryptedJson
            );

            return Ok(new VaultDecryptResponse { VaultData = vaultData });
        }
        catch (Exception)
        {
            return BadRequest(
                new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Decryption Failed",
                    Detail = "Could not decrypt the vault. The master password may be incorrect.",
                }
            );
        }
    }

    private Guid GetCurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

// --- DTOs ---

/// <summary>
/// Login credentials for a specific domain.
/// </summary>
public record LoginInfo
{
    [Required]
    public required string Username { get; init; }

    [Required]
    public required string Password { get; init; }
}

/// <summary>
/// Request body for encrypting and updating the vault.
/// </summary>
public record VaultUpdateRequest
{
    [Required]
    public required string MasterPassword { get; init; }

    /// <summary>
    /// Structured vault data, mapping a domain to its login info.
    /// </summary>
    [Required]
    public required Dictionary<string, LoginInfo> VaultData { get; init; }
}

/// <summary>
/// Request body for decrypting the vault.
/// </summary>
public record VaultDecryptRequest
{
    [Required]
    public required string MasterPassword { get; init; }
}

/// <summary>
/// Response body when decrypting the vault.
/// </summary>
public record VaultDecryptResponse
{
    /// <summary>
    /// The decrypted, structured vault data.
    /// </summary>
    public required Dictionary<string, LoginInfo>? VaultData { get; init; }
}
