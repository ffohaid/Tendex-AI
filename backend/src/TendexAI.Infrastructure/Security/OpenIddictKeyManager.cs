using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace TendexAI.Infrastructure.Security;

/// <summary>
/// Manages persistent RSA key pairs for OpenIddict token signing and encryption.
/// Keys are stored as PEM files on disk, ensuring tokens remain valid across server restarts.
/// 
/// In development: Keys are auto-generated and stored in a local directory.
/// In production: Keys are loaded from a configurable path (typically a Docker volume).
/// 
/// SECURITY: Key files are protected with restrictive file permissions (owner-only read/write).
/// </summary>
public sealed class OpenIddictKeyManager
{
    private const string SigningKeyFileName = "openiddict-signing-key.pem";
    private const string EncryptionKeyFileName = "openiddict-encryption-key.pem";
    private const int RsaKeySize = 2048;

    private readonly string _keyDirectory;
    private readonly ILogger<OpenIddictKeyManager> _logger;

    public OpenIddictKeyManager(IConfiguration configuration, ILogger<OpenIddictKeyManager> logger)
    {
        _logger = logger;

        // Allow configurable key directory; default to app data directory
        _keyDirectory = configuration["OpenIddict:KeyDirectory"]
            ?? Path.Combine(AppContext.BaseDirectory, "keys");
    }

    /// <summary>
    /// Gets or creates the RSA signing key used for token signing.
    /// The key is persisted to disk so tokens survive server restarts.
    /// </summary>
    public RsaSecurityKey GetOrCreateSigningKey()
    {
        var keyPath = Path.Combine(_keyDirectory, SigningKeyFileName);
        return GetOrCreateRsaKey(keyPath, "signing");
    }

    /// <summary>
    /// Gets or creates the RSA encryption key used for token encryption.
    /// The key is persisted to disk so tokens survive server restarts.
    /// </summary>
    public RsaSecurityKey GetOrCreateEncryptionKey()
    {
        var keyPath = Path.Combine(_keyDirectory, EncryptionKeyFileName);
        return GetOrCreateRsaKey(keyPath, "encryption");
    }

    private RsaSecurityKey GetOrCreateRsaKey(string keyPath, string purpose)
    {
        EnsureKeyDirectoryExists();

        if (File.Exists(keyPath))
        {
            _logger.LogInformation(
                "Loading existing OpenIddict {Purpose} key from {Path}",
                purpose, keyPath);

            return LoadRsaKeyFromPem(keyPath);
        }

        _logger.LogInformation(
            "Generating new OpenIddict {Purpose} RSA-{KeySize} key at {Path}",
            purpose, RsaKeySize, keyPath);

        return GenerateAndSaveRsaKey(keyPath, purpose);
    }

    private void EnsureKeyDirectoryExists()
    {
        if (!Directory.Exists(_keyDirectory))
        {
            Directory.CreateDirectory(_keyDirectory);
            _logger.LogInformation("Created OpenIddict key directory: {Directory}", _keyDirectory);

            // Set restrictive permissions on Linux (owner-only)
            if (!OperatingSystem.IsWindows())
            {
                try
                {
                    File.SetUnixFileMode(_keyDirectory,
                        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Could not set restrictive permissions on key directory {Directory}",
                        _keyDirectory);
                }
            }
        }
    }

    private RsaSecurityKey GenerateAndSaveRsaKey(string keyPath, string purpose)
    {
        var rsa = RSA.Create(RsaKeySize);

        // Export the private key in PEM format
        var pemContent = rsa.ExportRSAPrivateKeyPem();
        File.WriteAllText(keyPath, pemContent);

        // Set restrictive file permissions on Linux (owner-only read/write)
        if (!OperatingSystem.IsWindows())
        {
            try
            {
                File.SetUnixFileMode(keyPath,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Could not set restrictive permissions on key file {Path}",
                    keyPath);
            }
        }

        _logger.LogInformation(
            "Successfully generated and saved OpenIddict {Purpose} key ({KeySize}-bit RSA)",
            purpose, RsaKeySize);

        var key = new RsaSecurityKey(rsa)
        {
            KeyId = GenerateKeyId(purpose)
        };

        return key;
    }

    private static RsaSecurityKey LoadRsaKeyFromPem(string keyPath)
    {
        var pemContent = File.ReadAllText(keyPath);
        var rsa = RSA.Create();
        rsa.ImportFromPem(pemContent);

        var key = new RsaSecurityKey(rsa)
        {
            KeyId = GenerateKeyId(Path.GetFileNameWithoutExtension(keyPath))
        };

        return key;
    }

    /// <summary>
    /// Generates a stable key ID based on the purpose.
    /// This ensures the key ID remains consistent across restarts.
    /// </summary>
    private static string GenerateKeyId(string purpose)
    {
        return $"tendex-{purpose}-key";
    }
}
