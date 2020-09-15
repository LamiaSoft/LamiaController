
using System.Collections.Generic;

namespace LamiaController {

  public interface ISharedResource {

    string GetString(string text);

    string GetString(string text, List<string> replacements);

  }

}