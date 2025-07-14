namespace Funds.Abstractions;

/// <summary>
/// Command contract
/// </summary>
/// <typeparam name="T"></typeparam>
public interface ICommandHandler<in T>
{
    /// <summary>
    /// Execute the command.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task ExecuteAsync(T request, CancellationToken cancellationToken = default);
}