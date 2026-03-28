namespace TendexAI.Domain.Enums;

/// <summary>
/// Represents the color-coded text type in template-based RFP sections.
/// Maps to the template color system defined in PRD section 8.2.
/// </summary>
public enum TextColorType
{
    /// <summary>Black - Mandatory fixed text, cannot be edited or deleted.</summary>
    Mandatory = 0,

    /// <summary>Green - Editable text within regulatory constraints.</summary>
    Editable = 1,

    /// <summary>Red - Example text that must be replaced before approval.</summary>
    Example = 2,

    /// <summary>Blue - Instructions/guidance, auto-removed on export.</summary>
    Instruction = 3
}
