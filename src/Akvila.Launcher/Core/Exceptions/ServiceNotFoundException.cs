using System;

namespace Akvila.Launcher.Core.Exceptions;

public class ServiceNotFoundException(Type eType) : Exception {
    public Type NotFoundedService { get; } = eType;
}
