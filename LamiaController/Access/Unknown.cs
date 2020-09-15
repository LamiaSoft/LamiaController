using System.Collections.Generic;
using LamiaController.Controllers;
using LamiaController.Models;

namespace LamiaController.Access {

  public class Unknown : Access {
    
    public override bool AuthDelete<T>(PlatformContext context, Account authenticatedAccount, object record) {
      return false;
    }

    public override bool AuthGet<T>(PlatformContext context, Account authenticatedAccount, Dictionary<string, object> criteria) {
      return false;
    }

    public override bool AuthGetById<T>(PlatformContext context, Account authenticatedAccount, int id, Dictionary<string, object> criteria) {
      if (typeof(T) == typeof(Account)) {
        if (authenticatedAccount.accountId == id)
          return true;
      }
      return false;
    }

    public override bool AuthPatch<T>(PlatformContext context, Account authenticatedAccount, object record, object patchValue) {
      if (typeof(T) == typeof(Account)) {
        if (authenticatedAccount.accountId == ((Account)record).accountId)
          return true;
      }
      return false;
    }

    public override bool AuthPost<T>(PlatformContext context, Account authenticatedAccount, object postValue) {
      return false;
    }
  }
}
