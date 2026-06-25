using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BankDeposits.Models;
using OfficeOpenXml;

namespace BankDeposits.Services;

public class LoadExcelService : ILoadInterface
{
    public string FileExtension => ".xlsx";
    public string FileFilter => "Excel files (*.xlsx)|*.xlsx";

    public LoadExcelService()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public async Task<(List<Client> clients, List<Vklad> vklads, List<Account> accounts)> LoadAsync(string filePath)
    {
        return await Task.Run(() =>
        {
            var clients = new List<Client>();
            var vklads = new List<Vklad>();
            var accounts = new List<Account>();

            using var package = new ExcelPackage(new FileInfo(filePath));

            foreach (var worksheet in package.Workbook.Worksheets)
            {
                var name = worksheet.Name.ToLowerInvariant();

                if (name.Contains("клиент"))
                {
                    for (int row = 2; row <= worksheet.Dimension?.Rows; row++)
                    {
                        var fio = worksheet.Cells[row, 2].Text;
                        var parts = fio.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        clients.Add(new Client
                        {
                            Id = int.Parse(worksheet.Cells[row, 1].Text),
                            LastName = parts.Length > 0 ? parts[0] : "",
                            FirstName = parts.Length > 1 ? parts[1] : "",
                            MiddleName = parts.Length > 2 ? parts[2] : null,
                            Passport = worksheet.Cells[row, 3].Text,
                            Address = worksheet.Cells[row, 4].Text,
                            Phone = worksheet.Cells[row, 5].Text
                        });
                    }
                }
                else if (name.Contains("вклад"))
                {
                    for (int row = 2; row <= worksheet.Dimension?.Rows; row++)
                    {
                        vklads.Add(new Vklad
                        {
                            Id = int.Parse(worksheet.Cells[row, 1].Text),
                            Name = worksheet.Cells[row, 2].Text,
                            TermMonths = int.Parse(worksheet.Cells[row, 3].Text),
                            Rate = double.Parse(worksheet.Cells[row, 4].Text)
                        });
                    }
                }
                else if (name.Contains("счет"))
                {
                    for (int row = 2; row <= worksheet.Dimension?.Rows; row++)
                    {
                        var closeStr = worksheet.Cells[row, 6].Text;
                        accounts.Add(new Account
                        {
                            Id = int.Parse(worksheet.Cells[row, 1].Text),
                            ClientId = int.Parse(worksheet.Cells[row, 2].Text),
                            VkladId = int.Parse(worksheet.Cells[row, 3].Text),
                            OpenDate = DateTime.Parse(worksheet.Cells[row, 4].Text),
                            CloseDate = string.IsNullOrWhiteSpace(closeStr) ? null : DateTime.Parse(closeStr),
                            Amount = decimal.Parse(worksheet.Cells[row, 7].Text)
                        });
                    }
                }
            }

            return (clients, vklads, accounts);
        });
    }

    public async Task SaveAsync(string filePath, List<Client> clients, List<Vklad> vklads, List<Account> accounts)
    {
        await Task.Run(() =>
        {
            using var package = new ExcelPackage();

            if (vklads.Count > 0)
            {
                var ws = package.Workbook.Worksheets.Add("Вклады");
                ws.Cells[1, 1].Value = "Код";
                ws.Cells[1, 2].Value = "Наименование";
                ws.Cells[1, 3].Value = "Срок (мес.)";
                ws.Cells[1, 4].Value = "Ставка (%)";
                for (int i = 0; i < vklads.Count; i++)
                {
                    ws.Cells[i + 2, 1].Value = vklads[i].Id;
                    ws.Cells[i + 2, 2].Value = vklads[i].Name;
                    ws.Cells[i + 2, 3].Value = vklads[i].TermMonths;
                    ws.Cells[i + 2, 4].Value = vklads[i].Rate;
                }
                ws.Cells[ws.Dimension.Address].AutoFitColumns();
            }

            if (clients.Count > 0)
            {
                var ws = package.Workbook.Worksheets.Add("Клиенты");
                ws.Cells[1, 1].Value = "Код";
                ws.Cells[1, 2].Value = "ФИО";
                ws.Cells[1, 3].Value = "Паспорт";
                ws.Cells[1, 4].Value = "Адрес";
                ws.Cells[1, 5].Value = "Телефон";
                for (int i = 0; i < clients.Count; i++)
                {
                    ws.Cells[i + 2, 1].Value = clients[i].Id;
                    ws.Cells[i + 2, 2].Value = clients[i].FullName;
                    ws.Cells[i + 2, 3].Value = clients[i].Passport;
                    ws.Cells[i + 2, 4].Value = clients[i].Address;
                    ws.Cells[i + 2, 5].Value = clients[i].Phone;
                }
                ws.Cells[ws.Dimension.Address].AutoFitColumns();
            }

            if (accounts.Count > 0)
            {
                var ws = package.Workbook.Worksheets.Add("Счета");
                ws.Cells[1, 1].Value = "№ счета";
                ws.Cells[1, 2].Value = "Код клиента";
                ws.Cells[1, 3].Value = "Код вклада";
                ws.Cells[1, 4].Value = "Дата открытия";
                ws.Cells[1, 5].Value = "Дата закрытия";
                ws.Cells[1, 6].Value = "Сумма";
                for (int i = 0; i < accounts.Count; i++)
                {
                    ws.Cells[i + 2, 1].Value = accounts[i].Id;
                    ws.Cells[i + 2, 2].Value = accounts[i].ClientId;
                    ws.Cells[i + 2, 3].Value = accounts[i].VkladId;
                    ws.Cells[i + 2, 4].Value = accounts[i].OpenDate.ToString("yyyy-MM-dd");
                    ws.Cells[i + 2, 5].Value = accounts[i].CloseDate?.ToString("yyyy-MM-dd") ?? "";
                    ws.Cells[i + 2, 6].Value = accounts[i].Amount;
                }
                ws.Cells[ws.Dimension.Address].AutoFitColumns();
            }

            package.SaveAs(new FileInfo(filePath));
        });
    }
}
