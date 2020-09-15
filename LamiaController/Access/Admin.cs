using System.Collections.Generic;
using LamiaController.Models;

namespace LamiaController.Access {

  public class Admin : Access {

    public override bool AuthDelete<T>(PlatformContext context, Account authenticatedAccount, object record) {
      return true;
    }

    public override bool AuthGet<T>(PlatformContext context, Account authenticatedAccount, Dictionary<string, object> criteria) {
      return true;
    }

    public override bool AuthGetById<T>(PlatformContext context, Account authenticatedAccount, int id, Dictionary<string, object> criteria) {
      return true;
    }

    public override bool AuthPatch<T>(PlatformContext context, Account authenticatedAccount, object record, object patchValue) {
      return true;
    }

    public override bool AuthPost<T>(PlatformContext context, Account authenticatedAccount, object postValue) {
      return true;
    }

  }

}

