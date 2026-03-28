using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using TendexAI.Application.Common.Interfaces;

namespace TendexAI.Infrastructure.Security;

/// <summary>
/// AES-256-CBC encryption service for connection strings and sensitive data.
/// The encryption key is loaded from configuration (environment variable or appsettings).
/// CRITICAL: The encryption key must NEVER be hardcoded in source code.
/// </summary>
public sealed class ConnectionStringEncryptor : IConnectionStringEncryptor
{
    private readonly byte[] _key;

    public ConnectionStringEncryptor(IConfiguration configuration)
    {
        var keyBase64 = configuration["Security:EncryptionKey"]
            ?? throw new InvalidOperationException(
                "Security:EncryptionKey is not configured. " +
                "Set it via environment variable 'Security__EncryptionKey' or in appsettings.");

        _key = Convert.FromBase64String(keyBase64);

        if (_key.Length != 32)
        {
            throw new InvalidOperationException(
                "Security:EncryptionKey must be a 256-bit (32-byte) key encoded in Base64.");
        }
    }

    /// <inheritdoc />
    public string Encrypt(string plainText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainText);

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Prepend IV to cipher text for decryption
        var result = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

        return Convert.ToBase64String(result);
    }

    /// <inheritdoc />
    public string Decrypt(string cipherText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cipherText);

        var fullCipher = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        // Extract IV from the beginning of the cipher text
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
