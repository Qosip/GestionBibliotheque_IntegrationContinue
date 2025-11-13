using System;

namespace Library.Application;

public interface IClock
{
    DateTime UtcNow { get; }
}
