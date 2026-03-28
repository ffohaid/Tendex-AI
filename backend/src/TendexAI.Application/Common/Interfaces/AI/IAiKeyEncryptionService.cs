namespace TendexAI.Application.Common.Interfaces.AI;

/// <summary>
/// Service for encrypting and decrypting AI provider API keys using AES-256.
/// API keys are stored encrypted in the AiConfiguration table and
/// decrypted in-memory only during execution (STRICT CONSTRAINT).
/// </summary>
public interface IAiKeyEncryptionService
{
    /// <summary>
    /// Encrypts a plain-text API key using AES-256-CBC.
    /// The IV is prepended to the cipher text for self-contained decryption.
    /// </summary>
    /// <param name="plainApiKey">The plain-text API key to encrypt.</param>
    /// <returns>Base64-encoded string containing IV + cipher text.</returns>
    string Encrypt(string plainApiKey);

    /// <summary>
    /// Decrypts an AES-256-CBC encrypted API key.
    /// The decrypted key exists only in-memory and must never be persisted.
    /// </summary>
    /// <param name="encryptedApiKey">Base64-encoded string containing IV + cipher text.</param>
    /// <returns>The plain-text API key (in-memory only).</returns>
    string Decrypt(string encryptedApiKey);
}
