using Microsoft.Extensions.Logging;
using Moq;
using TendexAI.Application.Features.Rfp.Commands.AddBoqItem;
using TendexAI.Application.Features.Rfp.Commands.BatchAddBoqItems;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Tests.Application.Rfp;

public sealed class BoqCommandHandlerTests
{
    private readonly Mock<ICompetitionRepository> _repositoryMock;
    private readonly Mock<ILogger<AddBoqItemCommandHandler>> _addLoggerMock;
    private readonly Mock<ILogger<BatchAddBoqItemsCommandHandler>> _batchLoggerMock;

    public BoqCommandHandlerTests()
    {
        _repositoryMock = new Mock<ICompetitionRepository>();
        _addLoggerMock = new Mock<ILogger<AddBoqItemCommandHandler>>();
        _batchLoggerMock = new Mock<ILogger<BatchAddBoqItemsCommandHandler>>();
    }

    [Fact]
    public async Task AddBoqItem_Handler_Should_Use_Direct_Insert_With_Next_Sort_Order()
    {
        // Arrange
        var competitionId = Guid.NewGuid();
        var command = new AddBoqItemCommand(
            competitionId,
            "1",
            "توريد أجهزة حاسب",
            "Computer supply",
            BoqItemUnit.Each,
            5,
            1000m,
            "Hardware",
            "user-1");

        BoqItem? persistedItem = null;

        _repositoryMock
            .Setup(r => r.IsCompetitionModifiableAsync(competitionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _repositoryMock
            .Setup(r => r.GetBoqItemCountAsync(competitionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        _repositoryMock
            .Setup(r => r.AddBoqItemDirectAsync(It.IsAny<BoqItem>(), It.IsAny<CancellationToken>()))
            .Callback<BoqItem, CancellationToken>((item, _) => persistedItem = item)
            .Returns(Task.CompletedTask);

        var handler = new AddBoqItemCommandHandler(_repositoryMock.Object, _addLoggerMock.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(persistedItem);
        Assert.Equal(competitionId, persistedItem!.CompetitionId);
        Assert.Equal(3, persistedItem.SortOrder);
        Assert.Equal("1", persistedItem.ItemNumber);
        Assert.Equal(5, persistedItem.Quantity);
        _repositoryMock.Verify(r => r.AddBoqItemDirectAsync(It.IsAny<BoqItem>(), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.GetByIdWithDetailsForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddBoqItem_Handler_Should_Fail_When_Competition_Does_Not_Exist()
    {
        // Arrange
        var competitionId = Guid.NewGuid();
        var command = new AddBoqItemCommand(
            competitionId,
            "1",
            "وصف",
            "Description",
            BoqItemUnit.Each,
            1,
            null,
            null,
            "user-1");

        _repositoryMock
            .Setup(r => r.IsCompetitionModifiableAsync(competitionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(competitionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Competition?)null);

        var handler = new AddBoqItemCommandHandler(_repositoryMock.Object, _addLoggerMock.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Competition not found.", result.Error);
        _repositoryMock.Verify(r => r.AddBoqItemDirectAsync(It.IsAny<BoqItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BatchAddBoqItems_Handler_Should_Use_Direct_Insert_And_Reset_Sort_Order_When_Replacing()
    {
        // Arrange
        var competitionId = Guid.NewGuid();
        var command = new BatchAddBoqItemsCommand(
            competitionId,
            new List<BatchBoqItemInput>
            {
                new("1", "البند الأول", "Item 1", BoqItemUnit.Each, 2, 50m, "Cat A"),
                new("2", "البند الثاني", "Item 2", BoqItemUnit.LinearMeter, 4, 75m, "Cat B"),
            },
            true,
            "user-1");

        IEnumerable<BoqItem>? persistedItems = null;
        bool clearExisting = false;

        _repositoryMock
            .Setup(r => r.IsCompetitionModifiableAsync(competitionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _repositoryMock
            .Setup(r => r.AddBoqItemsDirectAsync(It.IsAny<IEnumerable<BoqItem>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<BoqItem>, bool, CancellationToken>((items, clear, _) =>
            {
                persistedItems = items.ToList();
                clearExisting = clear;
            })
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(r => r.GetBoqItemCountAsync(competitionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var handler = new BatchAddBoqItemsCommandHandler(_repositoryMock.Object, _batchLoggerMock.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(clearExisting);
        Assert.NotNull(persistedItems);
        var items = persistedItems!.ToList();
        Assert.Equal(2, items.Count);
        Assert.Equal(1, items[0].SortOrder);
        Assert.Equal(2, items[1].SortOrder);
        _repositoryMock.Verify(r => r.GetBoqItemCountAsync(competitionId, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.AddBoqItemsDirectAsync(It.IsAny<IEnumerable<BoqItem>>(), true, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.GetByIdWithDetailsForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BatchAddBoqItems_Handler_Should_Return_Failure_When_Direct_Insert_Throws()
    {
        // Arrange
        var competitionId = Guid.NewGuid();
        var command = new BatchAddBoqItemsCommand(
            competitionId,
            new List<BatchBoqItemInput>
            {
                new("1", "البند الأول", "Item 1", BoqItemUnit.Each, 2, 50m, "Cat A"),
            },
            false,
            "user-1");

        _repositoryMock
            .Setup(r => r.IsCompetitionModifiableAsync(competitionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _repositoryMock
            .Setup(r => r.GetBoqItemCountAsync(competitionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(4);

        _repositoryMock
            .Setup(r => r.AddBoqItemsDirectAsync(It.IsAny<IEnumerable<BoqItem>>(), false, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("duplicate item number"));

        var handler = new BatchAddBoqItemsCommandHandler(_repositoryMock.Object, _batchLoggerMock.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("duplicate item number", result.Error);
    }
}
