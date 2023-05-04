using System;
using System.Collections.Generic;
using Meziantou.Framework.Win32;

namespace PoeShared.Services;

public interface ICredentialManager
{
    void DeleteCredential(string applicationName);
    void WriteCredential(string applicationName, string userName, string secret, string comment, CredentialPersistence persistence);
    void WriteCredential(string applicationName, string userName, string secret, CredentialPersistence persistence);
    Credential ReadCredential(string applicationName);
    CredentialResult PromptForCredentialsConsole(string target, string userName = null, CredentialSaveOption saveCredential = CredentialSaveOption.Unselected);
    CredentialResult PromptForCredentials(IntPtr owner = default, string messageText = null, string captionText = null, string userName = null, CredentialSaveOption saveCredential = CredentialSaveOption.Unselected);
    IReadOnlyList<Credential> EnumerateCredentials(string filter);
    IReadOnlyList<Credential> EnumerateCredentials();
}