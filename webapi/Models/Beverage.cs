﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace webapi.Models;

[Keyless]
public partial class Beverage
{
    [Column("food_id")]
    public int FoodId { get; set; }

    [Column("beverage_id")]
    public int BeverageId { get; set; }
}