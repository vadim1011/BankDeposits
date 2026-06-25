using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BankDeposits.Models;
using CsvHelper;
using CsvHelper.Configuration;

namespace BankDeposits.Services;

public class LoadCSVService : ILoadInterface
{
    public string FileExtension => ".csv";
    public string FileFilter => "CSV files (*.csv)|*.csv";

    public async Task<(List<Client> clients, List<Vklad> vklads, List<Account> accounts)> LoadAsync(string filePath)
    {
        return await Task.Run(() =>
        {
            using var reader = new StreamReader(filePath, Encoding.UTF8);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = ";", Encoding = Encoding.UTF8 });

            var clients = new List<Client>();
            var vklads = new List<Vklad>();
            var accounts = new List<Account>();

            csv.Read();
            csv.ReadHeader();
            var headers = csv.HeaderRecord;

            if (headers == null) return (clients, vklads, accounts);

            if (headers.Contains("фио") || headers.Contains("ФИО"))
            {
                while (csv.Read())
                {
                    var fio = csv.GetField("фио") ?? csv.GetField("ФИО") ?? "";
                    var parts = fio.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    clients.Add(new Client
                    {
                        Id = csv.GetField<int>("код_клиента"),
                        LastName = parts.Length > 0 ? parts[0] : "",
                        FirstName = parts.Length > 1 ? parts[1] : "",
                        MiddleName = parts.Length > 2 ? parts[2] : null,
                        Passport = csv.GetField("номер_паспорта") ?? csv.GetField("паспорт") ?? "",
                        Address = csv.GetField("адрес") ?? csv.GetField("Адрес"),
                        Phone = csv.GetField("телефон") ?? csv.GetField("Телефон")
                    });
                }
            }
            else if (headers.Contains("наименование_вклада") || headers.Contains("Вклад"))
            {
                while (csv.Read())
                {
                    vklads.Add(new Vklad
                    {
                        Id = csv.GetField<int>("код_вклада"),
                        Name = csv.GetField("наименование_вклада") ?? csv.GetField("Вклад") ?? "",
                        TermMonths = csv.GetField<int>("срок_хранения_месяцев"),
                        Rate = csv.GetField<double>("ставка_процентов")
                    });
                }
            }
            else if (headers.Contains("сумма") || headers.Contains("сумма_руб"))
            {
                while (csv.Read())
                {
                    var closeStr = csv.GetField("дата_закрытия");
                    accounts.Add(new Account
                    {
                        Id = csv.GetField<int>("номер_счета"),
                        ClientId = csv.GetField<int>("код_клиента"),
                        VkladId = csv.GetField<int>("код_вклада"),
                        OpenDate = DateTime.Parse(csv.GetField("дата_открытия")),
                        CloseDate = string.IsNullOrWhiteSpace(closeStr) ? null : DateTime.Parse(closeStr),
                        Amount = csv.GetField<decimal>("сумма_руб")
                    });
                }
            }

            return (clients, vklads, accounts);
        });
    }

    public async Task SaveAsync(string filePath, List<Client> clients, List<Vklad> vklads, List<Account> accounts)
    {
        await Task.Run(() =>
        {
            if (clients.Count > 0)
            {
                using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
                using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = ";", Encoding = Encoding.UTF8 });
                csv.WriteField("код_клиента");
                csv.WriteField("фио");
                csv.WriteField("номер_паспорта");
                csv.WriteField("адрес");
                csv.WriteField("телефон");
                csv.NextRecord();
                foreach (var c in clients)
                {
                    csv.WriteField(c.Id);
                    csv.WriteField(c.FullName);
                    csv.WriteField(c.Passport);
                    csv.WriteField(c.Address);
                    csv.WriteField(c.Phone);
                    csv.NextRecord();
                }
            }
            else if (vklads.Count > 0)
            {
                using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
                using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = ";", Encoding = Encoding.UTF8 });
                csv.WriteField("код_вклада");
                csv.WriteField("наименование_вклада");
                csv.WriteField("срок_хранения_месяцев");
                csv.WriteField("ставка_процентов");
                csv.NextRecord();
                foreach (var v in vklads)
                {
                    csv.WriteField(v.Id);
                    csv.WriteField(v.Name);
                    csv.WriteField(v.TermMonths);
                    csv.WriteField(v.Rate);
                    csv.NextRecord();
                }
            }
            else if (accounts.Count > 0)
            {
                using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
                using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = ";", Encoding = Encoding.UTF8 });
                csv.WriteField("номер_счета");
                csv.WriteField("код_клиента");
                csv.WriteField("код_вклада");
                csv.WriteField("дата_открытия");
                csv.WriteField("дата_закрытия");
                csv.WriteField("сумма_руб");
                csv.NextRecord();
                foreach (var a in accounts)
                {
                    csv.WriteField(a.Id);
                    csv.WriteField(a.ClientId);
                    csv.WriteField(a.VkladId);
                    csv.WriteField(a.OpenDate.ToString("yyyy-MM-dd"));
                    csv.WriteField(a.CloseDate?.ToString("yyyy-MM-dd") ?? "");
                    csv.WriteField(a.Amount);
                    csv.NextRecord();
                }
            }
        });
    }
}
