using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.StarterKit.Portal
{
    [Table("Portal_Contacts")]
    public class ContactItem : IContactItem
    {
        #region IContactItem Members

        [Key]
        public int ItemId { get; set; }

        public int ModuleId { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedByUser { get; set; }
        public string Name { get; set; }
        public string Role { get; set; }
        public string Email { get; set; }
        public string Contact1 { get; set; }
        public string Contact2 { get; set; }

        #endregion
    }
}