using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using TendexAI.Application.Common.Interfaces.AI;

namespace TendexAI.Infrastructure.Security;

/// <summary>
/// AES-256-CBC encryption service specifically for AI provider API keys.
/// Implements the strict constraint: keys are encrypted at rest and decrypted
/// in-memory only during execution. The encryption master key is loaded from
/// configuration and must NEVER be hardcoded in source code.
/// </summary>
public sealed class AiKeyEncryptionService : IAiKeyEncryptionService
{
    private readonly byte[] _masterKey;

    public AiKeyEncryptionService(IConfiguration configuration)
    {
        var keyBase64 = configuration["Security:AiEncryptionKey"]
            ?? configuration["Security:EncryptionKey"]
            ?? throw new InvalidOperationException(
                "Neither Security:AiEncryptionKey nor Security:EncryptionKey is configured. " +
                "Set it via environment variable or in appsettings (non-production only).");

        _masterKey = Convert.FromBase64String(keyBase64);

        if (_masterKey.Length != 32)
        {
            throw new InvalidOperationException(
                "AI encryption key must be a 256-bit (32-byte) key encoded in Base64.");
        }
    }

    /// <inheritdoc />
    public string Encrypt(string plainApiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainApiKey);

        using var aes = Aes.Create();
        aes.Key = _masterKey;
        aes.GenerateIV();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainApiKey);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Prepend IV to cipher text for self-contained decryption
        var result = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

        return Convert.ToBase64String(result);
    }

    /// <inheritdoc />
    public string Decrypt(string encryptedApiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(encryptedApiKey);

        var fullCipher = Convert.FromBase64String(encryptedApiKey);

        using var aes = Aes.Create();
        aes.Key = _masterKey;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        // Extract IV (first 16 bytes) from the cipher text
        var iv = new byte[aes.BlockSize / 8];
        var cipher = new byte[fullCipher.Length - iv.Length];
        Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);

        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

        return Encoding.UTF8.GetString(plainBytes);
    }
}
