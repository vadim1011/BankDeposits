using BankDeposits.Models;
using iText.Layout.Element;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace BankDeposits.Services;

public class LoadJSONService : ILoadInterface
{
    public string FileExtension => ".json";
    public string FileFilter => "JSON files (*.json)|*.json";

    public async Task<(List<Client> clients, List<Vklad> vklads, List<Account> accounts)> LoadAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        var data = JsonSerializer.Deserialize<JsonData>(json);

        var clients = new List< Client > ();
        var vklads = new List<Vklad>();
        var accounts = new List< Account > ();

        if (data?.Vklads != null)
        {
            foreach (var v in data.Vklads)
            {
                vklads.Add(new Vklad
                {
                    Id = v.id_vklad,
                    Name = v.namevklad,
                    TermMonths = v.srok,
                    Rate = v.stavka_procent
                });
            }
        }

        if (data?.Clients != null)
        {
            foreach (var c in data.Clients)
            {
                var parts = c.FIO.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                clients.Add(new Client
                {
                    Id = c.id_client,
                    LastName = parts.Length > 0 ? parts[0] : "",
                    FirstName = parts.Length > 1 ? parts[1] : "",
                    MiddleName = parts.Length > 2 ? parts[2] : null,
                    Passport = c.numberpassport,
                    Address = c.Address,
                    Phone = c.phone
                });
            }
        }

        if (data?.Accounts != null)
        {
            foreach (var a in data.Accounts)
            {
                DateTime? closeDate = null;
                if (!string.IsNullOrWhiteSpace(a.DataClose) && DateTime.TryParse(a.DataClose, out var parsedDate))
                {
                    closeDate = parsedDate;
                }

                accounts.Add(new Account
                {
                    Id = a.id_account,
                    ClientId = a.id_client,
                    VkladId = a.id_vklad,
                    OpenDate = DateTime.Parse(a.DataOpen),
                    CloseDate = closeDate,
                    Amount = a.sum
                });
            }
        }

        return (clients, vklads, accounts);
    }

    public async Task SaveAsync(string filePath, List<Client> clients, List<Vklad> vklads, List<Account> accounts)
    {
        var data = new JsonData
        {
            Vklads = vklads.ConvertAll(v => new JsonVklad
            {
                id_vklad = v.Id,
                namevklad = v.Name,
                srok = v.TermMonths,
                stavka_procent = v.Rate
            }),
            Clients = clients.ConvertAll(c => new JsonClient
            {
                id_client = c.Id,
                FIO = c.FullName,
                numberpassport = c.Passport,
                Address = c.Address,
                phone = c.Phone
            }),
            Accounts = accounts.ConvertAll(a => new JsonAccount
            {
                id_account = a.Id,
                id_client = a.ClientId,
                id_vklad = a.VkladId,
                DataOpen = a.OpenDate.ToString("yyyy-MM-dd"),
                DataClose = a.CloseDate?.ToString("yyyy-MM-dd"),
                sum = a.Amount
            })
        };

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);
    }

    private class JsonData
    {
        public List<JsonVklad> Vklads { get; set; } = new();
        public List<JsonClient> Clients { get; set; } = new();
        public List<JsonAccount> Accounts { get; set; } = new();
    }

    private class JsonVklad
    {
        public int id_vklad { get; set; }
        public string namevklad { get; set; } = string.Empty;
        public int srok { get; set; }
        public double stavka_procent { get; set; }
    }

    private class JsonClient
    {
        public int id_client { get; set; }
        public string FIO { get; set; } = string.Empty;
        public string numberpassport { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string phone { get; set; } = string.Empty;
    }

    private class JsonAccount
    {
        public int id_account { get; set; }
        public int id_client { get; set; }
        public int id_vklad { get; set; }
        public string DataOpen { get; set; } = string.Empty;
        public string? DataClose { get; set; }
        public decimal sum { get; set; }
    }
}