
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LamiaController.Models {
  public abstract class Model {

    public abstract IQueryable Get(PlatformContext context, Dictionary<string, object> criteria);
    public abstract IQueryable GetById(PlatformContext context, int id, Dictionary<string, object> criteria);
    public abstract void Post(PlatformContext context);

    protected virtual void FilterByStatus<T>(ref IQueryable<T> queryable, Dictionary<string, object> criteria) {
      IQueryable<ModelWithStatus> test = queryable as IQueryable<ModelWithStatus>;
      if (test != null) {
        if ((!criteria.ContainsKey("fromDate") || criteria["fromDate"] == null) &&
            (!criteria.ContainsKey("toDate") || criteria["toDate"] == null)) {
          if (!criteria.ContainsKey("status") || criteria["status"] == null) {
            queryable = queryable.Where(c => ((ModelWithStatus)c).status != "deleted");
          } else if ((string)criteria["status"] == "active" || (string)criteria["status"] == "inactive" || (string)criteria["status"] == "deleted") {
            queryable = queryable.Where(c => ((ModelWithStatus)c).status == (string)criteria["status"]);
          }
        }

        if (criteria.ContainsKey("fromDate") && criteria["fromDate"] != null) {
          DateTime fromDate = Convert.ToDateTime(criteria["fromDate"]);
          queryable = queryable.Where(c => ((ModelWithStatus)c).creationDate >= fromDate || ((ModelWithStatus)c).deletionDate == null || ((ModelWithStatus)c).deletionDate >= fromDate);
        }
        if (criteria.ContainsKey("toDate") && criteria["toDate"] != null) {
          DateTime toDate = Convert.ToDateTime(criteria["toDate"]);
          queryable = queryable.Where(c => ((ModelWithStatus)c).creationDate < toDate.AddDays(1));
        }
      }
    }

    public virtual bool Delete(PlatformContext context, bool save = true) {
      ModelWithStatus model = this as ModelWithStatus;
      if (model != null) {
        if (model.status != "deleted") {
          model.status = "deleted";
          model.deletionDate = DateTime.UtcNow;
        }
      }
      if (save) {
        context.Update(this);
        context.SaveChanges();
      }
      return true;
    }

    public virtual void Patch(PlatformContext context, object patchedRecord) {
      if (PatchRecord(patchedRecord)) {
        context.Update(this);
        context.SaveChanges();
      }
    }

    public virtual void Configuration(ISharedResource localizer) { }

    protected virtual bool PatchRecord(object patchedRecord) {
      foreach (PropertyInfo property in patchedRecord.GetType().GetProperties()) {
        var value = property.GetValue(patchedRecord, null);
        var orig = property.GetValue(this, null);

        if (value != null) {
          property.SetValue(this, value);
          Console.WriteLine("property " + property.Name + " patch: " + value.ToString());
        } else {
          Console.WriteLine("property " + property.Name + " patch: null");
        }

        if (orig != null) {
          Console.WriteLine("property " + property.Name + " orig: " + orig.ToString());
        } else {
          Console.WriteLine("property " + property.Name + " orig: null");
        }

      }

      return true;
    }
  }
}
