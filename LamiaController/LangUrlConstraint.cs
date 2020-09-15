using System;
using System.Collections.Generic;
using System.Globalization;
using System.Resources;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace LamiaController {
  
  public class LangUrlConstraint : IRouteConstraint {
    private ISharedResource _localizer;
    List<string> supportedCultures = new List<string>() { "nl", "en" };

    public LangUrlConstraint(ISharedResource sharedResource) {
      _localizer = sharedResource;
    }

    public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection) {
      if (routeDirection.ToString() == "IncomingRequest") {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo((string)values["lang"]);
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo((string)values["lang"]);
      }
      return true;
    }

  }
}