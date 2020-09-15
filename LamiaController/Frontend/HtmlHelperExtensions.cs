using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace LamiaController.Frontend {

  public static class HtmlHelperExtensions {

    public static ISharedResource sharedResource;


    public static HtmlString FormErrorText<TModel>(this IHtmlHelper<TModel> htmlHelper, string fieldName) {
      if (htmlHelper.ViewBag.validation != null) {
        Dictionary<string, object> validation = (Dictionary<string, object>)htmlHelper.ViewBag.validation;

        if (validation.ContainsKey(fieldName)) {
          List<Dictionary<string, string>> error = (List<Dictionary<string, string>>)validation[fieldName];
          return new HtmlString(sharedResource.GetString(error[0]["code"]));
        }
      }

      return new HtmlString("");
    }

    public static string ErrorClassFor<TModel, TProperty>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> field, string errorClass, string regularClass = "") {
      if (htmlHelper.ViewBag.validation != null && field != null) {

        var expressionProvider = htmlHelper.ViewContext.HttpContext.RequestServices
            .GetService(typeof(ModelExpressionProvider)) as ModelExpressionProvider;

        string fieldName = expressionProvider.GetExpressionText(field);

        Dictionary<string, List<Dictionary<string, string>>> validation = (Dictionary<string, List<Dictionary<string, string>>>)htmlHelper.ViewBag.validation;
        if (validation.ContainsKey(fieldName)) {
          return regularClass + " " + errorClass;
        }
      }

      return regularClass;
    }


    public static IHtmlContent ErrorFor<TModel, TProperty>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> field, IDictionary<string, object> htmlAttributes = null) {
      if (htmlHelper.ViewBag.validation != null && field != null) {

        var expressionProvider = htmlHelper.ViewContext.HttpContext.RequestServices
            .GetService(typeof(ModelExpressionProvider)) as ModelExpressionProvider;

        string fieldName = expressionProvider.GetExpressionText(field);

        Dictionary<string, List<Dictionary<string, string>>> validation = (Dictionary<string, List<Dictionary<string, string>>>)htmlHelper.ViewBag.validation;
        if (validation.ContainsKey(fieldName)) {
          List<Dictionary<string, string>> error = validation[fieldName];

          var tag = new TagBuilder("span");
          tag.Attributes.Add("class", "text-danger");
          tag.InnerHtml.Append(sharedResource.GetString(error[0]["code"]));

          if (htmlAttributes != null) {
            tag.MergeAttributes(htmlAttributes);
          }

          return tag;
        }
      }

      return new StringHtmlContent("");
    }
  }

}
