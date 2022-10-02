﻿using System;
using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.DatePicker;

public class FormValidationDateRangePickerModel
{
    [Required]
    public DateTimeOffset? Date { get; set; }
}
