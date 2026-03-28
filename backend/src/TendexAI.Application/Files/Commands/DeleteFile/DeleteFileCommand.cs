using TendexAI.Application.Common.Messaging;

namespace TendexAI.Application.Files.Commands.DeleteFile;

/// <summary>
/// Command to soft-delete a file attachment.
/// The file remains in storage but is marked as deleted in the database.
/// </summary>
public sealed record DeleteFileCommand(Guid FileId, string? DeletedBy = null) : ICommand;
