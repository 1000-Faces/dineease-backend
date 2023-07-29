﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace webapi.Models;

public partial class Food
{
    [Key]
    [Column("food_id")]
    [StringLength(10)]
    public string FoodId { get; set; }

    [Required]
    [Column("food_name")]
    [StringLength(50)]
    [Unicode(false)]
    public string FoodName { get; set; }

    [Column("description")]
    public string Description { get; set; }

    [Required]
    [Column("categoryID")]
    [StringLength(10)]
    public string CategoryId { get; set; }

    [Required]
    [Column("food_type")]
    [Unicode(false)]
    public string FoodType { get; set; }

    [Required]
    [Column("availability")]
    [StringLength(50)]
    [Unicode(false)]
    public string Availability { get; set; }

    [InverseProperty("Food")]
    public virtual ICollection<CalenderDate> CalenderDate { get; set; } = new List<CalenderDate>();

    [ForeignKey("CategoryId")]
    [InverseProperty("Food")]
    public virtual FoodCategory Category { get; set; }

    [InverseProperty("Food")]
    public virtual FoodPortions FoodPortions { get; set; }

    [InverseProperty("Food")]
    public virtual ICollection<MealFoods> MealFoods { get; set; } = new List<MealFoods>();

    [ForeignKey("FoodId")]
    [InverseProperty("Food")]
    public virtual ICollection<User> User { get; set; } = new List<User>();
}