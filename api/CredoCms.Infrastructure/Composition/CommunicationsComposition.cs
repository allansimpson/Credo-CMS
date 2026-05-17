using CredoCms.Application.Common;
using CredoCms.Application.Email;
using CredoCms.Application.Rss;
using CredoCms.Application.Sms;
using CredoCms.Application.UserManagement;
using CredoCms.Infrastructure.Email;
using CredoCms.Infrastructure.Rss;
using CredoCms.Infrastructure.Sms;
using Microsoft.Extensions.DependencyInjection;

namespace CredoCms.Infrastructure.Composition;

internal static class CommunicationsComposition
{
    public static IServiceCollection AddCommunications(this IServiceCollection services)
    {
        services.AddScoped<IInvitationEmailComposer, InvitationEmailComposer>();

        // Three IEmailService impls + the router that picks one per request
        // based on SiteSettings.EmailProvider.
        services.AddScoped<LoggingEmailService>();
        services.AddScoped<SendGridEmailService>();
        services.AddScoped<SmtpEmailService>();
        services.AddSingleton<ISendGridClientFactory, SendGridClientFactory>();
        services.AddSingleton<IMailKitSmtpClientFactory, MailKitSmtpClientFactory>();
        services.AddScoped<IEmailService, EmailServiceRouter>();

        services.AddScoped<IEmailSuppressionRepository, EmailSuppressionRepository>();
        services.AddScoped<IEmailSuppressionService, EmailSuppressionService>();
        services.AddScoped<ITestEmailService, TestEmailService>();
        services.AddScoped<IWebhookEventLogRepository, WebhookEventLogRepository>();
        services.AddScoped<IEmailBroadcastRepository, EmailBroadcastRepository>();
        services.AddScoped<IEmailBroadcastRecipientRepository, EmailBroadcastRecipientRepository>();
        services.AddSingleton<ISendGridWebhookVerifier, SendGridWebhookVerifier>();
        services.AddScoped<ISendGridWebhookEventProcessor, SendGridWebhookEventProcessor>();
        services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
        services.AddScoped<IEmailTemplateService, EmailTemplateService>();
        services.AddScoped<IEmailTemplateRenderer, EmailTemplateRenderer>();
        services.AddScoped<IRecipientResolver, RecipientResolver>();
        services.AddScoped<IEmailBroadcastService, EmailBroadcastService>();
        services.AddScoped<IEmailOnPublishService, EmailOnPublishService>();
        services.AddScoped<IAdminNotificationLastSentRepository, AdminNotificationLastSentRepository>();

        // SMS — always NoOpSmsService for v1; Twilio class exists for v1.5 swap-in.
        services.AddScoped<ISmsService, NoOpSmsService>();

        services.AddScoped<IUnsubscribeTokenService, UnsubscribeTokenService>();

        services.AddSingleton<IRssFeedBuilder, RssFeedBuilder>();

        return services;
    }
}
