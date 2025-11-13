using System;
using Library.Domain;

namespace Library.Application;

public interface IUserRepository
{
    UserAccount? GetById(Guid id);
}
