using System;
using System.Threading.Tasks;
using Akvila.Web.Api.Dto.Integration;

namespace Akvila.Launcher.Core.Services;

public interface IApplicationStateService {
    /// <summary>
    /// Gets the current auth type data
    /// </summary>
    AuthTypeReadDto? AuthType { get; }

    /// <summary>
    /// Event triggered when auth type data changes
    /// </summary>
    event EventHandler<AuthTypeReadDto?>? AuthTypeChanged;

    /// <summary>
    /// Sets the auth type data
    /// </summary>
    /// <param name="authType">The auth type data to set</param>
    Task SetAuthTypeAsync(AuthTypeReadDto? authType);

    /// <summary>
    /// Clears all cached application state
    /// </summary>
    void ClearState();

    /// <summary>
    /// Checks if auth type data is available
    /// </summary>
    bool HasAuthType { get; }
}
