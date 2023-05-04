using System;
using System.Collections.Generic;
using Meziantou.Framework.Win32;

namespace PoeShared.Services;

internal sealed class MeziantouCredentialManager : ICredentialManager
{
    public MeziantouCredentialManager()
    {
    }

    public void DeleteCredential(string applicationName)
    {
        CredentialManager.DeleteCredential(applicationName);
    }

    public void WriteCredential(string applicationName, string userName, string secret, string comment, CredentialPersistence persistence)
    {
        CredentialManager.WriteCredential(applicationName, userName, secret, comment, persistence);
    }

    public void WriteCredential(string applicationName, string userName, string secret, CredentialPersistence persistence)
    {
        CredentialManager.WriteCredential(applicationName, userName, secret, persistence);
    }

    public Credential ReadCredential(string applicationName)
    {
        return CredentialManager.ReadCredential(applicationName);
    }

    public CredentialResult PromptForCredentialsConsole(string target, string userName = null, CredentialSaveOption saveCredential = CredentialSaveOption.Unselected)
    {
        return CredentialManager.PromptForCredentialsConsole(target, userName, saveCredential);
    }

    public CredentialResult PromptForCredentials(IntPtr owner = default, string messageText = null, string captionText = null, string userName = null, CredentialSaveOption saveCredential = CredentialSaveOption.Unselected)
    {
        return CredentialManager.PromptForCredentials(owner, messageText, captionText, userName, saveCredential);
    }

    public IReadOnlyList<Credential> EnumerateCredentials(string filter)
    {
        return CredentialManager.EnumerateCredentials(filter);
    }

    public IReadOnlyList<Credential> EnumerateCredentials()
    {
        return CredentialManager.EnumerateCredentials();
    }
}