using System.Security.Cryptography;
using System.Text;
using Isopoh.Cryptography.Argon2;

namespace bitwardenclone.src.services;

public class CryptoService
{
    public byte[] DeriveKeyFromPassword(string password, byte[] salt)
    {
        var config = new Argon2Config
        {
            Type = Argon2Type.DataIndependentAddressing,
            Version = Argon2Version.Nineteen,
            TimeCost = 16,
            MemoryCost = 256 * 1024, // 256MB
            Lanes = 4,
            Threads = Environment.ProcessorCount,
            Password = Encoding.UTF8.GetBytes(password),
            Salt = salt,
            HashLength = 32, // 32 bytes for AES-256
        };

        var output =  new Argon2(config).Hash().Buffer;
        return output;
    }

    public string Encrypt(string plaintext, byte[] key)
    {
        if (string.IsNullOrEmpty(plaintext))
            throw new ArgumentException("Plaintext cannot be null or empty.", nameof(plaintext));
        if (key is not { Length: 32 })
            throw new ArgumentException("Key must be 32 bytes for AES-256.", nameof(key));

        byte[] nonce = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize);
        byte[] tag = new byte[AesGcm.TagByteSizes.MaxSize];
        byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        byte[] ciphertext = new byte[plaintextBytes.Length];

        using var aes = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        byte[] encryptedData = new byte[nonce.Length + ciphertext.Length + tag.Length];
        Buffer.BlockCopy(nonce, 0, encryptedData, 0, nonce.Length);
        Buffer.BlockCopy(ciphertext, 0, encryptedData, nonce.Length, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, encryptedData, nonce.Length + ciphertext.Length, tag.Length);

        return Convert.ToBase64String(encryptedData);
    }

    public string Decrypt(string base64Ciphertext, byte[] key)
    {
        if (string.IsNullOrEmpty(base64Ciphertext))
            throw new ArgumentException(
                "Ciphertext cannot be null or empty.",
                nameof(base64Ciphertext)
            );
        if (key is not { Length: 32 })
            throw new ArgumentException("Key must be 32 bytes for AES-256.", nameof(key));

        byte[] encryptedData = Convert.FromBase64String(base64Ciphertext);

        if (encryptedData.Length < AesGcm.NonceByteSizes.MaxSize + AesGcm.TagByteSizes.MaxSize)
            throw new ArgumentException("Invalid ciphertext length.", nameof(base64Ciphertext));

        var nonce = encryptedData[..AesGcm.NonceByteSizes.MaxSize];
        var tag = encryptedData[^AesGcm.TagByteSizes.MaxSize..];
        var ciphertext = encryptedData[AesGcm.NonceByteSizes.MaxSize..^AesGcm.TagByteSizes.MaxSize];
        byte[] plaintextBytes = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);

        try
        {
            aes.Decrypt(nonce, ciphertext, tag, plaintextBytes);
            return Encoding.UTF8.GetString(plaintextBytes);
        }
        catch (CryptographicException)
        {
            throw new UnauthorizedAccessException("Invalid key or corrupted data.");
        }
    }
}
