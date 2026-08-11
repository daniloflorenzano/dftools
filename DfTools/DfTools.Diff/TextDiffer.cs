using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using DiffPlexDiffPiece = DiffPlex.DiffBuilder.Model.DiffPiece;

namespace DfTools.Diff;

public class TextDiffer : ITextDiffer
{
    private readonly ISideBySideDiffBuilder _diffBuilder;

    public TextDiffer() : this(new SideBySideDiffBuilder(new Differ()))
    {
    }

    public TextDiffer(ISideBySideDiffBuilder diffBuilder)
    {
        _diffBuilder = diffBuilder ?? throw new ArgumentNullException(nameof(diffBuilder));
    }

    public SideBySideDiffResult CompareSideBySide(string? oldText, string? newText)
    {
        oldText ??= string.Empty;
        newText ??= string.Empty;

        SideBySideDiffModel diffModel = _diffBuilder.BuildDiffModel(oldText, newText);

        var oldPane = MapPane(diffModel.OldText);
        var newPane = MapPane(diffModel.NewText);

        bool hasDifferences = oldPane.Lines.Any(l => l.Type != DiffChangeType.Unchanged) ||
                             newPane.Lines.Any(l => l.Type != DiffChangeType.Unchanged);

        return new SideBySideDiffResult(oldPane, newPane, hasDifferences);
    }

    private static DiffPaneResult MapPane(DiffPaneModel paneModel)
    {
        var lines = paneModel.Lines.Select(MapPiece).ToList();
        return new DiffPaneResult(lines);
    }

    private static DiffPiece MapPiece(DiffPlexDiffPiece pieceModel)
    {
        var subPieces = pieceModel.SubPieces != null
            ? pieceModel.SubPieces.Select(MapPiece).ToList()
            : [];

        return new DiffPiece(
            pieceModel.Position,
            pieceModel.Text ?? string.Empty,
            MapChangeType(pieceModel.Type),
            subPieces
        );
    }

    private static DiffChangeType MapChangeType(ChangeType type)
    {
        return type switch
        {
            ChangeType.Unchanged => DiffChangeType.Unchanged,
            ChangeType.Deleted => DiffChangeType.Deleted,
            ChangeType.Inserted => DiffChangeType.Inserted,
            ChangeType.Imaginary => DiffChangeType.Imaginary,
            ChangeType.Modified => DiffChangeType.Modified,
            _ => DiffChangeType.Unchanged
        };
    }
}
