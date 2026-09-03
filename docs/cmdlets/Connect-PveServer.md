---
external help file: PSProxmoxVE.dll-Help.xml
Module Name: PSProxmoxVE
online version:
schema: 2.0.0
---

# Connect-PveServer

## SYNOPSIS
{{ Fill in the Synopsis }}

## SYNTAX

### Credential (Default)
```
Connect-PveServer [-Server] <String> [-Port <Int32>] -Credential <PSCredential> [-SkipCertificateCheck]
 [-TimeoutSeconds <Int32>] [-PassThru] [-Quiet] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

### ApiToken
```
Connect-PveServer [-Server] <String> [-Port <Int32>] -ApiToken <SecureString> [-SkipCertificateCheck]
 [-TimeoutSeconds <Int32>] [-PassThru] [-Quiet] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION
{{ Fill in the Description }}

## EXAMPLES

### Example 1
```powershell
PS C:\> {{ Add example code here }}
```

{{ Add example description here }}

## PARAMETERS

### -ApiToken
API token in USER@REALM!TOKENID=UUID format, as a SecureString.

Build one with `Read-Host -AsSecureString`, or read it from a secret vault. `ConvertTo-SecureString 'root@pam!mytoken=...' -AsPlainText -Force` also works, but a token written as a literal lands in shell history and in any transcript.
A plain string is still accepted in this release and emits a deprecation warning; it is removed in the next major release.

```yaml
Type: SecureString
Parameter Sets: ApiToken
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Credential
Username and password.
Username must include realm (e.g.
root@pam).

```yaml
Type: PSCredential
Parameter Sets: Credential
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -PassThru
Output the session object to the pipeline.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Port
API port.
Defaults to 8006.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Server
Hostname or IP of the Proxmox VE server.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -SkipCertificateCheck
Skip TLS certificate validation.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ProgressAction
{{ Fill ProgressAction Description }}

```yaml
Type: ActionPreference
Parameter Sets: (All)
Aliases: proga

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Quiet
Do not output the session object to the pipeline.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -TimeoutSeconds
HTTP timeout in seconds (0 = infinite). Default 100s.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### PSProxmoxVE.Core.Authentication.PveSession
## NOTES

## RELATED LINKS
