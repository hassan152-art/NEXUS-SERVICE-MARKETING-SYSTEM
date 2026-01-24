using System;
using System.ComponentModel.DataAnnotations;

public class Customer
{
    public int CustomerId { get; set; }

    // Walk-in customer → NULL
    public int? UserId { get; set; }

    [Required]
    public string FullName { get; set; }

    [Required]
    public string CNIC { get; set; }

    [Required]
    public string ContactNo { get; set; }

    public string Email { get; set; }

    [Required]
    public string Address { get; set; }

    public string RegisteredBy { get; set; } // Employee
    public DateTime CreatedDate { get; set; }
}
