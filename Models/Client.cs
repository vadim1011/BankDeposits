using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankDeposits.Models;

[Table("clients")]
public class Client
{
    [Key]
    [Column("id_client")]
    public int Id { get; set; }

    [Column("last_name")]
    [Required]
    public string LastName { get; set; } = string.Empty;

    [Column("first_name")]
    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Column("middle_name")]
    public string? MiddleName { get; set; }

    [Column("passport")]
    [Required]
    public string Passport { get; set; } = string.Empty;

    [Column("address")]
    public string? Address { get; set; }

    [Column("phone")]
    public string? Phone { get; set; }

    public List<Account> Accounts { get; set; } = new();

    [NotMapped]
    public string FullName => $"{LastName} {FirstName} {MiddleName}".Trim();

    internal static void Remove(Client selectedClient)
    {
        throw new NotImplementedException();
    }
}