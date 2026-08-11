using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace DfTools.Tests.LearningTests;

[TestFixture]
public class DiffPlexSideBySideLearningTests
{
    private ISideBySideDiffBuilder _diffBuilder = null!;

    [SetUp]
    public void SetUp()
    {
        _diffBuilder = new SideBySideDiffBuilder(new Differ());
    }

    [Test]
    public void BuildDiffModel_WithIdenticalText_ShouldReturnUnchangedLinesOnBothSides()
    {
        // Arrange
        const string oldText = "Hello World\nSecond Line";
        const string newText = "Hello World\nSecond Line";

        // Act
        var result = _diffBuilder.BuildDiffModel(oldText, newText);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.OldText.Lines, Has.Count.EqualTo(2));
            Assert.That(result.NewText.Lines, Has.Count.EqualTo(2));
            
            Assert.That(result.OldText.Lines[0].Type, Is.EqualTo(ChangeType.Unchanged));
            Assert.That(result.OldText.Lines[0].Text, Is.EqualTo("Hello World"));
            Assert.That(result.NewText.Lines[0].Type, Is.EqualTo(ChangeType.Unchanged));
            Assert.That(result.NewText.Lines[0].Text, Is.EqualTo("Hello World"));

            Assert.That(result.OldText.Lines[1].Type, Is.EqualTo(ChangeType.Unchanged));
            Assert.That(result.OldText.Lines[1].Text, Is.EqualTo("Second Line"));
            Assert.That(result.NewText.Lines[1].Type, Is.EqualTo(ChangeType.Unchanged));
            Assert.That(result.NewText.Lines[1].Text, Is.EqualTo("Second Line"));
        });
    }

    [Test]
    public void BuildDiffModel_WithAddedAndDeletedLines_ShouldReflectDeletionsOnOldAndAdditionsOnNew()
    {
        // Arrange
        const string oldText = "Line 1\nLine 2\nLine 3";
        const string newText = "Line 1\nLine 2 Modified\nLine 3\nLine 4 Added";

        // Act
        var result = _diffBuilder.BuildDiffModel(oldText, newText);

        // Assert
        Assert.Multiple(() =>
        {
            // Line 2 modified shows as Deleted in OldText and Inserted in NewText (or Modified depending on DiffPlex sub-pieces)
            // Let's verify line counts and types
            Assert.That(result.OldText.Lines, Has.Count.GreaterThan(0));
            Assert.That(result.NewText.Lines, Has.Count.GreaterThan(0));
            
            Assert.That(result.OldText.Lines[0].Type, Is.EqualTo(ChangeType.Unchanged));
            Assert.That(result.NewText.Lines[0].Type, Is.EqualTo(ChangeType.Unchanged));

            // Line 4 added at the end
            var lastIndexNew = result.NewText.Lines.Count - 1;
            Assert.That(result.NewText.Lines[lastIndexNew].Type, Is.EqualTo(ChangeType.Inserted));
            Assert.That(result.NewText.Lines[lastIndexNew].Text, Is.EqualTo("Line 4 Added"));
        });
    }

    [Test]
    public void BuildDiffModel_WithModifiedLine_ShouldProvideSubPieceSubPiecesForCharacterDiffs()
    {
        // Arrange
        const string oldText = "The quick brown fox";
        const string newText = "The fast brown fox";

        // Act
        var result = _diffBuilder.BuildDiffModel(oldText, newText);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.OldText.Lines[0].Type, Is.EqualTo(ChangeType.Modified));
            Assert.That(result.NewText.Lines[0].Type, Is.EqualTo(ChangeType.Modified));

            Assert.That(result.OldText.Lines[0].SubPieces, Is.Not.Null.And.Not.Empty);
            Assert.That(result.NewText.Lines[0].SubPieces, Is.Not.Null.And.Not.Empty);
        });

        var oldSubPieces = result.OldText.Lines[0].SubPieces;
        var newSubPieces = result.NewText.Lines[0].SubPieces;

        // Verify subpieces flag changes within the line
        var deletedSubPiece = oldSubPieces.FirstOrDefault(sp => sp.Type == ChangeType.Deleted);
        var insertedSubPiece = newSubPieces.FirstOrDefault(sp => sp.Type == ChangeType.Inserted);

        Assert.Multiple(() =>
        {
            Assert.That(deletedSubPiece, Is.Not.Null);
            Assert.That(deletedSubPiece!.Text, Is.EqualTo("quick"));

            Assert.That(insertedSubPiece, Is.Not.Null);
            Assert.That(insertedSubPiece!.Text, Is.EqualTo("fast"));
        });
    }

    [Test]
    public void BuildDiffModel_WithEmptyStrings_ShouldReturnEmptyDiffModelResult()
    {
        // Arrange
        const string oldText = "";
        const string newText = "";

        // Act
        var result = _diffBuilder.BuildDiffModel(oldText, newText);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.OldText.Lines, Is.Empty);
            Assert.That(result.NewText.Lines, Is.Empty);
        });
    }

    [Test]
    public void BuildDiffModel_WithSpaceSeparatedText_IdentifiesWordLevelDifferencesInSubPieces()
    {
        // Arrange: "hello world 1" vs "hello world 2"
        const string oldText = "hello world 1";
        const string newText = "hello world 2";

        // Act
        var result = _diffBuilder.BuildDiffModel(oldText, newText);

        // Assert: DiffPlex splits words and identifies 'hello' and 'world' as Unchanged, and '1' vs '2' as Deleted/Inserted
        var oldSubPieces = result.OldText.Lines[0].SubPieces;
        var newSubPieces = result.NewText.Lines[0].SubPieces;

        Assert.Multiple(() =>
        {
            Assert.That(oldSubPieces.Where(p => p.Type == ChangeType.Unchanged).Select(p => p.Text), Is.EquivalentTo(new[] { "hello", " ", "world", " " }));
            Assert.That(oldSubPieces.First(p => p.Type == ChangeType.Deleted).Text, Is.EqualTo("1"));

            Assert.That(newSubPieces.Where(p => p.Type == ChangeType.Unchanged).Select(p => p.Text), Is.EquivalentTo(new[] { "hello", " ", "world", " " }));
            Assert.That(newSubPieces.First(p => p.Type == ChangeType.Inserted).Text, Is.EqualTo("2"));
        });
    }

    [Test]
    public void BuildDiffModel_WithSingleWordDifference_TreatsEntireWordAsSubPiece()
    {
        // Arrange: "text1" vs "text2" (single contiguous word token without spaces)
        const string oldText = "text1";
        const string newText = "text2";

        // Act
        var result = _diffBuilder.BuildDiffModel(oldText, newText);

        // Assert: Because DiffPlex defaults to word chunking (space/punctuation delimiters),
        // single continuous tokens like "text1" vs "text2" treat the whole token as a single deleted/inserted subpiece.
        var oldSubPieces = result.OldText.Lines[0].SubPieces;
        var newSubPieces = result.NewText.Lines[0].SubPieces;

        Assert.Multiple(() =>
        {
            Assert.That(oldSubPieces, Has.Count.EqualTo(1));
            Assert.That(oldSubPieces[0].Text, Is.EqualTo("text1"));
            Assert.That(oldSubPieces[0].Type, Is.EqualTo(ChangeType.Deleted));

            Assert.That(newSubPieces, Has.Count.EqualTo(1));
            Assert.That(newSubPieces[0].Text, Is.EqualTo("text2"));
            Assert.That(newSubPieces[0].Type, Is.EqualTo(ChangeType.Inserted));
        });
    }
}
