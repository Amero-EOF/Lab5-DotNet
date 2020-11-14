using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Lab5.Models
{
    public class AnswerImage
    {
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int AnswerImageID { get; set; }

        [Required]
        [DisplayName("File Name")]
        public string FileName { get; set; }

        [Required]
        [Url]
        public string Url { get; set; }



    }
}
