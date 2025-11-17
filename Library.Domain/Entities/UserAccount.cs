using System;

namespace Library.Domain.Entities;

public class UserAccount
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int ActiveLoansCount { get; private set; }
    public decimal AmountDue { get; private set; }

    // Ctor pour EF Core
    private UserAccount()
    {
    }

    // Cas normal : création d'un utilisateur avec un nom
    public UserAccount(Guid id, string name)
        : this(id, name, 0, 0m)
    {
    }

    // Ctor complet, utile pour tests ou scénarios avancés
    public UserAccount(Guid id, string name, int activeLoansCount, decimal amountDue)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));

        Id = id;
        Name = name.Trim();
        ActiveLoansCount = activeLoansCount;
        AmountDue = amountDue;
    }

    public void IncrementLoans() => ActiveLoansCount++;
    public void DecrementLoans() => ActiveLoansCount--;
    public void AddAmount(decimal amount) => AmountDue += amount;
    public void PayAmount(decimal amount) => AmountDue -= amount;
}
