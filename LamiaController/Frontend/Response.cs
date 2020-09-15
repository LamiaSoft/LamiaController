using System;
using System.Collections.Generic;

namespace LamiaController.Frontend {
  
  public class Response<T> {
    public int httpCode;
    public string responseCode = "";
    public List<T> data = new List<T>();
    public Dictionary<string, object> pagination = new Dictionary<string, object>();
    public Dictionary<string, List<Dictionary<string, string>>> errors = new Dictionary<string, List<Dictionary<string, string>>>();
  }
}
