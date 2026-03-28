using OtpNet;
using TendexAI.Application.Common.Interfaces.Identity;

namespace TendexAI.Infrastructure.Services.Identity;

/// <summary>
/// Provides TOTP (Time-based One-Time Password) operations for MFA.
/// Uses RFC 6238 compliant TOTP generation and validation.
/// </summary>
public sealed class TotpService : ITotpService
{
    private const int TotpStep = 30; // seconds
    private const int TotpDigits = 6;
    private const int VerificationWindow = 1; // Allow 1 step tolerance

    /// <inheritdoc />
    public string GenerateSecretKey()
    {
        var key = KeyGeneration.GenerateRandomKey(20); // 160-bit key
        return Base32Encoding.ToString(key);
    }

    /// <inheritdoc />
    public string GenerateQrCodeUri(string email, string issuer, string secretKey)
    {
        return $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}" +
               $"?secret={secretKey}&issuer={Uri.EscapeDataString(issuer)}&digits={TotpDigits}&period={TotpStep}";
    }

    /// <inheritdoc />
    public bool ValidateCode(string secretKey, string code)
    {
        var keyBytes = Base32Encoding.ToBytes(secretKey);
        var totp = new Totp(keyBytes, step: TotpStep, totpSize: TotpDigits);
        return totp.VerifyTotp(code, out _, new VerificationWindow(VerificationWindow));
    }

    /// <inheritdoc />
    public string GenerateCurrentCode(string secretKey)
    {
        var keyBytes = Base32Encoding.ToBytes(secretKey);
        var totp = new Totp(keyBytes, step: TotpStep, totpSize: TotpDigits);
        return totp.ComputeTotp();
    }
}
