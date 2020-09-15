using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using LamiaController.Models;
using LamiaController.Access;
using Microsoft.Extensions.Configuration;
using System;

namespace LamiaController.Controllers {

  [ApiVersion("1.0")]
  [Route("[controller]")]
  [ApiController]
  public class AccountController : AppController {
       
    public AccountController(IConfiguration configuration) : base(configuration) { }

    [HttpGet("authentication")]
    public IActionResult Authentication(string email) {
      //if (!(GetAccessLevel() is Service))
        //return StatusCode(403);

      StringValues value;
      Request.Headers.TryGetValue("Authorization", out value);
      int startIndex = (((string)value).Substring(0, 5).ToLower() == "hmac ") ? 5 : 12;

      string[] values = ((string)value).Substring(startIndex).Split(':');
      string[] apiIdParts = values[0].Split('+');

      // Prepare error object
      Dictionary<string, List<Dictionary<string, string>>> returnValue = new Dictionary<string, List<Dictionary<string, string>>>();
      returnValue.Add("email", new List<Dictionary<string, string>>());
      returnValue["email"].Add(new Dictionary<string, string>() { { "code", "ERR.WRONG_CREDENTIALS" }, { "message", "Wrong credentials" } });

      if (apiIdParts.Count() > 1) {
        if (email == null) {
          returnValue["email"][0]["code"] = "ERR.NO_EMAIL";
          returnValue["email"][0]["message"] = "You did not enter an e-mail address";
          return BadRequest(new Dictionary<string, object>() { { "errors", returnValue } });
        }
        string password = string.Join("+", apiIdParts.Skip(1));

        IQueryable<Account> authMember = GetPlatformContext().accounts.Where(u => u.email == email && u.status == "active");
        if (authMember.Count() == 1) {
          if (BCrypt.Net.BCrypt.Verify(password, authMember.SingleOrDefault().password)) {
            return ResultOne("200", null, authMember);//, AccountDto.AsDto);
          } else {
            return BadRequest(new Dictionary<string, object>() { { "errors", returnValue } });
          }
        } else {
          return BadRequest(new Dictionary<string, object>() { { "errors", returnValue } });
        }
      }
      returnValue["email"][0]["code"] = "ERR.INVALID_REQUEST";
      returnValue["email"][0]["message"] = "Invalid authentication request";
      return BadRequest(new Dictionary<string, object>() { { "errors", returnValue } });
    }

    [HttpGet]
    public IActionResult Get() {
      IActionResult result;
      IQueryable<Account> records = GetModel<Account>(out result);
      if (result != null)
        return result;

      return ResultMany("200", null, records, AccountDto.AsDto, true, true); 
    }

    [HttpGet("{id:int}", Name = "GetAccount")]
    public IActionResult Get(int id) {
      IActionResult result;
      IQueryable<Account> record = GetModelById<Account>(id, out result);
      if (result != null)
        return result;

      return ResultOne("200", null, record, AccountDto.AsDto);
    }

    protected override void SetAccessLevel(int accountId, int accountTypeId, ref Access.Access accessLevel) { }
  }
}
