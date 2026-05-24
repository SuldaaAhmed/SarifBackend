using Backend.Models.Customers;
using Backend.Models.Setup;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
[HttpGet("account-type-lookup")]


public IActionResult GetAccountTypes()
{
    var data = Enum.GetValues(typeof(AccountTypeEnum))
        .Cast<AccountTypeEnum>()
        .Select(x => new
        {
            id = (int)x,
            name = x.ToString()
        });

    return Ok(data);
}
namespace Backend.Models.Accounts
{
    public class Account : BaseEntity
    {
        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        // 👉 Business Type (UI / logic)
        public AccountTypeEnum AccountType { get; set; }

        // 👉 Accounting Nature (Reports)
        public AccountNatureEnum Nature { get; set; }

        public Guid? ReferenceId { get; set; }

        public int CurrencyId { get; set; }

        [ForeignKey(nameof(CurrencyId))]
        public Currency Currency { get; set; } = null!;

        public Guid AgencyId { get; set; }
        public Guid? BranchId { get; set; }

        [ForeignKey(nameof(AgencyId))]
        public Agency Agency { get; set; } = null!;

        [ForeignKey(nameof(BranchId))]
        public Branch? Branch { get; set; }

        public ICollection<Deposit> Deposits { get; set; } = new List<Deposit>();
    }

    // 👉 Business Types (SARIF)
    public enum AccountTypeEnum
    {
        Cash = 1,
        Bank = 2,
        Wallet = 3,
        Customer = 4,
        Loan = 5,
        Expense = 6,
        Revenue = 7,
        Capital = 8 ,
        RECEIVABLE=9,
        PAYABLE = 10
    }

    // 👉 Accounting Nature (Reports)
    public enum AccountNatureEnum
    {
        Asset = 1,
        Liability = 2,
        Equity = 3,
        Revenue = 4,
        Expense = 5
    }

    // 👉 Helper for auto assigning Nature
    // 👉 Helper for auto assigning Nature
    public static class AccountHelper
    {
        public static AccountNatureEnum GetNature(AccountTypeEnum type)
        {
            return type switch
            {
                AccountTypeEnum.Cash => AccountNatureEnum.Asset,
                AccountTypeEnum.Bank => AccountNatureEnum.Asset,
                AccountTypeEnum.Wallet => AccountNatureEnum.Asset,
                AccountTypeEnum.Customer => AccountNatureEnum.Asset,
                AccountTypeEnum.RECEIVABLE => AccountNatureEnum.Asset,

                // PAYABLE waa Liability (Deynta lagugu leeyahay)
                AccountTypeEnum.PAYABLE => AccountNatureEnum.Liability,
                AccountTypeEnum.Loan => AccountNatureEnum.Liability,

                AccountTypeEnum.Expense => AccountNatureEnum.Expense,
                AccountTypeEnum.Revenue => AccountNatureEnum.Revenue,
                AccountTypeEnum.Capital => AccountNatureEnum.Equity,

                _ => AccountNatureEnum.Asset
            };
        }
    }
}