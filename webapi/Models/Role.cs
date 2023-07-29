﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace webapi.Models;

[Table("Role", Schema = "userMGT")]
public partial class Role
{
    [Key]
    [Column("role_id")]
    [StringLength(10)]
    [Unicode(false)]
    public string RoleId { get; set; }

    [Required]
    [Column("role_name")]
    [StringLength(50)]
    [Unicode(false)]
    public string RoleName { get; set; }

    [InverseProperty("RoleNavigation")]
    public virtual ICollection<Account> Account { get; set; } = new List<Account>();
}