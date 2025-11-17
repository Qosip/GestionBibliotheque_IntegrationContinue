using Library.Domain.Entities;

namespace Library.Application.Repositories;

public interface IUserRepository
{
    UserAccount? GetById(Guid id);
    IEnumerable<UserAccount> GetAll();

    void Add(UserAccount user);
    void Update(UserAccount user);
}