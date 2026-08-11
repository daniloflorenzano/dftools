namespace DfTools.Diff;

public enum DiffChangeType
{
    Unchanged,
    Deleted,
    Inserted,
    Imaginary,
    Modified
}

public record DiffPiece(
    int? Position,
    string Text,
    DiffChangeType Type,
    IReadOnlyList<DiffPiece> SubPieces
);

public record DiffPaneResult(
    IReadOnlyList<DiffPiece> Lines
);

public record SideBySideDiffResult(
    DiffPaneResult OldText,
    DiffPaneResult NewText,
    bool HasDifferences
);

public interface ITextDiffer
{
    SideBySideDiffResult CompareSideBySide(string? oldText, string? newText);
}
