﻿using Bit.Websites.Sales.Api.Models.Emailing;
using Bit.Websites.Sales.Api.Resources;
using Bit.Websites.Sales.Shared.Dtos.ContactUs;
using FluentEmail.Core;

namespace Bit.Websites.Sales.Api.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public partial class ContactUsController : ControllerBase
{
    [AutoInject] private IFluentEmail _fluentEmail;
    [AutoInject] private IOptionsSnapshot<AppSettings> _appSettings;

    [HttpPost]
    public async Task<IActionResult> Create(ContactUsDto contactUsDto, CancellationToken cancellationToken)
    {
        var receiverEmail = _appSettings.Value.ReceiverEmailSetting.Email;
        var assembly = typeof(Program).Assembly;

        await _fluentEmail
            .To(receiverEmail)
            .Subject(EmailStrings.NewContactUsSubmitSubject)
            .UsingTemplateFromEmbedded("Bit.Websites.Sales.Api.Resources.NewContactUsSubmit.cshtml",
                new NewContactUsSubmitModel
                {
                    ContactUsInfo = contactUsDto,
                    HostUri = new Uri($"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.PathBase}")
                }, assembly)
            .SendAsync(cancellationToken);

        return Ok();
    }
}
