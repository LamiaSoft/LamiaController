using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using FluentValidation;

namespace LamiaController.Models {

  [Table("account")]
  public class Account : Model {

    public int? accountId { get; set; }
    public string email { get; set; }
    public string password { get; set; }
    public string status { get; set; }
    public int? accountTypeId { get; set; }
    public string apiId { get; set; }
    public string apiKey { get; set; }

    public AccountType AccountType { get; set; }

   

    public override IQueryable Get(PlatformContext context, Dictionary<string, object> criteria) {
      return context.accounts;
    }

    public override IQueryable GetById(PlatformContext context, int id, Dictionary<string, object> criteria) {
      return context.accounts.Where(a => a.accountId == id);
    }

    public override void Post(PlatformContext context) {
      accountId = null;
      status = "active";
      apiKey = Access.Access.GenerateApiToken(this);
      apiId = Access.Access.GetSha1Hash(password + apiKey).Substring(0, 15);
                    
      string pw = password;
      password = BCrypt.Net.BCrypt.HashPassword(Access.Access.GetSha1Hash(pw));
        
      context.accounts.Add(this);
      context.SaveChanges();

      apiId = apiId.Substring(0, 15 - accountId.ToString().Length) + accountId;
        
      context.Update(this);
      context.SaveChanges();
    }

    public override void Patch(PlatformContext context, object patchedRecord) {
      PatchRecord(patchedRecord);
      context.accounts.Update(this);
      context.SaveChanges();
    }

    public override bool Delete(PlatformContext context, bool save) {
      throw new NotImplementedException();
    }
  }

  public class AccountValidator : AbstractValidator<Account> {
    public AccountValidator() {
      RuleSet("Creation", () => {
        RuleFor(a => a.email).NotEmpty().EmailAddress().Must(EmailNotExists).WithMessage("E-mail address already exists.");
        RuleFor(a => a.password).NotEmpty().MinimumLength(8);
      });
    }

    protected bool EmailNotExists(string email) {
      using (PlatformContext context = new PlatformContext(PlatformContext.GetConnectionString())) {
        if (context.accounts.FirstOrDefault(a => a.email == email) == null) {
          return true;
        }
      }
      return false;
    }
  }


  public class AccountDto {
    public int? accountId { get; set; }
    public string email { get; set; }
    public string status { get; set; }
    public int? accountTypeId { get; set; }
    public DateTime? creationDate { get; set; }

    public static readonly Expression<Func<Account, AccountDto>> AsDto =
     x => new AccountDto {
       accountId = x.accountId,
       email = x.email,
       status = x.status,
       accountTypeId = x.accountTypeId
     };

    public static AccountDto ToDto(Account x) {
      return new AccountDto {
        accountId = x.accountId,
        email = x.email,
        status = x.status,
        accountTypeId = x.accountTypeId
      };
    }


  }
}
