using System.ComponentModel.DataAnnotations;

namespace M183.Models
{
  public class UsersAudit
  {
    [Key]
    public Guid Id { get; set; }
    public string Username { get; set; }
    public bool IsAdmin { get; set; }
    public DateTime InstertedOn { get; set; }
  }
}
