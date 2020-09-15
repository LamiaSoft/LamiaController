using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace LamiaController {

  public static class Routing {
    static List<string> supportedCultures = new List<string>();


    public static void SetCultures(params string[] cultures) {
      supportedCultures.Clear();
      foreach (string culture in cultures) {
        supportedCultures.Add(culture);
      }
    }

    public static List<string> GetCultures() {
      return supportedCultures;
    }

    public static IRouteBuilder GetRoutes(this IApplicationBuilder app, IRouteBuilder routeBuilder) {
      XDocument config;
      try {
        config = XDocument.Load(File.OpenRead("routing.config"));
      } catch (FileNotFoundException e) {
        throw new FileNotFoundException("Cannot get routes from file \"routing.config\": file does not exist.");  
      }

      var sections = config.Descendants("configuration").Descendants("routes");

      string controllerName, actionName, template;
      List<Route> routes = new List<Route>();

      foreach (var routing in sections) {
        foreach (var controller in routing.Elements()) {
          controllerName = controller.Name.LocalName;
          foreach (var action in controller.Elements()) {
            actionName = action.Name.LocalName;
            template = action.Value;
            routes.Add(new Route(new Default(controllerName, actionName), template, false));
          }
        }
      } 

      ISharedResource _localizer = (ISharedResource)app.ApplicationServices.GetService(typeof(ISharedResource));
      IRouteConstraint constraint = new LangUrlConstraint((ISharedResource)app.ApplicationServices.GetService(typeof(ISharedResource)));

      foreach (string locale in supportedCultures) {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo(locale);
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo(locale);

        foreach (Route route in routes) {
          template = route.template;

          MatchCollection matches = Regex.Matches(template, @"[^\@]?(\@[A-Z_]+)", RegexOptions.IgnoreCase);
          foreach (Match match in matches) {
            template = template.Replace(match.Groups[1].Value, _localizer.GetString(match.Groups[1].Value.Substring(1)));
          }

          routeBuilder.MapRoute(name: route.defaults.controller + "_" + route.defaults.action + "_" + locale,
                                template: template,
                                defaults: new { controller = route.defaults.controller, action = route.defaults.action, lang = locale },
                                constraints: new { template = constraint } );
        }
      }

      Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(routeBuilder.Routes));
      routeBuilder.MapRoute("default",
                            "",
                            new { controller = "Home", action = "Index", lang = "en" },
                            new { controller = constraint });

      return routeBuilder;
    }
  }









  public class Route {
    public Default defaults;
    public string template;
    public bool constraint;

    public Route(Default defaults, string url_tag, bool contraint) {
      this.defaults = defaults;
      this.template = url_tag;
      this.constraint = contraint;
    }
  }

  public class Default {
    public string controller;
    public string action;

    public Default(string controller, string action) {
      this.controller = controller;
      this.action = action;
    }
  }

}