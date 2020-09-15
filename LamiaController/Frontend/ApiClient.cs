using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using LamiaController.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;


namespace LamiaController.Frontend {
  public class ApiClient {
    protected string baseUrl = "http://localhost:63310";
    string apiId = "1230f75280";
    string apiKey = "15040aa09d9176859f62335e6699bb67049a2104";
    string nonce;
    long timestamp;


    public async Task<Response<T>> Get<T>(string endpoint, string apiVersion, string query = "", Controller controller = null, ISession sessionCredentials = null) {
      return await Retrieve<T>("GET", endpoint, apiVersion, query, "", null, controller, sessionCredentials);
    }

    public async Task<Response<T>> Post<T>(string endpoint, string apiVersion, string query = "", string jsonBody = "", Controller controller = null, ISession sessionCredentials = null) {
      return await Retrieve<T>("POST", endpoint, apiVersion, query, jsonBody, null, controller, sessionCredentials);
    }

    public async Task<Response<T>> Patch<T>(string endpoint, string apiVersion, string query = "", string jsonBody = "", Controller controller = null, ISession sessionCredentials = null) {
      return await Retrieve<T>("PATCH", endpoint, apiVersion, query, jsonBody, null, controller, sessionCredentials);
    }

    public async Task<Response<T>> Delete<T>(string endpoint, string apiVersion, string query = "", Controller controller = null, ISession sessionCredentials = null) {
      return await Retrieve<T>("DELETE", endpoint, apiVersion, query, "", null, controller, sessionCredentials);
    }

    public async Task<Response<Account>> Authenticate(string email, string password, string apiVersion = "1.0") {
      return await Retrieve<Account>("GET", "account/authentication", apiVersion, "email=" + email, "", password, null);
    }

    protected async Task<Response<T>> Retrieve<T>(string method, string endpoint, string apiVersion, string query = "", string jsonBody = "", string authPassword = null, Controller controller = null, ISession sessionCredentials = null) {
      Response<T> response;
      string requestApiId;
      string requestApiKey = apiKey;

      if (endpoint.Substring(0, 1) != "/") {
        endpoint = "/" + endpoint;
      }

      if (sessionCredentials == null) {
        requestApiId = (endpoint != "/account/authentication") ? apiId : apiId + "+" + Access.Access.GetSha1Hash(authPassword);
      } else {
        requestApiId = sessionCredentials.GetString("Account.apiId");
        requestApiKey = sessionCredentials.GetString("Account.apiKey");
      }

      timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
      nonce = Access.Access.GetSha1Hash(timestamp.ToString() + endpoint);

      using (var client = new HttpClient()) {
        UriBuilder builder = new UriBuilder(baseUrl + endpoint);
        builder.Query = query;

        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
          
        string hash = Access.Access.CreateHash(requestApiId, requestApiKey, method, nonce, timestamp.ToString(), endpoint, jsonBody);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("hmac", string.Format("{0}:{1}:{2}:{3}", requestApiId, hash, nonce, timestamp));
        client.DefaultRequestHeaders.Add("api-version", apiVersion);

        Console.WriteLine(method + ": " + endpoint);
        //Console.WriteLine(requestApiId + " , " + authPassword);
        Console.WriteLine(jsonBody);


        HttpResponseMessage Res;

        switch (method) {
          case "GET":
            Res = await client.GetAsync(builder.Uri);
            break;
          case "POST":
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            Res = await client.PostAsync(builder.Uri, content);
            break;
          case "PATCH":
            var patchMethod = new HttpMethod("PATCH");
            var patchContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(patchMethod, builder.Uri) { Content = patchContent };
            Res = await client.SendAsync(request);
            break;
          case "DELETE":
            Res = await client.DeleteAsync(builder.Uri);
            break;
          default:
            throw new Exception("Api Client: " + "Method " + method + " not supported");
        }

        response = new Response<T>();
        var result = Res.Content.ReadAsStringAsync().Result;
        Console.WriteLine(result);
        T record;
        string errorText = "API_UNKNOWN";

        switch ((int)Res.StatusCode) {
          case 200:
            var jsonDynDict = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(result);
            if (jsonDynDict.ContainsKey("data")) {
              response = JsonConvert.DeserializeObject<Response<T>>(result);
            } else {
              record = JsonConvert.DeserializeObject<T>(result);
              response.data.Add(record);
            }
            break;
          case 201:
            record = JsonConvert.DeserializeObject<T>(result);
            response.data.Add(record);
            break;
          case 400:
            errorText = "API_BAD_REQUEST";
            Dictionary<string, object> jsonDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(result);
            if (jsonDict.ContainsKey("errors")) {
              var errorDict = JsonConvert.DeserializeObject<Response<T>>(result);
              response.errors = errorDict.errors;
              if (controller != null) {
                controller.ViewBag.validation = response.errors;
              }
            } 
            break;
          case 404:
            errorText = "API_NOT_FOUND";
            break;
          case 401:
          case 403:
            errorText = "API_FORBIDDEN";
            break;
        }
        response.httpCode = (int)Res.StatusCode;

        if (response.httpCode != 200 && controller != null) {
          controller.ViewBag.APIError = errorText;
          controller.ViewBag.APIStatusCode = response.httpCode;
        }
      }

      return response;
    }

  }
}
