using System.Collections.Generic;
using LamiaController.Models;

namespace LamiaController.Access {

  public class Unauthorized : Access {
    
    public override bool AuthDelete<T>(PlatformContext context, Account authenticatedAccount, object record) {
      return false;
    }

    public override bool AuthGet<T>(PlatformContext context, Account authenticatedAccount, Dictionary<string, object> criteria) {
      return false;
    }

    public override bool AuthGetById<T>(PlatformContext context, Account authenticatedAccount, int id, Dictionary<string, object> criteria) {
      return false;
    }

    public override bool AuthPatch<T>(PlatformContext context, Account authenticatedAccount, object record, object patchValue) {
      return false;
    }

    public override bool AuthPost<T>(PlatformContext context, Account authenticatedAccount, object postValue) {
      return false;
    }
  }
}
