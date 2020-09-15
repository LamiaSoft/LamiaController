using System;
namespace LamiaController.Models {

  public interface ModelWithStatus {
    string status { get; set; }
    DateTime? creationDate { get; set; }
    DateTime? deletionDate { get; set; }
  }
}
