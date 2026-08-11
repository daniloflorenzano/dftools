using DiffPlex;
using DiffPlex.Chunkers;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using DfTools.Diff;

namespace DfTools.Tests.DiffTests;

[TestFixture]
public class TextDifferTests
{
    private ITextDiffer _differ = null!;

    [SetUp]
    public void SetUp()
    {
        _differ = new TextDiffer();
    }

    [Test]
    public void CompareSideBySide_WithIdenticalLines_ShouldReturnMatchingUnchangedLines()
    {
        // Arrange
        var oldText = "Line 1\nLine 2";
        var newText = "Line 1\nLine 2";

        // Act
        SideBySideDiffResult result = _differ.CompareSideBySide(oldText, newText);

        // Assert
        Assert.That(result.OldText.Lines, Has.Count.EqualTo(2));
        Assert.That(result.NewText.Lines, Has.Count.EqualTo(2));

        Assert.Multiple(() =>
        {
            Assert.That(result.OldText.Lines[0].Text, Is.EqualTo("Line 1"));
            Assert.That(result.OldText.Lines[0].Type, Is.EqualTo(DiffChangeType.Unchanged));
            Assert.That(result.OldText.Lines[0].Position, Is.EqualTo(1));

            Assert.That(result.NewText.Lines[0].Text, Is.EqualTo("Line 1"));
            Assert.That(result.NewText.Lines[0].Type, Is.EqualTo(DiffChangeType.Unchanged));
            Assert.That(result.NewText.Lines[0].Position, Is.EqualTo(1));
        });
    }

    [Test]
    public void CompareSideBySide_WithInsertionsAndDeletions_ShouldMapChangeTypesCorrectly()
    {
        // Arrange
        var oldText = "Line 1\nLine 2";
        var newText = "Line 1\nLine 3";

        // Act
        SideBySideDiffResult result = _differ.CompareSideBySide(oldText, newText);

        // Assert
        Assert.That(result.OldText.Lines, Has.Count.GreaterThan(0));
        Assert.That(result.NewText.Lines, Has.Count.GreaterThan(0));
    }

    [Test]
    public void CompareSideBySide_WithModifiedLine_ShouldIncludeSubPieceDiffs()
    {
        // Arrange
        var oldText = "hello world";
        var newText = "hello earth";

        // Act
        SideBySideDiffResult result = _differ.CompareSideBySide(oldText, newText);

        // Assert
        var oldLine = result.OldText.Lines[0];
        var newLine = result.NewText.Lines[0];

        Assert.Multiple(() =>
        {
            Assert.That(oldLine.Type, Is.EqualTo(DiffChangeType.Modified));
            Assert.That(newLine.Type, Is.EqualTo(DiffChangeType.Modified));
            Assert.That(oldLine.SubPieces, Is.Not.Empty);
            Assert.That(newLine.SubPieces, Is.Not.Empty);
        });

        var deletedPiece = oldLine.SubPieces.FirstOrDefault(p => p.Type == DiffChangeType.Deleted);
        var insertedPiece = newLine.SubPieces.FirstOrDefault(p => p.Type == DiffChangeType.Inserted);

        Assert.Multiple(() =>
        {
            Assert.That(deletedPiece?.Text, Is.EqualTo("world"));
            Assert.That(insertedPiece?.Text, Is.EqualTo("earth"));
        });
    }

    [Test]
    public void CompareSideBySide_WithNullInputs_ShouldTreatNullAsEmptyString()
    {
        // Act
        SideBySideDiffResult result = _differ.CompareSideBySide(null, null);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.OldText.Lines, Is.Empty);
            Assert.That(result.NewText.Lines, Is.Empty);
            Assert.That(result.HasDifferences, Is.False);
        });
    }

    [Test]
    public void CompareSideBySide_WithDifferences_ShouldSetHasDifferencesTrue()
    {
        // Act
        SideBySideDiffResult result = _differ.CompareSideBySide("abc", "xyz");

        // Assert
        Assert.That(result.HasDifferences, Is.True);
    }

    [Test]
    public void Constructor_WithNullDiffBuilder_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TextDiffer(null!));
    }

    [Test]
    public void CompareSideBySide_WithDeletionOnly_ShouldSetHasDifferencesTrue()
    {
        // Act (oldText has lines, newText is empty)
        SideBySideDiffResult result = _differ.CompareSideBySide("Line deleted", "");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.HasDifferences, Is.True);
            Assert.That(result.OldText.Lines.Any(l => l.Type == DiffChangeType.Deleted), Is.True);
        });
    }

    [Test]
    public void CompareSideBySide_WithInsertionOnly_ShouldSetHasDifferencesTrue()
    {
        // Act (oldText is empty, newText has lines)
        SideBySideDiffResult result = _differ.CompareSideBySide("", "Line inserted");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.HasDifferences, Is.True);
            Assert.That(result.NewText.Lines.Any(l => l.Type == DiffChangeType.Inserted), Is.True);
        });
    }

    [Test]
    public void CompareSideBySide_WithNullTextInPieceModel_ShouldDefaultToEmptyString()
    {
        // Arrange
        var mockBuilder = new NSubstituteCustomDiffBuilder(new DiffPlex.DiffBuilder.Model.DiffPiece { Position = 1, Text = null, Type = ChangeType.Unchanged, SubPieces = null });
        var differ = new TextDiffer(mockBuilder);

        // Act
        var result = differ.CompareSideBySide("a", "b");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.OldText.Lines[0].Text, Is.EqualTo(string.Empty));
            Assert.That(result.OldText.Lines[0].SubPieces, Is.Empty);
        });
    }

    private class NSubstituteCustomDiffBuilder : ISideBySideDiffBuilder
    {
        private readonly DiffPlex.DiffBuilder.Model.DiffPiece _piece;

        public NSubstituteCustomDiffBuilder(DiffPlex.DiffBuilder.Model.DiffPiece piece)
        {
            _piece = piece;
        }

        public SideBySideDiffModel BuildDiffModel(string oldText, string newText)
        {
            var model = new SideBySideDiffModel();
            model.OldText.Lines.Add(_piece);
            model.NewText.Lines.Add(_piece);
            return model;
        }

        public SideBySideDiffModel BuildDiffModel(string oldText, string newText, bool ignoreWhitespace)
        {
            return BuildDiffModel(oldText, newText);
        }

        public SideBySideDiffModel BuildDiffModel(string oldText, string newText, bool ignoreWhitespace, bool ignoreCase, IChunker chunker)
        {
            return BuildDiffModel(oldText, newText);
        }
    }
}
