using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankDeposits.Models;

[Table("accounts")]
public class Account
{
    [Key]
    [Column("id_account")]
    public int Id { get; set; }

    [Column("id_client")]
    [Required]
    public int ClientId { get; set; }

    [ForeignKey(nameof(ClientId))]
    public Client? Client { get; set; }

    [Column("id_vklad")]
    [Required]
    public int VkladId { get; set; }

    [ForeignKey(nameof(VkladId))]
    public Vklad? Vklad { get; set; }

    [Column("open_date")]
    [Required]
    public DateTime OpenDate { get; set; }

    [Column("close_date")]
    public DateTime? CloseDate { get; set; }

    [Column("amount")]
    [Required]
    public decimal Amount { get; set; }

    [NotMapped]
    public DateTime EndDate => OpenDate.AddMonths(Vklad?.TermMonths ?? 0);

    [NotMapped]
    public decimal PayoutAmount
    {
        get
        {
            var rate = (double)(Vklad?.Rate ?? 0);
            var months = Vklad?.TermMonths ?? 0;
            return Amount * (decimal)Math.Pow(1 + rate / 100 / 12, months);
        }
    }

    [NotMapped]
    public decimal Income => PayoutAmount - Amount;

    [NotMapped]
    public string Status
    {
        get
        {
            if (CloseDate == null) return "Активен";
            return CloseDate.Value.Date >= DateTime.Now.Date ? "Активен" : "Закрыт";
        }
    }
}