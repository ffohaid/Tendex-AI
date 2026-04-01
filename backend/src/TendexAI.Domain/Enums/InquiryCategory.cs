namespace TendexAI.Domain.Enums;

/// <summary>
/// Categorizes supplier inquiries by domain area.
/// Used for routing inquiries to the appropriate department/specialist.
/// </summary>
public enum InquiryCategory
{
    /// <summary>General inquiries not fitting other categories.</summary>
    General = 0,

    /// <summary>Technical inquiries about specifications, scope, or deliverables.</summary>
    Technical = 1,

    /// <summary>Financial inquiries about pricing, BOQ, or payment terms.</summary>
    Financial = 2,

    /// <summary>Administrative inquiries about timelines, eligibility, or documents.</summary>
    Administrative = 3,

    /// <summary>Legal inquiries about contract terms, guarantees, or compliance.</summary>
    Legal = 4
}
