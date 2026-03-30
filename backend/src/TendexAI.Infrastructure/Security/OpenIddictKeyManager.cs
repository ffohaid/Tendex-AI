using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace TendexAI.Infrastructure.Security;

/// <summary>
/// Manages persistent RSA key pairs for token signing and encryption.
/// Keys are stored as PEM files on disk, ensuring tokens remain valid across server restarts.
/// 
/// IMPORTANT: Keys are cached in memory (singleton) to ensure the exact same RSA instance
/// is used for both signing (TokenService) and validation (JwtAuthenticationHandler).
/// This prevents signature mismatch issues caused by creating multiple RSA instances.
/// </summary>
public sealed class OpenIddictKeyManager
{
    private const string SigningKeyFileName = "openiddict-signing-key.pem";
    private const string EncryptionKeyFileName = "openiddict-encryption-key.pem";
    private const int RsaKeySize = 2048;

    private readonly string _keyDirectory;
    private readonly ILogger<OpenIddictKeyManager> _logger;
    
    // Cached keys — loaded once, reused for all signing and validation operations
    private RsaSecurityKey? _cachedSigningKey;
    private RsaSecurityKey? _cachedEncryptionKey;
    private readonly object _signingKeyLock = new();
    private readonly object _encryptionKeyLock = new();

    public OpenIddictKeyManager(IConfiguration configuration, ILogger<OpenIddictKeyManager> logger)
    {
        _logger = logger;

        _keyDirectory = configuration["OpenIddict:KeyDirectory"]
            ?? Path.Combine(AppContext.BaseDirectory, "keys");
    }

    /// <summary>
    /// Gets or creates the RSA signing key used for token signing.
    /// The key is cached in memory after first load to guarantee consistency.
    /// </summary>
    public RsaSecurityKey GetOrCreateSigningKey()
    {
        if (_cachedSigningKey is not null)
            return _cachedSigningKey;

        lock (_signingKeyLock)
        {
            if (_cachedSigningKey is not null)
                return _cachedSigningKey;

            var keyPath = Path.Combine(_keyDirectory, SigningKeyFileName);
            _cachedSigningKey = GetOrCreateRsaKey(keyPath, "signing");
            
            _logger.LogInformation(
                "Signing key cached. KeyId: {KeyId}, RSA KeySize: {KeySize}",
                _cachedSigningKey.KeyId, _cachedSigningKey.Rsa?.KeySize ?? 0);
            
            return _cachedSigningKey;
        }
    }

    /// <summary>
    /// Gets or creates the RSA encryption key used for token encryption.
    /// The key is cached in memory after first load to guarantee consistency.
    /// </summary>
    public RsaSecurityKey GetOrCreateEncryptionKey()
    {
        if (_cachedEncryptionKey is not null)
            return _cachedEncryptionKey;

        lock (_encryptionKeyLock)
        {
            if (_cachedEncryptionKey is not null)
                return _cachedEncryptionKey;

            var keyPath = Path.Combine(_keyDirectory, EncryptionKeyFileName);
            _cachedEncryptionKey = GetOrCreateRsaKey(keyPath, "encryption");
            return _cachedEncryptionKey;
        }
    }

    private RsaSecurityKey GetOrCreateRsaKey(string keyPath, string purpose)
    {
        EnsureKeyDirectoryExists();

        if (File.Exists(keyPath))
        {
            _logger.LogInformation(
                "Loading existing {Purpose} key from {Path}",
                purpose, keyPath);

            return LoadRsaKeyFromPem(keyPath, purpose);
        }

        _logger.LogInformation(
            "Generating new {Purpose} RSA-{KeySize} key at {Path}",
            purpose, RsaKeySize, keyPath);

        return GenerateAndSaveRsaKey(keyPath, purpose);
    }

    private void EnsureKeyDirectoryExists()
    {
        if (!Directory.Exists(_keyDirectory))
        {
            Directory.CreateDirectory(_keyDirectory);
            _logger.LogInformation("Created key directory: {Directory}", _keyDirectory);

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

        var pemContent = rsa.ExportRSAPrivateKeyPem();
        File.WriteAllText(keyPath, pemContent);

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
            "Successfully generated and saved {Purpose} key ({KeySize}-bit RSA)",
            purpose, RsaKeySize);

        var keyId = $"tendex-{purpose}-key";
        var key = new RsaSecurityKey(rsa) { KeyId = keyId };
        return key;
    }

    private RsaSecurityKey LoadRsaKeyFromPem(string keyPath, string purpose)
    {
        var pemContent = File.ReadAllText(keyPath);
        
        // Use RSA parameters import to ensure clean key state
        var tempRsa = RSA.Create();
        tempRsa.ImportFromPem(pemContent);
        
        // Export and re-import parameters to ensure a clean RSA instance
        var parameters = tempRsa.ExportParameters(true);
        tempRsa.Dispose();
        
        var rsa = RSA.Create();
        rsa.ImportParameters(parameters);

        var keyId = $"tendex-{purpose}-key";
        var key = new RsaSecurityKey(rsa) { KeyId = keyId };

        _logger.LogInformation(
            "Loaded {Purpose} key. KeyId: {KeyId}, RSA key size: {KeySize} bits", 
            purpose, keyId, rsa.KeySize);
        
        return key;
    }
}
