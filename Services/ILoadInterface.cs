using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BankDeposits.Models;

namespace BankDeposits.Services;

public interface ILoadInterface
{
    string FileExtension { get; }
    string FileFilter { get; }
    Task<(List<Client> clients, List<Vklad> vklads, List<Account> accounts)> LoadAsync(string filePath);
    Task SaveAsync(string filePath, List<Client> clients, List<Vklad> vklads, List<Account> accounts);
}
