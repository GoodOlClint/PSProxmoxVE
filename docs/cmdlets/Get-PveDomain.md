---
external help file: PSProxmoxVE.dll-Help.xml
Module Name: PSProxmoxVE
online version:
schema: 2.0.0
---

# Get-PveDomain

## SYNOPSIS
Lists Proxmox VE authentication domains/realms.

## SYNTAX

```
Get-PveDomain [[-Realm] <String>] [-Session <PveSession>] [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION
Returns all authentication domains (realms) from the Proxmox VE access management system. Optionally filter by a specific realm name.

## EXAMPLES

### Example 1
```powershell
PS C:\> Get-PveDomain
```

Returns all authentication domains.

### Example 2
```powershell
PS C:\> Get-PveDomain -Realm "pam"
```

Returns only the pam authentication domain.

## PARAMETERS

### -Realm
Optional realm name to filter results.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Session
{{ Fill Session Description }}

```yaml
Type: PveSession
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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String
## OUTPUTS

### PSProxmoxVE.Core.Models.Users.PveDomain
## NOTES

## RELATED LINKS
