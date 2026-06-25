using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BankDeposits.Models;
using Microsoft.EntityFrameworkCore;

namespace BankDeposits.Services;

public class DbService : IDbService
{
    public async Task<List<Client>> GetClientsAsync()
    {
        await using var db = new AppDbContext();
        return await db.Clients.ToListAsync();
    }

    public async Task<List<Vklad>> GetVkladsAsync()
    {
        await using var db = new AppDbContext();
        return await db.Vklads.ToListAsync();
    }

    public async Task<List<Account>> GetAccountsAsync()
    {
        await using var db = new AppDbContext();
        return await db.Accounts
            .Include(a => a.Client)
            .Include(a => a.Vklad)
            .ToListAsync();
    }

    public async Task<Client?> GetClientByIdAsync(int id)
    {
        await using var db = new AppDbContext();
        return await db.Clients.FindAsync(id);
    }

    public async Task<Vklad?> GetVkladByIdAsync(int id)
    {
        await using var db = new AppDbContext();
        return await db.Vklads.FindAsync(id);
    }

    public async Task<Account?> GetAccountByIdAsync(int id)
    {
        await using var db = new AppDbContext();
        return await db.Accounts
            .Include(a => a.Client)
            .Include(a => a.Vklad)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task AddClientAsync(Client client)
    {
        await using var db = new AppDbContext();
        db.Clients.Add(client);
        await db.SaveChangesAsync();
    }

    public async Task AddVkladAsync(Vklad vklad)
    {
        await using var db = new AppDbContext();
        db.Vklads.Add(vklad);
        await db.SaveChangesAsync();
    }

    public async Task AddAccountAsync(Account account)
    {
        await using var db = new AppDbContext();
        db.Accounts.Add(account);
        await db.SaveChangesAsync();
    }

    public async Task UpdateClientAsync(Client client)
    {
        await using var db = new AppDbContext();
        db.Clients.Update(client);
        await db.SaveChangesAsync();
    }

    public async Task UpdateVkladAsync(Vklad vklad)
    {
        await using var db = new AppDbContext();
        db.Vklads.Update(vklad);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAccountAsync(Account account)
    {
        await using var db = new AppDbContext();
        db.Accounts.Update(account);
        await db.SaveChangesAsync();
    }
    public async Task DeleteClientAsync(int id)
    {
        await using var db = new AppDbContext();

        // Сначала удаляем все счета клиента
        var accounts = await db.Accounts.Where(a => a.ClientId == id).ToListAsync();
        db.Accounts.RemoveRange(accounts);

        // Потом удаляем клиента
        var client = await db.Clients.FindAsync(id);
        if (client != null)
        {
            db.Clients.Remove(client);
        }

        await db.SaveChangesAsync();
    }
    public async Task DeleteVkladAsync(int id)
    {
        await using var db = new AppDbContext();

        // Сначала удаляем все счета, связанные с этим вкладом
        var accounts = await db.Accounts.Where(a => a.VkladId == id).ToListAsync();
        db.Accounts.RemoveRange(accounts);

        // Потом удаляем сам вклад
        var vklad = await db.Vklads.FindAsync(id);
        if (vklad != null)
        {
            db.Vklads.Remove(vklad);
        }

        await db.SaveChangesAsync();
    }

    
    public async Task DeleteAccountAsync(int id)
    {
        await using var db = new AppDbContext();
        var account = await db.Accounts.FindAsync(id);
        if (account != null)
        {
            db.Accounts.Remove(account);
            await db.SaveChangesAsync();
        }
    }

    public async Task SeedDataAsync()
    {
        await using var db = new AppDbContext();
        await db.Database.EnsureCreatedAsync();

        if (await db.Vklads.AnyAsync()) return;

        var clientsPath = Path.Combine(AppContext.BaseDirectory, "клиенты.json");
        var vkladsPath = Path.Combine(AppContext.BaseDirectory, "вклады.json");
        var accountsPath = Path.Combine(AppContext.BaseDirectory, "счета.json");

        if (!File.Exists(clientsPath))
            clientsPath = Path.Combine(Directory.GetCurrentDirectory(), "клиенты.json");
        if (!File.Exists(vkladsPath))
            vkladsPath = Path.Combine(Directory.GetCurrentDirectory(), "вклады.json");
        if (!File.Exists(accountsPath))
            accountsPath = Path.Combine(Directory.GetCurrentDirectory(), "счета.json");

        if (File.Exists(vkladsPath))
        {
            var json = await File.ReadAllTextAsync(vkladsPath);
            var vkladsData = JsonSerializer.Deserialize<List<JsonVklad>>(json);
            if (vkladsData != null)
            {
                foreach (var v in vkladsData)
                {
                    db.Vklads.Add(new Vklad
                    {
                        Id = v.код_вклада,
                        Name = v.наименование_вклада,
                        TermMonths = v.срок_хранения_месяцев,
                        Rate = v.ставка_процентов
                    });
                }
            }
        }

        if (File.Exists(clientsPath))
        {
            var json = await File.ReadAllTextAsync(clientsPath);
            var clientsData = JsonSerializer.Deserialize<List<JsonClient>>(json);
            if (clientsData != null)
            {
                foreach (var c in clientsData)
                {
                    var parts = c.фио.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    db.Clients.Add(new Client
                    {
                        Id = c.код_клиента,
                        LastName = parts.Length > 0 ? parts[0] : "",
                        FirstName = parts.Length > 1 ? parts[1] : "",
                        MiddleName = parts.Length > 2 ? parts[2] : null,
                        Passport = c.номер_паспорта,
                        Address = c.адрес,
                        Phone = c.телефон
                    });
                }
            }
        }

        await db.SaveChangesAsync();

        if (File.Exists(accountsPath))
        {
            var json = await File.ReadAllTextAsync(accountsPath);
            var accountsData = JsonSerializer.Deserialize<List<JsonAccount>>(json);
            if (accountsData != null)
            {
                foreach (var a in accountsData)
                {
                    db.Accounts.Add(new Account
                    {
                        Id = a.номер_счета,
                        ClientId = a.код_клиента,
                        VkladId = a.код_вклада,
                        OpenDate = DateTime.Parse(a.дата_открытия),
                        CloseDate = string.IsNullOrEmpty(a.дата_закрытия) ? null : DateTime.Parse(a.дата_закрытия),
                        Amount = a.сумма_руб
                    });
                }
            }
        }

        await db.SaveChangesAsync();
    }

    private class JsonVklad
    {
        public int код_вклада { get; set; }
        public string наименование_вклада { get; set; } = string.Empty;
        public int срок_хранения_месяцев { get; set; }
        public double ставка_процентов { get; set; }
    }

    private class JsonClient
    {
        public int код_клиента { get; set; }
        public string фио { get; set; } = string.Empty;
        public string номер_паспорта { get; set; } = string.Empty;
        public string адрес { get; set; } = string.Empty;
        public string телефон { get; set; } = string.Empty;
    }

    private class JsonAccount
    {
        public int номер_счета { get; set; }
        public int код_клиента { get; set; }
        public int код_вклада { get; set; }
        public string дата_открытия { get; set; } = string.Empty;
        public string? дата_закрытия { get; set; }
        public decimal сумма_руб { get; set; }
    }
}
