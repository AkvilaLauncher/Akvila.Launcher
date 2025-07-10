using System;
using System.Threading.Tasks;
using Akvila.Web.Api.Dto.Integration;

namespace Akvila.Launcher.Core.Services;

public class ApplicationStateService : IApplicationStateService {
    private AuthTypeReadDto? _authType;

    public AuthTypeReadDto? AuthType => _authType;
    public bool HasAuthType => _authType != null;
    public event EventHandler<AuthTypeReadDto?>? AuthTypeChanged;

    public async Task SetAuthTypeAsync(AuthTypeReadDto? authType) {
        var previousAuthType = _authType;
        _authType = authType;

        if (!AreAuthTypesEqual(previousAuthType, authType)) {
            AuthTypeChanged?.Invoke(this, authType);
        }
    }

    public void ClearState() {
        _authType = null;
        AuthTypeChanged?.Invoke(this, null);
    }

    private static bool AreAuthTypesEqual(AuthTypeReadDto? first, AuthTypeReadDto? second) {
        if (first == null && second == null) return true;
        if (first == null || second == null) return false;

        return first.AuthType == second.AuthType &&
               string.Equals(first.Data, second.Data, StringComparison.Ordinal);
    }
}
