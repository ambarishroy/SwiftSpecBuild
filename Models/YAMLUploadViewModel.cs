using System.ComponentModel.DataAnnotations;

namespace SwiftSpecBuild.Models
{
    public class YAMLUploadViewModel
    {
        [DataType(DataType.Upload)]
        [Required(ErrorMessage = "Please select a YAML file to upload.")]
        public IFormFile File { get; set; }
    }
}
