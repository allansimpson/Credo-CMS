using System.Reflection;

namespace CredoCms.Infrastructure.Seeding.EmailTemplates;

internal static class EmailTemplateResources
{
    private const string ResourcePrefix = "CredoCms.Infrastructure.Seeding.EmailTemplates.";

    public static string Load(string fileName)
    {
        var asm = typeof(EmailTemplateResources).GetTypeInfo().Assembly;
        var name = ResourcePrefix + fileName;
        using var stream = asm.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException(
                $"Embedded email template resource '{name}' was not found. " +
                $"Ensure the file exists under Seeding/EmailTemplates and is listed as <EmbeddedResource> in the csproj.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
