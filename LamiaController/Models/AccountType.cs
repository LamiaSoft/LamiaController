using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using FluentValidation;

namespace LamiaController.Models {

  [Table("account_type")]
  public class AccountType : Model {

    public int? accountTypeId { get; set; }
    public string name { get; set; }

    public override bool Delete(PlatformContext context, bool save) {
      throw new NotImplementedException();
    }

    public override IQueryable Get(PlatformContext context, Dictionary<string, object> criteria) {
      throw new NotImplementedException();
    }

    public override IQueryable GetById(PlatformContext context, int id, Dictionary<string, object> criteria) {
      throw new NotImplementedException();
    }

    public override void Patch(PlatformContext context, object patchedRecord) {
      throw new NotImplementedException();
    }

    public override void Post(PlatformContext context) {
      throw new NotImplementedException();
    }
  }
}
