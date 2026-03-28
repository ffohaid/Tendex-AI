namespace TendexAI.Domain.Enums;

/// <summary>
/// Represents the unit of measurement for a Bill of Quantities (BOQ) item.
/// </summary>
public enum BoqItemUnit
{
    /// <summary>Each / unit.</summary>
    Each = 0,

    /// <summary>Lump sum.</summary>
    LumpSum = 1,

    /// <summary>Square meter.</summary>
    SquareMeter = 2,

    /// <summary>Linear meter.</summary>
    LinearMeter = 3,

    /// <summary>Cubic meter.</summary>
    CubicMeter = 4,

    /// <summary>Kilogram.</summary>
    Kilogram = 5,

    /// <summary>Ton.</summary>
    Ton = 6,

    /// <summary>Hour.</summary>
    Hour = 7,

    /// <summary>Day.</summary>
    Day = 8,

    /// <summary>Month.</summary>
    Month = 9,

    /// <summary>Year.</summary>
    Year = 10,

    /// <summary>Trip.</summary>
    Trip = 11,

    /// <summary>Set.</summary>
    Set = 12,

    /// <summary>Other (specify in description).</summary>
    Other = 99
}
