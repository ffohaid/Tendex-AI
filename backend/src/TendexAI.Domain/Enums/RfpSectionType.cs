namespace TendexAI.Domain.Enums;

/// <summary>
/// Represents the type of content in an RFP booklet section.
/// Used to enforce template color-coding rules from PRD section 8.2.
/// </summary>
public enum RfpSectionType
{
    /// <summary>General information section.</summary>
    GeneralInformation = 0,

    /// <summary>Technical specifications section.</summary>
    TechnicalSpecifications = 1,

    /// <summary>Terms and conditions section.</summary>
    TermsAndConditions = 2,

    /// <summary>Evaluation criteria section.</summary>
    EvaluationCriteria = 3,

    /// <summary>Bill of Quantities (BOQ) section.</summary>
    BillOfQuantities = 4,

    /// <summary>Attachments and appendices section.</summary>
    Attachments = 5,

    /// <summary>Custom section added by the entity.</summary>
    Custom = 6
}
