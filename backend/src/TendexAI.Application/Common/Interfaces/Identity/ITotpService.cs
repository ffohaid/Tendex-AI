namespace TendexAI.Application.Common.Interfaces.Identity;

/// <summary>
/// Abstraction for TOTP operations used in MFA.
/// </summary>
public interface ITotpService
{
    /// <summary>Generates a new random secret key for TOTP.</summary>
    string GenerateSecretKey();

    /// <summary>Generates a QR code URI for authenticator app setup.</summary>
    string GenerateQrCodeUri(string email, string issuer, string secretKey);

    /// <summary>Validates a TOTP code against the secret key.</summary>
    bool ValidateCode(string secretKey, string code);

    /// <summary>Generates the current TOTP code (for testing purposes).</summary>
    string GenerateCurrentCode(string secretKey);
}
