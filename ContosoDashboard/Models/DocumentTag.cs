using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models
{
    /// <summary>
    /// Represents a tag/label assigned to a document for flexible categorization
    /// </summary>
    [Table("DocumentTags")]
    public class DocumentTag
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TagId { get; set; }

        /// <summary>
        /// Reference to the document
        /// </summary>
        public int DocumentId { get; set; }

        [ForeignKey(nameof(DocumentId))]
        public virtual Document Document { get; set; }

        /// <summary>
        /// Tag name/value
        /// </summary>
        [Required]
        [StringLength(100)]
        public string TagName { get; set; }
    }
}
