namespace TendexAI.Domain.Enums;

/// <summary>
/// Represents how the RFP booklet was initially created.
/// Maps to the 4 creation methods defined in PRD section 8.1.
/// </summary>
public enum RfpCreationMethod
{
    /// <summary>Manual wizard - 6 steps for basic data entry.</summary>
    ManualWizard = 0,

    /// <summary>Created from an approved ECA template.</summary>
    FromTemplate = 1,

    /// <summary>Cloned from a previous competition.</summary>
    CopiedFromPrevious = 2,

    /// <summary>AI-generated draft from text description.</summary>
    AiGenerated = 3,

    /// <summary>Created by uploading an existing document and extracting content via AI.</summary>
    UploadExtract = 4
}
