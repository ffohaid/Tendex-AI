namespace TendexAI.Domain.Enums;

/// <summary>
/// Categorizes file attachments for classification, filtering, and access control.
/// </summary>
public enum FileCategory
{
    /// <summary>
    /// General-purpose file with no specific category.
    /// </summary>
    General = 0,

    /// <summary>
    /// RFP (Request for Proposal) document or specification.
    /// </summary>
    RfpDocument = 1,

    /// <summary>
    /// Vendor proposal submission.
    /// </summary>
    Proposal = 2,

    /// <summary>
    /// Technical evaluation report.
    /// </summary>
    TechnicalEvaluation = 3,

    /// <summary>
    /// Financial evaluation report.
    /// </summary>
    FinancialEvaluation = 4,

    /// <summary>
    /// Committee meeting minutes.
    /// </summary>
    MeetingMinutes = 5,

    /// <summary>
    /// Official letter or correspondence.
    /// </summary>
    OfficialLetter = 6,

    /// <summary>
    /// Generated PDF report.
    /// </summary>
    Report = 7,

    /// <summary>
    /// Organization logo or branding asset.
    /// </summary>
    BrandingAsset = 8,

    /// <summary>
    /// Supporting document or evidence.
    /// </summary>
    SupportingDocument = 9,

    /// <summary>
    /// Template document.
    /// </summary>
    Template = 10
}
