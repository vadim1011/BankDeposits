using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankDeposits.Models;

[Table("vklads")]
public class Vklad
{
    [Key]
    [Column("id_vklad")]
    public int Id { get; set; }

    [Column("name")]
    [Required]
    public string Name { get; set; } = string.Empty;

    [Column("term_months")]
    [Required]
    public int TermMonths { get; set; }

    [Column("rate")]
    [Required]
    public double Rate { get; set; }

    public List<Account> Accounts { get; set; } = new();
}