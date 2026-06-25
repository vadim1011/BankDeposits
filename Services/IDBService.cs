using System.Collections.Generic;
using System.Threading.Tasks;
using BankDeposits.Models;

namespace BankDeposits.Services;

public interface IDbService
{
    Task<List<Client>> GetClientsAsync();
    Task<List<Vklad>> GetVkladsAsync();
    Task<List<Account>> GetAccountsAsync();
    Task<Client?> GetClientByIdAsync(int id);
    Task<Vklad?> GetVkladByIdAsync(int id);
    Task<Account?> GetAccountByIdAsync(int id);
    Task AddClientAsync(Client client);
    Task AddVkladAsync(Vklad vklad);
    Task AddAccountAsync(Account account);
    Task UpdateClientAsync(Client client);
    Task UpdateVkladAsync(Vklad vklad);
    Task UpdateAccountAsync(Account account);
    Task DeleteClientAsync(int id);
    Task DeleteVkladAsync(int id);
    Task DeleteAccountAsync(int id);
    Task SeedDataAsync();
}
