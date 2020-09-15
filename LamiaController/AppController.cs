using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Reflection;
using FluentValidation.Results;
using LamiaController.Access;
using LamiaController.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;

namespace LamiaController {

  public abstract class AppController : Controller {
    PlatformContext _context;
    Account authenticatedAccount;
    Access.Access accessLevel;
    Dictionary<string, int> pagination { get; set; } = new Dictionary<string, int>();


    protected Dictionary<string, object> criteria = new Dictionary<string, object>();
    protected int defaultItemsPerPage { get; set; } = 10;
    protected int maxItemsPerPage { get; set; } = 50;
    protected string defaultSortBy { get; set; } = null;

    public AppController(IConfiguration configuration) {
      if (configuration != null)
        SetDbContext(new PlatformContext(configuration.GetSection("Connection").Value));
    }

    protected void SetDbContext(PlatformContext context) {
      _context = context;
    }

    protected abstract void SetAccessLevel(int accountId, int accountTypeId, ref Access.Access accessLevel);

    protected Account GetAuthenticatedAccount() {
      return authenticatedAccount;
    }

    protected PlatformContext GetPlatformContext() {
      return _context;
    }

    protected Access.Access GetAccessLevel() {
      if (accessLevel == null) {
        accessLevel = new Unauthorized();
        authenticatedAccount = accessLevel.Authenticate(Request, _context);
        if (authenticatedAccount != null) {
          _context.Entry(authenticatedAccount).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
          SetDefaultAccessLevel((int)authenticatedAccount.accountTypeId);
          SetAccessLevel((int)authenticatedAccount.accountId, (int)authenticatedAccount.accountTypeId, ref accessLevel);
        } 
      }
      Console.WriteLine("GET ACCESS LEVEL: " + accessLevel.GetType().Name);
      return accessLevel;
    }

    protected virtual void SetDefaultAccessLevel(int accountTypeId) {
      switch (accountTypeId) {
        case 1:
          accessLevel = new Admin();
          break;
        case 2:
          accessLevel = new Service();
          break;
        default:
          accessLevel = new Unknown();
          break;
      }
    }

    private IActionResult Result<T, T2>(bool many, string responseCode, string message, IQueryable<T> records, Expression<Func<T, T2>> Dto, bool paginate = false, bool sort = false) {
      List<T> resources = new List<T>();
      IQueryable<T2> recordsDto = null;

      if (records != null) {
        if (Dto != null) {
          recordsDto = records.Select(Dto);
          if (sort) {
            recordsDto = GetSortedResults(recordsDto);
          }
          if (paginate) {
            recordsDto = GetPaginatedResults(recordsDto);
          }
        } else {
          if (sort) {
            records = GetSortedResults(records);
          }
          if (paginate) {
            records = GetPaginatedResults(records);
          }
        }

        if (Dto != null) {
          List<T2> resourcesDto;
          resourcesDto = recordsDto.ToList();
          if (resourcesDto.Count() > 0) {
            return Ok(ApiResponse.response<T2>(responseCode, message, resourcesDto, this.pagination));
          }
        } else {
          resources = records.ToList();
          if (resources.Count() > 0) {
            return Ok(ApiResponse.response<T>(responseCode, message, resources, this.pagination));
          }
        }
      }

      if (many) {
        return Ok(ApiResponse.response<T>(responseCode, message, resources, this.pagination));
      }

      return NotFound(ApiResponse.response("404", "Resource not found", null));
    }

    public IActionResult ResultOne<T>(string responseCode, string message, IQueryable<T> records, bool pagination = false, bool sort = false) {
      return Result<T, T>(false, responseCode, message, records, null, pagination, sort);
    }

    public IActionResult ResultOne<T, T2>(string responseCode, string message, IQueryable<T> records, Expression<Func<T, T2>> Dto, bool pagination = false, bool sort = false) {
      return Result<T, T2>(false, responseCode, message, records, Dto, pagination, sort);
    }

    public IActionResult ResultMany<T>(string responseCode, string message, IQueryable<T> records, bool pagination = false, bool sort = false) {
      return Result<T, T>(true, responseCode, message, records, null, pagination, sort);
    }

    public IActionResult ResultMany<T, T2>(string responseCode, string message, IQueryable<T> records, Expression<Func<T, T2>> Dto, bool pagination = false, bool sort = false) {
      return Result<T, T2>(true, responseCode, message, records, Dto, pagination, sort);
    }

    private IQueryable<T> GetPaginatedResults<T>(IQueryable<T> records) {
      pagination.Clear();

      Microsoft.Extensions.Primitives.StringValues page, itemsPerPage;
      Request.Query.TryGetValue("page", out page);
      Request.Query.TryGetValue("itemsPerPage", out itemsPerPage);

      int currentPage = 1, currentPerPage = defaultItemsPerPage;
      int numberOfItems = records.Count();

      if (page.Count > 0) {
        Int32.TryParse(page.Single(), out currentPage);
        if (currentPage < 1) {
          currentPage = 1;
        }
      }
      if (itemsPerPage.Count > 0) {
        Int32.TryParse(itemsPerPage.Single(), out currentPerPage);
        if (currentPerPage == 0 || currentPerPage > maxItemsPerPage) {
          currentPerPage = defaultItemsPerPage;
        }
      }

      pagination.Add("page", currentPage);
      pagination.Add("numberOfItems", numberOfItems);
      pagination.Add("itemsPerPage", currentPerPage);
      pagination.Add("numberOfPages", NumberOfPages(numberOfItems, currentPerPage));

      return Paginate<T>(records, currentPage, currentPerPage);
    }

    private IQueryable<T> GetSortedResults<T>(IQueryable<T> records) {
      Microsoft.Extensions.Primitives.StringValues sort;
      Request.Query.TryGetValue("sort", out sort);
      Console.WriteLine(sort);
      if (sort.Count > 0) {
        return SortBy<T>(records, sort);
      }
      return records;
    }

    private IQueryable<T> Paginate<T>(IQueryable<T> records, int page, int itemsPerPage) {
      if (itemsPerPage == -1) {
        return records;
      }
      page--;
      return records.Skip(itemsPerPage * page).Take(itemsPerPage);
    }

    private int NumberOfPages(int numberOfItems, int itemsPerPage) {
      if (itemsPerPage == -1) {
        return 1;
      }
      return (int)Math.Ceiling((double)numberOfItems / itemsPerPage);
    }

    private IQueryable<T> SortBy<T>(IQueryable<T> records, string sortBy) {
      try {
        return records.OrderBy(sortBy);
      } catch (Exception e) {
        Console.WriteLine(e.Message);
        return records;
      }
    }

    protected IQueryable<T> GetModel<T>(out IActionResult actionResult) {
      if (GetAccessLevel() is Unauthorized) {
        actionResult = StatusCode(401);
        return null;
      }

      actionResult = null;
      if (criteria == null)
        criteria = new Dictionary<string, object>();
      
      MethodInfo method = accessLevel.GetType().GetMethod("AuthGet");
      MethodInfo generic = method.MakeGenericMethod(typeof(T));
      bool result = (bool)generic.Invoke(accessLevel, new object[] { _context, authenticatedAccount, criteria });
      if (result) {
        if (typeof(T).IsSubclassOf(typeof(Model))) {
          return (IQueryable<T>)((Model)(object)Activator.CreateInstance<T>()).Get(_context, criteria);
        }
        throw new Exception(typeof(T).Name + " is not a Model.");
      }
      actionResult = StatusCode(403);
      return null;
    }

    public IQueryable<T> GetModelById<T>(int id, out IActionResult actionResult) {
      if (GetAccessLevel() is Unauthorized) {
        actionResult = StatusCode(401);
        return null;
      }

      actionResult = null;
      if (criteria == null)
        criteria = new Dictionary<string, object>();

      MethodInfo method = accessLevel.GetType().GetMethod("AuthGetById");
      MethodInfo generic = method.MakeGenericMethod(typeof(T));
      bool result = (bool)generic.Invoke(accessLevel, new object[] { _context, authenticatedAccount, id, criteria });
      if (result) {
        if (typeof(T).IsSubclassOf(typeof(Model))) {
          return (IQueryable<T>)((Model)(object)Activator.CreateInstance<T>()).GetById(_context, id, criteria);
        }
        throw new Exception(typeof(T).Name + " is not a Model.");
      }
      actionResult = StatusCode(403);
      return null;
    }

    public void PostModel<T>(T postValue, out IActionResult actionResult) {
      if (GetAccessLevel() is Unauthorized) {
        actionResult = StatusCode(401);
        return;
      }

      actionResult = null;

      if (postValue == null) {
        Console.WriteLine("IS NULL");
        actionResult = BadRequest(ApiResponse.response(null, ModelState, "errors"));
        return;
      }
     
      MethodInfo method = accessLevel.GetType().GetMethod("AuthPost");
      MethodInfo generic = method.MakeGenericMethod(typeof(T));
      bool result = (bool)generic.Invoke(accessLevel, new object[] { _context, authenticatedAccount, postValue });

      if (result) {
        if (typeof(T).IsSubclassOf(typeof(Model))) {
          if (!ModelState.IsValid) {
            Console.WriteLine("IS NOT VALID");
            actionResult = BadRequest(ApiResponse.response(null, ModelState, "errors"));
            return;
          }
          ((Model)(object)postValue).Post(_context);
          return;
        }
        throw new Exception(typeof(T).Name + " is not a Model.");
      }
      actionResult = StatusCode(403);
    }

    private bool CheckPatchedProperties<T>(object patchValue, T fullRecord) {
      List<string> allowedProperties = accessLevel.GetPatchableProperties<T>();
      List<string> excludedProperties = accessLevel.GetExcludedPatchableProperties<T>();
      List<string> allowedPropertiesFromController = accessLevel.GetPatchablePropertiesFromController<T>();

      foreach (PropertyInfo property in patchValue.GetType().GetProperties()) {
        var value = property.GetValue(patchValue, null);
        var orig = property.GetValue(fullRecord, null);

        if (value != null && (orig == null || value.ToString() != orig.ToString())) {
          if (excludedProperties.Contains(property.Name) && !(accessLevel is Admin)) {
            return false;
          }
          if (allowedPropertiesFromController.Count > 0 && allowedPropertiesFromController.Count > 0 && !allowedPropertiesFromController.Contains(property.Name)) {
            Console.WriteLine("OEPS1! " + property.Name + ": " + value);
            return false;
          }
          if (excludedProperties.Count == 0 && !allowedProperties.Contains(property.Name) && !(accessLevel is Admin)) {
            Console.WriteLine("OEPS2! " + property.Name + " orig: " + orig);
            Console.WriteLine("OEPS2! " + property.Name + ": " + value);
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(orig.GetType()));
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(value.GetType()));
            return false;
          }
        } 
      }

      return true;
    }

    public void PatchModel<T>(int id, T patchValue, out IActionResult actionResult, params string[] patachableProperties) {
      if (GetAccessLevel() is Unauthorized) {
        actionResult = StatusCode(401);
        return;
      }
     
      accessLevel.SetPatchablePropertiesFromController<T>(patachableProperties);
      actionResult = null;

      T record = ((Model)(object)Activator.CreateInstance<T>()).GetById(_context, id, criteria).FirstOrDefault();
      if (record == null) {
        if (!ModelState.IsValid) {
          actionResult = BadRequest(ApiResponse.response(null, ModelState, "errors"));
        } else {
          actionResult = NotFound();
        }
        return;
      }

      if (patchValue == null) {
        actionResult = BadRequest(ApiResponse.response(null, ModelState, "errors"));
        return;
      }

      if (!CheckPatchedProperties<T>(patchValue, record)) {
        actionResult = StatusCode(403);
        return;
      }

      if (!CustomPatchValidation<T>(record, patchValue, out actionResult)) {
        return;
      }

      MethodInfo method = accessLevel.GetType().GetMethod("AuthPatch");
      MethodInfo generic = method.MakeGenericMethod(typeof(T));
      bool result = (bool)generic.Invoke(accessLevel, new object[] { _context, authenticatedAccount, record, patchValue });
      if (result) {
        if (typeof(T).IsSubclassOf(typeof(Model))) {
          if (!ModelState.IsValid) {
            actionResult = BadRequest(ApiResponse.response(null, ModelState, "errors"));
            return;
          }
          ((Model)(object)record).Patch(_context, patchValue);
          return;
        }
        throw new Exception(typeof(T).Name + " is not a Model.");
      }
      actionResult = StatusCode(403);
    }

    public bool DeleteModel<T>(int id, out IActionResult actionResult) {
      if (GetAccessLevel() is Unauthorized) {
        actionResult = StatusCode(401);
        return false;
      }
    
      actionResult = null;

      T record = ((Model)(object)Activator.CreateInstance<T>()).GetById(_context, id, criteria).FirstOrDefault();
      if (record == null) {
        actionResult = NotFound(ApiResponse.response("404", "Resource not found", null));
        return false;
      }

      MethodInfo method = accessLevel.GetType().GetMethod("AuthDelete");
      MethodInfo generic = method.MakeGenericMethod(typeof(T));
      bool result = (bool)generic.Invoke(accessLevel, new object[] { _context, authenticatedAccount, record });
      if (result) {
        if (typeof(T).IsSubclassOf(typeof(Model))) {
          ((Model)(object)record).Delete(_context);
          return true;
        }
        throw new Exception(typeof(T).Name + " is not a Model.");
      }
      actionResult = StatusCode(403);
      return false;
    }

    protected IActionResult Deprecated() {
      Console.WriteLine(Request.Path);
      return BadRequest(ApiResponse.response("400", "This endpoint has been deprecated in this API version.", null));
    }

    protected override void Dispose(bool disposing) {
      //_context.Dispose();
      base.Dispose(disposing);
    }

    protected virtual bool CustomPatchValidation<T>(T record, T patchedValue, out IActionResult result) {
      result = null;
      return true;
    }

  }
}
