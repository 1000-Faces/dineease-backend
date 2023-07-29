﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace webapi.Models;

[Table("table")]
public partial class Table
{
    [Key]
    [Column("tableNo")]
    public int TableNo { get; set; }

    [Column("seating")]
    [StringLength(10)]
    public string Seating { get; set; }

    [Column("availability")]
    [StringLength(3)]
    public string Availability { get; set; }

    [InverseProperty("TableNoNavigation")]
    public virtual ICollection<Reservation> Reservation { get; set; } = new List<Reservation>();
}