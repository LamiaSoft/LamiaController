using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LamiaController {
  public class ValidationError {
    
    public string Field { get; }

    public string Message { get; }

    public ValidationError(string field, string message) {
      Field = field != string.Empty ? field : null;
      Message = message;
    }
  }

  public static class ApiResponse {
    
    public static Dictionary<string, object> response<T>(string responseCode, string message, T record) {
      var responseObject = new Dictionary<string, object>();

      if (responseCode != null)
        responseObject.Add("responseCode", responseCode);
      if (message != null) {
        responseObject.Add("message", message);
      }
      responseObject.Add("data", new List<T>() { record });

      return responseObject;
    }

    public static Dictionary<string, object> response<T>(string responseCode, string message, List<T> records, Dictionary<string, int> pagination) {
      var responseObject = new Dictionary<string, object>();

      if (responseCode != null)
        responseObject.Add("responseCode", responseCode);
      if (message != null) {
        responseObject.Add("message", message);
      }
      if (records != null) {
        responseObject.Add("data", records);
      }
      if (pagination != null && pagination.Count > 0) {
        responseObject.Add("pagination", pagination);
      }

      return responseObject;
    }

    public static Dictionary<string, object> response(string responseCode, string message, Dictionary<string, object> data, string propertyName = null) {
      var responseObject = new Dictionary<string, object>();

      if (responseCode != null)
        responseObject.Add("responseCode", responseCode);
      if (message != null) {
        responseObject.Add("message", message);
      }
      if (data != null && data.Count > 0) {
        if (propertyName == null) {
          foreach (var property in data) {
            responseObject.Add(property.Key, property.Value);
          }
        } else {
          responseObject.Add(propertyName, data);
        }
      }
      return responseObject;
    }

    public static Dictionary<string, object> response<T>(string responseCode, string message, List<T> data, string propertyName) {
      var responseObject = new Dictionary<string, object>();

      if (responseCode != null)
        responseObject.Add("responseCode", responseCode);
      if (message != null) {
        responseObject.Add("message", message);
      }
      if (data != null) {
        responseObject.Add(propertyName, data);
      }
      return responseObject;
    }
       
    public static Dictionary<string, object> response(string responseCode, ModelStateDictionary modelState, string propertyName) {
      var responseObject = new Dictionary<string, object>();
      var errors = new Dictionary<string, List<Dictionary<string, string>>>();

      foreach (var pair in modelState) {
        if (pair.Key != "id") {
          if (pair.Value.Errors.Count > 0) { 
            List<Dictionary<string, string>> currentError = new List<Dictionary<string, string>>();

            foreach (ModelError e in pair.Value.Errors) {
              string pattern = @"^\[(.*?)\] ";
              Match m = Regex.Match(e.ErrorMessage, pattern);
              if (m.Success) {
                string message = Regex.Replace(e.ErrorMessage, pattern, "");
                currentError.Add(new Dictionary<string, string> { { "code", m.Value.Trim(new Char[] {' ', '[', ']' }) }, { "message", message } });
              } else {
                if (e.ErrorMessage.Length == 0) {
                  currentError.Add(new Dictionary<string, string> { { "code", "ERR.INVALID_VALUE" }, { "message", "Invalid value provided" } });
                } else {
                  currentError.Add(new Dictionary<string, string> { { "code", "XX" }, { "message", e.ErrorMessage } });
                }
              }
            }

            errors.Add(pair.Key, currentError);
          }
        }
      }

      if (responseCode != null)
        responseObject.Add("responseCode", responseCode);
      responseObject.Add(propertyName, errors);

      return responseObject;
    }
       
    public static Dictionary<string, object> response(string responseCode, ICollection<ValidationResult> validationResults, string propertyName) {
      var responseObject = new Dictionary<string, object>();
      var errors = new Dictionary<string, List<Dictionary<string, string>>>();

      foreach (var pair in validationResults) {
        var key = pair.MemberNames.FirstOrDefault();
        var value = pair.ErrorMessage;

        Dictionary<string, string> currentError = new Dictionary<string, string>();

        string pattern = @"^\[(.*?)\] ";
        string code = "XX";
        string message = value;
        Match m = Regex.Match(value, pattern);
        if (m.Success) {
          message = Regex.Replace(value, pattern, "");
          Console.WriteLine("EEE: " + message);
          code = m.Value.Trim(new Char[] { ' ', '[', ']' });
        }

        if (!errors.ContainsKey(key)) {
          errors.Add(key, new List<Dictionary<string, string>>());
        }
        errors[key].Add(currentError);
      }

      if (responseCode != null)
        responseObject.Add("responseCode", responseCode);
      responseObject.Add(propertyName, errors);

      return responseObject;
    }
  }
}
