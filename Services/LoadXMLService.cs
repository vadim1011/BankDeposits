using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using BankDeposits.Models;

namespace BankDeposits.Services;

public class LoadXMLService : ILoadInterface
{
    public string FileExtension => ".xml";
    public string FileFilter => "XML files (*.xml)|*.xml";

    public async Task<(List<Client> clients, List<Vklad> vklads, List<Account> accounts)> LoadAsync(string filePath)
    {
        return await Task.Run(() =>
        {
            var doc = XDocument.Load(filePath);
            var clients = new List<Client>();
            var vklads = new List<Vklad>();
            var accounts = new List<Account>();

            foreach (var vkladElem in doc.Descendants("Вклад"))
            {
                vklads.Add(new Vklad
                {
                    Id = (int)vkladElem.Attribute("Код"),
                    Name = (string)vkladElem.Attribute("Наименование"),
                    TermMonths = (int)vkladElem.Attribute("СрокМесяцев"),
                    Rate = (double)vkladElem.Attribute("Ставка")
                });
            }

            foreach (var clientElem in doc.Descendants("Клиент"))
            {
                var fio = (string)clientElem.Attribute("ФИО");
                var parts = fio.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                clients.Add(new Client
                {
                    Id = (int)clientElem.Attribute("Код"),
                    LastName = parts.Length > 0 ? parts[0] : "",
                    FirstName = parts.Length > 1 ? parts[1] : "",
                    MiddleName = parts.Length > 2 ? parts[2] : null,
                    Passport = (string)clientElem.Attribute("Паспорт"),
                    Address = (string)clientElem.Attribute("Адрес"),
                    Phone = (string)clientElem.Attribute("Телефон")
                });
            }

            foreach (var accountElem in doc.Descendants("Счет"))
            {
                var closeStr = (string)accountElem.Attribute("ДатаЗакрытия");
                accounts.Add(new Account
                {
                    Id = (int)accountElem.Attribute("Номер"),
                    ClientId = (int)accountElem.Attribute("КодКлиента"),
                    VkladId = (int)accountElem.Attribute("КодВклада"),
                    OpenDate = DateTime.Parse((string)accountElem.Attribute("ДатаОткрытия")),
                    CloseDate = string.IsNullOrEmpty(closeStr) ? null : DateTime.Parse(closeStr),
                    Amount = (decimal)accountElem.Attribute("Сумма")
                });
            }

            return (clients, vklads, accounts);
        });
    }

    public async Task SaveAsync(string filePath, List<Client> clients, List<Vklad> vklads, List<Account> accounts)
    {
        await Task.Run(() =>
        {
            var root = new XElement("BankDeposits");

            var vkladsEl = new XElement("Вклады");
            foreach (var v in vklads)
            {
                vkladsEl.Add(new XElement("Вклад",
                    new XAttribute("Код", v.Id),
                    new XAttribute("Наименование", v.Name),
                    new XAttribute("СрокМесяцев", v.TermMonths),
                    new XAttribute("Ставка", v.Rate)));
            }
            root.Add(vkladsEl);

            var clientsEl = new XElement("Клиенты");
            foreach (var c in clients)
            {
                clientsEl.Add(new XElement("Клиент",
                    new XAttribute("Код", c.Id),
                    new XAttribute("ФИО", c.FullName),
                    new XAttribute("Паспорт", c.Passport),
                    new XAttribute("Адрес", c.Address ?? ""),
                    new XAttribute("Телефон", c.Phone ?? "")));
            }
            root.Add(clientsEl);

            var accountsEl = new XElement("Счета");
            foreach (var a in accounts)
            {
                accountsEl.Add(new XElement("Счет",
                    new XAttribute("Номер", a.Id),
                    new XAttribute("КодКлиента", a.ClientId),
                    new XAttribute("КодВклада", a.VkladId),
                    new XAttribute("ДатаОткрытия", a.OpenDate.ToString("yyyy-MM-dd")),
                    new XAttribute("ДатаЗакрытия", a.CloseDate?.ToString("yyyy-MM-dd") ?? ""),
                    new XAttribute("Сумма", a.Amount)));
            }
            root.Add(accountsEl);

            new XDocument(root).Save(filePath);
        });
    }
}
