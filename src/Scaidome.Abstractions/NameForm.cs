namespace Scaidome.Abstractions;

/// <summary>The character rule a name is checked against.</summary>
public enum NameForm
{
    /// <summary>
    /// Letters, digits, underscores, hyphens and spaces: areas, topology records, user-defined types and evaluator templates.
    /// The default form.
    /// </summary>
    Loose,

    /// <summary>Letters, digits and underscores: tag names. The strict form.</summary>
    TagName,

    /// <summary>Tag-name segments joined by dots, none of them empty: property names.</summary>
    DottedIdentifier,
}
