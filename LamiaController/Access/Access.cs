using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using LamiaController.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace LamiaController.Access {

  public abstract class Access {

    Dictionary<Type, List<string>> patchableProperties = new Dictionary<Type, List<string>>();
    Dictionary<Type, List<string>> excludedPatchableProperties = new Dictionary<Type, List<string>>();
    Dictionary<Type, List<string>> controllerPatchableProperties = new Dictionary<Type, List<string>>();

    public abstract bool AuthGet<T>(PlatformContext context, Account authenticatedAccount, Dictionary<string, object> criteria);
    public abstract bool AuthGetById<T>(PlatformContext context, Account authenticatedAccount, int id, Dictionary<string, object> criteria);
    public abstract bool AuthPost<T>(PlatformContext context, Account authenticatedAccount, object postValue);
    public abstract bool AuthPatch<T>(PlatformContext context, Account authenticatedAccount, object record, object patchValue);
    public abstract bool AuthDelete<T>(PlatformContext context, Account authenticatedAccount, object record);

    protected void SetPatchableProperties<T>(params string[] properties) {
      if (!patchableProperties.ContainsKey(typeof(T))) {
        patchableProperties.Add(typeof(T), new List<string>());
      }

      patchableProperties[typeof(T)].Clear();

      foreach (string property in properties) {
        patchableProperties[typeof(T)].Add(property);
      }
    }

    public void SetPatchablePropertiesFromController<T>(params string[] properties) {
      if (!controllerPatchableProperties.ContainsKey(typeof(T))) {
        controllerPatchableProperties.Add(typeof(T), new List<string>());
      }

      controllerPatchableProperties[typeof(T)].Clear();

      foreach (string property in properties) {
        controllerPatchableProperties[typeof(T)].Add(property);
      }
    }

    protected void ExcludePatchableProperties<T>(params string[] properties) {
      if (!excludedPatchableProperties.ContainsKey(typeof(T))) {
        excludedPatchableProperties.Add(typeof(T), new List<string>());
      }

      excludedPatchableProperties[typeof(T)].Clear();

      foreach (string property in properties) {
        excludedPatchableProperties[typeof(T)].Add(property);
      }
    }

    public List<string> GetPatchableProperties<T>() {
      if (patchableProperties.ContainsKey(typeof(T))) {
        return patchableProperties[typeof(T)];
      }
      return new List<string>();
    }

    public List<string> GetPatchablePropertiesFromController<T>() {
      if (controllerPatchableProperties.ContainsKey(typeof(T))) {
        return controllerPatchableProperties[typeof(T)];
      }
      return new List<string>();
    }

    public List<string> GetExcludedPatchableProperties<T>() {
      if (excludedPatchableProperties.ContainsKey(typeof(T))) {
        return excludedPatchableProperties[typeof(T)];
      }
      return new List<string>();
    }

    public Account Authenticate(HttpRequest request, PlatformContext context) {
      StringValues value;

      if (request.Headers.TryGetValue("Authorization", out value)) {
        int startIndex = (((string)value).Substring(0, 5).ToLower() == "hmac ") ? 5 : 0;
        startIndex = (startIndex == 0 && ((string)value).Substring(0, 12).ToLower() == "bearer hmac ") ? 12 : startIndex;

        if (startIndex > 0) {
          string[] values = ((string)value).Substring(startIndex).Split(':');
          if (values.Count() == 4) {
            long timestampNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long timestamp;
            long.TryParse(values[3], out timestamp);
            if ((timestamp - 3) > timestampNow || timestampNow - timestamp >= 10) {
              //Console.WriteLine("INVALID TIMESTAMP");
              //return null;
            }
            string[] apiIdParts = values[0].Split('+');
            string apiId = apiIdParts[0];
              
            Account authMember = context.accounts.SingleOrDefault(u => u.apiId == apiId && u.status == "active");
            if (authMember != null) {
              var reader = new StreamReader(request.Body);
              var body = reader.ReadToEnd();
              reader.Dispose();
              Console.WriteLine(request.Path);
              string hash = CreateHash(authMember.apiId, authMember.apiKey, request.Method, values[2], values[3], request.Path, body);
              if (hash == values[1]) {
                return authMember;
              } else {
                Console.WriteLine("INVALID HASH");
                return authMember;
              }
            } else {
              Console.WriteLine("USER NOT FOUND");
              return null;
            }
          } else {
            Console.WriteLine("WRONG PARAMETER COUNT");
            return null;
          }
        } else {
          Console.WriteLine("INVALID ALGORITHM");
          return null;
        }
      } else {
        Console.WriteLine("NO AUTH HEADER");
      }
      return null;
    }

    public static string CreateHash(string appid, string apikey, string method, string nonce, string timestamp, string url, string json_body) {
      var base64JSON = "";
      if (json_body.Length > 0) {
        var plainTextBytes = Encoding.UTF8.GetBytes(json_body);
        base64JSON = Convert.ToBase64String(plainTextBytes);
      }

      ASCIIEncoding encoding = new ASCIIEncoding();
      Byte[] textBytes = encoding.GetBytes(appid + method + url + timestamp + nonce);// + base64JSON);
      Byte[] keyBytes = encoding.GetBytes(apikey);

      HMACSHA256 hash = new HMACSHA256(keyBytes);
      Byte[] hashBytes = hash.ComputeHash(textBytes);
      hash.Dispose();

      return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }

    public static string GenerateApiToken(Account member) {
      long milliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();
      return GetSha1Hash(milliseconds.ToString() + member.email + DateTimeOffset.UtcNow.Ticks.ToString());
    }

    public static string GetSha1Hash(string input) {
      ASCIIEncoding encoding = new ASCIIEncoding();
      Byte[] textBytes = encoding.GetBytes(input);
      var sha1 = SHA1.Create();
      var output = BitConverter.ToString(sha1.ComputeHash(textBytes)).Replace("-", "").ToLower();
      sha1.Dispose();
      return output;
    }


  }
}
