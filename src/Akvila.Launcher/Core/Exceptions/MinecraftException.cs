using System;

namespace Akvila.Launcher.Core.Exceptions;

public class MinecraftException(string message) : Exception(message);
